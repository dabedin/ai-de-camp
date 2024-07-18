# AI-de-camp
Playing with the words ["AI"](https://learn.microsoft.com/en-us/ai/playbook/technology-guidance/generative-ai/) and ["aide-de-camp"](https://en.wikipedia.org/wiki/Aide-de-camp), this repository is an experiment on how GenerativeAI could be leveraged in wargaming, starting with simple 1:72 toy soldier and maybe, in the future, addressing proper tabletop wargaming scenario.

<img src="docs/AI-de-camp%20design.png" alt="AI-de-camp" style="width:50%;">

*You are a wargaming assistant, helping gamers to calculate modifiers of a firing toy soldier against a target toy soldier.*

## A (little) story first

I loved playing with 1:72 toy soldiers as a kid, especially from Napoleonic Wars and WWII, yet not reaching the "proper" tabletop wargaming level proficiency.
40 years forward, my kids found my collection of unpainted toy soldiers and started playing with them, creating their own scenarios and rules. Rules so complex that a signle action would take ages to process.
Therefore I had this idea: what if I could find a way to leverage AI to help them in their game?

## First test

Using Azure OpenAI Studio playground on a GPT4o deployment, help calculate the outcome of a wargame action, with the following goals:
- Identify the firing and target toy soldiers
- Understand pose: prone, crouched, standing
- Identify weapon: rifle, machine gun, submachine gun, pistol
- Estimate distance
- Calculate all modifiers, draw a random number and calculate the outcome

It's just a starting point, but it's a good one.

<img src="docs/aoai-test.png" alt="AI-de-camp" style="width:80%;">

## (a) Plan

Here is a high-level plan for the project:
- [ ] Create a simple orchestration API, leveraging Semantic Kernel, with rich logging
- [ ] Create a simple UX to ease up the tests from a mobile device
- [ ] Address pain points:
    - [ ] Estimate distance, from different point of views and perspectives
    - [ ] Handle dense/crowded scenarios with lots of toy soldiers ---> crucial for tabletop wargaming
    - [ ] Handle different terrains, i.e. in the garden
