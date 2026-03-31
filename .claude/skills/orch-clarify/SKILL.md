---
name: orch-clarify
description: Clarify classification ambiguity. Activates when Intent Classification (Skill 2) produces confidence below threshold — two or more conflicting signals detected.
---

# Skill: Clarify Classification Ambiguity
## Agent: Any Orchestrator Agent

---

### Semantic Role Labeling for Skill Definition

| Role | Value |
|------|-------|
| **[P] Predicate** | Clarify |
| **[A] Agent** | Any Orchestrator Agent |
| **[Pt] Patient** | Ambiguous user input with conflicting intent signals |
| **[R] Recipient** | Skill 2 (Intent Classification) — receives clarified input for re-classification |
| **[Arg-TMP] Temporal** | After Skill 2 fails to reach confidence threshold |
| **[Arg-MNR] Manner** | Single multiple-choice question — never open-ended |

### Pre-conditions
- Skill 2 (Intent Classification) has executed and returned confidence below threshold
- At least two conflicting signals were detected in the input
- The orchestrator's classification matrix is loaded (from AGENT.md Step 2)

### Execution Steps

1. **Frame the clarification question** — Construct a single multiple-choice question using the orchestrator's classification types. Always present exactly the types defined in the instance's classification matrix.
   Default framing: _"Is this a new requirement, a refinement of existing work, or a significant scope change?"_
   Adapt labels to match the instance's type names if they differ from the default.

2. **Present to user** — Display the question with numbered options. Include a brief explanation of what each option means in context:
   ```
   I need to clarify your intent before proceeding:
   
   1. **[Type 1 label]** — [1-sentence description of what this triggers]
   2. **[Type 2 label]** — [1-sentence description of what this triggers]
   3. **[Type 3 label]** — [1-sentence description of what this triggers]
   
   Which best describes your request?
   ```

3. **Capture answer** — Record the user's selection. Map it to the corresponding classification type.

4. **Return to Skill 2** — Pass the clarified intent back to Intent Classification with the original input. The classification should now resolve to a single type with full confidence.

### Output Specification
| Output | Format | File Path |
|--------|--------|-----------|
| Clarified classification type | In-memory (passed to Skill 2) | N/A — no file output |

### Termination Condition
User has selected one of the presented options AND the selection maps to a single classification type with no remaining ambiguity.

### Post-conditions
- A single classification type is confirmed
- Confidence is restored — Skill 2 can proceed to Skill 4
- The user's original input is preserved in full (not modified by clarification)

### Error Handling
| Condition | Response |
|-----------|----------|
| User refuses to choose / provides ambiguous answer | Re-present the question once with additional context. If still unresolved → surface to user with full context and ask them to rephrase their original request. |
| User's answer doesn't map to any classification type | List the valid types again. Ask user to pick one. |
| Skill invoked without prior Skill 2 failure | Skip — classification already has confidence. Proceed to Skill 4. |
