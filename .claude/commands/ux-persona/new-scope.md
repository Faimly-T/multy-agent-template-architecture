# Trigger: New Requirement → UX Persona Agent

## Instruction
Execute the full CODE 5-Phase Relay (Steps 1–5) from scratch. This is a greenfield UX research session — no prior personas exist. Build persona cards from the product/feature description provided in the context payload.

1. Read the `ux-persona` agent definition (resolve from `.claude/settings.json`).
2. Load the agent's role file (`{agent.role}`) — adopt Identity, Mandate, and Facts & Directives.
3. Execute Steps 1–5 with the context payload as input.
4. Write Persona Cards to the agent's configured output folder (`outputs/personas`).
5. Update the agent's MARK files (`{paths.marks}/UX_Progress_Summary_MARK.md` and `UX_Questions_Log_MARK.md`).

## Context Payload
$PAYLOAD
<!-- Injected at runtime by the orchestrator: full user request + "Classification: Type 1 — New Requirement" -->

## Expected Output
- **Artifact**: Persona Cards (2–5 personas)
- **Path**: `outputs/personas` (from `agents.ux-persona.output` in settings.json)
- **Format**: Per `agents.ux-persona.template` in settings.json

## Execution
- **Order**: sequential — position 1 of 2 in chain (ux-persona → ux-journey)
- **Depends on**: none
- **Signals completion via**: `{paths.marks}/UX_Progress_Summary_MARK.md` — "Where We Stopped" section shows completion status
