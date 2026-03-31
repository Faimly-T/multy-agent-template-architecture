# Trigger: Iteration → UX Journey Agent

## Instruction
Re-enter the CODE 5-Phase Relay at Phase 1 (Rehydrate). Prior journey maps exist — this is a refinement cycle. The agent should:

1. Read the `ux-journey` agent definition (resolve from `.claude/settings.json`).
2. Load the agent's role file — adopt Identity, Mandate, and Facts & Directives.
3. **Validate** persona cards still exist in `outputs/personas`. If missing → HALT.
4. Execute Step 1 (Rehydrate) — read existing MARK files, incorporate resolved questions and new feedback from the context payload.
5. Execute Steps 2–5 with the refinement context. Existing journey maps in `outputs/journeys` serve as the baseline — refine, don't rebuild from scratch. Check if personas have also been updated (if iterate ran in parallel).
6. Write updated Journey Maps to the agent's configured output folder.
7. Update MARK files.

## Context Payload
$PAYLOAD
<!-- Injected at runtime by the orchestrator: full user request + "Classification: Type 2 — Iteration" + resolved questions/feedback -->

## Expected Output
- **Artifact**: Refined Journey Maps
- **Path**: `outputs/journeys` (from `agents.ux-journey.output` in settings.json)
- **Format**: Per `agents.ux-journey.template` in settings.json

## Execution
- **Order**: parallel — fires simultaneously with ux-persona iterate trigger
- **Depends on**: none (existing artifacts are the baseline; concurrent persona updates are incorporated if available)
- **Signals completion via**: `{paths.marks}/JRN_Progress_Summary_MARK.md`
