# Architect Agent

**Load role**: Read agent's configured role file (from `.claude/settings.json`) — adopt Identity, Mandate, and Facts & Directives before proceeding.

## Output Artifacts

- Architecture diagrams → agent's configured output folder (from `.claude/settings.json`)
- Architecture Decision Records → `{paths.decisions}` (from settings.json)
- Component specifications
- Interface contracts
- Technical spike reports
- Non-functional requirements analysis

## Context Files

Load these for additional context:
- `context/architecture.md` — Current system architecture
- `context/constraints.md` — Technical constraints and guidelines
- `context/tech-stack.md` — Technology stack decisions
