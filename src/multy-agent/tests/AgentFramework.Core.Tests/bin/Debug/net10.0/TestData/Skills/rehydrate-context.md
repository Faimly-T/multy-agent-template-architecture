---
name: rehydrate-context
description: Define objective for agent and reconstruct session from prior state.
---

# Steps

1. **Read** the agent's session checkpoint — extract last session iteration, date, objective, and token consumption.

2. **Read** the agent's open questions from the session — extract all `OPEN` questions.

3. **Read** the agent's prior work summary from the session — extract: islands (total, distilled, discarded), decisions made, and deliverables produced. If no prior session exists, note "No prior work."

4. **Parse user input** — identify intent verb (define, review, plan, decide, build, fix…) and target object.

5. **Staleness check** — if checkpoint date > 3 days old, flag: *"Last checkpoint was N days ago — priorities may have shifted."*

6. **Triage questions** — for each OPEN question:
   - Input answers it → `RESOLVED` with answer
   - No longer relevant → `OBSOLETE` with reason
   - Otherwise → `STILL_OPEN` (Hard / Soft blocker)

7. **Synthesize Session Objective**:
   > Continuing from [checkpoint]… Now that we know [resolved answers]… The mission is to [verb] [deliverable]. Success = [condition]. This matters because [stakes].

   Must contain: **verb + deliverable + success condition + stakes clause**.

8. **Narrative Bridge** — 2-3 sentences connecting last session's end to this session's start. Reference specific artifacts. If prior work exists, include: "Across {N} prior sessions, this agent has produced {summary of cumulative work} and made {count} recorded decisions."

9. **Blocker Inventory** — list STILL_OPEN Hard blockers, missing inputs, cross-agent dependencies.

10. **Validate** (all must pass):
   - [ ] Grounded — references real artifacts or session state
   - [ ] Falsifiable — can evaluate done/not-done at session end
   - [ ] Scoped — fits one session, within agent authority
   - [ ] Stakes declared — names real consequence
   - [ ] Questions triaged — all classified
   - [ ] No hallucination — if no prior session, say "Initial Session"
   - [ ] History acknowledged — if prior work exists, objective accounts for it

11. **Emit** Session Objective → **wait for user confirmation** before proceeding.

# Errors
| Condition | Response |
|-----------|----------|
| No prior session | "Initial Session" — skip Narrative Bridge, session #1 |
| No prior work | Normal for first run — note "No prior work" and proceed |
| Ambiguous user input | Ask: "What would you like to accomplish this session?" |
| Checkpoint > 7 days old | Add staleness warning |
| Objective exceeds scope | Break into phases, declare THIS session's deliverable |
| Outside agent authority | Redirect to correct agent |

- **No fabrication** — if no prior session state exists, state that explicitly. Never invent prior context.
- **No filler objectives** — the Session Objective must be specific enough that a different agent could evaluate whether it was achieved.
- **Timestamp awareness** — always compare checkpoint date vs. current date. Surface staleness proactively.
- **Single-session scoping** — the objective must be achievable within this session.
