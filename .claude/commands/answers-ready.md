# Answers Ready

Notify agents that their open questions have been answered by the Question Interviewer.

## Trigger

Emitted automatically by the Question Interviewer agent (project-manager) after completing an interview session (Step 6). Can also be run manually.

## Workflow

For each agent named in: $ARGUMENTS

1. Read the specified agent's `AGENT.md`.
2. Read their updated `*_Questions_Log_MARK.md` — focus on the Resolved Questions section for entries with `Source = "PjM Interview"`.
3. Re-run the agent's Phase 1 (`rehydrate-context`) to pick up the new answers.
4. The agent should incorporate resolved answers into its next session's work — reviewing each answer and taking the appropriate domain action.

## Instructions

- Pass agent names (space-separated) as arguments.
- Example: `/answers-ready ux-persona architect`
- If no arguments provided, check ALL agents' Question Log MARKs for recently resolved questions (Source = "PjM Interview").
- Each agent re-reads its own MARK files and resumes its workflow, incorporating answers into domain decisions.

## Expected Agent Behavior After Notification

Each notified agent should:
1. Re-enter Phase 1 (`rehydrate-context`) — new answers appear as RESOLVED in their Questions Log MARK
2. Adjust prior work or produce new artifacts based on the answers
3. Log any follow-up questions that the answers may have generated
4. Complete the full CODE 5-Phase Relay with the updated context
