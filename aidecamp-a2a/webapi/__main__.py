"""Main application entry point with dual API support."""

import logging
import os
from io import BytesIO
from pathlib import Path

import click
from a2a.server.apps import A2AStarletteApplication
from a2a.server.request_handlers import DefaultRequestHandler
from a2a.server.tasks import InMemoryTaskStore
from a2a.types import AgentCapabilities, AgentCard, AgentSkill
from dotenv import load_dotenv
from fastapi import FastAPI, File, HTTPException, UploadFile
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
from starlette.applications import Starlette
from starlette.middleware import Middleware
from starlette.routing import Mount, Route

from agent import SemanticKernelWargamingAgent
from agent_executor import WargamingAgentExecutor

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

load_dotenv()


async def combat_endpoint(request):
    """Traditional /combat endpoint for webapp compatibility."""
    try:
        # Parse multipart form data
        form = await request.form()
        
        if "image" not in form:
            return JSONResponse(
                status_code=400,
                content={"error": "No image file uploaded."}
            )
        
        file = form["image"]
        
        if file is None or file.filename == "":
            return JSONResponse(
                status_code=400,
                content={"error": "No image file uploaded."}
            )
        
        if not file.content_type.startswith("image/"):
            return JSONResponse(
                status_code=400,
                content={"error": "Uploaded file is not an image."}
            )
        
        # Read image bytes
        image_bytes = await file.read()
        
        # Save the image file to data/uploads directory (following .NET pattern)
        uploads_dir = Path(__file__).parent / "data" / "uploads"
        uploads_dir.mkdir(parents=True, exist_ok=True)
        
        file_path = uploads_dir / file.filename
        with open(file_path, "wb") as f:
            f.write(image_bytes)
        
        logger.info(f"Image saved to: {file_path}")
        
        try:
            # Process with our agent
            logger.info("Creating SemanticKernelWargamingAgent")
            agent = SemanticKernelWargamingAgent()
            logger.info("Agent created successfully")
            
            logger.info("Processing image with agent")
            result = await agent.process_image(image_bytes, "", "default")
            logger.info(f"Processing complete, result: {result}")
        except Exception as e:
            logger.error(f"Error during agent processing: {e}")
            import traceback
            logger.error(f"Full traceback: {traceback.format_exc()}")
            raise e
        
        # Return JSON response compatible with original API
        response_data = result.model_dump(by_alias=True)
        
        return JSONResponse(
            status_code=200,
            content=response_data
        )
        
    except Exception as e:
        logger.error(f"Error in combat endpoint: {e}")
        return JSONResponse(
            status_code=500,
            content={"error": str(e)}
        )


async def home_endpoint(request):
    """Home endpoint."""
    return JSONResponse({
        "message": "AI de Camp - A2A Wargaming Service",
        "endpoints": {
            "traditional_api": "/api/combat",
            "a2a_protocol": "/a2a"
        }
    })


def create_combined_app(host: str, port: int) -> Starlette:
    """Create a combined Starlette app with both A2A and traditional APIs."""
    
    # Create the A2A server
    request_handler = DefaultRequestHandler(
        agent_executor=WargamingAgentExecutor(),
        task_store=InMemoryTaskStore(),
    )

    a2a_server = A2AStarletteApplication(
        agent_card=get_agent_card(host, port), 
        http_handler=request_handler
    )
    
    # Traditional API routes
    traditional_routes = [
        Route("/", home_endpoint, methods=["GET"]),
        Route("/combat", combat_endpoint, methods=["POST"]),
    ]
    
    # CORS middleware for traditional API
    cors_middleware = Middleware(
        CORSMiddleware,
        allow_origins=["http://localhost:8080", "https://dbtest123.azurewebsites.net"],
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )
    
    # Create traditional API app
    traditional_app = Starlette(
        routes=traditional_routes,
        middleware=[cors_middleware]
    )
    
    # Build the A2A app
    a2a_app = a2a_server.build()
    
    # Combine both apps
    combined_routes = [
        Route("/", home_endpoint, methods=["GET"]),  # Root endpoint
        Mount("/api", traditional_app),  # Traditional API under /api
        Mount("/a2a", a2a_app),         # A2A protocol at /a2a
    ]
    
    combined_app = Starlette(routes=combined_routes)
    
    return combined_app


def get_agent_card(host: str, port: int) -> AgentCard:
    """Returns the Agent Card for the Wargaming Agent."""
    # Build the agent card
    capabilities = AgentCapabilities(streaming=False)
    skill_wargaming = AgentSkill(
        id='wargaming_analysis',
        name='Wargaming Scenario Analysis',
        description=(
            'Analyzes images of toy soldiers to identify firing and target soldiers, '
            'determines their characteristics (pose, weapon), calculates distance, '
            'and computes wargame combat outcomes using turn-based rules.'
        ),
        tags=['wargaming', 'image-analysis', 'tactical', 'combat', 'semantic-kernel'],
        examples=[
            'Analyze this toy soldier battle scene and calculate the combat outcome.',
            'Identify the firing and target soldiers in this wargame setup.',
            'What is the result of this combat scenario?',
        ],
    )

    agent_card = AgentCard(
        name='AI de Camp Wargaming Agent',
        description=(
            'Specialized wargaming agent that analyzes toy soldier battle scenes. '
            'Uses computer vision to identify soldiers, their poses and weapons, '
            'and applies tactical combat rules to determine battle outcomes.'
        ),
        url=f'http://{host}:{port}/a2a',
        version='1.0.0',
        defaultInputModes=['text', 'image'],
        defaultOutputModes=['text'],
        capabilities=capabilities,
        skills=[skill_wargaming],
    )

    return agent_card


@click.command()
@click.option('--host', default='localhost', help='Host to bind to')
@click.option('--port', default=10020, type=int, help='Port to bind to')
def main(host: str, port: int):
    """Starts the Wargaming Agent server with both A2A and traditional API support."""
    
    # Override with environment variables if available
    host = os.getenv('HOST', host)
    port = int(os.getenv('PORT', str(port)))
    
    # Create the combined application
    app = create_combined_app(host, port)
    
    import uvicorn
    
    logger.info(f"Starting server with:")
    logger.info(f"  A2A Protocol: http://{host}:{port}/a2a")
    logger.info(f"  Traditional API: http://{host}:{port}/api/")
    logger.info(f"  Combat endpoint: http://{host}:{port}/api/combat")
    
    uvicorn.run(app, host=host, port=port)


if __name__ == '__main__':
    main()