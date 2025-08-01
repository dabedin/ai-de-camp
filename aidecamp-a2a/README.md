# AI-de-camp - A2A Implementation

This is a Python implementation of the AI-de-camp wargaming application that supports both the traditional REST API and the A2A (Agent-to-Agent) protocol using Semantic Kernel.

## Architecture

- **webapp**: Exact copy of the original webapp from `aidecamp-app/webapp` - no changes required
- **webapi**: Python implementation using Semantic Kernel with dual API support:
  - Traditional `/api/combat` endpoint for existing webapp compatibility
  - A2A protocol endpoint at `/a2a` for agent-to-agent communication (requires full dependencies)

## Key Features

| Component | Description |
|-----------|-------------|
| **Image Analysis** | Uses Semantic Kernel with Azure OpenAI Vision to analyze toy soldier images |
| **Wargame Logic** | Python implementation of TurnManager combat calculations |
| **Dual API** | Supports both traditional REST API and A2A protocol |
| **Session Management** | Maintains conversation history for multi-turn interactions |
| **File Management** | Saves uploaded images to data/uploads folder following the previous sample pattern |

## Implementation Status

✅ **Core Functionality Complete**:
- Data models ported from C# to Python (Pydantic)
- TurnManager combat logic fully implemented
- Traditional REST API (`/api/combat`) working and tested
- Webapp integration successful - frontend works unchanged
- JSON response format matches original .NET implementation

✅ **A2A Protocol**: Framework ready, requires dependency installation
- Agent structure created following A2A sample patterns at [Semantic Kernel A2A Sample](https://github.com/a2aproject/a2a-samples/tree/main/samples/python/agents/semantickernel)
- AgentExecutor implemented for A2A integration
- Server architecture supports both REST and A2A protocols

## Installation

### Option 1: Real Server (Semantic Kernel + A2A)
```bash
cd aidecamp-a2a/webapi
pip install -r requirements.txt
```

### Option 2: Mock Server (Testing)
```bash
cd aidecamp-a2a/webapi
pip install -r requirements-minimal.txt
```

### Option 3: Development Installation
```bash
cd aidecamp-a2a/webapi
pip install -e .
```

## Quick Start

### Mock Server (Testing)

For testing without AI dependencies, use the mock server that returns hardcoded sample data:

```bash
cd aidecamp-a2a/webapi
pip install -r requirements-minimal.txt
python mock_server.py
```

### Real Server (Production)

For the full server with Semantic Kernel (Azure OpenAI) and A2A protocol support:

```bash
cd aidecamp-a2a/webapi
pip install -r requirements.txt

# Configure your environment
cp .env.example .env
# Edit .env with your Azure OpenAI credentials

# Start the real server
python __main__.py --host 0.0.0.0 --port 10020
```

## API Compatibility

The server provides backward-compatible endpoints for the existing webapp:

- **Base URL**: `http://localhost:10020/api/`
- **Combat Endpoint**: `POST http://localhost:10020/api/combat`
  - **Input**: multipart/form-data with image file
  - **Output**: JSON with scenario and outcome data
  - **Compatible with existing webapp**

**Example Response**:
```json
{
  "scenario": {
    "firing": {
      "pose": "standing",
      "weapon": "rifle",
      "coordinates": null
    },
    "target": {
      "pose": "crouched", 
      "weapon": "SMG",
      "coordinates": null
    },
    "distance": {
      "value": 60,
      "unit": "cm",
      "estimated": true
    }
  },
  "outcome": {
    "firing-modifier": 1,
    "target-modifier": 3, 
    "distance-modifier": 0,
    "rolled-dice": 16,
    "hit-or-miss": true
  }
}
```

### A2A Protocol

The A2A (Agent-to-Agent) protocol is available at the `/a2a` endpoint:

- **A2A Base URL**: `http://localhost:10020/a2a`
- **Protocol**: JSON-RPC for agent-to-agent communication
- **Features**: Multi-turn conversations, session management, streaming responses

For full A2A protocol support with real AI analysis, use the real server:

```bash
cd aidecamp-a2a/webapi
pip install -r requirements.txt
python __main__.py --host 0.0.0.0 --port 10020
```

## Testing

The webapp works with both the mock server (testing) and real server:

### Testing with Mock Server:
1. Start the mock server: `python mock_server.py`
2. Update webapp to point to `http://localhost:10020/api/combat`
3. Upload any image - mock server returns sample data for testing

### Production with Real Server:
1. Configure Azure OpenAI credentials in `.env`
2. Start the real server: `python __main__.py`
3. Upload toy soldier images - actual AI analysis with Semantic Kernel

### ✅ API Testing

```bash
# Test the combat endpoint
curl -X POST -F "image=@test-image.jpg" http://localhost:10020/api/combat

# Expected response: JSON with scenario and outcome
```

### A2A Inspector Testing

For testing the A2A protocol implementation, you can use the [A2A Inspector](https://github.com/a2aproject/a2a-inspector) tool. The recommended approach is to use the Docker container:

#### Using Docker Container

1. **Start the A2A server** (ensure it's running on port 10020):
```bash
cd aidecamp-a2a/webapi
pip install -r requirements.txt
python __main__.py --host 0.0.0.0 --port 10020
```

2. **Run A2A Inspector in Docker**:
```bash
# Pull and run the A2A inspector container
docker run -p 8080:8080 a2aproject/a2a-inspector
```

3. **Configure A2A Inspector**:
   - Open A2A Inspector in your browser at `http://localhost:8080`
   - Use the A2A endpoint: `http://host.docker.internal:10020/a2a`
   - The `host.docker.internal` hostname allows the Docker container to access services running on the host machine

4. **Test the A2A protocol**:
   - Upload toy soldier images through the A2A Inspector interface
   - Verify multi-turn conversations and session management
   - Test streaming responses and agent-to-agent communication features

> **Note**: The A2A protocol requires the full server with Semantic Kernel dependencies. Make sure you've installed the complete requirements.txt and configured Azure OpenAI credentials in your `.env` file.

## Data Models

The implementation uses Pydantic models that mirror the original C# classes:

- **ScenarioModel**: Represents the identified wargame scenario
- **ScenarioOutcome**: Contains the calculated combat result  
- **CombatResult**: Combines scenario and outcome for API responses

### Supported Poses
- standing
- crouched
- prone

### Supported Weapons
- rifle
- machine_gun
- SMG
- pistol

## Combat Rules

The Python implementation maintains the exact same combat calculation logic:

1. **Firing Modifier**: Based on weapon type (rifle=1, machine_gun=2, SMG=3, pistol=5)
2. **Target Modifier**: Based on pose (standing=0, crouched=3, prone=6)
3. **Distance Modifier**: +3 if distance ≥ 70cm, otherwise 0
4. **Dice Roll**: Random 1-20
5. **Hit Calculation**: Hit if dice roll > (firing + target + distance modifiers)

## Files Structure

```
aidecamp-a2a/
├── webapp/               # Frontend (unchanged from original)
│   └── public/
│       └── index.html    # Modified to point to new API
└── webapi/               # Python backend
    ├── models.py         # Pydantic data models
    ├── turn_manager.py   # Combat calculation logic
    ├── agent.py          # Semantic Kernel agent (A2A)
    ├── agent_executor.py # A2A protocol integration  
    ├── __main__.py       # Real server (Semantic Kernel + A2A)
    ├── mock_server.py    # Mock server (testing only)
    └── data/
        └── prompts/
            └── prompt.md # System prompt for image analysis
```
