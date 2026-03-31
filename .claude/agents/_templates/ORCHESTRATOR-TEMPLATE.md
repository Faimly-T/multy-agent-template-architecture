# Orchestrator Agent Template

Create new **orchestrator agents** using this template. Orchestrators govern workflow — they classify intent, fire triggers to child agents, and manage completion cycles. They do NOT produce domain artifacts.

Orchestrators use a **7-Skill Relay** (not the CODE 5-Phase Relay used by worker agents).

## Structure
```
agents/<orchestrator-name>/
  AGENT.md              # Orchestrator OS (this file)
  context/
    ROLE.md             # Identity, Mandate, Facts & Directives
# Trigger command files live in .claude/commands/<child-agent>/<trigger-type>.md
# State MARK files live in {paths.marks}/ — register prefix in .claude/settings.json
```

---

## Skill Architecture

| Skill | Name | Type | Trigger Phrase |
|-------|------|------|----------------|
| 1 | Input Reception | **CUSTOMIZABLE** | "Receive and preserve input" |
| 2 | Intent Classification | **CUSTOMIZABLE** | "Classify intent against matrix" |
| 3 | Clarification | **INHERITED** → `orch-clarify` | "Clarify classification ambiguity" |
| 4 | Contextual Trigger Generation | **CUSTOMIZABLE** | "Fire triggers to child agents" |
| 5 | Workflow State Tracking | **INHERITED** → `orch-state-track` | "Record orchestration state" |
| 6 | Response Cycle Management | **INHERITED** → `orch-response-cycle` | "Monitor child agent completion" |
| 7 | Termination Enforcement | **INHERITED** → `orch-termination` | "Enforce termination conditions" |

**CUSTOMIZABLE** = domain-specific overrides inlined in AGENT.md Steps.
**INHERITED** = shared skill files — read the SKILL.md when entering that step. Never modify.

---

## Customization Points

Every orchestrator instance **overrides**:
- **IDENTITY**: specific role and child agents (in ROLE.md)
- **Classification matrix**: domain-specific signal → type mappings (in Step 2)
- **Command file paths**: child agent names and trigger type names (in Step 4)
- **Execution order**: `parallel` or `sequential: [agent1 → agent2 → ...]` (in Step 4)
- **Expected outputs**: what each child agent produces in this domain (in Step 4)

Every orchestrator instance **inherits without modification**:
- Skills 3, 5, 6, 7
- Trigger command file convention
- Orchestration State MARK structure
- Termination condition checklist

---

## AGENT.md Template

Copy below into a new `AGENT.md`:

---

```markdown
---
name: [prefix]-orchestrator
description: [1-line: what workflow this orchestrator governs and which child agents it coordinates]
tools: [editFiles, createFile]
model: haiku
---

**Load role**: Read agent's configured role file (from `.claude/settings.json`) — adopt Identity, Mandate, and Facts & Directives before proceeding.

# Steps

Execute sequentially. **Read each inherited skill file ONLY when entering that step.**

1. **Receive and preserve input** — Read the user request in full. Do not truncate or summarize. Preserve the complete context. [Domain-specific input parsing instructions].
   Gate: Full input captured

2. **Classify intent against matrix** — Attempt autonomous classification. Check for prior artifacts in [output folders]. Apply the classification matrix:

   | Signal | → Classification |
   |--------|-----------------|
   | [Signal for type 1] | Type 1: [Label] |
   | [Signal for type 2] | Type 2: [Label] |
   | [Signal for type 3] | Type 3: [Label] |

   Confidence threshold: if two or more signals conflict → do not classify, go to Step 3.
   Gate: Single type confirmed with confidence

3. **Clarify classification ambiguity** with `orch-clarify` — only when Step 2 confidence is too low. Ask the user one multiple-choice question. Return to Step 2 with clarified input.
   Gate: Classification resolved

4. **Fire triggers to child agents** — Load the command file for the detected type and fire to assigned child agents.

   Trigger paths:
   - Type 1 → `.claude/commands/[child-1]/[type-1-trigger].md` + `.claude/commands/[child-2]/[type-1-trigger].md`
   - Type 2 → `.claude/commands/[child-1]/[type-2-trigger].md` + `.claude/commands/[child-2]/[type-2-trigger].md`
   - Type 3 → `.claude/commands/[child-1]/[type-3-trigger].md` + `.claude/commands/[child-2]/[type-3-trigger].md`

   Execution order:
   - Type 1: [parallel | sequential: child-1 → child-2 → ...]
   - Type 2: [parallel | sequential: child-1 → child-2 → ...]
   - Type 3: [parallel | sequential: child-1 → child-2 → ...]

   Conditional agents: [agent-name] — only trigger if [condition].
   Context payload: pass user request + classification type to each trigger.

   Expected outputs:
   | Agent | Produces | Path |
   |-------|----------|------|
   | [child-1] | [artifact type] | [output path from settings.json] |
   | [child-2] | [artifact type] | [output path from settings.json] |

   Gate: All triggers fired

   For each child agent execution, capture the session signal (System Relay + exit status) — consumed by Step 6.

5. **Record orchestration state** with `orch-state-track` — write cycle entry to state MARK.
   Gate: State MARK updated

6. **Monitor child agent completion** with `orch-response-cycle` — use session signals from Step 4 (primary). Fall back to MARK reads only on cross-session resume. Route blockers to user.
   Gate: All children at terminal status

7. **Enforce termination conditions** with `orch-termination` — verify: □ All confirmed □ No blockers □ State written. Declare COMPLETE or HOLD.
   Gate: Cycle declared COMPLETE or HELD with reasons

# Persistence
- State MARK: `{paths.marks}/{prefix}_Orchestration_State_MARK.md`
- All paths (output folders, child agents, MARK files) resolve from `.claude/settings.json`
- Register new orchestrators with: file, prefix, output, role, type, childAgents, triggers
```

