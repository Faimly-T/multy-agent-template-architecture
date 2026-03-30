---
name: autonomous-capture
description: Generate unfiltered Island Backlog from the Session Objective.
---

### Steps

1. **Load Session Objective** — extract verb, deliverable, success condition, stakes. These anchor every island.

2. **Pass 1 — Objective Decomposition**: What sub-deliverables, decisions, missing info, risks, and dependencies does the objective require?

3. **Pass 2 — Edge Exploration**: What adjacent concerns, stakeholder challenges, or unvalidated assumptions exist?

4. **Tag each island:**

   | Tag | Meaning |
   |-----|---------|
   | WORK | Concrete task → artifact |
   | DECISION | Choice point to resolve |
   | QUESTION | Info needed from user/agent |
   | RISK | Could block/degrade objective |
   | DEPENDENCY | Must happen first/parallel |
   | PREREQUISITE | Existing artifact to review |

5. **Number** sequentially: ISL-001, ISL-002, etc.

### Output
Inline Island Backlog (not persisted — Phase 3 consumes it):

```
| ID | Type | Island Description | Source | Relates To |
```

Footer: total count + breakdown by type.

### Rules
- **DIVERGENT mode** — no filtering, no judging, no prioritizing
- Quantity over polish — every possible item gets an island
- Apply the agent's **Capture Lens** to cast the widest domain-specific net
- Quality floor: < 3 islands for non-trivial objective → re-run Edge Exploration

### Error Handling

| Condition | Response |
|-----------|----------|
| Session Objective is not confirmed | Halt. Return to Phase 1. Do not capture without a confirmed objective. |
| Agent's Capture Lens is not defined in AGENT.md | Use generic capture (all three passes) without domain filtering. Log: "No Capture Lens defined — using generic divergent capture." |
| Domain context files are missing or empty | Proceed with Objective Decomposition and Edge Exploration only. Note missing context in a `PREREQUISITE` island: "Context file [path] is missing — must be populated before informed capture." |
| Capture produces 0 islands | This should not happen for a confirmed objective. Log error: "Capture produced zero islands for a confirmed objective — the objective may be too abstract. Recommend re-running Phase 1 with more specificity." Return to Phase 1. |
| Duplicate islands detected | Keep both — deduplication is Phase 3's job. Capture never filters. |

---

### Divergent Thinking Rules

These rules enforce the DIVERGENT mode during capture. Violating them contaminates the creative phase with premature convergence:

1. **No killing** — Every idea becomes an island. Even "bad" ideas may reveal hidden assumptions.
2. **No sequencing** — Do not think about order. That is Phase 3.
3. **No estimating** — Do not think about effort. That is Phase 3-4.
4. **No grouping** — Do not cluster yet. That is Phase 3.
5. **No polishing** — Island descriptions can be rough. Refinement is Phase 4.
6. **Yes to quantity** — More islands = more raw material for Phase 3 to work with.
7. **Yes to edge cases** — The third pass exists specifically to capture things others miss.
8. **Yes to concerns** — Risks and questions are first-class islands, not afterthoughts.
