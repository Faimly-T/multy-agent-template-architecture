# Skill: Backlog Management

## Purpose

Create, prioritize, organize, and groom a product backlog to maintain a healthy pipeline of well-defined work items. Use this skill to keep the team focused on the highest-value work.

## When to Use

- Creating initial backlog from requirements
- Sprint/iteration planning
- Backlog grooming and refinement sessions
- Reprioritization after scope changes

## Techniques

### 1. Prioritization Frameworks

**MoSCoW Method**
| Category | Meaning | Guideline |
|---|---|---|
| Must Have | Critical for launch | ~60% of effort |
| Should Have | Important but not critical | ~20% of effort |
| Could Have | Desirable if time permits | ~20% of effort |
| Won't Have | Explicitly excluded (this time) | 0% |

**WSJF (Weighted Shortest Job First)**
- Score = (Business Value + Time Criticality + Risk Reduction) / Job Size

### 2. Story Splitting Patterns
- **Workflow steps** — Split along the user's workflow
- **Business rule variations** — One rule per story
- **Data variations** — Simple data first, complex later
- **Interface variations** — Core UI first, polish later
- **Operations** — CRUD: Create first, then Read, Update, Delete

### 3. Definition of Ready
A backlog item is ready for work when:
- [ ] User story is clearly written
- [ ] Acceptance criteria are defined
- [ ] Dependencies are identified and resolved (or planned)
- [ ] Estimate is provided
- [ ] No open blockers

## Output Template

```markdown
## Backlog Item: [ID] [Title]

**Type**: Story / Task / Bug / Spike
**Priority**: P0 / P1 / P2 / P3
**Estimate**: [points or t-shirt size]
**Sprint**: [target sprint or Backlog]

### User Story
As a [role], I want [capability] so that [benefit].

### Acceptance Criteria
- [ ] Criterion 1
- [ ] Criterion 2

### Tasks
- [ ] Task 1
- [ ] Task 2

### Dependencies
- [ID] [Related item]

### Notes
- 
```