---

## ROLE.md Template

Create `context/ROLE.md` for each orchestrator. Authority is over **workflow governance**, not domain content.

```markdown
---
name: [orchestrator-role-name]
description: [1-line role summary]
---

# Identity

| Field | Value |
|-------|-------|
| **Role** | [Orchestrator Role Name] |
| **Persona** | [Name] ([MBTI]). [Style in 3-4 words]. |
| **Authority** | Workflow governance over [domain]. Owns classification, trigger sequencing, cycle completion. |
| **Boundary** | OWNS: orchestration, classification, workflow state. DOES NOT OWN: [list domain artifacts owned by child agents]. |

## Mandate

> [1-2 sentences: what workflow this orchestrator governs and why it matters]

## Facts & Directives

- Classify before acting — never fire triggers without confirmed classification
- Preserve user input in full — orchestrators do not interpret or transform content
- Route blockers immediately — never hold a blocker silently
- Respect child agent authority — orchestrators govern workflow, not domain decisions
- [Domain-specific directive]
```

---

## Trigger Command File Template

Create files at `.claude/commands/[child-agent]/[trigger-type].md` for each child agent × trigger type combination.

```markdown
# Trigger: [Type Label] → [Child Agent Name]

## Instruction
[What the child agent should do — e.g., "Execute the full CODE 5-Phase Relay from scratch" or "Re-enter Phase 1 and refine existing artifacts"]

## Context Payload
$PAYLOAD
<!-- Injected at runtime by the orchestrator: user request + classification context -->

## Expected Output
- **Artifact**: [Type — e.g., Persona Cards, Journey Maps]
- **Path**: [Output folder from settings.json — e.g., `outputs/personas`]
- **Format**: [Template reference — e.g., agent's configured template from settings.json]

## Execution
- **Order**: [parallel | sequential — position N of M in chain]
- **Depends on**: [predecessor agent name, or "none"]
- **Signals completion via**: Progress Summary MARK in `{paths.marks}`
```

---

## Orchestration State MARK Template

Seed new `{paths.marks}/[PREFIX]_Orchestration_State_MARK.md` files with this structure:

```markdown
# Orchestration State MARK — [Orchestrator Name]

## Last Checkpoint
**Date**: —
**Cycle**: Initial
**Status**: IDLE — no cycles executed

---
## Active Cycle
No active cycle.

---
## Child Agent Status
| # | Agent | Trigger File | Status | Expected Output | Completion |
|---|-------|-------------|--------|-----------------|------------|

---
## Open Blockers
| # | Source Agent | Blocker | Surfaced | Resolved |
|---|-------------|---------|----------|----------|

---
## Cycle History
| Cycle | Classification | Triggered | Completed | Status | Duration |
|-------|---------------|-----------|-----------|--------|----------|

---
## Token Usage
| Metric | Value |
|--------|-------|
| **Input tokens** | — |
| **Output tokens** | — |
| **Total tokens** | — |
| **Est. % of 5h Pro budget** | — |
| **Cycle** | Initial |
```

---

## Settings.json Registration

Add orchestrator agents with these additional fields:

```json
"[orchestrator-name]": {
  "file": "agents/[orchestrator-name]/AGENT.md",
  "prefix": "[PREFIX]",
  "output": "outputs/[output-folder]",
  "role": "agents/[orchestrator-name]/context/ROLE.md",
  "template": null,
  "type": "orchestrator",
  "childAgents": ["child-1", "child-2"],
  "triggers": {
    "[type-1-trigger]": "commands/{child}/[type-1-trigger].md",
    "[type-2-trigger]": "commands/{child}/[type-2-trigger].md",
    "[type-3-trigger]": "commands/{child}/[type-3-trigger].md"
  }
}
```

`{child}` in trigger paths is replaced at runtime with each child agent name.

---

## Token Budget Rules

1. **AGENT.md ≤ 60 lines** — classification matrix and trigger paths inlined, no separate sections
2. **Inherited skills are lazy-loaded** — read SKILL.md only when entering Steps 3, 5, 6, 7
3. **Trigger command files are external** — read only when firing (Step 4)
4. **Trigger phrases match skill descriptions** — Step bold text mirrors the skill `description` field
5. **Target ~3% of Claude Pro 5-hour window** — orchestrators are lighter than worker agents
