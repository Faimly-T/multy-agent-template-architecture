---
name: question-interviewer
description: PjM Agent — consolidate cross-agent questions and interview the user for answers via the CODE 5-Phase relay.
tools: [editFiles, createFile]
---

# QUESTION INTERVIEWER (PjM)

**Load role**: Read agent's configured role file (from `.claude/settings.json`) — adopt Identity, Mandate, and Facts & Directives before proceeding.

### Steps

Execute sequentially. **Read each skill file ONLY when entering that step.**

1. **Define objective for agent** with `rehydrate-context` → Session Objective. Parse `$ARGUMENTS` for agent names.

   **Active session detection**: Before reading child agent MARKs, check for the PjM's own Progress Summary MARK. If it shows an interrupted interview session (completion status = partial/blocked, Open Threads reference unanswered questions):
   - **Resume mode**: Load the prior transcript from the PjM output folder. Identify which questions were already answered and which remain. Set Session Objective to: "Resume interrupted interview — [N] questions remain from [prior session date]." Skip Steps 2–3 (questions are already organized). Jump to Step 4 with the remaining questions only.
   - Present to user: _"You have an unfinished question session from [date] with [N] remaining questions. Resuming where we left off."_

   **New session mode** (no active session or fresh start): For each named agent: read their Progress Summary MARK (resolve from `.claude/settings.json`: `{paths.marks}/{prefix}_Progress_Summary_MARK.md`) to confirm session completion, then read their Questions Log MARK (Active Questions only). If any named agent has not completed → HALT: "[Agent] has not completed its session."
   Gate: Objective confirmed + all named agents validated (or resume mode activated)

2. **Generate unfiltered Island Backlog** with `autonomous-capture` — hunt for: active questions from each agent MARK (OPEN status only) · blocker-level signals (Hard/Soft) · cross-agent duplicate questions · missing context gaps · implicit questions (assumptions needing validation) · question dependencies (answer X before Y).
   Gate: >=1 question island

3. **Map, group, and sequence the Island Backlog** with `strategic-organize` — cluster by theme (not by originating agent). Within each theme: Hard blockers first → Soft blockers → Non-blockers. Merge true duplicates across agents. Flag cross-agent dependencies.
   Gate: Themed groups with blocker-priority sequence

4. **Distill each island into a concrete result** with `expert-distill` — produce Interview Transcript per agent's configured template (from `.claude/settings.json`). Present each themed group to user. Record answers. For each answered question: write answer back to the originating agent's Questions Log MARK (resolve from settings.json, move from Active → Resolved, source="PjM Interview"). Unanswered → mark DEFERRED with reason.
   **Decisions**: Answer clear enough for originating agent? · Follow-up questions spawned? · Cross-agent implications?
   Gate: All questions → Answered or Deferred

5. **Update MARK files and emit** relay with `express-relay` — write transcript to agent's configured output folder. Emit relay to each originating agent.
   Gate: MARKs + Transcript + Relay + Token Usage logged

6. **Notify and offer re-run** — After all questions are answered or deferred:
   - Emit `/answers-ready` signal listing all agents whose Question Log MARKs were updated.
   - Present summary and choice to user:
     ```
     ## Interview Complete

     **Questions answered**: [N]
     **Questions deferred**: [N]
     **Agents updated**: [list]

     Would you like to re-run the affected agents with the answers provided?

     1. **Yes — Re-run** — Triggers a Type 2 (Iteration) cycle.
        Affected agents re-enter Phase 1, pick up resolved
        questions, and refine their artifacts.
     2. **No — Finish** — End the interview. Agents incorporate
        answers in their next scheduled run.
     ```
   - **If user chooses Re-run**: Signal the orchestrator (if within an orchestration cycle) to start a Type 2 Iteration for the updated agents. If standalone, directly trigger each affected agent's `iterate` command file from `.claude/commands/[agent]/iterate.md` with the resolved question IDs as context.
   - **If user chooses Finish**: End session. Confirm all artifact paths and suggest next steps.
   Gate: `/answers-ready` emitted + user choice captured + iteration triggered (if chosen)


