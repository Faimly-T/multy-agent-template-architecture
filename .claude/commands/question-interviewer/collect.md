# Trigger: Collect Questions → Question Interviewer (PjM)

## Instruction
Execute the full CODE 5-Phase Relay (Steps 1–6) to consolidate and resolve open questions from the agents listed in the context payload.

1. Read the `question-interviewer` agent definition (resolve from `.claude/settings.json`).
2. Load the agent's role file (`{agent.role}`) — adopt Identity, Mandate, and Facts & Directives.
3. **Active session detection**: If the PjM's own Progress Summary MARK shows an interrupted interview (completion = partial), resume from where it left off — skip Steps 2–3, jump to Step 4 with remaining questions.
4. **New session**: Read each named agent's Question Log MARK. Collect all Active (OPEN) questions. Execute Steps 1–5 (Capture → Organize → Distill interview → Express).
5. Write Interview Transcript to `outputs/meetings`.
6. Write answers back to each originating agent's Questions Log MARK (Active → Resolved).
7. Present re-run offer to user: "Would you like to re-run the affected agents with these answers?" If yes → signal orchestrator to start a Type 2 (Iteration) cycle.

## Context Payload
$PAYLOAD
<!-- Injected at runtime by the orchestrator: list of child agents that completed + classification context + "Conditional trigger: open questions detected in [agent] Question Log MARKs" -->

## Expected Output
- **Artifact**: Interview Transcript
- **Path**: `outputs/meetings` (from `agents.question-interviewer.output` in settings.json)
- **Format**: Per `agents.question-interviewer.template` in settings.json

## Execution
- **Order**: conditional — fires after all primary child agents complete, only if open questions exist
- **Depends on**: ux-persona + ux-journey (both must be COMPLETED)
- **Signals completion via**: `{paths.marks}/PJM_Progress_Summary_MARK.md`
- **Re-run signal**: If user chooses re-run, emits Type 2 Iteration request back to orchestrator
