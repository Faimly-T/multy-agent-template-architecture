---
name: rehydrate-context
description: Define objective for agent and reconstruct session from MARK files.
---

### Steps

1. **Read** the agent's Progress Summary MARK at `{paths.marks}/{prefix}_Progress_Summary_MARK.md` (resolve `paths.marks` and `prefix` from `.claude/settings.json`) — extract Last Checkpoint, Open Threads, Momentum Direction, Artifacts Modified.

2. **Read** the agent's Questions Log MARK at `{paths.marks}/{prefix}_Questions_Log_MARK.md` — extract all `OPEN` questions.

3. **Read** the agent's Distill History Log at `{paths.marks}/{prefix}_Distill_History_MARK.md` — extract: total prior runs, last run summary, cumulative decisions made, and pattern of work across iterations. If the file does not exist, this agent has no distillation history yet — note "No prior distill runs." If the file is large (>200 entries), read only the header and last 10 run summaries.

4. **Parse user input** — identify intent verb (define, review, plan, decide, build, fix…) and target object.

5. **Staleness check** — if MARK date > 3 days old, flag: *"Last checkpoint was N days ago — priorities may have shifted."*

6. **Triage questions** — for each OPEN question:
   - Input answers it → `RESOLVED` with answer
   - No longer relevant → `OBSOLETE` with reason
   - Otherwise → `STILL_OPEN` (Hard / Soft blocker)

7. **Synthesize Session Objective**:
   > Continuing from [checkpoint]… Now that we know [resolved answers]… The mission is to [verb] [deliverable]. Success = [condition]. This matters because [stakes].

   Must contain: **verb + deliverable + success condition + stakes clause**.

8. **Narrative Bridge** — 2-3 sentences connecting last session's end to this session's start. Reference specific artifacts. If Distill History exists, include: "Across {N} prior distill runs, this agent has produced {summary of cumulative work} and made {count} recorded decisions. Key prior decisions: {list top 3 most impactful}."

9. **Blocker Inventory** — list STILL_OPEN Hard blockers, missing inputs, cross-agent dependencies.

10. **Validate** (all must pass):
   - [ ] Grounded — references real artifacts/MARK entries
   - [ ] Falsifiable — can evaluate done/not-done at session end
   - [ ] Scoped — fits one session, within agent authority
   - [ ] Stakes declared — names real consequence
   - [ ] Questions triaged — all classified
   - [ ] No hallucination — if no MARK, say "Initial Session"
   - [ ] History acknowledged — if Distill History exists, objective accounts for prior work

11. **Emit** Session Objective → **wait for user confirmation** before proceeding.

### Errors
| Condition | Response |
|-----------|----------|
| MARK missing/empty | "Initial Session" — skip Narrative Bridge, session #1 |
| Distill History missing | Normal for first run — note "No prior distill runs" and proceed |
| Distill History corrupted | Log warning, proceed without history context. Do not delete the file. |
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
