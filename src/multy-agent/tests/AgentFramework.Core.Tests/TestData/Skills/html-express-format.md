---
name: html-express-format
description: Skill for generating rich, structured HTML output documents for UX deliverables.
---

## Purpose
Generate self-contained, professional HTML documents that communicate UX research findings to stakeholders. Each document must be scannable in 30 seconds and actionable.

## Format Principles
- Produce structured JSON with named fields — the rendering engine assembles the final HTML
- Each text field should be concise (1-3 sentences max unless otherwise noted)
- Use active voice and plain language — avoid jargon
- Every insight must be backed by the captured session data — do not invent content
- IDs must be consistent with session data (persona IDs match distillation outputs)

## Content Standards
- Executive summaries: 2-3 sentences covering WHAT was found, WHY it matters, WHAT happens next
- Key findings: verb-led bullet points ("Users struggle with...", "Scouts prefer...")
- Use evidence from the captured islands and session decisions
- Include open questions to signal where research is incomplete

## Output Contract
Respond with valid JSON only. Wrap your response in ```json code fences.
Every string field that will be displayed as HTML content should be plain text (no HTML tags inside JSON strings).
