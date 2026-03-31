# Trigger: Scope Change (Redesign) → UX Persona Agent

## Instruction
Execute the full CODE 5-Phase Relay (Steps 1–5) with a redesign lens. Prior persona cards exist but the scope has changed significantly. The agent should:

1. Read the `ux-persona` agent definition (resolve from `.claude/settings.json`).
2. Load the agent's role file — adopt Identity, Mandate, and Facts & Directives.
3. Execute Step 1 (Rehydrate) — read existing MARK files. Note: scope has changed. The session objective should reflect the redesign.
4. Execute Steps 2–5. Load existing persona cards from `outputs/personas` as a **baseline reference** — assess which personas survive the scope change, which need redesign, and which are new. Do not assume prior personas are still valid.
5. Write redesigned Persona Cards to the agent's configured output folder. Use versioned filenames if preserving originals is warranted.
6. Update MARK files.

## Context Payload
$PAYLOAD
<!-- Injected at runtime by the orchestrator: full user request + "Classification: Type 3 — Scope Change (Redesign)" + description of what changed -->

## Expected Output
- **Artifact**: Redesigned Persona Cards (may differ in count from originals)
- **Path**: `outputs/personas` (from `agents.ux-persona.output` in settings.json)
- **Format**: Per `agents.ux-persona.template` in settings.json

## Execution
- **Order**: sequential — position 1 of 2 in chain (ux-persona → ux-journey)
- **Depends on**: none
- **Signals completion via**: `{paths.marks}/UX_Progress_Summary_MARK.md`
