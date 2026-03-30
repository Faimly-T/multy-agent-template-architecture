# Agent Workflow: CODE Multi-Agent Planning System V2

## What This Is

A multi-agent planning team for knowledge capture, powered by the **CODE 5-Phase Execution Relay** — adapted from Tiago Forte's Capture → Organize → Distill → Express methodology for AI agent workflows.

Each agent follows the same 5-step relay, powered by 5 shared skills. Agents add domain expertise; skills provide the execution mechanics.

---

## CODE 5-Phase Relay

Every agent session runs these 5 phases in order. No phase is skipped or reordered.

| Step | Skill | Mode | What Happens |
|------|-------|------|--------------|
| 1. Re-Hydrate | `rehydrate-context` | Entry | Read MARK files → set Session Objective → user confirms |
| 2. Capture | `autonomous-capture` | DIVERGENT | Generate unfiltered Island Backlog (no judging, quantity over quality) |
| 3. Organize | `strategic-organize` | CONVERGENT | Deduplicate → cluster → sequence → Execution Roadmap |
| 4. Distill | `expert-distill` | CONVERGENT | Process each island → concrete Result or formal Concern |
| 5. Relay | `express-relay` | Exit | Update MARK files (incl. token usage) → write artifacts → emit System Relay |

**Critical boundary**: Phase 2→3 = DIVERGENT switches to CONVERGENT. Capture freely, then structure.

---

## Path Resolution

All paths are centralized in `.claude/settings.json`. No hardcoded paths in agents, skills, or commands.

| Variable | Resolves To | Source |
|----------|-------------|--------|
| `{paths.outputs}` | `outputs` | `settings.json → paths.outputs` |
| `{paths.marks}` | `outputs/contextAgent` | `settings.json → paths.marks` |
| `{paths.decisions}` | `outputs/decisions` | `settings.json → paths.decisions` |
| `{agent.prefix}` | Agent's MARK prefix (UX, JRN…) | `settings.json → agents.[name].prefix` |
| `{agent.output}` | Agent's artifact folder | `settings.json → agents.[name].output` |
| `{agent.template}` | Agent's output template | `settings.json → agents.[name].template` |
| `{agent.role}` | Agent's ROLE.md file | `settings.json → agents.[name].role` |

**MARK files**: `{paths.marks}/{agent.prefix}_Progress_Summary_MARK.md`
**Artifacts**: `{agent.output}/{filename}.md`
**Decisions**: `{paths.decisions}/ADR-XXX-{title}.md`
**Roles**: `{agent.role}` (Identity + Mandate + Facts & Directives)

---

## Agents

### Active (CODE-upgraded)

| Agent | File | Domain | Writes To |
|-------|------|--------|-----------|
| **UX Persona** | `.claude/agents/ux-persona/AGENT.md` | Personas, segments, JTBD, empathy maps | `{agent.output}` |
| **UX Journey** | `.claude/agents/ux-journey/AGENT.md` | Journey maps, touchpoints, emotional arcs | `{agent.output}` |
| **Question Interviewer (PjM)** | `.claude/agents/question-interviewer/AGENT.md` | Question consolidation, user interviews, answer distribution | `{agent.output}` |

**Execution order**: UX Persona → UX Journey (Persona Step 6 chains to Journey automatically). Journey Step 1 validates that personas exist before proceeding. Question Interviewer is triggered on-demand via `/collect-questions` when agents have open questions.

### Legacy (not yet CODE-upgraded)

| Agent | File | Domain |
|-------|------|--------|
| Product Owner | `.claude/agents/product-owner/AGENT.md` | Vision, backlog, requirements |
| Architect | `.claude/agents/architect/AGENT.md` | Architecture, ADRs, interfaces |

---

## Skills (shared by all agents)

Skills are lazy-loaded — agents read each skill file ONLY when entering that step.

| Skill | File | Trigger Phrase (in agent Steps) |
|-------|------|---------------------------------|
| `rehydrate-context` | `.claude/skills/rehydrate-context/SKILL.md` | "Define objective for agent" |
| `autonomous-capture` | `.claude/skills/autonomous-capture/SKILL.md` | "Generate unfiltered Island Backlog" |
| `strategic-organize` | `.claude/skills/strategic-organize/SKILL.md` | "Map, group, and sequence the Island Backlog" |
| `expert-distill` | `.claude/skills/expert-distill/SKILL.md` | "Distill each island into a concrete result" |
| `express-relay` | `.claude/skills/express-relay/SKILL.md` | "Update MARK files and emit" |

### How trigger phrases work

Each agent Step uses a **bold phrase** that matches the skill's `description` field. This creates a contract:
- The agent step says **what** to do + adds domain-specific context
- The skill defines **how** to do it (mechanics, error handling, rules)

---

## Agent Step Pattern

Every CODE-upgraded agent follows this structure in `AGENT.md`:

```
**Load role**: Read agent's configured role file (from .claude/settings.json)

### Steps
1. **Define objective for agent** with `rehydrate-context` → [domain input]. Gate: ...
2. **Generate unfiltered Island Backlog** with `autonomous-capture` — hunt for: [domain targets]. Gate: ...
3. **Map, group, and sequence the Island Backlog** with `strategic-organize` — [clustering]. Gate: ...
4. **Distill each island into a concrete result** with `expert-distill` — produce [artifacts]. Gate: ...
5. **Update MARK files and emit** relay with `express-relay` — write to agent's configured output folder. Gate: ...
6. (Optional) **Chain to next agent** — invoke [next-agent] passing agent output folder. Gate: ...
```

