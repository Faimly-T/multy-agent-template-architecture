---
name: express-relay
description: Update MARK files and emit System Relay.
---

### Steps

1. **Compile session state** from all phases: Session Objective, Island count, Roadmap stats, Results Ledger.

2. **Overwrite** the agent's Progress Summary MARK at `{paths.marks}/{prefix}_Progress_Summary_MARK.md` (resolve from `.claude/settings.json`) with:
   - Last Checkpoint (date, session #, objective)
   - What Was Accomplished (deliverables produced, decisions made, key actions)
   - Where We Stopped (last island processed, completion status: full/partial/blocked)
   - Open Threads (deferred + concern items with next action + priority)
   - Momentum Direction (heading + recommended next focus + confidence: High/Medium/Low)
   - Token Usage (estimated input tokens, output tokens, total, % of 5-hour Pro budget)
   - Artifacts Modified (paths + change types)

   **Note**: The full cumulative history of distillation work lives in `{paths.marks}/{prefix}_Distill_History_MARK.md` (managed by Phase 4 — `expert-distill`). The Progress Summary MARK captures only THIS session's accomplishments. The Distill History Log captures ALL runs across ALL sessions. Do NOT duplicate the Distill History here — reference it.

3. **Update** the agent's Questions Log MARK at `{paths.marks}/{prefix}_Questions_Log_MARK.md`:
   - RESOLVED questions → move to Resolved section with detail
   - OBSOLETE questions → move with reason
   - New OPEN questions from Phases 2-4 → add to Active section

4. **Emit System Relay** — cross-agent handoff summary:
   ```
   | Target Agent | Signal | Action Needed |
   ```

5. **Emit Efficiency Scorecard**:
   - Islands: captured → processed → completed / deferred / blocked
   - Session Objective: achieved / partial / blocked
   - Time allocation by phase

### Errors
| Condition | Response |
|-----------|----------|
| Only Phase 1 ran | Persist Session Objective + question triage only |
| Mid-session /save | Persist current state, mark "interrupted" |
| MARK write fails | Display content for manual copy-paste |

### MARK File Contract

Paths resolve from `.claude/settings.json`: `{paths.marks}/{prefix}_Progress_Summary_MARK.md`, `{paths.marks}/{prefix}_Questions_Log_MARK.md`, and `{paths.marks}/{prefix}_Distill_History_MARK.md`. Session N-1 Phase 5 WRITES → Session N Phase 1 READS → Session N Phase 5 WRITES → Session N+1 reads. The Distill History MARK is append-only (managed by Phase 4, not Phase 5).

**Invariants:**
- MARK files are ALWAYS updated at session end, even for short or interrupted sessions.
- MARK files are the SINGLE SOURCE OF TRUTH for session continuity.
- If a MARK file is corrupted or missing, the next session's Phase 1 handles it gracefully (Initial Session mode).
- MARK files are append-safe: never delete history, only add and reclassify.
