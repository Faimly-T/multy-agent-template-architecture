# Skill: Requirements Analysis

## Purpose

Elicit, validate, and refine requirements through structured techniques. Use this skill when you need to discover what stakeholders actually need, uncover hidden assumptions, and produce clear, testable requirements.

## When to Use

- Starting a new project or feature
- Requirements are vague or conflicting
- Stakeholders disagree on what's needed
- Validating existing requirements for completeness

## Techniques

### 1. Stakeholder Interview
- Ask open-ended questions first, then narrow down
- Probe for "why" behind every stated requirement
- Identify unstated assumptions
- Capture exact quotes for traceability

### 2. User Story Mapping
- Map user journey from left to right (activities → tasks → stories)
- Identify the walking skeleton (MVP path)
- Slice vertically by value, not horizontally by layer

### 3. Requirements Checklist
For each requirement, verify:
- [ ] **Specific** — Single, unambiguous interpretation
- [ ] **Measurable** — Has acceptance criteria
- [ ] **Achievable** — Technically feasible within constraints
- [ ] **Relevant** — Maps to a user need or business goal
- [ ] **Testable** — Can be validated objectively

### 4. Gap Analysis
Compare current state vs. desired state:
| Area | Current State | Desired State | Gap | Priority |
|---|---|---|---|---|

## Output Template

```markdown
## Requirement: [REQ-XXX] [Title]

**Source**: [Stakeholder/Document]
**Priority**: P0/P1/P2/P3
**Status**: Draft / Reviewed / Approved

### Description
[Clear, concise description]

### User Story
As a [role], I want [capability] so that [benefit].

### Acceptance Criteria
- Given [context], when [action], then [outcome]

### Assumptions
- 

### Dependencies
- 

### Open Questions
- 
```
