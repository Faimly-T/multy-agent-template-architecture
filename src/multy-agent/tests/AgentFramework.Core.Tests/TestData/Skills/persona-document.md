---
name: persona-document
description: Skill for synthesising buyer persona cards from distilled research islands.
---

## Persona Card Standards

### Mandatory Fields (every persona must have all of these)
- **Name**: A realistic first name + archetype label (e.g. "Alex Chen — The Ambitious Recruit")
- **JTBD**: "When [situation], I want to [motivation], so I can [outcome]" — use exact format
- **Goals**: 3-5 concrete, observable objectives (what success looks like to them)
- **Pains**: 3-5 friction points — focus on emotional and functional obstacles
- **Behaviors**: 3-5 observable patterns (what they actually do, not what they say they do)
- **Usage Scenario**: One vivid, context-rich scene (device + location + emotional state + action)
- **Quotable Quote**: One authentic-sounding quote that captures their core frustration or desire

### Persona Differentiation Rule
Each persona must be distinct enough that it would produce DIFFERENT design decisions.
If two personas share all the same pain points, merge them.

### Anti-Persona Rule
Include at least one anti-persona — the user you are explicitly NOT designing for.
Anti-personas prevent scope creep and focus the product.

### Demographics Guidance
- Age range (not exact age): prefer ranges like "17-22" or "28-35"
- Location context: regional/type (e.g. "rural Midwest", "coastal metro") not exact city
- Education/role: current stage relevant to the product

## Output Format
Respond with valid JSON only (```json code fences).
Produce one entry per distinct user type found in the distillation groups.
