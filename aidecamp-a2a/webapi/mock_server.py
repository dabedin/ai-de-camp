"""Mock server for testing without external AI dependencies.

This is a simplified mock server that provides the same API endpoints as the real server
but returns hardcoded sample data instead of using Semantic Kernel or Azure OpenAI.
Use this for testing and development when you don't have AI service credentials.

For the real server with full Semantic Kernel and A2A support, use __main__.py instead.
"""

import asyncio
import logging
import os
import json
from io import BytesIO
from pathlib import Path

from dotenv import load_dotenv

# Import only what we need that's already installed
from models import CombatResult, ScenarioModel, ScenarioOutcome, Weapon, Pose

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

load_dotenv()


class MockWargamingAgent:
    """Mock wargaming agent for testing without AI dependencies."""

    def __init__(self):
        self.system_prompt = self._load_system_prompt()

    def _load_system_prompt(self) -> str:
        """Load the system prompt from file."""
        prompt_path = Path(__file__).parent / "data" / "prompts" / "prompt.md"
        try:
            return prompt_path.read_text(encoding="utf-8")
        except FileNotFoundError:
            logger.error(f"System prompt file not found: {prompt_path}")
            return "You are a wargaming assistant."

    def _calculate_firing_modifier(self, weapon: Weapon) -> int:
        """Calculate firing modifier based on weapon type."""
        modifiers = {
            Weapon.RIFLE: 1,
            Weapon.MACHINE_GUN: 2,
            Weapon.SMG: 3,
            Weapon.PISTOL: 5,
        }
        return modifiers.get(weapon, 1)

    def _calculate_target_modifier(self, pose: Pose) -> int:
        """Calculate target modifier based on pose."""
        modifiers = {
            Pose.STANDING: 0,
            Pose.CROUCHED: 3,
            Pose.PRONE: 6,
        }
        return modifiers.get(pose, 0)

    def _calculate_distance_modifier(self, distance_cm: int) -> int:
        """Calculate distance modifier."""
        return 3 if distance_cm >= 70 else 0

    def _calculate_outcome(self, scenario: ScenarioModel) -> ScenarioOutcome:
        """Calculate the outcome using the same logic as the real server."""
        import random
        
        firing_modifier = self._calculate_firing_modifier(scenario.firing.weapon)
        target_modifier = self._calculate_target_modifier(scenario.target.pose)
        distance_modifier = self._calculate_distance_modifier(scenario.distance.value)
        
        # Roll dice (1-20)
        rolled_dice = random.randint(1, 20)
        
        # Calculate hit/miss
        total_modifier = firing_modifier + target_modifier + distance_modifier
        hit_or_miss = rolled_dice > total_modifier
        
        return ScenarioOutcome(
            firing_modifier=firing_modifier,
            target_modifier=target_modifier,
            distance_modifier=distance_modifier,
            rolled_dice=rolled_dice,
            hit_or_miss=hit_or_miss
        )

    async def process_image_mock(self, image_bytes: bytes, user_input: str = "") -> CombatResult:
        """Mock image processing for testing - returns a sample scenario."""
        logger.info(f"Processing image of {len(image_bytes)} bytes")
        
        # Create a mock scenario for testing
        from models import Characteristics, Distance
        
        scenario = ScenarioModel(
            firing=Characteristics(pose=Pose.STANDING, weapon=Weapon.RIFLE),
            target=Characteristics(pose=Pose.CROUCHED, weapon=Weapon.SMG), 
            distance=Distance(value=60, unit="cm", estimated=True)
        )
        
        # Calculate outcome using the same logic as the real server
        outcome = self._calculate_outcome(scenario)
        
        return CombatResult(scenario=scenario, outcome=outcome)


def create_mock_http_server():
    """Create a mock HTTP server without external AI dependencies."""
    import http.server
    import socketserver
    import urllib.parse
    import json
    
    class WargamingHandler(http.server.BaseHTTPRequestHandler):
        
        def do_GET(self):
            if self.path == "/" or self.path == "/api/":
                self.send_response(200)
                self.send_header('Content-type', 'application/json')
                self.send_header('Access-Control-Allow-Origin', '*')
                self.end_headers()
                response = {"message": "AI de Camp - Mock Wargaming Service (Testing)", "status": "running"}
                self.wfile.write(json.dumps(response).encode())
            else:
                self.send_response(404)
                self.end_headers()
        
        def do_OPTIONS(self):
            # Handle CORS preflight
            self.send_response(200)
            self.send_header('Access-Control-Allow-Origin', '*')
            self.send_header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS')
            self.send_header('Access-Control-Allow-Headers', 'Content-Type')
            self.end_headers()
        
        def do_POST(self):
            if self.path == "/combat" or self.path == "/api/combat":
                self._handle_combat()
            else:
                self.send_response(404)
                self.end_headers()
        
        def _handle_combat(self):
            try:
                # Parse multipart form data
                content_type = self.headers.get('content-type')
                if not content_type or not content_type.startswith('multipart/form-data'):
                    self._send_error(400, "Expected multipart/form-data")
                    return
                
                # Get content length
                content_length = int(self.headers.get('content-length', 0))
                if content_length == 0:
                    self._send_error(400, "No content provided")
                    return
                
                # Read the raw data
                raw_data = self.rfile.read(content_length)
                
                # Look for image data in the multipart content
                # This is a simplified parser - in production you'd use a proper library
                if b'image' not in raw_data:
                    self._send_error(400, "No image file found in upload")
                    return
                
                # Mock processing with sample data
                agent = MockWargamingAgent()
                result = asyncio.run(agent.process_image_mock(raw_data, ""))
                
                # Send response
                self.send_response(200)
                self.send_header('Content-type', 'application/json')
                self.send_header('Access-Control-Allow-Origin', '*')
                self.end_headers()
                
                response_data = result.model_dump(by_alias=True)
                self.wfile.write(json.dumps(response_data).encode())
                
                logger.info("Successfully processed combat request")
                
            except Exception as e:
                logger.error(f"Error processing combat request: {e}")
                self._send_error(500, f"Internal server error: {str(e)}")
        
        def _send_error(self, code: int, message: str):
            self.send_response(code)
            self.send_header('Content-type', 'application/json')
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            error_response = {"error": message}
            self.wfile.write(json.dumps(error_response).encode())
        
        def log_message(self, format, *args):
            # Override to use our logger
            logger.info(f"{self.address_string()} - {format % args}")
    
    return WargamingHandler


def main():
    """Start the mock server for testing."""
    import socketserver
    
    PORT = int(os.getenv('PORT', 10020))
    HOST = os.getenv('HOST', 'localhost')
    
    Handler = create_mock_http_server()
    
    with socketserver.TCPServer((HOST, PORT), Handler) as httpd:
        logger.info(f"Starting mock server at http://{HOST}:{PORT}")
        logger.info(f"Combat endpoint: http://{HOST}:{PORT}/api/combat")
        logger.info("Note: This is a MOCK server for testing - returns hardcoded sample data")
        logger.info("For the real server with Semantic Kernel and A2A support, use: python __main__.py")
        try:
            httpd.serve_forever()
        except KeyboardInterrupt:
            logger.info("Server stopped")


if __name__ == '__main__':
    main()