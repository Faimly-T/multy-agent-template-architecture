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
8. All resolutions are logged as decisions in `outputagent/decisions/`

### File I/O Permissions

Agents and skills have **full read/write access** to `outputagent/` and agent `context/` folders without requiring user confirmation. Create files and directories as needed during execution.

### Quality Standards

- All outputs must be actionable and specific
- Avoid vague language — quantify where possible
- Reference source context when making claims
- Use consistent terminology (see `glossary.md`)
- Date all artifacts
- Apply Progressive Summarization to all artifacts (bold key passages, highlight critical takeaways)

### Knowledge Capture Rule

Every significant discussion, decision, or insight must be captured:
- Decisions → `outputagent/decisions/` (ADR format)
- Lessons learned → `outputagent/lessons/`
- Meeting outcomes → `outputagent/meetings/`
- Session state → MARK files (updated every Phase 5)
