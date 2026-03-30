# Questions Log MARK — UX Journey Architect

## Active Questions

| ID | Date Asked | Question | Context | Directed To | Blocker Level | Status |
|---|---|---|---|---|---|---|
| JQ-001 | 2026-03-29 | Does COPPA/LGPD require a parental consent stage before Gabriel can use the Home Viewer? | Gabriel's journey assumes zero-friction — a consent step would fundamentally alter Stage 2. | Legal review | HARD | OPEN |
| JQ-002 | 2026-03-29 | What is the minimum number of confirmed hypotheses required for a profile to appear on Ivan's Scout Sheet? | Journey assumes ≥3. This threshold directly affects the Gabriel-to-Ivan handoff timing. | Product team / Profiling AI OS design | SOFT | OPEN |
| JQ-003 | 2026-03-29 | Is the Home Viewer app-only or does it have a web version in Stage 1? | Affects Gabriel's discovery channel (deep-link vs. app store) and Luciana's ability to preview content. | Product team | SOFT | OPEN |
| JQ-004 | 2026-03-29 | What is the hypothesis taxonomy for Stage 1? How many hypotheses, what categories? | Priya's journey depends on taxonomy size for override workflow design. Ivan's Scout Sheet depth depends on taxonomy granularity. | Product team | HARD | OPEN |
| JQ-005 | 2026-03-29 | Does Gabriel ever see his own profile or scholarship matches explicitly in Stage 1, or is it fully invisible until Ivan outreach? | Affects whether Gabriel's journey has a self-service consideration stage or is entirely mediated by Ivan. | Product team | SOFT | DEFERRED (Stage 2) |

---

## Resolved Questions

| ID | Question | Resolution | Resolved Date | Source |
|---|---|---|---|---|
| — | Does the UX Persona agent have validated personas for all Catch MVP users? | Yes — 4 personas produced (Gabriel, Ivan, Priya, Luciana) + Persona Map + Anti-Personas. All validated. | 2026-03-29 | UX Persona Session #1 MARK |

---

## Inherited from UX Persona (Cross-Reference)

| Original ID | Question | Status in Journey Context |
|---|---|---|
| Q-001 | Primary target market geography? | Assumed: LATAM + West Africa. Carried into journey stage design (WhatsApp as primary outreach channel, localization needs). |
| Q-002 | Users under 13? | Escalated to JQ-001 (COPPA/LGPD consent). |
| Q-003 | Hypothesis taxonomy? | Escalated to JQ-004. |
| Q-004 | Who is Ivan — one person or role? | Journey designed for Ivan as a role type (any scout). Works for both single-user and multi-user. |
| Q-005 | Home Viewer registration required? | Assumed: no registration. Escalated to JQ-003 (app vs. web). |
| Q-006 | Content volume target? | Not blocking for journey design. Relevant for Priya's sprint load validation (PjM concern). |

---

## Obsolete Questions

| ID | Question | Reason Obsolete | Date Marked |
|---|---|---|---|

---

*Log updated: 2026-03-29 (Session #1)*
