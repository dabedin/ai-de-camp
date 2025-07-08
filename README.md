# AI-de-camp
Playing with the words ["AI"](https://learn.microsoft.com/en-us/ai/playbook/technology-guidance/generative-ai/) and ["aide-de-camp"](https://en.wikipedia.org/wiki/Aide-de-camp), this repository is a learning experiment on how GenerativeAI could be leveraged in wargaming, starting with simple 1:72 toy soldier figurines and maybe, in the future, addressing proper tabletop wargaming scenario.

<div style="text-align: center;">
    <img src="docs/AI-de-camp%20design.png" alt="AI-de-camp" style="width:50%;">
    <p><em>You are a wargaming assistant, helping gamers to calculate modifiers of a firing toy soldier against a target toy soldier</em></p>
</div>

## A (little) story first

I loved playing with 1:72 toy soldiers as a kid, especially from Napoleonic Wars and WWII, yet not reaching the "proper" tabletop wargaming level proficiency.  
40 years forward, my kids found my collection of unpainted toy soldiers and started playing with them, creating their own scenarios and rules. Rules so complex that a single action would take ages to process.  
Therefore I had this idea: what if I could find a way to leverage AI to help them in their game, taking the opportunity to experiment with GenAI to create a Copilot, or even better, an AI-de-camp?

## (a) Plan

Here is a high-level plan for the project:
- [x] Create a simple orchestration API, leveraging Semantic Kernel, with rich logging
- [x] Create a simple UX to ease up the tests from a mobile device
- [ ] Address pain points:
    - [x] Estimate distance, from different point of views and perspectives
    - [ ] Handle dense/crowded scenarios with lots of toy soldiers ---> crucial for tabletop wargaming
    - [ ] Handle different terrains, i.e. in the garden

# Work in progress

Here are the progresses on this silly project:
- [Simple prompt](#aidecamp-prompt)
- [Semantic Kernel from a console](#aidecamp-console)
- [Semantic Kernel with plugin(s) and automatic planning](#aidecamp-plugins-native-console)
- [End-to-end scenario: webapp to API leveraging Semantic Kernel](#aidecamp-app)
- [Python implementation with A2A protocol support](#aidecamp-a2a)

## aidecamp-prompt

See the [prompt](aidecamp-simple-prompt/) for the first test.

Using Azure OpenAI Studio playground on a GPT4o deployment, revised a [prompt](aidecamp-simple-prompt/ChatSetup.json) to calculate the outcome of a wargame action, with the following goals:
1. Identify the firing and target toy soldier figurine in a simple set
2. Understand figurine pose: prone, crouched, standing
3. Identify the weapon used by the figurine
4. Estimate distance
5. Apply all modifiers, draw a random number and calculate the outcome

It's just a starting point, but it's a good one.

<img src="docs/aoai-test.png" alt="AI-de-camp" style="width:80%;">

## aidecamp-console

See the [project folder](aidecamp-console/) for the first console test leveraging [Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/overview/).

Implemented a simple console application to test the Semantic Kernel approach to the scenario.
A test image is passed with the user prompt to a GPT-4o model, the [prompt](aidecamp-console/Prompts/HandleCombat/) is configured as a file base plugin.

## aidecamp-plugins-native-console

The [project](aidecamp-plugins-native-console/) is an evolution of console test leveraging [Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/overview/) with few major additions:
- Switched to [Azure.AI.OpenAI SDK support](https://devblogs.microsoft.com/semantic-kernel/support-for-azure-ai-openai-and-openai-v2-is-here/) in >=1.18.1 version of Semantic Kernel.
- Implemented [rich logging](https://devblogs.microsoft.com/semantic-kernel/unlock-the-power-of-telemetry-in-semantic-kernel-sdk/) to understand the flow of the application and the Semantic Kernel interaction.
- Use of Semantic Kernel [native code plugin](https://learn.microsoft.com/en-us/semantic-kernel/concepts/plugins/adding-native-plugins?pivots=programming-language-csharp) to move the calculation of modifiers and outcome outside of the prompt.
- Via [automatic function calling](https://devblogs.microsoft.com/semantic-kernel/planning-with-semantic-kernel-using-automatic-function-calling/), application code is not instructing to pick the right plugin, Semantic Kernel is facilitating it via automatic planning logic. 
- Leveraging Json response format so the application code can be more flexible in handling the response.

While mine is a (very) simple example, it's a good starting point to understand how to leverage Semantic Kernel in a more complex scenario.

## aidecamp-app

Everything is coming together: a very basic plain JS [webapp](aidecamp-app/webapp) project help the gamer take a snapshot of the wargame and post it to the API in [webapi](aidecamp-app/webapi) project, leveraging Semantic Kernel with the [new (starting with 1.20) automatic function calling approach](https://devblogs.microsoft.com/semantic-kernel/new-function-calling-model-available-in-net-for-semantic-kernel/) and OpenTelemetry integration.

Here is what the webapp looks like:

<img src="docs/app test 20241028.png" alt="AI-de-camp web application" style="width:80%;">

No shirts have been hurt in the making of this webapp: spare buttons seemed a more available option than using dices.

Monitoring in Semantic Kernel is super rich, here is a sample of the telemetry:

<img src="docs/e2e transaction details.png" alt="Open Telemetry applied to Semantic Kernel" style="width:80%;">

Yes, it's a lot of data, but it's super useful to understand the flow of the application interactions with the Semantic Kernel. See [Observability in Semantic Kernel](https://devblogs.microsoft.com/semantic-kernel/observability-in-semantic-kernel/) for more details!

## aidecamp-a2a

The [project](aidecamp-a2a/) is a Python implementation that brings the AI-de-camp wargaming application to the A2A (Agent-to-Agent) protocol ecosystem while maintaining full backward compatibility.

Key features of this implementation:
- **Dual API Support**: Maintains the existing `/api/combat` REST endpoint for webapp compatibility, while adding A2A protocol support at `/a2a`
- **Python + Semantic Kernel**: Complete port from C# to Python using Semantic Kernel for AI orchestration
- **A2A Protocol Integration**: Enables agent-to-agent communication following A2A standards, allowing the wargaming assistant to interact with other AI agents
- **Session Management**: Supports multi-turn conversations and maintains context across interactions
- **Identical Functionality**: Same image analysis, combat calculations, and wargame logic as the original implementation

The A2A protocol endpoint enables the wargaming assistant to be discovered and used by other AI agents, opening possibilities for automated wargaming scenarios and multi-agent interactions. The agent can be inspected and tested using A2A Inspector tools, providing a standardized interface for agent-to-agent communication in wargaming contexts.

