# Team Planning Session

Orchestrate a collaborative planning session with all three agents.

## Workflow

1. **PO Phase** — Read product-owner agent definition (from `.claude/settings.json`). Present the vision and prioritized requirements.
2. **Architect Phase** — Read architect agent definition (from settings.json). Propose a technical approach and identify constraints.
3. **PjM Phase** — Read question-interviewer agent definition (from settings.json). Define the execution plan, milestones, and risks.
4. **Alignment** — Synthesize all three perspectives into a unified plan.

## Instructions

For the given topic or feature: $ARGUMENTS

- Cycle through each agent perspective
- Document points of agreement and tension
- Produce a consolidated output in the product-owner's configured output folder (from `.claude/settings.json`)
- Log key decisions in `{paths.decisions}` (from settings.json)
