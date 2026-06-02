---
name: research-validation-guide
description: Skill for generating deep-research prompts and validation frameworks to test buyer personas against real-world data.
---

## Purpose
Equip the human researcher with everything they need to validate or invalidate the AI-generated personas using top LLM deep-research tools (ChatGPT Deep Research, Claude, Perplexity Pro, Gemini Deep Research).

## Research Prompt Design Principles
A good research prompt for LLM deep research must:
1. **State the question clearly** — "Find evidence for/against the existence of [persona type] in [market]"
2. **Specify the evidence type** — market reports, surveys, academic studies, news, forum posts
3. **Set scope boundaries** — geography, time frame, industry segment
4. **List what to look for** — specific data points, statistics, qualitative signals
5. **Ask for counter-evidence** — explicitly request disconfirming data

## Validation Point Design
Each validation point should:
- Be falsifiable (can be confirmed OR denied by research)
- Map to a specific persona assumption (goal, pain, behavior, or demographics)
- Include why it matters to product decisions

## Post-Research Questions
After reviewing the research, the human should be able to answer:
- Is this user type large enough to build for?
- Are their stated pains real or just assumed?
- Are there existing solutions addressing this need?
- What would make them switch to a new product?

## Output Format
Respond with valid JSON only (```json code fences).
Include one entry per persona plus a general market research section.
