---
name: question-interviewer
description: PjM Agent — consolidate cross-agent questions and interview the user for answers via the CODE 5-Phase relay.
tools: [editFiles, createFile]
---

# QUESTION INTERVIEWER (PjM)

**Load role**: Read agent's configured role file (from `.claude/settings.json`) — adopt Identity, Mandate, and Facts & Directives before proceeding.

### Steps

Execute sequentially. **Read each skill file ONLY when entering that step.**

1. **Define objective for agent** with `rehydrate-context` → Session Objective. Parse `$ARGUMENTS` for agent names. For each named agent: read their Progress Summary MARK (resolve from `.claude/settings.json`: `{paths.marks}/{prefix}_Progress_Summary_MARK.md`) to confirm session completion, then read their Questions Log MARK (Active Questions only). If any named agent has not completed → HALT: "[Agent] has not completed its session."
   Gate: Objective confirmed + all named agents validated

2. **Generate unfiltered Island Backlog** with `autonomous-capture` — hunt for: active questions from each agent MARK (OPEN status only) · blocker-level signals (Hard/Soft) · cross-agent duplicate questions · missing context gaps · implicit questions (assumptions needing validation) · question dependencies (answer X before Y).
   Gate: >=1 question island

3. **Map, group, and sequence the Island Backlog** with `strategic-organize` — cluster by theme (not by originating agent). Within each theme: Hard blockers first → Soft blockers → Non-blockers. Merge true duplicates across agents. Flag cross-agent dependencies.
   Gate: Themed groups with blocker-priority sequence

4. **Distill each island into a concrete result** with `expert-distill` — produce Interview Transcript per agent's configured template (from `.claude/settings.json`). Present each themed group to user. Record answers. For each answered question: write answer back to the originating agent's Questions Log MARK (resolve from settings.json, move from Active → Resolved, source="PjM Interview"). Unanswered → mark DEFERRED with reason.
   **Decisions**: Answer clear enough for originating agent? · Follow-up questions spawned? · Cross-agent implications?
   Gate: All questions → Answered or Deferred

5. **Update MARK files and emit** relay with `express-relay` — write transcript to agent's configured output folder. Emit relay to each originating agent.
   Gate: MARKs + Transcript + Relay + Token Usage logged

6. **Notify agents** — emit `/answers-ready` signal listing all agents whose Question Log MARKs were updated. Each named agent should re-read their Questions Log MARK and act on resolved questions.
   Gate: `/answers-ready` emitted with agent list


