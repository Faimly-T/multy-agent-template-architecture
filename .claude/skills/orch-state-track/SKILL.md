---
name: orch-state-track
description: Record orchestration state. Activates after Skill 4 (Contextual Trigger Generation) fires triggers to child agents.
---

# Skill: Record Orchestration State
## Agent: Any Orchestrator Agent

---

### Semantic Role Labeling for Skill Definition

| Role | Value |
|------|-------|
| **[P] Predicate** | Record |
| **[A] Agent** | Any Orchestrator Agent |
| **[Pt] Patient** | Cycle metadata — classification type, triggered agents, timestamps, expected outputs |
| **[R] Recipient** | Skills 6 and 7 — consume state to monitor completion and enforce termination |
| **[Arg-TMP] Temporal** | Immediately after Skill 4 fires all triggers |
| **[Arg-LOC] Location** | `{paths.marks}/{prefix}_Orchestration_State_MARK.md` |

### Pre-conditions
- Skill 4 (Contextual Trigger Generation) has completed — all triggers fired
- Classification type is confirmed (from Skill 2 or Skill 3)
- Child agent names and trigger file paths are known
- Orchestrator prefix and MARK path are resolvable from `.claude/settings.json`

### Execution Steps

1. **Resolve state MARK path** — From `.claude/settings.json`: `{paths.marks}/{agent.prefix}_Orchestration_State_MARK.md`. If the file does not exist, create it using the Orchestration State MARK template from the orchestrator template.

2. **Read current state** — Load the existing MARK file. Parse the Active Cycle section (if any prior cycle exists, it should already be in Cycle History).

3. **Compose cycle entry** — Build the cycle record:
   ```markdown
   ## Active Cycle
   **Cycle**: #[N] (increment from last cycle number, or 1 if first)
   **Classification**: [Type N: Label]
   **Triggered**: YYYY-MM-DD HH:MM
   **Execution Order**: [parallel | sequential: agent1 → agent2 → ...]
   **Input Summary**: [1-sentence summary of user request]
   
   ### Child Agent Status
   | # | Agent | Trigger File | Status | Expected Output | Completion |
   |---|-------|-------------|--------|-----------------|------------|
   | 1 | [agent-name] | [trigger-path] | TRIGGERED | [artifact type + path] | — |
   | 2 | [agent-name] | [trigger-path] | TRIGGERED | [artifact type + path] | — |
   ```

4. **Move prior Active Cycle to history** — If a previous Active Cycle section exists, move it to the Cycle History section with its final status.

5. **Write state MARK** — Overwrite the file with the updated content. Preserve all other sections (Last Checkpoint, Open Blockers, Token Usage, Cycle History).

### Output Specification
| Output | Format | File Path |
|--------|--------|-----------|
| Orchestration State MARK | MD | `{paths.marks}/{prefix}_Orchestration_State_MARK.md` |

### Termination Condition
The state MARK file contains the current cycle entry with all triggered child agents listed, status = TRIGGERED, and the Active Cycle section is current.

### Post-conditions
- State MARK reflects the current cycle accurately
- All fired triggers are logged with agent name, trigger file, and expected output
- Prior cycles are preserved in Cycle History
- File is well-formed and parseable by Skills 6 and 7

### Error Handling
| Condition | Response |
|-----------|----------|
| MARK file corrupted or unparseable | Rebuild from current cycle data. Log warning: "State MARK rebuilt — prior history may be incomplete." |
| MARK file missing | Create from template. Set Cycle #1. |
| Trigger count mismatch (fired ≠ logged) | Surface discrepancy. Log all triggers that were fired. Flag missing ones as UNKNOWN. |
