---
name: orch-termination
description: Enforce termination conditions. Activates after Skill 6 (Response Cycle Management) reports all child agents have reached a terminal status.
---

# Skill: Enforce Termination Conditions
## Agent: Any Orchestrator Agent

---

### Semantic Role Labeling for Skill Definition

| Role | Value |
|------|-------|
| **[P] Predicate** | Enforce |
| **[A] Agent** | Any Orchestrator Agent |
| **[Pt] Patient** | Orchestration cycle completion checklist |
| **[R] Recipient** | User (receives cycle completion or hold notification) |
| **[Arg-TMP] Temporal** | After Skill 6 reports all children at terminal status |
| **[Arg-MNR] Manner** | Strict three-condition checklist — never round up, never declare partial as complete |

### Pre-conditions
- Skill 6 (Response Cycle Management) has completed monitoring
- All child agents have a terminal status (COMPLETED, SKIPPED, or BLOCKED) in the state MARK
- State MARK is current and readable

### Execution Steps

1. **Load state MARK** — Read the orchestrator's Orchestration State MARK. Parse Active Cycle and Child Agent Status table.

2. **Evaluate termination checklist** — Check each condition:

   ```
   □ Condition 1: All child agents have confirmed output
     - Every non-SKIPPED agent has status COMPLETED
     - Expected output artifacts exist at their declared paths
   
   □ Condition 2: No open blockers exist
     - No agent has status BLOCKED
     - Open Blockers section in state MARK is empty
   
   □ Condition 3: State has been written to MARK file
     - Active Cycle section is current
     - Child Agent Status table reflects final states
     - Cycle entry is complete (classification, triggers, timestamps, outputs)
   ```

3. **If ALL conditions met** — Declare cycle COMPLETE:
   - Move Active Cycle to Cycle History with status: **COMPLETE**
   - Record completion timestamp
   - Present summary to user:
     ```
     ✓ Orchestration cycle #[N] complete.
     
     Classification: [Type N: Label]
     Child agents completed: [list with output paths]
     
     All outputs are available. What would you like to do next?
     ```

4. **If ANY condition is unmet** — HOLD:
   - Do NOT declare cycle complete
   - List each unmet condition explicitly:
     ```
     ⏸ Orchestration cycle #[N] is HELD.
     
     Unmet conditions:
     - [ ] [Description of unmet condition]
     - [ ] [Description of unmet condition]
     
     The cycle cannot be completed until all conditions are met.
     ```
   - Keep Active Cycle in state MARK — do not move to history
   - If blockers exist → remind user of the blockers from Skill 6

5. **Update state MARK** — Write final status (COMPLETE or HELD) to the state MARK. Update Token Usage if available.

### Output Specification
| Output | Format | File Path |
|--------|--------|-----------|
| Updated Orchestration State MARK | MD | `{paths.marks}/{prefix}_Orchestration_State_MARK.md` |
| Cycle completion or hold notification | User-facing message | N/A — displayed to user |

### Termination Condition
The cycle has been evaluated and declared either COMPLETE (moved to history) or HELD (remains active with explicit unmet conditions listed).

### Post-conditions
- If COMPLETE: Active Cycle is in Cycle History with timestamp. No active cycle remains. All artifacts confirmed.
- If HELD: Active Cycle remains with HELD status. Unmet conditions are explicitly listed. User has been notified.
- State MARK is consistent — no contradictions between status fields
- Never: a cycle declared complete with open blockers, missing outputs, or unwritten state

### Error Handling
| Condition | Response |
|-----------|----------|
| State MARK missing or corrupted | Cannot evaluate termination. HOLD with reason: "State MARK unreadable — rebuild required." |
| Expected output artifact missing despite agent reporting COMPLETED | Override to HELD. Note: "[Agent] reported complete but output not found at [path]." |
| Partial completion (some agents complete, some blocked) | HOLD. List which completed and which are blocked. Do not declare partial success as complete. |
| SKIPPED agents | SKIPPED agents do not prevent cycle completion. They are excluded from Condition 1 — only non-SKIPPED agents must have confirmed output. |
