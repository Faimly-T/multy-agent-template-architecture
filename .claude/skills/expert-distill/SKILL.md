---
name: expert-distill
description: Distill each island into a concrete result or formal concern.
---

### Steps

1. **Load Distill History** — read the agent's Distill History Log at `{paths.marks}/{prefix}_Distill_History_MARK.md` (resolve from `.claude/settings.json`). If it exists, extract: total prior runs, last run date, cumulative decisions, and prior results summary. This gives full context of all previous distillation work for this agent. If the file does not exist, this is Run #1 — proceed normally.

2. **Resolve blocks first** — surface BLOCKED-USER items. If answered → READY. If deferred → Parking Lot.

3. **Process queue top-to-bottom**, applying the agent's **Distill Lens**:

   | Island Type | Action | Mark As |
   |-------------|--------|---------|
   | PREREQUISITE | Read, confirm current, extract info | RESULT:REVIEWED |
   | DECISION | State options (≥2), apply Decision Framework, recommend/decide | RESULT:DECIDED or CONCERN:NEEDS-APPROVAL |
   | WORK | Produce artifact using agent's output template, write to agent's configured output folder (from `.claude/settings.json`) | RESULT:PRODUCED |
   | QUESTION | Record answer + source | RESULT:ANSWERED |
   | RISK | Assess probability/impact, propose mitigation | RESULT:MITIGATED or CONCERN:ESCALATED |
   | DEPENDENCY | Check resolution status | RESULT:RESOLVED or CONCERN:BLOCKED |

   **Decision Registration** — for EVERY island that involves a decision (not just DECISION-type islands), log a Decision Entry:
   ```
   | Island ID | Decision Made | Options Considered | Rationale | Impact | ADR (if any) |
   ```
   A WORK island may decide on a format or approach. A RISK island may decide on a mitigation strategy. A QUESTION island may decide between interpretations. Capture ALL of these — not just explicit DECISION islands.

4. **Update Results Ledger** after each island:
   ```
   | ID | Type | Status | Outcome | Decision (if any) | Artifact Path |
   ```

5. **Scope guard** — if running long, complete current island, mark remaining as DEFERRED-TIME, proceed to Phase 5. Don't rush.

6. **Progressive Summarization** on every artifact: scannable in 30 seconds. **Bold** key passages, highlight critical takeaway.

7. **Append to Distill History Log** — write (append, NEVER overwrite) the current run's entry to `{paths.marks}/{prefix}_Distill_History_MARK.md`. Each run entry uses this format:

   ```markdown
   ---
   ### Run #{N} — {date}
   **Session**: #{session_number} | **Objective**: {session_objective}

   #### Islands Processed
   | ID | Type | Status | Outcome | Decision (if any) |
   |-----|------|--------|---------|-------------------|
   (copy from Results Ledger)

   #### Decisions Made This Run
   | Island ID | Decision | Rationale | Impact | ADR |
   |-----------|----------|-----------|--------|-----|
   (copy from Decision Registration — only rows where a decision was made)

   #### Artifacts Produced
   | Path | Change Type |
   |------|-------------|

   #### Run Summary
   {2-3 sentence summary: what was accomplished, what changed from prior runs, what remains}
   ---
   ```

   **Rules**:
   - Run number increments from the last entry in the file (or starts at 1).
   - NEVER delete or modify previous run entries — append only.
   - If the file doesn't exist, create it with a header and Run #1.
   - The file header format:
     ```markdown
     # {Agent Name} — Distill History Log
     **Agent**: {agent_name} | **Prefix**: {prefix}
     **Purpose**: Full history of all distillation runs — append-only.
     ```

### Output
- **Results Ledger** (inline → Phase 5 consumes it)
- **Decision Registry** (inline → Phase 5 + Distill History consume it)
- **Domain artifacts** → agent's configured output folder (`{agent.output}` from `.claude/settings.json`)
- **Decision records** → `{paths.decisions}/ADR-XXX-[title].md` (from `.claude/settings.json`)
- **Distill History Log** → `{paths.marks}/{prefix}_Distill_History_MARK.md` (append-only)

### Errors
| Condition | Response |
|-----------|----------|
| A WORK island produces an artifact that conflicts with an existing artifact | Do NOT overwrite silently. Log as CONCERN: "New artifact conflicts with existing [path]. Both versions preserved. User must resolve." Save new artifact with `-V[N+1]` suffix. |
| A DECISION island has no clear winner among options | Present all options with trade-offs to user. Mark as `CONCERN:NEEDS-APPROVAL`. Do not force a decision without sufficient basis. |
| An island takes unexpectedly long | After completing the current island, assess remaining queue. If >50% remains, switch to "highlight mode" — produce skeleton/outline results for remaining islands instead of full distillation. Mark as `RESULT:OUTLINE`. |
| A DEPENDENCY island is still unresolved | Mark as `CONCERN:BLOCKED` with specific detail. Do not skip downstream islands silently — mark them as `BLOCKED-UPSTREAM:ISL-XXX`. |
| Agent's Distill Lens is not defined | Use generic quality criteria: clarity, completeness, actionability. Log: "No Distill Lens defined — using generic distillation criteria." |
| Distill History Log is corrupted or unreadable | Log warning: "History file unreadable — starting fresh count at Run #1. Previous history preserved as-is." Do not delete the file. Append new entry at the end. |
| Distill History Log is very large (>200 entries) | Read only the last 10 run summaries for context. Reference the file for full history. Log: "History truncated for context — full log at {path}." |

---

### Distillation Quality Standards

Every RESULT produced in Phase 4 must meet these standards:

1. **Concrete** — names specific things, not abstractions. "We will use PostgreSQL" not "We will use an appropriate database."
2. **Attributable** — states the reasoning or source. Every decision has a "because."
3. **Actionable** — a reader knows exactly what to do next.
4. **Scannable** — Progressive Summarization applied. Bold the key, highlight the critical.
5. **Scoped** — does not exceed the island's scope. If the work grew, log a new island for Phase 5 to capture.
6. **Honest** — if the agent is uncertain, it says so. Concerns are first-class outputs, not failures.

### Decision Registration Standards

Every Decision Entry must meet these standards:

1. **Options Listed** — at least 2 options considered (even if one was obviously superior). "Only one option" is a red flag — dig deeper.
2. **Rationale Explicit** — the "because" is mandatory. No undocumented decisions.
3. **Impact Scoped** — what does this decision affect? Name specific artifacts, agents, or downstream work.
4. **Reversibility Noted** — is this a one-way door or two-way door? One-way doors get extra scrutiny.
5. **ADR-linked** — significant decisions (one-way doors, cross-agent impact, user-facing) produce a formal ADR at `{paths.decisions}`. Minor decisions stay in the Decision Registry only.
