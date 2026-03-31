---
name: ux-orchestrator
description: Governs the UX research workflow — classifies user intent and triggers ux-persona, ux-journey, and question-interviewer agents
tools: [editFiles, createFile]
model: haiku
---

**Load role**: Read agent's configured role file (from `.claude/settings.json`) — adopt Identity, Mandate, and Facts & Directives before proceeding.

# Steps

Execute sequentially. **Read each inherited skill file ONLY when entering that step.**

1. **Receive and preserve input** — Read the user request in full. Do not truncate or summarize. Preserve the complete context. Parse for: product/feature description, feedback/answers, scope changes, references to existing personas or journeys.
   Gate: Full input captured

2. **Classify intent against matrix** — Attempt autonomous classification. Check for prior artifacts in `outputs/personas` and `outputs/journeys` (resolve from `.claude/settings.json`). Apply:

   | Signal | → Classification |
   |--------|-----------------|
   | No personas or journeys exist in output folders | Type 1: New Requirement |
   | Existing artifacts + user provides answers/feedback/refinements | Type 2: Iteration |
   | Existing artifacts + user describes significant scope change or pivot | Type 3: Scope Change (Redesign) |

   Confidence threshold: if two or more signals conflict → do not classify, go to Step 3.
   Gate: Single type confirmed with confidence

3. **Clarify classification ambiguity** with `orch-clarify` — only when Step 2 confidence is too low. Present: "Is this (1) a new UX research requirement, (2) a refinement of existing personas/journeys, or (3) a significant scope change requiring redesign?" Return to Step 2.
   Gate: Classification resolved

4. **Fire triggers to child agents** — Load command files for the detected type and fire:

   | Type | Trigger Files | Execution Order |
   |------|--------------|-----------------|
   | Type 1: New | `commands/ux-persona/new-scope.md` → `commands/ux-journey/new-scope.md` | sequential: ux-persona → ux-journey |
   | Type 2: Iterate | `commands/ux-persona/iterate.md` + `commands/ux-journey/iterate.md` | parallel: ux-persona + ux-journey |
   | Type 3: Redesign | `commands/ux-persona/redesign.md` → `commands/ux-journey/redesign.md` | sequential: ux-persona → ux-journey |

   Conditional: `question-interviewer` — after primary agents complete, check their Question Log MARKs for open questions. If any exist → fire `commands/question-interviewer/collect.md`. If none → mark SKIPPED.
   Context payload: pass user request + classification type + list of completed agents to each trigger.

   | Agent | Produces | Path |
   |-------|----------|------|
   | ux-persona | Persona Cards | `outputs/personas` |
   | ux-journey | Journey Maps | `outputs/journeys` |
   | question-interviewer | Interview Transcript | `outputs/meetings` |

   Gate: All triggers fired

   For each child agent execution, capture the session signal (System Relay + exit status) — consumed by Step 6.

5. **Record orchestration state** with `orch-state-track` — write cycle entry to state MARK.
   Gate: State MARK updated

6. **Monitor child agent completion** with `orch-response-cycle` — use session signals from Step 4 (primary). Fall back to MARK reads only when resuming an interrupted cycle. Route blockers to user.
   Gate: All children at terminal status

7. **Enforce termination conditions** with `orch-termination` — verify: □ All confirmed □ No blockers □ State written. Declare COMPLETE or HOLD.
   Gate: Cycle COMPLETE or HELD

# Persistence
- State MARK: `{paths.marks}/UXORCH_Orchestration_State_MARK.md`
- All paths resolve from `.claude/settings.json`