Identity, Mandate, and Facts & Directives live in the agent's `ROLE.md` file (loaded at session start). Each step inlines its domain lens — no separate sections needed.

---

## Persistence

### MARK Files (session continuity)
Each agent's MARK files live in `{paths.marks}` — path configured in `.claude/settings.json`:
- `{prefix}_Progress_Summary_MARK.md` — accomplishments, open threads, momentum, **token usage**
- `{prefix}_Questions_Log_MARK.md` — open/resolved/obsolete questions

Written at every session end (Step 5). Read at every session start (Step 1).

### Token Monitoring
Every Progress Summary MARK includes a Token Usage table:
```
| Metric | Value |
|--------|-------|
| Input tokens | ... |
| Output tokens | ... |
| Total tokens | ... |
| Est. % of 5h Pro budget | ... |
```

---

## File I/O

- Agents have `tools: [editFiles, createFile]` in YAML frontmatter
- Full read/write access to `{paths.outputs}` and agent `context/` folders — no user confirmation needed
- All generated artifacts → `{agent.output}/`
- Decision records → `{paths.decisions}/ADR-XXX-[title].md`

---

## Workspace Structure

```
CLAUDE.md                          # ← You are here. Framework reference.
.claude/
  settings.json                    # Central config (paths, agents, skills, authority)
  commands/                        # Slash commands
    ux-research-cycle.md           # Full UX cycle: Personas → Journeys → Interview → Iterate
    team-planning.md               # Multi-agent planning session
    capture-decision.md            # ADR capture
    collect-questions.md           # Trigger question interview
    answers-ready.md               # Notify agents answers are ready
  agents/
    ux-persona/                    # CODE-upgraded
      AGENT.md                     # Agent OS (6 steps)
      context/
        ROLE.md                    # Identity + Mandate + Facts & Directives
        persona-card-template.md   # Output template
        persona-patterns.md        # Archetype reference
    ux-journey/                    # CODE-upgraded
      AGENT.md                     # Agent OS (5 steps)
      context/
        ROLE.md                    # Identity + Mandate + Facts & Directives
        journey-map-template.md    # Output template
    product-owner/                 # Legacy
      AGENT.md
      context/
        ROLE.md                    # Identity + Mandate + Facts & Directives
    question-interviewer/          # CODE-upgraded
      AGENT.md                     # Agent OS (6 steps — Question Interviewer)
      context/
        ROLE.md                    # Identity + Mandate + Facts & Directives
        question-interview-template.md  # Output template
    architect/                     # Legacy
      AGENT.md
      context/
        ROLE.md                    # Identity + Mandate + Facts & Directives
    _shared/
      protocols.md                 # Shared protocols (handoffs, conflict, quality)
    _templates/
      AGENT-TEMPLATE.md            # Template for new CODE agents
  skills/
    rehydrate-context/SKILL.md     # Step 1: Session boot
    autonomous-capture/SKILL.md    # Step 2: Divergent capture
    strategic-organize/SKILL.md    # Step 3: Convergent organizing
    expert-distill/SKILL.md        # Step 4: Produce results
    express-relay/SKILL.md         # Step 5: Persist + relay
    _templates/                    # Skill scaffolding
    requirements-analysis/         # Legacy skill
    backlog-management/            # Legacy skill
    risk-assessment/               # Legacy skill
    architecture-design/           # Legacy skill
    knowledge-capture/             # Legacy skill
outputs/                           # All generated artifacts ({paths.outputs})
  personas/                        # Persona cards ({agent.output} for ux-persona)
  journeys/                        # Journey maps ({agent.output} for ux-journey)
  decisions/                       # ADRs ({paths.decisions})
  architecture/                    # Architecture docs
  plans/                           # Project plans
  risks/                           # Risk registers
  status/                          # Status reports
  meetings/                        # Meeting notes
  lessons/                         # Lessons learned
  contextAgent/                    # MARK files ({paths.marks})
    {prefix}_Progress_Summary_MARK.md
    {prefix}_Questions_Log_MARK.md
```

---

## How to Use

### Run an agent
Invoke by name in VS Code Copilot Chat. The agent reads its AGENT.md and follows the Steps.

### Build a new agent
1. Copy `.claude/agents/_templates/AGENT-TEMPLATE.md`
2. Create `context/ROLE.md` with Identity table, Mandate, and Facts & Directives
3. Fill in 5 Steps with domain lenses and gates in AGENT.md
4. Create `context/` output template if needed
5. Register in `.claude/settings.json` (file, prefix, output, template, role)
6. Target: ≤60 lines for AGENT.md

### Token budget
Target ~5% of Claude Pro 5-hour window per agent run. Token usage is tracked in each agent's Progress Summary MARK.

---

## Conventions

- Agent entry point: `AGENT.md` with YAML frontmatter (`name`, `description`, `tools`)
- Skill entry point: `SKILL.md` with YAML frontmatter (`name`, `description`)
- All decisions logged in `{paths.decisions}`
- Progressive Summarization on all artifacts (bold key passages, highlight critical takeaways)
- MARK files are the single source of truth for cross-session continuity
