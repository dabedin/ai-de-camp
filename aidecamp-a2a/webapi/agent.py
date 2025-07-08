"""Wargaming agent implementation using Semantic Kernel and A2A protocol."""

import logging
import os
from enum import Enum
from pathlib import Path
from typing import TYPE_CHECKING, Any, Literal

from dotenv import load_dotenv
from pydantic import BaseModel
from semantic_kernel import Kernel
from semantic_kernel.connectors.ai.open_ai import (
    AzureChatCompletion,
    OpenAIChatCompletion,
    OpenAIChatPromptExecutionSettings,
)
from semantic_kernel.contents import (
    ChatHistory,
    ChatMessageContent,
    FunctionCallContent,
    FunctionResultContent,
    ImageContent,
    StreamingChatMessageContent,
    StreamingTextContent,
    TextContent,
)
from semantic_kernel.core_plugins import TextMemoryPlugin

from models import CombatResult, ScenarioModel, ScenarioOutcome
from turn_manager import TurnManagerPlugin

if TYPE_CHECKING:
    from semantic_kernel.connectors.ai.chat_completion_client_base import (
        ChatCompletionClientBase,
    )

logger = logging.getLogger(__name__)

load_dotenv()

# region Chat Service Configuration


class ChatServices(str, Enum):
    """Enum for supported chat completion services."""

    AZURE_OPENAI = 'azure_openai'
    OPENAI = 'openai'


service_id = 'default'


def get_chat_completion_service(
    service_name: ChatServices,
) -> 'ChatCompletionClientBase':
    """Return an appropriate chat completion service based on the service name.

    Args:
        service_name (ChatServices): Service name.

    Returns:
        ChatCompletionClientBase: Configured chat completion service.

    Raises:
        ValueError: If the service name is not supported or required environment variables are missing.
    """
    if service_name == ChatServices.AZURE_OPENAI:
        return _get_azure_openai_chat_completion_service()
    if service_name == ChatServices.OPENAI:
        return _get_openai_chat_completion_service()
    raise ValueError(f'Unsupported service name: {service_name}')


def _get_azure_openai_chat_completion_service() -> AzureChatCompletion:
    """Return Azure OpenAI chat completion service.

    Returns:
        AzureChatCompletion: The configured Azure OpenAI service.
    """
    return AzureChatCompletion(service_id=service_id)


def _get_openai_chat_completion_service() -> OpenAIChatCompletion:
    """Return OpenAI chat completion service.

    Returns:
        OpenAIChatCompletion: Configured OpenAI service.
    """
    return OpenAIChatCompletion(
        service_id=service_id,
        ai_model_id=os.getenv('OPENAI_MODEL_ID'),
        api_key=os.getenv('OPENAI_API_KEY'),
    )


# endregion

# region Response Format


class ResponseFormat(BaseModel):
    """A Response Format model to direct how the model should respond."""

    status: Literal['input_required', 'completed', 'error'] = 'input_required'
    message: str


# endregion

# region Semantic Kernel Wargaming Agent


