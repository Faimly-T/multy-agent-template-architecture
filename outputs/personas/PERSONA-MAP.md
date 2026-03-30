# Persona Map: Catch Football Scholarship Pathway MVP Stage 1 
**Created**: 2026-03-28 | **Session**: #1
**Product**: Catch MVP Stage 1 — AI-Driven Engagement Engine for Football Scholarship Profiling

---

## Persona Overview

| # | Name | Type | Product Surface | Core Job-To-Be-Done |
|---|------|------|-----------------|---------------------|
| PC-1 | Gabriel Ferreira | Primary | Home Viewer (Mobile) | Discover and be discovered for a US scholarship by consuming content I already love — without filling out a single form. |
| PC-2 | Ivan Calderon | Primary | Scout Sheet / Dashboard (Desktop) | Open a ranked list of high-confidence Reality Matches and make fast, credible outreach decisions. |
| PC-3 | Priya Nkosi | Secondary | CSI Dashboard (Desktop) | Process a weekly content batch efficiently with AI tagging that is accurate enough to approve quickly, not just override constantly. |
| PC-4 | Luciana Ferreira | Secondary | Trust/Landing Page (Mobile) | Verify this platform is safe and legitimate before allowing her minor child to continue using it. |

---

## Persona Relationships

```
                    ┌─────────────────────────────────┐
                    │        CATCH SYSTEM              │
                    │                                  │
  PRIYA (PC-3)      │  CSI ──► Profiling AI OS ──►    │      IVAN (PC-2)
  Tags content  ───►│  (tags)   (validates)   (ranks)  │◄── Reads Scout Sheet
                    │                                  │    Acts on leads
                    │         Home Viewer              │
  GABRIEL (PC-1)    │         (Reel Feed)              │
  Consumes reel ───►│  Generates engagement signals    │
                    │                                  │
                    └─────────────────────────────────┘
                              ▲
                              │ Trust gate
                    LUCIANA (PC-4)
                    Parent approval enables
                    Gabriel's continued access
```

---

## Primary Persona Designation

**PC-1: Gabriel Ferreira is the designated primary persona.**

Every Home Viewer design decision must answer the question: "Does this serve Gabriel?" If a feature would alienate, confuse, or lose Gabriel, it should not be built — regardless of other benefits.

**PC-2: Ivan Calderon is co-primary for the intelligence output layer.**

The Scout Sheet and Profiling AI OS output design must answer the question: "Does this help Ivan make a faster, more confident decision?" The entire backend pipeline exists to serve Ivan's ability to identify Reality Matches.

---

## Design Priority Hierarchy

1. Gabriel's zero-friction engagement (Home Viewer) — the source of all signal
2. Ivan's Scout Sheet clarity and actionability — the destination of all signal
3. Priya's CSI tagging accuracy — the quality gate for all signal
4. Luciana's trust threshold — the access gate to Gabriel

If these personas conflict (e.g., a transparency feature that satisfies Luciana but breaks Gabriel's implicit contract of invisible profiling), the resolution framework is:
- Serve Luciana's trust need through a **separate parent-facing surface** — not by changing the Home Viewer UX for Gabriel.

---

## Open Research Gaps (Assumptions Requiring Validation)

| Gap | Risk Level | Recommended Action |
|-----|------------|-------------------|
| Gabriel's daily app usage pattern (2-3h scroll assumption) | HIGH | Intercept interviews with 5-8 student-athletes in target markets (Brazil, Nigeria, Colombia, Mexico) |
| Gabriel's device profile (older Android, 4G assumption) | HIGH | Device analytics from comparable apps in target markets; consult App Annie / data.ai regional reports |
| Ivan's Scout Sheet workflow (batch review, 90-second scan assumption) | HIGH | 2-3 structured interviews with Ivan before Scout Sheet UI design begins |
| Priya's sprint volume (20-30 videos/week assumption) | MEDIUM | Internal Catch team validation; review current content ingestion workflow |
| Luciana's trust research behavior (Google + Facebook group pattern) | MEDIUM | Parent survey or guerrilla interviews in target markets |
| Legal compliance scope (COPPA / LGPD applicability for under-18 users) | CRITICAL | Legal review before any data collection architecture is finalized |

---

## Tertiary Stakeholder Reference: The High School Coach

Not a full persona for Stage 1 — no direct product interaction surface exists yet. However:
- Coaches are a primary referral channel driving Gabriel-type users to the platform.
- Coach-facing communication (e.g., a one-page explainer or referral link) is a low-cost acquisition lever.
- In Stage 2+, a coach dashboard or referral tracking feature may justify a full persona definition.
- **PjM signal**: Coach outreach / referral program should be on the go-to-market plan, not the product backlog.

---

## Anti-Personas Summary

| Anti-Persona | Why Excluded |
|---|---|
| The Casual Sports Fan | Corrupts hypothesis validation with non-intent engagement data |
| The Already-Committed Athlete | Post-decision behavior; does not convert |
| The Professional Athlete | Beyond NCAA eligibility window |
| The Recruiter Seeking Video Production Tools | Wrong product surface; Stage 2+ feature scope |
| The Academic-Only Student | Different scholarship pathway; different profiling model required |

Full anti-persona definitions: `outputs/personas/ANTI-PERSONAS.md`

---

*This Persona Map is the canonical reference for all downstream agents. PO, Architect, and PjM should anchor every decision to a named persona from this map.*
