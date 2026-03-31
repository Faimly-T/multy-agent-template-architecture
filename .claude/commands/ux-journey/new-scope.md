# Trigger: New Requirement → UX Journey Agent

## Instruction
Execute the full CODE 5-Phase Relay (Steps 1–5) from scratch. This is a greenfield journey mapping session. Persona cards must exist before this trigger fires.

1. Read the `ux-journey` agent definition (resolve from `.claude/settings.json`).
2. Load the agent's role file — adopt Identity, Mandate, and Facts & Directives.
3. **Validate** that persona cards exist in the ux-persona output folder (`outputs/personas` from settings.json). If the folder is empty → HALT and report: "Cannot create journey maps — no persona cards found."
4. Execute Steps 1–5 using the persona cards as input alongside the context payload.
5. Write Journey Maps to the agent's configured output folder (`outputs/journeys`).
6. Update the agent's MARK files (`{paths.marks}/JRN_Progress_Summary_MARK.md` and `JRN_Questions_Log_MARK.md`).

## Context Payload
$PAYLOAD
<!-- Injected at runtime by the orchestrator: full user request + "Classification: Type 1 — New Requirement" -->

## Expected Output
- **Artifact**: Journey Maps (one per persona)
- **Path**: `outputs/journeys` (from `agents.ux-journey.output` in settings.json)
- **Format**: Per `agents.ux-journey.template` in settings.json

## Execution
- **Order**: sequential — position 2 of 2 in chain (ux-persona → ux-journey)
- **Depends on**: ux-persona (must have COMPLETED and produced persona cards)
- **Signals completion via**: `{paths.marks}/JRN_Progress_Summary_MARK.md`
