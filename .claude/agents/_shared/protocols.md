# Shared Agent Protocols

## CODE Operating System

All agents in this team operate under the **CODE Execution Relay** — the 5-Phase framework based on Tiago Forte's Capture → Organize → Distill → Express methodology, adapted for AI agent workflows.

### The 5-Phase Relay

Every agent session follows this sequence. No phase is skipped. No phase is reordered.

| Step | Skill | Mode | Gate |
|------|-------|------|------|
| 1. Re-Hydrate | `rehydrate-context` | Entry | Session Objective confirmed |
| 2. Capture | `autonomous-capture` | DIVERGENT | Island Backlog produced |
| 3. Organize | `strategic-organize` | CONVERGENT | Execution Roadmap sequenced |
| 4. Distill | `expert-distill` | CONVERGENT | All islands processed |
| 5. Relay | `express-relay` | Exit | MARK files updated + Relay emitted |

Skills are lazy-loaded — read each skill file ONLY when entering that step.

### Trigger Phrase Contract

Each agent Step uses a **bold phrase** matching the skill's `description`. The agent says **what** (+ domain context); the skill defines **how**.

### Divergent → Convergent Boundary

The critical mode shift happens at the **Phase 2 → Phase 3 boundary**:
- **Phase 2 (Capture)** = DIVERGENT: generate freely, no filtering, quantity over quality
- **Phase 3+ (Organize, Distill, Express)** = CONVERGENT: structure, refine, decide, ship

---

## Communication Standards

All agents in this team follow these shared protocols for consistent, high-quality collaboration.

### Handoff Protocol

When transferring work or context between agents:

1. **Summarize** — Provide a concise summary of work done
2. **Highlight** — Call out open questions, blockers, or assumptions
3. **Artifacts** — Reference all generated artifacts by path
4. **Next Steps** — Clearly state what the receiving agent should do next

Cross-agent handoffs trigger a **System Relay** (from Phase 5) that is written to both the outgoing agent's status output and the receiving agent's Questions Log.

### Conflict Resolution

When agents disagree:

1. Each agent states their position with rationale
2. Identify the root trade-off (value vs. feasibility vs. timeline)
3. The UX Persona Architect has final authority on **persona definitions and user needs**
4. The UX Journey Architect has final authority on **journey maps, stage sequencing, and touchpoint flows**
5. The PO has final authority on **scope and priority**
6. The Architect has final authority on **technical approach**
7. The PjM (Question Interviewer) has final authority on **question consolidation, interview facilitation, and answer distribution**
8. All resolutions are logged as decisions in `{paths.decisions}` (from `.claude/settings.json`)

### File I/O Permissions

Agents and skills have **full read/write access** to `{paths.outputs}` (from `.claude/settings.json`) and agent `context/` folders without requiring user confirmation. Create files and directories as needed during execution.

### Quality Standards

- All outputs must be actionable and specific
- Avoid vague language — quantify where possible
- Reference source context when making claims
- Use consistent terminology (see `glossary.md`)
- Date all artifacts
- Apply Progressive Summarization to all artifacts (bold key passages, highlight critical takeaways)

### Knowledge Capture Rule

Every significant discussion, decision, or insight must be captured:
- Decisions → `{paths.decisions}` (from `.claude/settings.json`, ADR format)
- Lessons learned → `{paths.outputs}/lessons/`
- Meeting outcomes → agent's configured output folder
- Session state → MARK files (updated every Phase 5)

---

## Orchestrator Protocol

Orchestrator agents govern workflow execution — they classify intent, fire triggers to child agents, and track cycle completion. They do NOT produce domain artifacts.

### 7-Skill Relay

Orchestrators follow a different relay from worker agents:

| Skill | Name | Type | Gate |
|-------|------|------|------|
| 1 | Input Reception | Customizable | Full input captured |
| 2 | Intent Classification | Customizable | Single type confirmed |
| 3 | Clarification | Inherited (`orch-clarify`) | Classification resolved |
| 4 | Contextual Trigger Generation | Customizable | All triggers fired |
| 5 | Workflow State Tracking | Inherited (`orch-state-track`) | State MARK updated |
| 6 | Response Cycle Management | Inherited (`orch-response-cycle`) | All children at terminal status |
| 7 | Termination Enforcement | Inherited (`orch-termination`) | Cycle COMPLETE or HELD |

Skills 3, 5, 6, 7 are shared — identical across all orchestrator instances. Skills 1, 2, 4 are customized per instance in the orchestrator's AGENT.md Steps.

### Trigger Command Convention

Orchestrators fire triggers via command files at `.claude/commands/[child-agent]/[trigger-type].md`. Each file contains:
- **Instruction**: What the child agent should do
- **Context Payload**: `$PAYLOAD` injected at runtime (user request + classification context)
- **Expected Output**: Artifact type, path, and format
- **Execution**: Order position and predecessor dependencies

### Completion Signal Convention

Child agents signal completion in two ways, prioritized to minimize token consumption:

**1. Session Signal (primary)**: When running in the same session, the orchestrator captures the **System Relay** table emitted by the child's `express-relay` (Phase 5) directly from session output — zero MARK file reads needed for status determination.

**2. MARK Fallback (cross-session only)**: When resuming an interrupted cycle from a prior session, the orchestrator reads child Progress Summary MARKs:
- "Where We Stopped" → Completion status field
- "What Was Accomplished" → Deliverables produced

The child's `express-relay` System Relay table serves dual purpose: cross-agent handoff information (original) + orchestrator completion signal (zero-cost in-session monitoring).

### Blocker Routing

- **Domain blockers** (questions, missing context) → Route to `question-interviewer` via conditional trigger
- **Orchestration-level blockers** (child agent failure, missing prerequisites) → Route directly to user
- **Never hold a blocker silently** — surface immediately when detected

### Orchestrator → Child Agent Contract

1. Orchestrator classifies intent and fires the correct trigger command file
2. Child agent executes per the trigger's instructions, writes artifacts, updates its MARK files
3. Orchestrator captures session signal on child completion (or reads MARKs on cross-session resume)
4. Cycle completes only when all children confirm output, no blockers exist, and state is written

### Orchestration State MARK

Orchestrators use `{paths.marks}/{prefix}_Orchestration_State_MARK.md` instead of a Progress Summary MARK. This superset includes:
- Active Cycle (classification, triggered agents, timestamps)
- Child Agent Status table (agent, trigger, status, expected output, completion)
- Open Blockers
- Cycle History
- Token Usage
