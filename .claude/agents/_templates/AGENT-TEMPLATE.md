# Agent Template

Create new agents using this compressed template. All agents follow the **CODE 5-Phase Relay** (Capture→Organize→Distill→Express).

## Structure
```
agents/<agent-name>/
  AGENT.md              # Agent OS (this file)
  context/
    *.md                # Domain context + output templates
  outputs/
    [AGENT]_Progress_Summary_MARK.md
    [AGENT]_Questions_Log_MARK.md
```

---

## AGENT.md Template

Copy below into a new `AGENT.md`:

---

```markdown
---
name: [prefix]-[name]
description: [1-line: what this agent transforms and for whom]
tools: [editFiles, createFile]
---

# Identity
| Field | Value |
|-------|-------|
| **Role** | [Specialized Role] |
| **Persona** | [Name] ([MBTI]). [Style in 3-4 words]. |
| **Authority** | Full autonomy over [domain]. Owns [what]. |
| **Boundary** | OWNS: [list]. DOES NOT OWN: [list]. |

## Mandate
> [1-2 sentences: what this agent delivers and why it matters]

# Steps

Execute sequentially. **Read each skill file ONLY when entering that step.**

1. **Define objective for agent** with `rehydrate-context` → Session Objective. [Domain-specific input parsing].
   Gate: Objective confirmed

2. **Generate unfiltered Island Backlog** with `autonomous-capture` — hunt for: [domain-specific targets · separated · by · interpuncts].
   Gate: [minimum quantity gate]

3. **Map, group, and sequence the Island Backlog** with `strategic-organize` — [domain-specific clustering logic]. [Merge criteria]. [Classification scheme].
   Gate: [structure gate]

4. **Distill each island into a concrete result** with `expert-distill` — produce [Artifacts] per `context/[output-template].md`. [Domain quality criteria]. Progressive Summarization — scannable in 30s.
   **Decisions**: [3-5 decision criteria as · separated · list]
   **[Domain] check**: [Validation criteria as · separated · list]
   Gate: All → [Artifact] or Concern

5. **Update MARK files and emit** relay with `express-relay` — write [artifacts] to `outputagent/[domain]/`. Emit relay.
   Gate: MARKs + [Artifacts] + Relay

# Persistence
| File | Purpose |
|------|---------|
| `outputs/contextAgent/[AGENT]_Progress_Summary_MARK.md` | Session continuity |
| `outputs/contextAgent/[AGENT]_Questions_Log_MARK.md` | Open questions log |
| `outputs/contextAgent/[output-template].md` | Output template (read in Step 4 only) |
```

---

### Token Budget Rules
1. **AGENT.md ≤ 60 lines** — domain config inlined into Steps, no separate sections
2. **Externalize output templates** — put in `context/`, read only during Step 4
3. **Lazy-load skills** — Steps reference skill names, agent reads ONLY when entering that step
4. **Trigger phrases match skill descriptions** — Step bold text mirrors the skill `description` field
5. **No SRL tables in skills** — stripped for token efficiency

---

## Progress Summary MARK Template

Seed new `outputs/contextAgent/[PREFIX]_Progress_Summary_MARK.md` files with this structure:

```markdown
# Progress Summary MARK — [Agent Name]

## Last Checkpoint
**Date**: —
**Session**: Initial
**Session Objective Was**: N/A — No prior session

---
## What Was Accomplished
### Deliverables Produced
| # | Deliverable | Path | Status |
|---|---|---|---|

### Decisions Made
| # | Decision | ADR Ref | Impact |
|---|---|---|---|

### Key Actions Taken
- Agent operating system created and configured

---
## Where We Stopped
**Last action before session end**:
> Agent created. No work has begun yet.

**Completion status of Session Objective**:
- [x] Initial session — no prior objective

---
## Open Threads
| # | Thread | Status | Next Action | Priority |
|---|---|---|---|---|

---
## Momentum Direction
**Where the project is heading**:
> [Awaiting first session input]

**Recommended next session focus**:
> [First domain task]

---
## Token Usage
| Metric | Value |
|--------|-------|
| **Input tokens** | — |
| **Output tokens** | — |
| **Total tokens** | — |
| **Est. % of 5h Pro budget** | — |
| **Session** | Initial |
```

## Questions Log MARK Template

Seed new `outputs/contextAgent/[PREFIX]_Questions_Log_MARK.md` files:

```markdown
# Questions Log MARK — [Agent Name]

## Active Questions
| # | Question | Source | Hard/Soft Blocker | Date |
|---|---|---|---|---|

## Resolved Questions
| # | Question | Answer | Resolved By | Date |
|---|---|---|---|---|

## Obsolete Questions
| # | Question | Reason | Date |
|---|---|---|---|
```
