# Agent Template

Create new agents using this compressed template. All agents follow the **CODE 5-Phase Relay** (Capture→Organize→Distill→Express).

## Structure
```
agents/<agent-name>/
  AGENT.md              # Agent OS (this file)
  context/
    ROLE.md             # Identity, Mandate, Facts & Directives
    *.md                # Domain context + output templates
# MARK files live in {paths.marks}/ — register prefix in .claude/settings.json
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

**Load role**: Read agent's configured role file (from `.claude/settings.json`) — adopt Identity, Mandate, and Facts & Directives before proceeding.

# Steps

Execute sequentially. **Read each skill file ONLY when entering that step.**

1. **Define objective for agent** with `rehydrate-context` → Session Objective. [Domain-specific input parsing].
   Gate: Objective confirmed

2. **Generate unfiltered Island Backlog** with `autonomous-capture` — hunt for: [domain-specific targets · separated · by · interpuncts].
   Gate: [minimum quantity gate]

3. **Map, group, and sequence the Island Backlog** with `strategic-organize` — [domain-specific clustering logic]. [Merge criteria]. [Classification scheme].
   Gate: [structure gate]

4. **Distill each island into a concrete result** with `expert-distill` — produce [Artifacts] per agent's configured template (from `.claude/settings.json`). [Domain quality criteria]. Progressive Summarization — scannable in 30s.
   **Decisions**: [3-5 decision criteria as · separated · list]
   **[Domain] check**: [Validation criteria as · separated · list]
   Gate: All → [Artifact] or Concern

5. **Update MARK files and emit** relay with `express-relay` — write [artifacts] to agent's configured output folder. Emit relay.
   Gate: MARKs + [Artifacts] + Relay

# Persistence
All paths (MARK files, output folder, template, role) resolve from `.claude/settings.json`. Register new agents there with: file, prefix, output, template, role.
```

---

## ROLE.md Template

Create `context/ROLE.md` for each agent. To **reuse a role**, point multiple agents to the same ROLE.md in settings.json.

```markdown
---
name: [role-name]
description: [1-line role summary]
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

## Facts & Directives

- [Behavioral guideline 1]
- [Behavioral guideline 2]
- [Domain-specific constraint or preference]
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

Seed new `{paths.marks}/[PREFIX]_Progress_Summary_MARK.md` files (resolve `{paths.marks}` from `.claude/settings.json`) with this structure:

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

Seed new `{paths.marks}/[PREFIX]_Questions_Log_MARK.md` files:

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
