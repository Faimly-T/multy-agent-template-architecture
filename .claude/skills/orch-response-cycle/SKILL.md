---
name: orch-response-cycle
description: Monitor child agent completion. Activates after Skill 5 (Workflow State Tracking) writes state — captures session signals or falls back to MARK files for cross-session resume.
---

# Skill: Monitor Child Agent Completion
## Agent: Any Orchestrator Agent

---

### Signal Modes

This skill operates in two modes to minimize token consumption:

| Mode | When | How Status Is Determined | Token Cost |
|------|------|-------------------------|------------|
| **Session Signal** | Child agent ran in this session | Capture the System Relay emitted by child's `express-relay` (Phase 5) directly from session output | **Low** — no file reads |
| **MARK Fallback** | Resuming an interrupted cycle from a prior session | Read child agent Progress Summary MARKs | **Higher** — full file reads |

**Default: Session Signal mode.** Use MARK Fallback only when resuming a prior cycle (Active Cycle with TRIGGERED agents found in state MARK at session start, but no child execution observed in current session).

### Semantic Role Labeling for Skill Definition

| Role | Value |
|------|-------|
| **[P] Predicate** | Monitor |
| **[A] Agent** | Any Orchestrator Agent |
| **[Pt] Patient** | Child agent session signals (primary) or Progress Summary MARKs (fallback) |
| **[R] Recipient** | User (receives blocker notifications) · Skill 7 (receives completion status) |
| **[Arg-TMP] Temporal** | After each child agent execution returns, or on session resume |
| **[Arg-MNR] Manner** | Session signal capture (primary), MARK file read (fallback only) |

### Pre-conditions
- Skill 5 (Workflow State Tracking) has written the Active Cycle to state MARK
- Child Agent Status table exists with all triggered agents listed as TRIGGERED
- For Session Signal mode: child agent has finished executing in the current session
- For MARK Fallback mode: child agent MARK file paths are resolvable from `.claude/settings.json`

### Execution Steps

#### Step 0 — Determine mode

Check: did the orchestrator fire triggers and observe child agent execution in this session?
- **Yes** → Session Signal mode (Steps 1–6)
- **No** (resuming a prior cycle) → MARK Fallback mode (Step 7)

#### Session Signal Mode (default — in-session)

1. **Capture child agent result** — After each child agent completes execution, capture from session output:
   - **Exit status**: Did the agent complete Phase 5 (`express-relay`)? Or did it halt/error before?
   - **System Relay**: The `| Target Agent | Signal | Action Needed |` table emitted in Phase 5
   - **Artifacts produced**: File paths mentioned in the agent's Phase 5 output
   - **Blockers**: Any HALT, BLOCKED, or error signals emitted during execution

2. **Map to status** — Translate session signal to status:

   | Session Signal | → Status |
   |----------------|----------|
   | Agent completed Phase 5, System Relay emitted | **COMPLETED** |
   | Agent halted before Phase 5 with explicit blocker | **BLOCKED** |
   | Agent errored or produced no output | **FAILED** |

3. **Handle sequential chains** — If execution order is `sequential: agent1 → agent2 → ...`:
   - When agent N's session signal = COMPLETED → fire the trigger for agent N+1
   - Do NOT fire agent N+1 until agent N is COMPLETED
   - Capture agent N+1's session signal when it returns

4. **Handle parallel execution** — If execution order is `parallel`:
   - All agents were fired simultaneously by Skill 4
   - Collect each agent's session signal as it returns

5. **Route blockers** — If any child agent status is BLOCKED or FAILED:
   - Extract blocker from session output (the agent's halt message or error)
   - Surface to user immediately:
     ```
     ⚠ BLOCKER from [Agent Name]:
     [Blocker description from session output]
     
     This is preventing the orchestration cycle from completing.
     ```

6. **Handle conditional agents** — If an agent is marked as conditional:
   - For conditions that check MARK content (e.g., "open questions exist"): read **only** the specific MARK section needed (e.g., Questions Log Active section), not the full Progress Summary MARK
   - If condition met → fire conditional agent, capture its session signal
   - If not met → mark as SKIPPED

#### MARK Fallback Mode (cross-session resume only)

7. **Read child agent MARKs** — For each agent in the Child Agent Status table with status TRIGGERED or IN-PROGRESS:
   - Read the child agent's Progress Summary MARK (`{paths.marks}/{child.prefix}_Progress_Summary_MARK.md`)
   - Check "Where We Stopped" → Completion status:
     - `[x] full` or deliverables listed → **COMPLETED**
     - `blocked` or open blockers listed → **BLOCKED**
     - Otherwise → **IN-PROGRESS** or **NOT-STARTED**
   - Check the child's output folder for expected artifacts
   - Apply the same chain/parallel/blocker/conditional logic as Session Signal mode (Steps 3–6), using MARK data instead of session signals

#### Common (both modes)

8. **Update state MARK** — Write the updated Child Agent Status table to the orchestrator's state MARK after all agents are evaluated.

### Output Specification
| Output | Format | File Path |
|--------|--------|-----------|
| Updated Orchestration State MARK | MD | `{paths.marks}/{prefix}_Orchestration_State_MARK.md` |
| Blocker notifications | User-facing message | N/A — displayed to user |

### Termination Condition
All child agents in the Active Cycle have a terminal status: COMPLETED, SKIPPED, BLOCKED, or FAILED. No agents remain in TRIGGERED or IN-PROGRESS state.

### Post-conditions
- Every child agent has a final status in the state MARK
- All blockers have been surfaced to the user
- Sequential chain agents have been triggered in order as predecessors completed
- Conditional agents have been evaluated and either triggered or skipped
- State MARK is current — reflects actual child agent states
- **Session Signal mode: zero MARK file reads for status determination** (conditional agent checks may read specific MARK sections only)

### Error Handling
| Condition | Response |
|-----------|----------|
| Child agent session output ambiguous (no clear signal) | Fall back to MARK read for that specific agent only |
| Child agent MARK file missing (Fallback mode) | Treat as NOT-STARTED. Surface to user. |
| Sequential chain — predecessor BLOCKED or FAILED | Do NOT trigger successor. Mark successor as BLOCKED-UPSTREAM. Surface blocker. |
| All agents COMPLETED but conditional agent not yet evaluated | Evaluate conditional agent before passing to Skill 7. |
| Session interrupted mid-execution | State MARK retains TRIGGERED status. Next session enters MARK Fallback mode automatically. |
