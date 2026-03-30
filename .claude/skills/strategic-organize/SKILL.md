---
name: strategic-organize
description: Map, group, and sequence the Island Backlog into an Execution Roadmap.
---

### Steps

1. **Deduplicate** — merge true duplicates, cross-reference near-duplicates.

2. **Cluster into execution groups** by theme, dependency, and type. Use the agent's **Organize Lens** for domain-appropriate grouping. Each group → a roadmap section.

3. **Sequence PREREQUISITEs first**, then resolve DEPENDENCY order. Flag circular dependencies as blockers.

4. **Triage QUESTIONs:**

   | Classification | Action |
   |----------------|--------|
   | Answerable now | Convert to DECISION or resolve inline |
   | Needs user input | BLOCKED-USER |
   | Needs another agent | BLOCKED-AGENT:[agent] |
   | Not this session | DEFERRED → parking lot |

5. **Sequence within groups**: Prerequisites → Dependencies → Decisions → Quick wins → Complex items.

6. **Emit Execution Roadmap** inline (consumed by Phase 4):

### Output

```
# Execution Roadmap
Summary: [total] islands → [after dedup] | [N] groups | [N] blocked | [N] dropped

## Group: [Theme]
| Seq | ID | Type | Description | Priority | Size | Depends On | Status |

## Blocked Items
| ID | Blocked By | Resolution Path |

## Parking Lot
| ID | Reason Deferred |
```

Gate: ≥1 READY island in queue → proceed to Phase 4.

### Post-conditions
- All islands from the Island Backlog are accounted for (in Execution Queue, Blocked, Parking Lot, or Dropped).
- No island disappeared without a logged reason.
- Dependencies are resolved — no circular dependencies exist (or they are flagged as blockers).
- QUESTION islands are triaged — the agent knows which ones to ask the user before proceeding.
- The Execution Queue is sequenced — Phase 4 can work through it top to bottom.
- Effort estimates exist for every READY island.

### Error Handling

| Condition | Response |
|-----------|----------|
| Island Backlog is empty | This should not happen (Phase 2 guarantees ≥1 island). If it occurs, return to Phase 2. |
| Circular dependency detected | Flag all islands in the cycle as `BLOCKED-CIRCULAR`. Surface to user: "Circular dependency between ISL-XXX, ISL-YYY — need direction on which to resolve first." |
| All islands are BLOCKED | No READY items to execute. Surface the blocked items to the user and ask for resolution. Do not proceed to Phase 4 until at least 1 island is READY. |
| XL island cannot be meaningfully split | Flag as `BLOCKED-SCOPE`: "This island exceeds single-session capacity. Recommend re-scoping the Session Objective." |
| Agent's Organize Lens is not defined | Use default grouping (Theme + Type + Dependency). Log: "No Organize Lens defined — using default grouping strategy." |

---

### Convergent Thinking Rules (Phase 3)

These rules mark the shift FROM divergent TO convergent. The creative phase is over. Structure now.

1. **Every island gets classified** — nothing remains uncategorized.
2. **Duplicates are merged** — with attribution to both originals.
3. **Dependencies are explicit** — no implicit ordering assumptions.
4. **Questions are triaged** — not left as vague "TBD" items.
5. **Drops are justified** — every archived island has a logged reason.
6. **Sequence is commitment** — the order in the Execution Queue is the order of execution in Phase 4.
7. **Blocked items are surfaced** — not hidden and not silently skipped.
