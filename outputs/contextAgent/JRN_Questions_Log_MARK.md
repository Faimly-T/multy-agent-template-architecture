# UX Journey Architect — Questions Log MARK
**Agent**: ux-journey-architect (Tomás Herrera)
**Prefix**: JRN

---

## Active (OPEN)
_None — all questions resolved in PjM Interview Session #1 (2026-03-31)._

## Resolved
| ID | Question | Answer | Source | Resolved Date |
|----|----------|--------|--------|---------------|
| JRN-Q001 | What is the latency between Jaylen's engagement event and Ivan's Scout Sheet update? Real-time streaming or daily batch? | **Daily batch.** Scout Sheet refreshes once per day. Ivan works with previous day's data. Affects urgency framing and notification design in journey Stage 4. | PjM Interview | 2026-03-31 |
| JRN-Q002 | Is there a content moderation / safety check step between AI Tag Review and Content Push? Or is tag approval the only gate? | **Automated AI safety filter** runs after Ivan's tag approval, before content goes live. Platform philosophy: maximize AI leverage at every process step. New step added to journey flow between "Tag Approval" and "Content Push." | PjM Interview | 2026-03-31 |

## Inherited from UX Persona (cross-agent visibility) — Resolved
| ID | Question | Answer | Source | Resolved Date |
|----|----------|--------|--------|---------------|
| UX-Q001 | Does a student-athlete ever see their own profile/Reality Match? Affects Stage 6 of Jaylen's journey (conditional). | **Partial/Curated Visibility.** Stage 6 is now defined: the 70% AI Confidence Score unlock event triggers the Match Preview + parent CTA. Two data views: Light (student Discovery Dashboard) / Full (coach backend). | PjM Interview | 2026-03-31 |
| UX-Q006 | COPPA/FERPA compliance for behavioral profiling of minors | **Verified parental consent required** before any behavioral data is collected for under-18 users. Consent gate before Stage 1 profiling for minors. | PjM Interview | 2026-03-31 |
| UX-Q007 | Responsible engagement guardrails for dopamine-first design with underage users | **Minimal guardrails.** All engagement mechanics apply equally to all users. Note: tension with UX-Q006 flagged for PO. | PjM Interview | 2026-03-31 |

## Obsolete
_None._
