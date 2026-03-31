# UX Research Orchestrator

Trigger the UX Research Orchestrator to classify intent and run the appropriate UX research workflow.

**Replaces**: `/ux-research-cycle` (legacy sequential command). Prefer this command for all new UX research work.

## Input

User request: $ARGUMENTS

## Execution

1. Read the `ux-orchestrator` agent definition (resolve from `.claude/settings.json`).
2. Load the agent's role file (`{agent.role}` from settings.json) — adopt Identity, Mandate, and Facts & Directives.
3. Execute the full 7-Skill Relay (Steps 1–7) from the orchestrator's `AGENT.md`, passing `$ARGUMENTS` as the user request.

The orchestrator will autonomously:
- Classify intent (New / Iterate / Redesign) based on existing artifacts and the request
- Fire the correct trigger files to child agents (ux-persona, ux-journey)
- Conditionally invoke the question-interviewer if open questions exist
- Track state, monitor completion, and enforce termination

## Examples

```
/ux-research-orchestrator A mobile fitness tracking app for casual gym-goers
```
→ Detects no existing artifacts → Type 1 (New Requirement) → runs ux-persona then ux-journey sequentially.

```
/ux-research-orchestrator Users said the onboarding feels too long. Simplify it.
```
→ Detects existing personas/journeys + feedback → Type 2 (Iteration) → runs both agents in parallel.

```
/ux-research-orchestrator We're pivoting from fitness to wellness. Completely new target audience.
```
→ Detects scope change → Type 3 (Redesign) → re-runs ux-persona then ux-journey sequentially.
