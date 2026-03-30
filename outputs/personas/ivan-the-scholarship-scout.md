# Persona: Ivan Calderon
**Type**: Primary
**Created**: 2026-03-28 | **Session**: #1

---

## Quick Profile
| Field | Value |
|-------|-------|
| **Role / Occupation** | Athletic Scholarship Scout / Recruitment Specialist at Catch |
| **Tech Comfort** | Medium-High — comfortable with dashboards, CRMs, and spreadsheets; not a developer |
| **Context of Use** | Desktop browser, office or home office. Reviews Scout Sheets in focused 30-60 minute sessions, typically end-of-day or start-of-week. Also checks on mobile during travel. |
| **Key Behavior** | Batch-reviews ranked lead lists. Makes rapid go/no-go decisions based on fit signals. Reaches out to high-confidence matches directly. |

## Photo Prompt
> A sharp-eyed man in his mid-30s, sitting at a wide desk with two monitors, one showing a ranked table of athlete profiles and the other showing a short highlight clip. A coffee cup, a printed scouting sheet, and a notebook with handwritten annotations are visible. The office has a sports-agency feel — slightly cluttered, professional but informal.

---

## Goals
1. **Primary goal**: Identify the 5-10 student-athletes per month who are the highest-probability "Reality Matches" for specific US university programs — and contact them before a competitor scout does.
2. Spend his limited time on qualified leads, not on manually filtering raw data or chasing students who were never real prospects.
3. Build a pipeline of athletes for the 3-5 year recruitment window with enough lead time to manage eligibility, academics, and visa logistics.

## Pain Points
1. **Core frustration**: Traditional scouting is a volume game with terrible signal-to-noise. He reviews dozens of athletes who look good on paper but have no genuine interest in or fit for the US system. He needs pre-qualified leads, not raw lists.
2. The data he currently receives (if any) is unstructured — watch counts, follower numbers, self-reported stats. None of it maps to what university programs actually ask: "Does this student want to be here? Can they handle the academic load? Will they thrive in this environment?"
3. Outreach to unqualified leads wastes relationship capital with university coaches, who lose trust when Ivan sends prospects that do not fit.

## Behavioral Patterns
- Reviews the Scout Sheet as a ranked list, not a raw database. Scans the top 3-5 names first. Only goes deeper if the top tier is thin.
- Makes decisions rapidly — a profile needs to tell its story in under 90 seconds of reading. If he needs to dig for the core insight, the tool is failing him.
- Takes handwritten notes during review sessions, flagging athletes with a personal shorthand system.
- Follows up on high-confidence matches within 24-48 hours. Delays kill conversion.
- Decision-making style: analytical but gut-informed — trusts pattern recognition built over years of scouting, wants data to confirm instinct rather than replace it.
- Runs a mental "fit matrix" for each university program he serves: academic threshold, position needs, culture fit, visa pathway viability.

## Jobs-To-Be-Done
> **When** I sit down to review my weekly pipeline, **I want to** open a ranked Scout Sheet that tells me which students have the highest Reality Match score and why, **so I can** make fast, confident outreach decisions without spending hours cross-referencing raw engagement data.

1. JTBD-1: When a student's profile shows strong hypothesis confirmations for a specific university type, I want to see those confirmations summarized in plain language on the Scout Sheet, so I can assess fit in under 60 seconds.
2. JTBD-2: When I am preparing to contact a student, I want to understand their apparent academic preferences and lifestyle signals alongside their athletic data, so I can craft an outreach message that resonates with who they actually are.
3. JTBD-3: When a student who looked promising goes cold (stops engaging), I want to be notified and have that lead deprioritized automatically, so I do not waste outreach on someone who has disengaged from the pathway.

## Emotional Journey
| Phase | Emotion | Trigger |
|-------|---------|---------|
| Before use | Mildly frustrated | He has leads but cannot tell which ones are real. He has been burned by unqualified outreach before. |
| During use (Scout Sheet review) | Focused, scanning | The ranked list reduces cognitive load. He is in "hunter mode" — evaluating fast. |
| When a strong Reality Match surfaces | Energized, decisive | A profile that confirms multiple hypotheses triggers his pattern-recognition instinct. He moves immediately. |
| After successful match | Satisfied, validated | The system has proven it understands what a good lead looks like. Trust in the platform increases. |

## Scenario
> It is Monday morning. Ivan opens the Catch dashboard with his coffee. The Scout Sheet has been refreshed over the weekend. He sees 4 athletes ranked in the "High Match" tier. The top name — Gabriel Ferreira — has confirmed hypotheses for "High-Academic University Preference," "Urban Campus Environment," and "Central Midfielder / Positional IQ." The summary note reads: "3 confirmed hypotheses from 6 tagged videos. 2 re-watches, 1 save. Engagement pattern consistent over 18 days." Ivan does not need to know how the score was calculated. He needs to know it is credible. He clicks through to Gabriel's full profile, reads the behavioral summary in 40 seconds, and drafts an outreach message tailored to Gabriel's confirmed interest in academic-athletic balance. He sends it before 9 AM.

## Design Implications
- The Scout Sheet is Ivan's primary artifact. It must be scannable in under 90 seconds per profile. Ranked order, confidence indicators, and confirmed hypothesis labels must be above the fold.
- Ivan is not a data scientist. The Profiling AI OS output must be translated into plain-language summaries. No raw scores, engagement counts, or algorithm explanations. The format is: "This athlete appears interested in X, Y, Z based on consistent engagement patterns."
- Ivan needs a confidence signal, not just a score. A single number is insufficient — he needs to know if the match is based on 3 data points or 30.
- The Scout Sheet must be actionable: direct outreach initiation (contact info, draft message prompt) should be reachable in 1-2 clicks from a profile.
- Mobile-responsive Scout Sheet for travel use, but desktop is the primary surface — layout should prioritize desktop-first.

## Signals for Other Agents
| Agent | Signal |
|-------|--------|
| **PO** | Write user stories for: ranked Scout Sheet with plain-language hypothesis summaries, confidence-level indicator per athlete, one-click outreach initiation, automated deprioritization of disengaged leads, filter by position / university type / match tier. |
| **Architect** | Scout Sheet is a read-heavy, low-write surface. Can be generated asynchronously by the Profiling AI OS and cached. PDF export capability needed for offline review and university coach sharing. Plain-language summary generation requires a reliable LLM call — must be fault-tolerant with a fallback to structured data display. |
| **PjM** | Ivan is an identifiable real user. Recommend: 2-3 structured interviews with Ivan (or equivalent scouts) before Scout Sheet UI is designed. His feedback on what "Reality Match" means operationally is the single most important input for the Profiling AI OS hypothesis validation thresholds. |

---

*Validated against Empathy Validation Checklist: PASS — Named and vivid, goal-driven, pain-grounded, behaviorally distinct, context-rich, JTBD defined, testable, not a demographic bucket.*
