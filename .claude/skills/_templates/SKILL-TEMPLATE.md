# Skill Template

Use this template to create new skills for the agent team.

## Structure

```
skills/
  <skill-name>/
    SKILL.md        # Main skill definition (required)
    examples/       # Example outputs (optional)
    templates/      # Reusable templates (optional)
```

---

## SKILL.md Template

Copy everything below this line into a new `SKILL.md` file and fill in each section.

---

```markdown
---
name: [agent-prefix]-[verb]-[object]
description: [one-sentence description of what the skill does]. with [What event, state, or command causes this skill to activate. Be specific — reference agent gates, user commands, or artifact states.]
---

# Skill: [Verb] [Object]
## Agent: [Agent Name]

---

### Semantic Role Labeling for Skill Definition

this section should achieve goal of deconstructing the objective of the skill to answer the most fundamental question: Who did what to whom, when, where, and why?

| Role | Value |
|------|-------|
| **[P] Predicate** | [The core verb — The predicate is the heart of the skill, the central action or state of being. Every other role is defined in relation to it.: Produce, Analyze, Validate, Capture, etc.] |
| **[A] Agent** | [Which agent executes this skill: PO Agent, PjM Agent, Architect Agent, or Any Agent] |
| **[Pt] Patient** | [What is being acted upon, The patient is the entity directly affected or acted upon by the verb. — the input material consumed by this skill] |
| **[R] Recipient** | [Who receives or benefits from the output — the primary consumer] |

and Arguments, These are additional roles that provide context but are not central to the core action. They answer questions such as when, where, why, or how, below some examples:

| **[Arg-TMP] Temporal** | [Specifies when the action occurred. In our example, …in the morning is the Temporal modifier] |
| **[Arg-LOC] Location** | [Specifies where the action occurred. For example, …in the conference room is the location modifier.] |
| **[Arg-MNR] Manner** | [Specifies how the action was performed. For example, …with great confidence is the manner modifier.] |

### Pre-conditions
[What must be true before this skill can execute. List each required artifact, state, or approval.]
- [Pre-condition 1]
- [Pre-condition 2]
- [Pre-condition 3]

### Execution Steps
[Numbered sequence of concrete actions. Each step should reference specific file paths and describe what to extract or produce.]
1. [Action 1 — e.g., Read `path/to/source.md` — extract [specific content].]
2. [Action 2 — e.g., Analyze [extracted data] against [criteria].]
3. [Action 3 — e.g., Produce output artifact with the following sections: ...]
4. [Action 4 — e.g., Write completed artifact to `path/to/output.md`.]

### Output Specification
| Output | Format | File Path |
|--------|--------|-----------|
| [Artifact name] | MD | [path/to/output.md] |

### Termination Condition
[How the agent knows this skill is complete. Reference the output artifact and any completeness criteria.]

### Post-conditions
[What is true after the skill completes successfully. List guarantees the rest of the system can rely on.]
- [Post-condition 1]
- [Post-condition 2]
- [Post-condition 3]

### Error Handling
| Condition | Response |
|-----------|----------|
| [Error scenario 1 — e.g., Required input artifact is missing a section] | [What the agent does — e.g., Note as "pending — [source] incomplete" in the relevant section. Surface to user.] |
| [Error scenario 2 — e.g., Conflicting data between sources] | [What the agent does — e.g., Note the conflict explicitly. Do not resolve silently.] |
| [Error scenario 3 — e.g., Pre-condition not met] | [What the agent does — e.g., Halt execution, log reason, return to orchestrator.] |
```

---

## Naming Convention

Skill filenames follow the pattern: `[agent-prefix]-[verb]-[object]`

| Agent | Prefix | Example |
|-------|--------|---------|
| Product Owner | `po` | `po-prioritize-backlog` |
| Project Manager | `pjm` | `pjm-produce-status-report` |
| Architect | `arch` | `arch-evaluate-trade-offs` |
| Any Agent (shared) | `shared` | `shared-rehydrate-context` |
