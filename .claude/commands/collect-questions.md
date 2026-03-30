# Collect Questions

Consolidate open questions from agents and interview the user for answers.

## Trigger

Run this command when one or more agents have completed their sessions and have open questions (Active Questions in their Questions Log MARK).

## Workflow

1. Read `agents/project-manager/AGENT.md` and adopt the Question Interviewer role.
2. Apply all context from `agents/question-interviewer/context/` and shared protocols from `agents/_shared/`.
3. Execute the full CODE 5-Phase Relay as defined in the agent's Steps.
4. The agent will read the named agents' Question Log MARKs, consolidate questions, interview the user, and write answers back.

## Instructions

For the agents specified in: $ARGUMENTS

- Pass the agent names (space-separated) to the Question Interviewer agent's Step 1.
- Valid agent names: `ux-persona`, `ux-journey`, `product-owner`, `architect` (any combination).
- If no arguments provided, scan ALL agents' Question Log MARKs for active questions.
- Example: `/collect-questions ux-persona ux-journey`

## Agent Name → MARK Path Mapping

| Agent Name | Progress Summary MARK | Questions Log MARK |
|------------|----------------------|-------------------|
| `ux-persona` | `agents/ux-persona/context/UX_Progress_Summary_MARK.md` | `agents/ux-persona/context/UX_Questions_Log_MARK.md` |
| `ux-journey` | `agents/ux-journey/context/JRN_Progress_Summary_MARK.md` | `agents/ux-journey/context/JRN_Questions_Log_MARK.md` |
| `product-owner` | `agents/product-owner/context/PO_Progress_Summary_MARK.md` | `agents/product-owner/context/PO_Questions_Log_MARK.md` |
| `architect` | `agents/architect/context/ARCH_Progress_Summary_MARK.md` | `agents/architect/context/ARCH_Questions_Log_MARK.md` |
