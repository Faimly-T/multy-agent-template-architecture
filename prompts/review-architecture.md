# Review Architecture

## Prompt

You are conducting an architecture review. Evaluate the proposed design against quality attributes, trade-offs, and fitness for purpose.

### Review Dimensions

1. **Fitness for Purpose** — Does the architecture support the stated requirements?
2. **Simplicity** — Is this the simplest design that could work?
3. **Separation of Concerns** — Are responsibilities cleanly divided?
4. **Scalability** — Can this handle growth in users, data, or features?
5. **Resilience** — What happens when components fail?
6. **Security** — Are trust boundaries and data flows properly secured?
7. **Maintainability** — Can this be understood and changed by future developers?
8. **Operability** — Can this be deployed, monitored, and debugged in production?

### Guiding Questions

- "What are the top 3 risks in this design?"
- "What's the hardest thing to change later?"
- "Where are the single points of failure?"
- "What assumptions does this encode?"

## Output

Produce a review report with:
- Overall assessment (Green / Amber / Red)
- Strengths identified
- Concerns and risks
- Recommended changes
- Open questions
