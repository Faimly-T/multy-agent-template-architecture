---
name: six-thinking-hats
description: Apply Edward de Bono's Six Thinking Hats to systematically identify user insights from multiple cognitive perspectives.
---

# Six Thinking Hats — UX Island Identification

## Hat Perspectives

**White Hat — Facts & Data**
Focus on known user information: demographics, measured behaviours, usage patterns, stated needs.
Ask: What do we actually know? What data are we missing?

**Red Hat — Emotions & Intuition**
Surface emotional responses, feelings, and subjective user experiences without needing justification.
Ask: How do users feel? What emotional needs are unmet? What causes frustration or delight?

**Black Hat — Caution & Critical Judgment**
Identify pain points, barriers, risks, and failure modes from the user perspective.
Ask: Where do users struggle? What blocks goal completion? What causes abandonment?

**Yellow Hat — Optimism & Benefits**
Explore user goals, desired positive outcomes, and the value users expect to receive.
Ask: What do users want to achieve? What would make them successful? What delights them?

**Green Hat — Creativity & Alternatives**
Discover unconventional user types, emerging behaviours, and edge cases not yet considered.
Ask: Who might use this unexpectedly? What non-obvious patterns exist? What creative workarounds appear?

**Blue Hat — Process & Structure**
Identify overarching user journey patterns, system interactions, and structural user needs.
Ask: How does this fit into users' broader workflow? What systemic patterns shape the experience?

## Island Generation Rules

1. Generate one island per distinct user insight — do not combine multiple insights into one island.
2. Map each island to its primary Hat perspective in the `source` field: `six-hats:{hat-color}`.
3. Maximum 10 islands per analysis pass — quality over quantity.
4. Islands must be specific and actionable — avoid vague categories like "users want simplicity."
5. Do not duplicate insights already present in the existing island list.
6. Assign IDs sequentially: ISL-001 through ISL-010.
