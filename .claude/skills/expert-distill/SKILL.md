---
name: expert-distill
description: Distill each island into a concrete result or formal concern.
---

### Steps

1. **Resolve blocks first** — surface BLOCKED-USER items. If answered → READY. If deferred → Parking Lot.

2. **Process queue top-to-bottom**, applying the agent's **Distill Lens**:

   | Island Type | Action | Mark As |
   |-------------|--------|---------|
   | PREREQUISITE | Read, confirm current, extract info | RESULT:REVIEWED |
   | DECISION | State options (≥2), apply Decision Framework, recommend/decide | RESULT:DECIDED or CONCERN:NEEDS-APPROVAL |
   | WORK | Produce artifact using agent's output template, write to agent's configured output folder (from `.claude/settings.json`) | RESULT:PRODUCED |
   | QUESTION | Record answer + source | RESULT:ANSWERED |
   | RISK | Assess probability/impact, propose mitigation | RESULT:MITIGATED or CONCERN:ESCALATED |
   | DEPENDENCY | Check resolution status | RESULT:RESOLVED or CONCERN:BLOCKED |

3. **Update Results Ledger** after each island:
   ```
   | ID | Type | Status | Outcome | Artifact Path |
   ```

4. **Scope guard** — if running long, complete current island, mark remaining as DEFERRED-TIME, proceed to Phase 5. Don't rush.

5. **Progressive Summarization** on every artifact: scannable in 30 seconds. **Bold** key passages, highlight critical takeaway.

### Output
- **Results Ledger** (inline → Phase 5 consumes it)
- **Domain artifacts** → agent's configured output folder (`{agent.output}` from `.claude/settings.json`)
- **Decision records** → `{paths.decisions}/ADR-XXX-[title].md` (from `.claude/settings.json`)
| A WORK island produces an artifact that conflicts with an existing artifact | Do NOT overwrite silently. Log as CONCERN: "New artifact conflicts with existing [path]. Both versions preserved. User must resolve." Save new artifact with `-V[N+1]` suffix. |
| A DECISION island has no clear winner among options | Present all options with trade-offs to user. Mark as `CONCERN:NEEDS-APPROVAL`. Do not force a decision without sufficient basis. |
| An island takes unexpectedly long | After completing the current island, assess remaining queue. If >50% remains, switch to "highlight mode" — produce skeleton/outline results for remaining islands instead of full distillation. Mark as `RESULT:OUTLINE`. |
| A DEPENDENCY island is still unresolved | Mark as `CONCERN:BLOCKED` with specific detail. Do not skip downstream islands silently — mark them as `BLOCKED-UPSTREAM:ISL-XXX`. |
| Agent's Distill Lens is not defined | Use generic quality criteria: clarity, completeness, actionability. Log: "No Distill Lens defined — using generic distillation criteria." |

---

### Distillation Quality Standards

Every RESULT produced in Phase 4 must meet these standards:

1. **Concrete** — names specific things, not abstractions. "We will use PostgreSQL" not "We will use an appropriate database."
2. **Attributable** — states the reasoning or source. Every decision has a "because."
3. **Actionable** — a reader knows exactly what to do next.
4. **Scannable** — Progressive Summarization applied. Bold the key, highlight the critical.
5. **Scoped** — does not exceed the island's scope. If the work grew, log a new island for Phase 5 to capture.
6. **Honest** — if the agent is uncertain, it says so. Concerns are first-class outputs, not failures.
