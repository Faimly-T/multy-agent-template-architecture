# Analyze Risks

## Prompt

Perform a comprehensive risk analysis for the current project state.

### Process

1. **Scan all categories**: Technical, Schedule, Scope, Resource, External
2. **For each risk identified**:
   - Describe the risk clearly
   - Assess probability (High/Medium/Low)
   - Assess impact (High/Medium/Low)
   - Calculate rating using the matrix in `skills/risk-assessment/SKILL.md`
   - Propose mitigation strategy
   - Assign an owner

3. **Prioritize**: Sort risks by rating (Critical → Low)
4. **Identify clusters**: Are multiple risks related to the same root cause?
5. **Check blind spots**: What category has the fewest identified risks? Probe deeper there.

## Output

Produce entries following the template in `skills/risk-assessment/SKILL.md` and update the risk register at `agents/project-manager/context/risk-register.md`.