class SemanticKernelWargamingAgent:
    """Wraps Semantic Kernel-based agents to handle wargaming tasks."""

    SUPPORTED_CONTENT_TYPES = ['text', 'text/plain', 'image']

    def __init__(self):
        # Configure the chat completion service
        # Uses Azure OpenAI by default. Change to ChatServices.OPENAI for OpenAI service.
        chat_service = get_chat_completion_service(ChatServices.AZURE_OPENAI)

        # Build the kernel
        self.kernel = Kernel()
        self.kernel.add_service(chat_service)

        # Add plugins
        self.kernel.add_plugin(TurnManagerPlugin(), "TurnManager")
        
        # Get the calculate_outcome function
        self.calculate_outcome_function = self.kernel.get_function("TurnManager", "calculate_outcome")

        # Load the system prompt
        self.system_prompt = self._load_system_prompt()

        # Store session histories
        self.session_histories: dict[str, ChatHistory] = {}

    def _load_system_prompt(self) -> str:
        """Load the system prompt from file."""
        prompt_path = Path(__file__).parent / "data" / "prompts" / "prompt.md"
        try:
            return prompt_path.read_text(encoding="utf-8")
        except FileNotFoundError:
            logger.error(f"System prompt file not found: {prompt_path}")
            return "You are a wargaming assistant."

    async def process_image(self, image_bytes: bytes, user_input: str = "", session_id: str = "default") -> CombatResult:
        """Process an image to identify toy soldiers and calculate wargame outcome.
        
        Args:
            image_bytes: The image data
            user_input: Optional user input (e.g., distance information)
            session_id: Session identifier
            
        Returns:
            CombatResult: The scenario and outcome
        """
        # Get or create chat history for this session
        if session_id not in self.session_histories:
            self.session_histories[session_id] = ChatHistory()
            self.session_histories[session_id].add_system_message(self.system_prompt)

        history = self.session_histories[session_id]

        # Create the user message with image and text
        user_message_text = user_input if user_input else "Identify the firing and target toy soldiers in this picture, then calculate the outcome of the wargame scenario. Return the scenario and outcome as JSON"
        
        import base64
        
        message_items = [
            TextContent(text=user_message_text),
            ImageContent(data=base64.b64encode(image_bytes).decode('utf-8'), data_format="base64", mime_type="image/jpeg")
        ]
        
        history.add_user_message(message_items)

        # Configure execution settings with JSON output format
        execution_settings = OpenAIChatPromptExecutionSettings(
            temperature=0.1,
            max_tokens=4000,
            response_format={"type": "json_object"}
        )

        # Get chat completion service
        chat_service = self.kernel.get_service("default")

        # Get the AI response for scenario identification
        response = await chat_service.get_chat_message_content(
            chat_history=history,
            settings=execution_settings,
            kernel=self.kernel
        )

        # Add response to history
        history.add_assistant_message(response.content or "")

        logger.info(f"Assistant response: {response}")

        # Parse the response to extract scenario
        try:
            import json
            
            response_content = response.content or ""
            logger.info(f"Raw response content: {response_content}")
            scenario_data = json.loads(response_content)
            logger.info(f"Parsed scenario data: {scenario_data}")
            scenario = ScenarioModel.model_validate(scenario_data)
            logger.info(f"Validated scenario: {scenario}")
            
            # Calculate outcome using the TurnManager plugin
            outcome_result = await self.kernel.invoke(
                self.calculate_outcome_function,
                scenario=scenario
            )
            
            if hasattr(outcome_result, 'value'):
                outcome = outcome_result.value
            else:
                outcome = outcome_result
                
            return CombatResult(scenario=scenario, outcome=outcome)
                
        except Exception as e:
            logger.error(f"Error parsing response: {e}")
            logger.error(f"Response content: {response_content}")
            raise ValueError(f"Failed to process image: {e}")

    async def invoke(self, user_input: str, session_id: str, image_bytes: bytes = None) -> dict[str, Any]:
        """Handle synchronous tasks (like tasks/send).

        Args:
            user_input (str): User input message.
            session_id (str): Unique identifier for the session.
            image_bytes (bytes): Optional image data.

        Returns:
            dict: A dictionary containing the content, task completion status,
            and user input requirement.
        """
        try:
            if image_bytes:
                result = await self.process_image(image_bytes, user_input, session_id)
                return {
                    'is_task_complete': True,
                    'require_user_input': False,
                    'content': result.model_dump_json(by_alias=True),
                }
            else:
                # Text-only request
                return {
                    'is_task_complete': False,
                    'require_user_input': True,
                    'content': 'Please provide an image of toy soldiers for wargame analysis.',
                }
        except Exception as e:
            logger.error(f"Error processing request: {e}")
            return {
                'is_task_complete': False,
                'require_user_input': True,
                'content': f'Error processing request: {str(e)}',
            }




# endregion