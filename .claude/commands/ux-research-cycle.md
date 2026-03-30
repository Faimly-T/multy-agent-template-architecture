# UX Research Cycle

Run the full UX research workflow: Personas → Journeys → Question Interview → optional iteration.

## Input

Product or feature description: $ARGUMENTS

## Workflow

### Phase A — UX Persona Agent

1. Read the `ux-persona` agent definition (resolve from `.claude/settings.json`).
2. Load the agent's role file (`{agent.role}` from settings.json) — adopt Identity, Mandate, and Facts & Directives.
3. Execute the full CODE 5-Phase Relay (Steps 1–5) with the product description from `$ARGUMENTS` as input.
4. Write Persona Cards to the agent's configured output folder.
5. Update the agent's MARK files (`{paths.marks}/UX_Progress_Summary_MARK.md` and `UX_Questions_Log_MARK.md`).
6. **Do NOT chain to UX Journey automatically** — continue to Phase B below instead.

**Checkpoint**: Confirm Persona Cards are written. List the personas created and their output paths. If Phase A failed, STOP and report the error.

---

### Phase B — UX Journey Agent

1. Read the `ux-journey` agent definition (resolve from `.claude/settings.json`).
2. Load the agent's role file — adopt Identity, Mandate, and Facts & Directives.
3. Validate that persona cards exist in the ux-persona output folder (from settings.json). If missing → HALT.
4. Execute the full CODE 5-Phase Relay (Steps 1–5) using the persona cards as input.
5. Write Journey Maps to the agent's configured output folder.
6. Update the agent's MARK files (`{paths.marks}/JRN_Progress_Summary_MARK.md` and `JRN_Questions_Log_MARK.md`).

**Checkpoint**: Confirm Journey Maps are written. List the journeys created and their output paths. If Phase B failed, STOP and report the error.

---

### Phase C — Question Interview

1. Read **both** agents' Questions Log MARKs:
   - `{paths.marks}/UX_Questions_Log_MARK.md` (ux-persona)
   - `{paths.marks}/JRN_Questions_Log_MARK.md` (ux-journey)
2. Count the total Active (OPEN) questions across both agents.
3. **If zero open questions** → skip to Phase D directly, reporting: "No open questions from either agent."
4. **If open questions exist** → Read the `question-interviewer` agent definition (from settings.json). Load its role file. Execute the full CODE 5-Phase Relay with `ux-persona ux-journey` as arguments. Interview the user for answers. Write answers back to each originating agent's Questions Log MARK (Active → Resolved, source="PjM Interview"). Write transcript to the question-interviewer's output folder.

**Checkpoint**: Report how many questions were answered, how many deferred, and confirm MARK files updated.

---

### Phase D — Continue or Finish

Present the user with this summary and choice:

```
## UX Research Cycle — Summary

### Personas Created
[List each persona name + one-line description + path]

### Journey Maps Created
[List each journey name + one-line description + path]

### Questions Resolved
[Count answered / deferred / total]

---

**What would you like to do next?**

1. **Iterate** — Re-run the cycle incorporating the answers just provided. UX Persona and UX Journey agents will re-enter Phase 1, pick up resolved questions from their MARKs, and refine their artifacts.
2. **Finish** — End the UX research cycle. All artifacts and MARK files are saved. You can continue later with `/collect-questions` or `/answers-ready`, or proceed to `/team-planning` for PO + Architect perspectives.
```

**If the user chooses Iterate**: Go back to Phase A. The agents will re-read their MARK files (which now contain resolved questions) and produce refined artifacts. Repeat the full A → B → C → D cycle.

**If the user chooses Finish**: End the session. Confirm all artifact paths and suggest next steps.

## Path Resolution

All paths resolve from `.claude/settings.json`:
- Agent definitions: `agents.[name].file`
- Role files: `agents.[name].role`
- Output folders: `agents.[name].output`
- Templates: `agents.[name].template`
- MARK files: `{paths.marks}/{agent.prefix}_*_MARK.md`
