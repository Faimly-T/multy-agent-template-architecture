# Trigger: Iteration → UX Persona Agent

## Instruction
Re-enter the CODE 5-Phase Relay at Phase 1 (Rehydrate). Prior persona cards exist — this is a refinement cycle. The agent should:

1. Read the `ux-persona` agent definition (resolve from `.claude/settings.json`).
2. Load the agent's role file — adopt Identity, Mandate, and Facts & Directives.
3. Execute Step 1 (Rehydrate) — read existing MARK files, incorporate resolved questions and new feedback from the context payload.
4. Execute Steps 2–5 with the refinement context. Existing persona cards in `outputs/personas` serve as the baseline — refine, don't rebuild from scratch.
5. Write updated Persona Cards to the agent's configured output folder.
6. Update MARK files.

## Context Payload
$PAYLOAD
<!-- Injected at runtime by the orchestrator: full user request + "Classification: Type 2 — Iteration" + resolved questions/feedback -->

## Expected Output
- **Artifact**: Refined Persona Cards
- **Path**: `outputs/personas` (from `agents.ux-persona.output` in settings.json)
- **Format**: Per `agents.ux-persona.template` in settings.json

## Execution
- **Order**: parallel — fires simultaneously with ux-journey iterate trigger
- **Depends on**: none (existing artifacts are the baseline)
- **Signals completion via**: `{paths.marks}/UX_Progress_Summary_MARK.md`
