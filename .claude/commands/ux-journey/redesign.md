# Trigger: Scope Change (Redesign) → UX Journey Agent

## Instruction
Execute the full CODE 5-Phase Relay (Steps 1–5) with a redesign lens. Prior journey maps exist but the scope has changed significantly. Personas should have already been redesigned by the predecessor in the chain.

1. Read the `ux-journey` agent definition (resolve from `.claude/settings.json`).
2. Load the agent's role file — adopt Identity, Mandate, and Facts & Directives.
3. **Validate** that redesigned persona cards exist in `outputs/personas`. If missing → HALT.
4. Execute Step 1 (Rehydrate) — read existing MARK files. Note: scope has changed. The session objective should reflect the redesign.
5. Execute Steps 2–5. Load existing journey maps from `outputs/journeys` as a **baseline reference** — assess which journeys survive the scope change, which need redesign, and which are new. Map against the redesigned personas.
6. Write redesigned Journey Maps to the agent's configured output folder. Use versioned filenames if preserving originals is warranted.
7. Update MARK files.

## Context Payload
$PAYLOAD
<!-- Injected at runtime by the orchestrator: full user request + "Classification: Type 3 — Scope Change (Redesign)" + description of what changed -->

## Expected Output
- **Artifact**: Redesigned Journey Maps (may differ in count from originals)
- **Path**: `outputs/journeys` (from `agents.ux-journey.output` in settings.json)
- **Format**: Per `agents.ux-journey.template` in settings.json

## Execution
- **Order**: sequential — position 2 of 2 in chain (ux-persona → ux-journey)
- **Depends on**: ux-persona (must have COMPLETED redesigned persona cards)
- **Signals completion via**: `{paths.marks}/JRN_Progress_Summary_MARK.md`
