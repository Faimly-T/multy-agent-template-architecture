---
name: express-as-html
description: Produce a complete, self-contained HTML5 document. The LLM generates the entire HTML directly — no template engine or C# builder.
---

## Output Contract

Return a JSON object with a single `html` field:

```json
{ "html": "...complete HTML5 document as a string..." }
```

The value must be the **full, runnable document** — opening `<!DOCTYPE html>` through closing `</html>`. No fragments, no placeholders.

## Technical Requirements

- **Self-contained**: all CSS in `<style>` tags inside `<head>`. No `<link>` stylesheets, no CDN scripts, no external fonts.
- **Valid HTML5**: `<!DOCTYPE html>`, `<html lang="en">`, `<head>` with `<meta charset="UTF-8">` and viewport tag, `<body>`.
- **Semantic structure**: use `<main>`, `<section>`, `<article>`, `<header>`, `<footer>` where appropriate.
- **Responsive**: `max-width: 900px; margin: 0 auto; padding: 24px` on the container.

## Visual Standard — Dark Professional Theme

| Role | Value |
|---|---|
| Page background | `#0d1117` |
| Card / section background | `#161b22` |
| Primary text | `#e6edf3` |
| Muted / secondary text | `#8b949e` |
| Accent / interactive | `#58a6ff` |
| Positive / success / goals | `#3fb950` |
| Risk / pain / alert | `#f85149` |
| Warning / caution | `#d29922` |
| Borders | `#30363d` |

- Font: `-apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif`
- Body `line-height: 1.6`, `font-size: 16px`

## Content Fidelity Rule

Every data point must come from the session context provided in the message. Do **not** invent, hallucinate, or pad with generic text. When a field is absent, show a clear "—" or "not recorded" state rather than guessing.

## Scannability Rule

Each document must be scannable in 30 seconds: clear section headings, visual hierarchy, no walls of prose. Every section should end with a clear "so what" or next-step implication.
