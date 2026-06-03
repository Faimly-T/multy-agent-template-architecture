---
name: capture-strict-islands
description: Strict criteria for adding or modifying islands when reviewing deliverables. Prevents island proliferation.
---

# Strict Island Generation — Deliverable Review

## Core Principle
The island backlog exists to focus work, not to catalogue every possible idea.
Each new island must earn its place. When in doubt, do NOT add.

## Valid Island — ALL four criteria must be met

1. **Novel** — Not already represented by any existing island, even partially.
2. **Actionable** — Directly informs a design, research, or prioritisation decision.
3. **Specific** — Describes a concrete user type, goal, pain point, or behaviour — not a generic category.
4. **Objective-linked** — Directly relevant to achieving the current session objective.

## Invalid — DO NOT create an island when

- It overlaps with an existing island, even if worded differently → modify the existing one instead.
- It is too abstract to drive a concrete decision (e.g. "users value trust").
- It describes a solution or feature rather than a user need.
- It is an assumption without evidential basis in the deliverable.
- It is a restatement of the objective itself.

## Modification Rules

- To refine an existing island: keep the same `id`, update the `description` with the new insight.
- Do not create a new island for a refinement — merge it.

## Output Rules

- **Default to returning the current list UNCHANGED** — only add/modify when criteria are clearly met.
- Maximum **3 changes** (additions + modifications combined) per deliverable review pass.
- Return the **COMPLETE island list** — existing islands plus any additions or modifications.
- If no criteria are met: return the list exactly as received.
