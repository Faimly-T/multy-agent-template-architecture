# Skill: Risk Assessment

## Purpose

Systematically identify, analyze, and plan mitigations for project and technical risks. Use this skill to proactively manage uncertainty and prevent surprises.

## When to Use

- Project kickoff and planning
- Before major technical decisions
- When scope or timeline changes
- Periodic risk review sessions
- When something "feels wrong" but isn't yet defined

## Techniques

### 1. Risk Identification

**Categories to scan:**
- **Technical** — New technology, integration complexity, performance unknowns
- **Schedule** — Unrealistic deadlines, dependency delays, resource availability
- **Scope** — Requirements creep, ambiguous requirements, stakeholder changes
- **Resource** — Skill gaps, team availability, budget constraints
- **External** — Vendor dependencies, regulatory changes, market shifts

**Brainstorming prompts:**
- "What could prevent us from delivering on time?"
- "What assumptions are we making that could be wrong?"
- "What happened on similar projects that went wrong?"
- "What keeps you up at night about this project?"

### 2. Risk Analysis

**Probability × Impact Matrix:**

| | Low Impact | Medium Impact | High Impact |
|---|---|---|---|
| **High Prob** | Medium | High | Critical |
| **Medium Prob** | Low | Medium | High |
| **Low Prob** | Low | Low | Medium |

### 3. Response Strategies

| Strategy | When to Use | Example |
|---|---|---|
| **Avoid** | Can eliminate the cause | Change approach entirely |
| **Mitigate** | Can reduce probability or impact | Add buffer, create prototype |
| **Transfer** | Can shift to another party | Insurance, contract clause |
| **Accept** | Low impact or low probability | Monitor and have contingency |

## Output Template

```markdown
## Risk: [RISK-XXX] [Title]

**Category**: Technical / Schedule / Scope / Resource / External
**Probability**: High / Medium / Low
**Impact**: High / Medium / Low
**Rating**: Critical / High / Medium / Low
**Owner**: [Name/Role]

### Description
[What could happen and why]

### Trigger Indicators
- [How we'll know this risk is materializing]

### Mitigation Plan
1. [Action to reduce probability or impact]

### Contingency Plan
[What we'll do if the risk materializes despite mitigation]

### Status
- [Date] - [Update]
```
