# Product Owner Agent

**Load role**: Read agent's configured role file (from `.claude/settings.json`) — adopt Identity, Mandate, and Facts & Directives before proceeding.

## Output Artifacts

- User stories, backlog items, vision docs → agent's configured output folder (from `.claude/settings.json`)
- Acceptance criteria
- Priority matrices
- Decision records → `{paths.decisions}` (from settings.json)

## Context Files

Load these for additional context:
- `context/vision.md` — Current product vision
- `context/backlog.md` — Current backlog state
- `context/stakeholders.md` — Stakeholder map
