# Collect Questions

Consolidate open questions from agents and interview the user for answers.

## Trigger

Run this command when one or more agents have completed their sessions and have open questions (Active Questions in their Questions Log MARK).

## Workflow

1. Read the question-interviewer agent definition (resolve path from `.claude/settings.json`) and adopt the Question Interviewer role.
2. Apply all context from the agent's context folder and shared protocols from `agents/_shared/`.
3. Execute the full CODE 5-Phase Relay as defined in the agent's Steps.
4. The agent will read the named agents' Question Log MARKs, consolidate questions, interview the user, and write answers back.

## Instructions

For the agents specified in: $ARGUMENTS

- Pass the agent names (space-separated) to the Question Interviewer agent's Step 1.
- Valid agent names: `ux-persona`, `ux-journey`, `product-owner`, `architect` (any combination).
- If no arguments provided, scan ALL agents' Question Log MARKs for active questions.
- Example: `/collect-questions ux-persona ux-journey`

## Path Resolution

Resolve all agent MARK paths from `.claude/settings.json`:
- Progress MARK: `{paths.marks}/{agent.prefix}_Progress_Summary_MARK.md`
- Questions MARK: `{paths.marks}/{agent.prefix}_Questions_Log_MARK.md`
