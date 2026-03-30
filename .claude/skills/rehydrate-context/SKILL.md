---
name: rehydrate-context
description: Define objective for agent and reconstruct session from MARK files.
---

### Steps

1. **Read** the agent's Progress Summary MARK at `{paths.marks}/{prefix}_Progress_Summary_MARK.md` (resolve `paths.marks` and `prefix` from `.claude/settings.json`) — extract Last Checkpoint, Open Threads, Momentum Direction, Artifacts Modified.

2. **Read** the agent's Questions Log MARK at `{paths.marks}/{prefix}_Questions_Log_MARK.md` — extract all `OPEN` questions.

3. **Parse user input** — identify intent verb (define, review, plan, decide, build, fix…) and target object.

4. **Staleness check** — if MARK date > 3 days old, flag: *"Last checkpoint was N days ago — priorities may have shifted."*

5. **Triage questions** — for each OPEN question:
   - Input answers it → `RESOLVED` with answer
   - No longer relevant → `OBSOLETE` with reason
   - Otherwise → `STILL_OPEN` (Hard / Soft blocker)

6. **Synthesize Session Objective**:
   > Continuing from [checkpoint]… Now that we know [resolved answers]… The mission is to [verb] [deliverable]. Success = [condition]. This matters because [stakes].

   Must contain: **verb + deliverable + success condition + stakes clause**.

7. **Narrative Bridge** — 2-3 sentences connecting last session's end to this session's start. Reference specific artifacts.

8. **Blocker Inventory** — list STILL_OPEN Hard blockers, missing inputs, cross-agent dependencies.

9. **Validate** (all must pass):
   - [ ] Grounded — references real artifacts/MARK entries
   - [ ] Falsifiable — can evaluate done/not-done at session end
   - [ ] Scoped — fits one session, within agent authority
   - [ ] Stakes declared — names real consequence
   - [ ] Questions triaged — all classified
   - [ ] No hallucination — if no MARK, say "Initial Session"

10. **Emit** Session Objective → **wait for user confirmation** before proceeding.

### Errors
| Condition | Response |
|-----------|----------|
| MARK missing/empty | "Initial Session" — skip Narrative Bridge, session #1 |
| Ambiguous user input | Ask: "What would you like to accomplish this session?" |
| MARK > 7 days old | Add staleness warning |
| Objective exceeds scope | Break into phases, declare THIS session's deliverable |
| Outside agent authority | Redirect to correct agent |
| Conflicting MARK data | Surface conflict, ask user which version is current |

- **No fabrication** — if a MARK file is empty or missing, state that explicitly. Never invent prior context.
- **No filler objectives** — the Session Objective must be specific enough that a different agent could evaluate whether it was achieved. "Continue working on the project" is NOT acceptable.
- **Timestamp awareness** — always compare MARK date vs. current date. Surface staleness proactively.
- **Agent-scope respect** — the objective must stay within the current agent's authority. A PO does not set architecture objectives; an Architect does not reprioritize the backlog.
- **Single-session scoping** — the objective must be achievable within this session. If the user's intent is larger, break it down and declare what THIS session will deliver.
