# CODE Multi-Agent Planning System V2

## Architecture Reference Document

> A reusable, open-architecture framework for building multi-agent AI planning teams using Claude Code. Designed for knowledge capture, UX research, product planning, and any domain requiring structured collaborative intelligence.

---

## Table of Contents

1. [Overview](#overview)
2. [Theoretical Foundation](#theoretical-foundation)
3. [System Architecture](#system-architecture)
4. [The CODE 5-Phase Execution Relay](#the-code-5-phase-execution-relay)
5. [Agents](#agents)
6. [Skills](#skills)
7. [Persistence Model (MARK Files)](#persistence-model-mark-files)
8. [Agent Chaining & Cross-Agent Communication](#agent-chaining--cross-agent-communication)
9. [Workspace Structure](#workspace-structure)
10. [Templates & Extensibility](#templates--extensibility)
11. [Slash Commands](#slash-commands)
12. [Output Taxonomy](#output-taxonomy)
13. [Design Principles](#design-principles)
14. [How to Reuse This Framework](#how-to-reuse-this-framework)
15. [Token Budget Strategy](#token-budget-strategy)
16. [Glossary](#glossary)

---

## Overview

This system implements a **multi-agent planning team** where specialized AI agents collaborate on knowledge-intensive tasks. Each agent brings domain expertise (UX research, product ownership, architecture, etc.) while sharing a common execution engine -- the CODE 5-Phase Relay.

**Key insight**: Agents define *what* to do (domain lens, quality criteria); Skills define *how* to do it (execution mechanics, error handling). This separation makes agents lightweight and skills reusable across any domain.

### What problems it solves

- **Context loss across sessions**: MARK files persist state between conversations
- **Unfocused AI generation**: The Divergent-to-Convergent boundary forces creative exploration before structured decision-making
- **Agent sprawl**: A single execution relay (5 skills) powers unlimited domain agents
- **Handoff gaps**: System Relay protocol ensures clean cross-agent data transfer
- **Token waste**: Lazy-loaded skills, 60-line agent caps, and per-session tracking keep costs controlled

---

## Theoretical Foundation

The system adapts **Tiago Forte's CODE methodology** (from *Building a Second Brain*) for AI agent workflows:

| Forte's CODE | Agent Relay Adaptation | Thinking Mode |
|--------------|----------------------|---------------|
| **C**apture | Phase 2: Autonomous Capture | DIVERGENT |
| **O**rganize | Phase 3: Strategic Organize | CONVERGENT |
| **D**istill | Phase 4: Expert Distill | CONVERGENT |
| **E**xpress | Phase 5: Express Relay | CONVERGENT |

**Addition**: Phase 1 (Re-Hydrate) was added to handle cross-session continuity, since AI agents lack persistent memory by default.

The **critical boundary** is between Phase 2 and Phase 3: the system shifts from divergent thinking (generate freely, no judging) to convergent thinking (structure, sequence, decide). This prevents premature filtering of ideas while ensuring rigorous execution.

---

## System Architecture

```
                    ┌──────────────────────────────────────────────┐
                    │              CLAUDE.md                        │
                    │         (Framework Reference)                │
                    └──────────────┬───────────────────────────────┘
                                   │
          ┌────────────────────────┼────────────────────────┐
          │                        │                        │
          ▼                        ▼                        ▼
   ┌─────────────┐        ┌──────────────┐        ┌──────────────┐
   │   Agents     │        │    Skills     │        │  Persistence │
   │  (Domain)    │        │ (Mechanics)   │        │ (MARK Files) │
   ├─────────────┤        ├──────────────┤        ├──────────────┤
   │ UX Persona  │──uses──│ rehydrate    │──r/w──▶│ Progress     │
   │ UX Journey  │──uses──│ capture      │        │ Summary MARK │
   │ Quest Intrvw│──uses──│ organize     │        │ Questions    │
   │ Prod Owner* │        │ distill      │        │ Log MARK     │
   │ Architect*  │        │ relay        │        └──────────────┘
   └──────┬──────┘        └──────────────┘                │
          │                                                │
          │           ┌─────────────────┐                  │
          └──────────▶│  outputagent/    │◀────────────────┘
                      │  (All Artifacts) │
                      └─────────────────┘

   * = Legacy agents (not yet CODE-upgraded): Prod Owner, Architect
```

### Three-Layer Architecture

| Layer | Purpose | Components |
|-------|---------|-----------|
| **Agent Layer** | Domain expertise, quality criteria, chaining logic | AGENT.md files (one per domain) |
| **Skill Layer** | Execution mechanics, error handling, I/O contracts | SKILL.md files (shared across agents) |
| **Persistence Layer** | Cross-session continuity, cross-agent communication | MARK files, outputagent/ artifacts |

---

## The CODE 5-Phase Execution Relay

Every CODE-upgraded agent executes these 5 phases in strict order. No phase is skipped or reordered.

```
 Phase 1          Phase 2           Phase 3           Phase 4          Phase 5
┌──────────┐    ┌──────────────┐   ┌──────────────┐  ┌─────────────┐ ┌──────────────┐
│RE-HYDRATE│───▶│   CAPTURE    │──▶│   ORGANIZE   │─▶│   DISTILL   │▶│    RELAY     │
│          │    │              │   │              │  │             │ │              │
│ Read     │    │ Generate     │   │ Deduplicate  │  │ Process     │ │ Write MARKs  │
│ MARKs    │    │ Island       │   │ Cluster      │  │ each island │ │ Write        │
│ Set      │    │ Backlog      │   │ Sequence     │  │ into Result │ │ artifacts    │
│ Objective│    │ (unfiltered) │   │ Triage       │  │ or Concern  │ │ Emit System  │
│ Confirm  │    │              │   │ questions    │  │             │ │ Relay        │
└──────────┘    └──────────────┘   └──────────────┘  └─────────────┘ └──────────────┘
   ENTRY         DIVERGENT ──────── CONVERGENT ─────── CONVERGENT ──── EXIT
                       ▲                 ▲
                       └─── CRITICAL ────┘
                           BOUNDARY
```

### Phase Details

#### Phase 1: Re-Hydrate (`rehydrate-context`)
- **Input**: MARK files from previous session + user input
- **Process**: Read Progress Summary, read Questions Log, parse user intent, staleness check (>3 days warning), triage open questions, synthesize Session Objective
- **Session Objective formula**: `verb + deliverable + success condition + stakes`
- **Output**: Confirmed Session Objective
- **Gate**: User must confirm before proceeding
- **Error case**: Missing MARKs = treat as Initial Session (cold start)

#### Phase 2: Autonomous Capture (`autonomous-capture`)
- **Input**: Confirmed Session Objective + agent's capture lens
- **Process**: Two passes -- (1) Objective Decomposition (sub-deliverables, decisions, missing info, risks, dependencies), (2) Edge Exploration (adjacent concerns, stakeholder challenges, unvalidated assumptions)
- **Output**: Island Backlog -- a flat table of numbered items (ISL-001, ISL-002, ...) typed as WORK, DECISION, QUESTION, RISK, DEPENDENCY, or PREREQUISITE
- **Gate**: Minimum island count defined by agent (e.g., >=3 user-type islands for UX Persona)
- **Divergent rules**: No killing ideas, no sequencing, no estimating, no grouping, no polishing -- quantity over quality

#### Phase 3: Strategic Organize (`strategic-organize`)
- **Input**: Island Backlog + agent's organize lens
- **Process**: Deduplicate, cluster into execution groups, sequence (prerequisites first, then dependency order), triage questions (answerable now / BLOCKED-USER / BLOCKED-AGENT / DEFERRED)
- **Output**: Execution Roadmap with sequenced groups, blocked items, and parking lot
- **Gate**: All islands accounted for, no circular dependencies, questions triaged
- **Convergent rules**: Every island classified, duplicates merged, dependencies explicit, drops justified

#### Phase 4: Expert Distill (`expert-distill`)
- **Input**: Execution Roadmap + agent's distill lens + output templates
- **Process**: Resolve blocks first, process queue top-to-bottom with type-specific actions (PREREQUISITE=review, DECISION=options+recommend, WORK=produce artifact, QUESTION=record answer, RISK=assess+mitigate, DEPENDENCY=check status)
- **Output**: Results Ledger + domain artifacts (e.g., persona cards, journey maps, ADRs)
- **Gate**: All items resolved as RESULT or CONCERN
- **Quality**: Concrete, Attributable, Actionable, Scannable, Scoped, Honest
- **Progressive Summarization**: Bold key passages, highlight critical takeaways

#### Phase 5: Express Relay (`express-relay`)
- **Input**: All phase outputs + session state
- **Process**: Overwrite Progress Summary MARK, update Questions Log MARK, emit System Relay (target agent + signal + action needed), emit Efficiency Scorecard
- **Output**: Updated MARK files + written artifacts + System Relay message
- **MARK contract**: Session N reads MARKs from Session N-1, writes MARKs for Session N+1
- **Invariant**: MARKs are ALWAYS updated -- even for interrupted sessions

---

## Agents

### Agent Anatomy

Every CODE-upgraded agent is defined in a single `AGENT.md` file (target: <=60 lines) with:

```yaml
---
name: agent-name
description: One-line purpose
tools: [editFiles, createFile]
---
```

Followed by:
- **Identity**: Persona name, MBTI type, role title, characteristics
- **Authority/Boundary**: What they own vs. what they explicitly do NOT own
- **Mandate**: One sentence describing their transformation (input -> output)
- **Steps**: 5-6 numbered steps, each invoking a skill via trigger phrase + adding domain-specific lens and gate criteria
- **Persistence**: Table mapping MARK files and context files

### Active Agents (CODE-upgraded)

| Agent | Persona | Domain | Input | Output | Writes To |
|-------|---------|--------|-------|--------|-----------|
| **UX Persona** | Clara Mendes (ENFJ-A) | Personas, segments, JTBD, empathy maps | Product description | Persona Cards | `outputagent/personas/` |
| **UX Journey** | Tomas Herrera (INTJ-A) | Journey maps, touchpoints, emotional arcs | Validated Personas | Journey Maps | `outputagent/journeys/` |
| **Question Interviewer (PjM)** | Rafael Mori (ENFJ-T) | Question consolidation, user interviews, answer distribution | Agent Question Log MARKs | Interview Transcripts | `outputagent/meetings/` |

**Execution order**: UX Persona -> UX Journey (chained via Step 6). Question Interviewer is triggered on-demand via `/collect-questions` when agents have open questions.

### Legacy Agents (not yet CODE-upgraded)

| Agent | Domain | Skills Used | Output |
|-------|--------|-------------|--------|
| **Product Owner** | Vision, backlog, requirements, prioritization | requirements-analysis, backlog-management | User stories, backlog, vision docs |
| **Architect** | System design, components, trade-offs, ADRs | architecture-design, knowledge-capture | Architecture docs, ADRs, component specs |

Legacy agents use traditional role-based prompting without the 5-Phase Relay. They interact through the `team-planning` slash command.

### Agent Step Pattern (template for all CODE agents)

```markdown
### Steps
1. **Define objective for agent** with `rehydrate-context` -> [domain input]. Gate: ...
2. **Generate unfiltered Island Backlog** with `autonomous-capture` -- hunt for: [targets]. Gate: ...
3. **Map, group, and sequence the Island Backlog** with `strategic-organize` -- [clustering]. Gate: ...
4. **Distill each island into a concrete result** with `expert-distill` -- produce [artifacts]. Gate: ...
5. **Update MARK files and emit** relay with `express-relay` -- write to outputagent/. Gate: ...
6. (Optional) **Chain to next agent** -- invoke [next-agent] passing [data folder]. Gate: ...
```

Each step inlines its **domain lens** (what to look for), **gate criteria** (must pass to proceed), and **domain-specific instructions** (quality bar, templates to use).

### Trigger Phrase Contract

The bold phrase in each agent step (e.g., "Generate unfiltered Island Backlog") matches the `description` field in the corresponding skill's YAML frontmatter. This creates a stable contract:

- **Agent step** says *what* to do + adds domain context
- **Skill** defines *how* to do it (mechanics, error handling, output format)

Skills are **lazy-loaded** -- agents read each skill file ONLY when entering that phase, not upfront.

---

## Skills

### CODE Skills (5-Phase Relay)

| Skill | Phase | Mode | Purpose | Key Output |
|-------|-------|------|---------|------------|
| `rehydrate-context` | 1 | Entry | Reconstruct session, set objective | Session Objective |
| `autonomous-capture` | 2 | Divergent | Generate unfiltered ideas | Island Backlog |
| `strategic-organize` | 3 | Convergent | Structure and sequence | Execution Roadmap |
| `expert-distill` | 4 | Convergent | Produce concrete results | Results Ledger + Artifacts |
| `express-relay` | 5 | Exit | Persist state, hand off | Updated MARKs + System Relay |

### Legacy Skills (standalone)

| Skill | Used By | Purpose |
|-------|---------|---------|
| `requirements-analysis` | Product Owner | Elicit, validate, refine requirements (stakeholder interviews, user story mapping, SMART criteria, gap analysis) |
| `backlog-management` | Product Owner, PjM | Create, prioritize, groom backlogs (MoSCoW, WSJF, story splitting, Definition of Ready) |
| `risk-assessment` | Project Manager | Identify, analyze, mitigate risks (5 categories, Probability x Impact matrix, response strategies) |
| `architecture-design` | Architect | Design systems (C4 Model, Quality Attribute Workshops, trade-off analysis, ADR format, Mermaid diagrams) |
| `knowledge-capture` | All agents | Capture decisions, lessons, meeting notes (ADR Light, lesson-learned templates, knowledge base taxonomy) |

### Skill Anatomy

Each skill is defined in a `SKILL.md` with YAML frontmatter:

```yaml
---
name: skill-name
description: One sentence (this is the trigger phrase agents use)
---
```

CODE skills follow a structured contract format:
- **Pre-conditions**: What must be true before execution
- **Execution Steps**: Numbered sequence with specific file references
- **Output Specification**: Table of artifacts with format and destination path
- **Termination Condition**: How the agent knows the skill is complete
- **Post-conditions**: Guarantees for the rest of the system
- **Error Handling**: Table of error scenarios with prescribed agent responses

---

## Persistence Model (MARK Files)

### What are MARK files?

MARK files are the **single source of truth** for cross-session continuity. Each agent has two MARK files in its `context/` folder:

| File | Purpose | Updated When |
|------|---------|-------------|
| `[PREFIX]_Progress_Summary_MARK.md` | Accomplishments, open threads, momentum, token usage, artifacts modified | Every session end (Phase 5) |
| `[PREFIX]_Questions_Log_MARK.md` | Open/resolved/obsolete questions | Every session end (Phase 5) |

### MARK File Lifecycle

```
Session N-1                  Session N                    Session N+1
    │                           │                            │
    │  Phase 5: Write MARKs     │  Phase 1: Read MARKs       │
    │  ─────────────────▶       │  ─────────────────▶         │
    │                           │                            │
    │                           │  Phase 5: Write MARKs      │
    │                           │  ─────────────────▶        │
```

### MARK file invariants

1. **Always updated** at session end -- even for interrupted sessions
2. **Never deleted** -- questions are reclassified (OPEN -> RESOLVED -> OBSOLETE), never removed
3. **Single source of truth** -- no other file stores session state
4. **Append-safe** -- later sessions add to accumulated knowledge

### Progress Summary MARK structure

```markdown
## Last Checkpoint
- Date: [timestamp]
- Session Focus: [what was attempted]

## Deliverables Produced
| Artifact | Status | Path |

## Decisions Made
| Decision | Rationale | Impact |

## Where We Stopped
[Exact point of interruption]

## Open Threads
- [Unfinished work items]

## Momentum Direction
[Assessment: accelerating/stable/blocked + recommended next focus]

## Token Usage
| Metric | Value |
| Input tokens | ... |
| Output tokens | ... |
| Total tokens | ... |
| Est. % of 5h Pro budget | ... |
```

### Questions Log MARK structure

```markdown
## Active Questions (OPEN)
| # | Question | Context | Priority |

## Resolved Questions
| # | Question | Answer | Resolved Date |

## Obsolete Questions
| # | Question | Why Obsolete |
```

---

## Agent Chaining & Cross-Agent Communication

### Chaining mechanism

Agents can invoke the next agent in a pipeline via an optional Step 6. The chain passes a data folder path:

```
UX Persona (Step 6) ──passes outputagent/personas/──▶ UX Journey (Step 1)
```

UX Journey's Step 1 **validates** that personas exist before proceeding. If `outputagent/personas/` is empty, it halts with an error.

### System Relay (cross-agent handoff)

Phase 5 (`express-relay`) emits a **System Relay** message containing:

| Field | Purpose |
|-------|---------|
| Target Agent | Which agent should act next |
| Signal | What happened (e.g., "3 persona cards ready") |
| Action Needed | What the next agent should do |

### Handoff Protocol (from shared protocols)

Every cross-agent handoff follows **S.H.A.N.** format:
- **S**ummarize: What was accomplished
- **H**ighlight: Critical items the next agent must know
- **A**rtifacts: Files produced with paths
- **N**ext Steps: Recommended actions

### Conflict Resolution

When agents disagree, authority is resolved by domain ownership:

| Domain | Authority |
|--------|-----------|
| Persona definitions | UX Persona |
| Journey maps | UX Journey |
| Question consolidation & interviews | Question Interviewer (PjM) |
| Scope and priority | Product Owner |
| Technical approach | Architect |

All conflict resolutions are logged as ADRs in `outputagent/decisions/`.

### Cross-Agent Data Bridges

Output templates include **"Signals for Other Agents"** sections that extract agent-specific insights:

```markdown
## Signals for Other Agents
- **Product Owner**: [priority signals, feature implications]
- **Architect**: [technical requirements, integration needs]
- **Project Manager**: [effort signals, risk indicators]
```

This creates structured data bridges between the CODE-upgraded UX agents and the legacy planning agents.

### Question Collection Flow

The Question Interviewer (PjM) provides a cross-cutting question resolution service:

```
Any Agent (Phase 5) ──writes OPEN questions to own MARK──▶ session completes

User runs: /collect-questions agent-x agent-y

Question Interviewer (PjM) activates:
  Phase 1 → validates agents completed, reads their Question Log MARKs
  Phase 2 → builds Island Backlog from all OPEN questions
  Phase 3 → deduplicates, groups by theme, Hard blockers first
  Phase 4 → interviews user, writes answers back to each agent's MARK
  Phase 5 → writes transcript to outputagent/meetings/
  Step 6  → emits /answers-ready agent-x agent-y

User runs: /answers-ready agent-x agent-y
  → Each agent re-reads its MARK, finds resolved questions, takes action
```

---

## Workspace Structure

```
project-root/
│
├── CLAUDE.md                              # Framework reference (this is the "OS")
├── README.md                              # This document
│
├── .claude/
│   ├── settings.json                      # Agent & skill registry
│   ├── settings.local.json                # Local permission overrides
│   │
│   ├── commands/                          # Slash commands (user-invocable)
│   │   ├── team-planning.md               # Multi-agent planning orchestration
│   │   ├── capture-decision.md            # ADR capture utility
│   │   ├── collect-questions.md           # Trigger question interview
│   │   ├── answers-ready.md               # Notify agents answers are ready
│   │   ├── activate-po.md                 # Activate Product Owner
│   │   ├── activate-pjm.md               # Activate Question Interviewer
│   │   └── activate-architect.md          # Activate Architect
│   │
│   ├── agents/
│   │   ├── ux-persona/                    # CODE-upgraded
│   │   │   ├── AGENT.md                   # Agent definition (<=60 lines)
│   │   │   └── context/
│   │   │       ├── UX_Progress_Summary_MARK.md
│   │   │       ├── UX_Questions_Log_MARK.md
│   │   │       ├── persona-card-template.md    # Output template
│   │   │       └── persona-patterns.md         # Archetype reference
│   │   │
│   │   ├── ux-journey/                    # CODE-upgraded
│   │   │   ├── AGENT.md
│   │   │   └── context/
│   │   │       ├── JRN_Progress_Summary_MARK.md
│   │   │       ├── JRN_Questions_Log_MARK.md
│   │   │       └── journey-map-template.md
│   │   │
│   │   ├── product-owner/                 # Legacy
│   │   │   ├── AGENT.md
│   │   │   └── context/ (vision.md, backlog.md, stakeholders.md)
│   │   │
│   │   ├── project-manager/               # CODE-upgraded
│   │   │   ├── AGENT.md                   # Agent OS (6 steps — Question Interviewer)
│   │   │   └── context/
│   │   │       ├── PJM_Progress_Summary_MARK.md
│   │   │       ├── PJM_Questions_Log_MARK.md
│   │   │       └── question-interview-template.md
│   │   │
│   │   ├── architect/                     # Legacy
│   │   │   ├── AGENT.md
│   │   │   └── context/ (architecture.md, constraints.md, tech-stack.md)
│   │   │
│   │   ├── _shared/
│   │   │   ├── protocols.md               # Handoffs, conflicts, quality standards
│   │   │   └── glossary.md                # Shared terminology
│   │   │
│   │   └── _templates/
│   │       └── AGENT-TEMPLATE.md          # Blueprint for new agents
│   │
│   └── skills/
│       ├── rehydrate-context/
│       │   ├── SKILL.md
│       │   └── templates/                 # MARK file + objective templates
│       ├── autonomous-capture/SKILL.md
│       ├── strategic-organize/SKILL.md
│       ├── expert-distill/SKILL.md
│       ├── express-relay/SKILL.md
│       ├── requirements-analysis/SKILL.md     # Legacy
│       ├── backlog-management/SKILL.md        # Legacy
│       ├── risk-assessment/SKILL.md           # Legacy
│       ├── architecture-design/SKILL.md       # Legacy
│       ├── knowledge-capture/SKILL.md         # Legacy
│       └── _templates/
│           └── SKILL-TEMPLATE.md          # Blueprint for new skills
│
└── outputagent/                           # All generated artifacts
    ├── personas/                          # Persona cards
    ├── journeys/                          # Journey maps
    ├── decisions/                         # ADRs
    ├── architecture/                      # Architecture docs
    ├── plans/                             # Project plans
    ├── risks/                             # Risk registers
    ├── status/                            # Status reports
    ├── meetings/                          # Meeting notes
    └── lessons/                           # Lessons learned
```

---

## Templates & Extensibility

### Creating a New Agent

1. Copy `.claude/agents/_templates/AGENT-TEMPLATE.md`
2. Fill in Identity (persona name, MBTI, role), Authority/Boundary, Mandate
3. Write 5 Steps using trigger phrases that match skill descriptions
4. Add domain-specific lens and gate criteria to each step
5. Create `context/` folder with seeded MARK files (use templates from `rehydrate-context/templates/`)
6. Create output template(s) for Phase 4 artifacts
7. Register in `.claude/settings.json`
8. Target: <=60 lines for AGENT.md

### Creating a New Skill

1. Copy `.claude/skills/_templates/SKILL-TEMPLATE.md`
2. Set YAML frontmatter `name` (format: `[prefix]-[verb]-[object]`) and `description` (trigger phrase)
3. Define pre-conditions, execution steps, output specification
4. Define termination condition, post-conditions, error handling
5. Register in `.claude/settings.json`

### Token Budget Rules for Agent Files

- AGENT.md must be <=60 lines
- Externalize output templates to `context/` folder
- Skills are lazy-loaded (read at phase entry, not upfront)
- Trigger phrases must match skill descriptions exactly
- No SRL (Semantic Role Labeling) tables inside skills -- that complexity lives in the skill template only

---

## Slash Commands

| Command | Purpose | Agents Involved |
|---------|---------|-----------------|
| `/team-planning [topic]` | Full collaborative planning session | PO -> Architect -> PjM (synthesize) |
| `/collect-questions [agents]` | Consolidate cross-agent questions and interview user | Question Interviewer (PjM) |
| `/answers-ready [agents]` | Notify agents that answers are in their MARK files | All receiving agents |
| `/capture-decision [decision]` | Record an ADR | Any (uses knowledge-capture skill) |
| `/activate-po` | Activate Product Owner persona | Product Owner |
| `/activate-pjm` | Activate Question Interviewer persona | Question Interviewer (PjM) |
| `/activate-architect` | Activate Architect persona | Architect |

---

## Output Taxonomy

All agent output is written to `outputagent/` with a consistent folder structure:

| Folder | Contents | Written By |
|--------|----------|-----------|
| `personas/` | Persona Cards (behavioral profiles, JTBD, empathy data) | UX Persona |
| `journeys/` | Journey Maps (stages, touchpoints, emotional arcs) | UX Journey |
| `decisions/` | ADRs (Architecture Decision Records) | Any agent |
| `architecture/` | System diagrams, component specs, interface contracts | Architect |
| `plans/` | Project plans, milestones, timelines | Project Manager |
| `risks/` | Risk registers, mitigation strategies | Project Manager |
| `status/` | Status reports, progress updates | Project Manager |
| `meetings/` | Meeting notes, knowledge extracts, interview transcripts | Question Interviewer (PjM), any agent |
| `lessons/` | Lessons learned, retrospective insights | Any agent |

All artifacts follow **Progressive Summarization**: bold key passages, highlight critical takeaways, scannable in 30 seconds.

---

## Design Principles

### 1. Separation of Domain and Mechanics
Agents own *what*; skills own *how*. An agent file should read like a job description. A skill file should read like a procedure manual. This separation means adding a new domain requires only a new agent -- no skill changes.

### 2. Divergent-then-Convergent Thinking
Phase 2 is intentionally unfiltered. No idea is killed, no prioritization, no estimation. Phase 3+ enforces structure. This mirrors how effective human teams brainstorm (expansive) then plan (reductive).

### 3. Lazy Loading
Skills are read only when needed. An agent entering Phase 3 loads `strategic-organize/SKILL.md` at that moment -- not at session start. This keeps the active context window small and focused.

### 4. Single Source of Truth
MARK files are THE place for session state. Output templates are THE place for artifact format. `settings.json` is THE place for agent/skill registration. No duplication of authority.

### 5. Graceful Degradation
Missing MARKs = Initial Session (not an error). Interrupted session = partial MARK write. Failed MARK write = display content for manual copy. The system never silently loses data.

### 6. Token Consciousness
Every session tracks input/output tokens and estimates budget consumption. Agent files are capped at 60 lines. Templates are externalized. This is critical for sustainable use of paid API access.

### 7. Progressive Summarization
Borrowed from Forte's original methodology: every artifact is written to be scannable. Bold the key insights. Highlight the critical takeaways. Future agents (and humans) should understand an artifact in 30 seconds.

### 8. Authority Boundaries
Each agent has explicit Authority (what they own) and Boundary (what they do NOT own). Conflicts are resolved by domain ownership, not by hierarchy. All resolutions are logged as ADRs.

---

## How to Reuse This Framework

### For a different domain (e.g., data engineering, marketing, legal review)

1. **Keep the 5 CODE skills unchanged** -- they are domain-agnostic
2. **Create new agents** using `AGENT-TEMPLATE.md`:
   - Define a persona with domain expertise
   - Set authority boundaries
   - Write 5 Steps with domain-specific lenses and gates
   - Create output templates for your domain (data schemas, campaign briefs, contract reviews, etc.)
3. **Define your output taxonomy** -- create folders under `outputagent/` for your domain artifacts
4. **Set up chaining** if agents have pipeline dependencies (like Persona -> Journey)

### For a different AI platform

The architecture is platform-agnostic in concept. To port:
- **Agents** = system prompts with role definitions
- **Skills** = reusable prompt templates with pre/post-conditions
- **MARK files** = any persistent key-value store or file system
- **Trigger phrases** = function/tool dispatch mechanism
- **outputagent/** = any shared artifact store

The key patterns to preserve:
1. The 5-phase sequential relay
2. The divergent-to-convergent boundary
3. MARK-based session continuity
4. Lazy skill loading
5. Agent-to-agent chaining with validation gates

### Minimum viable implementation

At minimum, you need:
- 1 agent AGENT.md with 5 steps
- 5 skill SKILL.md files (rehydrate, capture, organize, distill, relay)
- 2 MARK files (Progress Summary, Questions Log)
- 1 output folder
- 1 settings registry

---

## Token Budget Strategy

| Target | Value |
|--------|-------|
| Per agent run | ~10% of Claude Pro 5-hour window |
| AGENT.md size | <=60 lines |
| Skill loading | Lazy (one at a time, at phase entry) |
| Context files | Externalized templates, not inlined |

Each Progress Summary MARK includes a Token Usage table:

```
| Metric              | Value   |
|---------------------|---------|
| Input tokens        | 45,000  |
| Output tokens       | 12,000  |
| Total tokens        | 57,000  |
| Est. % of 5h budget | 8.5%   |
```

This enables multi-agent sessions where 5+ agents can run sequentially within a single Claude Pro billing window.

---

## Glossary

| Term | Definition |
|------|-----------|
| **CODE** | Capture, Organize, Distill, Express -- the 4-phase methodology (5 with Re-Hydrate) |
| **Island** | A single captured idea/item in the Island Backlog (ISL-001, ISL-002...) |
| **Island Backlog** | The unfiltered list of islands produced during Phase 2 (Capture) |
| **Execution Roadmap** | The sequenced, grouped plan produced during Phase 3 (Organize) |
| **Results Ledger** | The tracking table of processed islands from Phase 4 (Distill) |
| **MARK file** | Persistent session state file (Progress Summary or Questions Log) |
| **System Relay** | Cross-agent handoff message emitted during Phase 5 |
| **Trigger Phrase** | The bold text in an agent step that matches a skill's description |
| **Domain Lens** | Agent-specific criteria for what to look for in each phase |
| **Gate** | Minimum quality bar that must pass before proceeding to the next phase |
| **Progressive Summarization** | Writing technique: bold key insights, highlight critical takeaways |
| **ADR** | Architecture Decision Record -- structured decision documentation |
| **JTBD** | Jobs-To-Be-Done -- user research framework ("When X, I want Y, so I can Z") |
| **S.H.A.N.** | Handoff format: Summarize, Highlight, Artifacts, Next Steps |
| **SRL** | Semantic Role Labeling -- used in skill templates to define who/what/whom |
| **Cold Start** | First session for an agent with no existing MARK files |

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| V2.1 | 2026-03-29 | Question Interviewer (PjM) CODE upgrade, `/collect-questions` and `/answers-ready` commands |
| V2.0 | 2026-03-28 | CODE 5-Phase Relay, UX Persona & Journey agents, shared skills architecture |
| V1.0 | Prior | Legacy 3-agent team (PO, PjM, Architect) with standalone skills |

---

*Generated from the CODE Multi-Agent Planning System V2 codebase. For the authoritative framework reference, see `CLAUDE.md`.*
