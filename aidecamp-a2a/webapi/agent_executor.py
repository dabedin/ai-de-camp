"""Agent executor for the wargaming agent using A2A protocol."""

import logging
from io import BytesIO

from a2a.server.agent_execution import AgentExecutor, RequestContext
from a2a.server.events.event_queue import EventQueue
from a2a.types import (
    TaskArtifactUpdateEvent,
    TaskState,
    TaskStatus,
    TaskStatusUpdateEvent,
)
from a2a.utils import (
    new_agent_text_message,
    new_task,
    new_text_artifact,
)
from agent import SemanticKernelWargamingAgent

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class WargamingAgentExecutor(AgentExecutor):
    """Wargaming Agent Executor for A2A protocol."""

    def __init__(self):
        self.agent = SemanticKernelWargamingAgent()

    async def execute(
        self,
        context: RequestContext,
        event_queue: EventQueue,
    ) -> None:
        """Execute the wargaming task."""
        query = context.get_user_input()
        task = context.current_task
        if not task:
            task = new_task(context.message)
            await event_queue.enqueue_event(task)

        # Check if there are any image attachments
        image_bytes = None
        if context.message and context.message.parts:
            for part in context.message.parts:
                if hasattr(part, 'type') and part.type == 'image':
                    # Extract image bytes
                    if hasattr(part, 'data'):
                        image_bytes = part.data
                    elif hasattr(part, 'content'):
                        image_bytes = part.content
                    break

        # Process the request directly (no streaming)
        result = await self.agent.invoke(query, task.contextId, image_bytes)
        
        require_input = result['require_user_input']
        is_done = result['is_task_complete']
        text_content = result['content']

        if require_input:
            await event_queue.enqueue_event(
                TaskStatusUpdateEvent(
                    status=TaskStatus(
                        state=TaskState.input_required,
                        message=new_agent_text_message(
                            text_content,
                            task.contextId,
                            task.id,
                        ),
                    ),
                    final=True,
                    contextId=task.contextId,
                    taskId=task.id,
                )
            )
        elif is_done:
            await event_queue.enqueue_event(
                TaskArtifactUpdateEvent(
                    append=False,
                    contextId=task.contextId,
                    taskId=task.id,
                    lastChunk=True,
                    artifact=new_text_artifact(
                        name='wargame_result',
                        description='Result of wargame scenario analysis.',
                        text=text_content,
                    ),
                )
            )
            await event_queue.enqueue_event(
                TaskStatusUpdateEvent(
                    status=TaskStatus(state=TaskState.completed),
                    final=True,
                    contextId=task.contextId,
                    taskId=task.id,
                )
            )

    async def cancel(
        self, context: RequestContext, event_queue: EventQueue
    ) -> None:
        """Cancel operation - not supported."""
        raise Exception('cancel not supported')