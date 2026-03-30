# Skill: Knowledge Capture

## Purpose

Capture, structure, and preserve decisions, insights, and lessons learned so that organizational knowledge is retained and accessible. Use this skill to ensure nothing important is lost.

## When to Use

- After any significant decision is made
- At the end of a planning session or sprint
- When resolving a difficult problem (capture the solution)
- During retrospectives
- When team members transition or leave
- Whenever someone says "we should remember this"

## Techniques

### 1. Decision Capture (ADR Light)
Quick-capture format for decisions:

```markdown
## Decision: [Title]
**Date**: YYYY-MM-DD | **Decided by**: [Role/Name]

**Context**: [1-2 sentences on why this came up]
**Decision**: [What was decided]
**Rationale**: [Why this option over alternatives]
**Impact**: [What changes as a result]
```

### 2. Lesson Learned
```markdown
## Lesson: [Title]
**Date**: YYYY-MM-DD | **Category**: Process / Technical / Communication

**What happened**: [Brief description of the situation]
**What we learned**: [The insight or takeaway]
**What we'll do differently**: [Concrete action for next time]
```

### 3. Meeting Knowledge Extract
After any meeting, capture:
- **Key decisions** made (with rationale)
- **Action items** (owner + deadline)
- **Open questions** that need follow-up
- **Insights** or new information surfaced

### 4. Knowledge Base Organization

```
outputagent/
  decisions/          # Architecture & product decisions (ADR format)
    INDEX.md          # Master list of all decisions
    ADR-001-*.md      # Individual decision records
  lessons/            # Lessons learned
    INDEX.md
  meetings/           # Meeting notes and action items
  plans/              # Project and sprint plans
  architecture/       # Architecture documents and diagrams
  stories/            # User stories
  backlog/            # Backlog snapshots
  risks/              # Risk assessments
  status/             # Status reports
  vision/             # Product vision documents
```

### 5. Capture Triggers

Set up automatic capture at these events:
| Event | What to Capture | Where |
|---|---|---|
| Decision made | ADR | `outputagent/decisions/` |
| Sprint completed | Retrospective + lessons | `outputagent/lessons/` |
| Meeting held | Notes + actions | `outputagent/meetings/` |
| Requirement changed | Change record + rationale | `outputagent/decisions/` |
| Risk identified | Risk entry | `outputagent/risks/` |
| Architecture changed | Updated diagram + ADR | `outputagent/architecture/` |

## Quality Checklist

For every captured artifact:
- [ ] **Dated** — When was this created/decided?
- [ ] **Attributed** — Who made this decision or learned this?
- [ ] **Contextualized** — Why did this come up?
- [ ] **Actionable** — What should someone do with this knowledge?
- [ ] **Findable** — Is it indexed and in the right location?
