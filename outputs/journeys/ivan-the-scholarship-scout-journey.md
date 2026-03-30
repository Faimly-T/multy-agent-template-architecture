# Journey Map: Ivan Calderon — Catch MVP Stage 1 (Scout Sheet)
**Persona**: Ivan Calderon (Primary — PC-2)
**Created**: 2026-03-29 | **Session**: #1

## Journey Overview
| Field | Value |
|-------|-------|
| **Trigger** | Scout Sheet refreshes with newly profiled athletes (weekly cycle) |
| **End State** | Ivan has identified ≥1 high-confidence Reality Match, sent personalized outreach, and is tracking response |
| **Total Stages** | 5 |
| **Primary Channel** | Desktop Dashboard (Scout Sheet); Mobile for travel review |

---

## Stage 1: Pre-Review Prep
**User Goal**: Know the Scout Sheet has new leads and allocate focused review time
**Emotional State**: Mildly anticipatory (Low)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Refresh notification** | Email / Dashboard badge | Ivan sees notification that Scout Sheet has been updated with new leads | Profiling AI OS completes weekly batch processing. Profiles with ≥3 confirmed hypotheses pushed to Scout Sheet. Plain-language summaries generated via LLM. |
| Calendar block | Personal calendar | Ivan schedules a 30-60 minute review window (typically Monday AM or Friday PM) | None — this is Ivan's personal workflow |

**Pain Points**:
- If notifications are noisy (too frequent, too many low-quality alerts), Ivan will start ignoring them
- Notification must convey signal density: "4 new leads, 2 in High Match tier" is useful. "Scout Sheet updated" is not.

**Opportunities**:
- Smart notification: include lead count and top-tier count in the notification itself — Ivan can decide whether to prioritize or defer
- Weekly digest email with a 3-line summary of what's new in the Scout Sheet

---

## Stage 2: Pipeline Review
**User Goal**: Scan the ranked lead list and identify the top 3-5 profiles worth a deep dive
**Emotional State**: Focused, scanning (Medium)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Scout Sheet ranked list** | Desktop dashboard | Opens Scout Sheet, scans top-to-bottom. Evaluates: name, position, match tier, confirmed hypothesis count, summary preview | Profiling AI OS ranked leads by Reality Match score. Profiles sorted by confidence (hypothesis count × engagement consistency). |
| **Tier filter** | Dashboard | Filters by High/Medium/Low match tier, or by position/university type | Filter criteria map to confirmed hypothesis dimensions |
| **Quick-scan summary** | Dashboard | Reads 1-line summary per profile: "Central midfielder, high-academic preference, urban campus, 18-day engagement consistency" | LLM-generated plain-language summary from confirmed hypothesis stack |

**Pain Points**:
- **If the ranked list is polluted with false positives** (casual fans, anti-persona engagement), Ivan's trust erodes within 2-3 review cycles
- Raw scores without context are useless — Ivan needs plain-language meaning, not numbers
- If the list is too long (>20 leads) without clear tier separation, Ivan wastes time on low-confidence noise

**Opportunities**:
- **Confidence indicator per lead**: not just a score, but "based on 6 confirmed hypotheses from 22 tagged videos over 18 days" — this is the credibility signal Ivan needs
- Above-the-fold layout: Rank, Name, Position, Match Tier, Hypothesis Count, 1-line Summary. Everything else is a click away.
- "New since last review" badge — Ivan wants to see what's fresh, not re-scan profiles he's already evaluated

> **MOMENT OF TRUTH #1**: Ivan's first Scout Sheet review sets his trust level for the entire platform. If the top 3 leads are credible and well-described, he's in. If even 1 of the top 3 is an obvious bad fit, he questions the intelligence.

---

## Stage 3: Profile Deep-Dive
**User Goal**: Read an athlete's full profile and assess Reality Match fit in under 90 seconds
**Emotional State**: Evaluative, pattern-matching (Medium → High if strong match)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Full Athlete Profile** | Desktop dashboard | Clicks into a ranked lead. Reads behavioral summary, confirmed hypotheses, engagement timeline | Profile assembled from Profiling AI OS output: confirmed hypotheses, engagement patterns, content interaction history |
| **Hypothesis detail view** | Dashboard | Expands specific hypotheses: "High-Academic University Preference — confirmed by: 3 high-academic videos watched >80%, 1 saved, 1 re-watched" | Hypothesis validation log per athlete |
| **Mental fit matrix** | Ivan's cognition | Ivan runs the profile against his internal model of university program requirements: "This kid fits Program X at University Y" | None — this is Ivan's domain expertise applied to platform data |

**Pain Points**:
- **If the behavioral summary reads like algorithm output** ("engagement_score: 0.87, hypothesis_confirmed: 3"), Ivan can't translate it into scouting language
- Missing information kills confidence: if the profile shows academic preference but no position data, Ivan can't make a match
- If Ivan can't see the evidence behind a hypothesis confirmation, he can't trust it — he needs "because" statements

**Opportunities**:
- Plain-language profile: "Gabriel appears interested in programs that balance academics and athletics. He consistently engages with content about mid-sized university environments and central midfielder development."
- Evidence trail: each confirmed hypothesis links to the specific engagement signals that confirmed it (without exposing raw data)
- Profile completeness indicator: "4 of 6 key dimensions confirmed" — Ivan sees what's strong and what's still developing

---

## Stage 4: Outreach Decision
**User Goal**: Decide to contact or pass, and craft a personalized outreach message
**Emotional State**: Decisive, energized (High — for strong matches)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Contact / Pass action** | Dashboard | Ivan clicks "Contact" or "Pass" on the profile | Decision logged. "Pass" with reason feeds back to system for future ranking calibration. |
| **Outreach drafting** | Dashboard → WhatsApp/Email | Ivan drafts a personalized message using confirmed signals as conversation anchors | System pre-populates outreach template with confirmed signal language: "We've noticed your interest in academic-athletic balance programs…" |
| **Channel selection** | Dashboard | Ivan selects outreach channel: WhatsApp (preferred in LATAM/Africa), email, or in-app | Contact channel recommendation based on market data (if available) |

**Pain Points**:
- If outreach requires switching between 3 tools (dashboard → CRM → WhatsApp), momentum is lost
- Generic outreach templates waste Ivan's domain expertise — he knows how to personalize, but he needs the signal data surfaced, not hidden
- **Delayed outreach = lost lead.** If Ivan reviews on Monday but can't send until Wednesday because the workflow is clunky, a competitor may reach the athlete first.

**Opportunities**:
- **One-click outreach initiation**: from the profile, launch a pre-drafted message in the preferred channel — Ivan edits and sends, not writes from scratch
- Pass with reason: when Ivan passes on a lead, capture why — "wrong position," "not enough data," "bad fit for my programs" — this trains the system
- Outreach sent timestamp: track when Ivan contacted a lead to measure time-to-outreach and optimize notification timing

> **MOMENT OF TRUTH #2**: The outreach message is where the entire Catch value chain converts into real-world impact. If Ivan's message resonates with Gabriel because it references things Gabriel actually cares about, the platform has proven its core thesis. If it feels generic, the implicit profiling was wasted.

---

## Stage 5: Follow-Up & Monitoring
**User Goal**: Track which outreach yielded responses and maintain a clean, up-to-date pipeline
**Emotional State**: Satisfied (if matches convert), vigilant (ongoing)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Response tracking** | Dashboard | Ivan logs whether a contacted athlete responded, and the quality of the interaction | Response status updates lead pipeline. Responded → Active. No response after X days → Deprioritized. |
| **Lead status updates** | Dashboard / Email alert | Ivan sees when a previously strong lead goes cold (engagement drops) | Profiling AI OS detects engagement decline. Auto-flags lead as "cooling" → Ivan is notified. |
| **Pipeline health view** | Dashboard | Ivan reviews overall pipeline: X active, Y contacted, Z converting | Aggregated pipeline metrics computed from Scout Sheet + outreach log |

**Pain Points**:
- If disengaged leads are not automatically deprioritized, Ivan wastes follow-up effort on dead leads
- No visibility into whether lack of response is "athlete not interested" or "wrong channel / bad timing"
- The 3-5 year recruitment window means some leads will be slow-burn — the system must distinguish between "cold" and "long-cycle"

**Opportunities**:
- **Auto-deprioritization**: if an athlete's engagement drops below a threshold for X days, flag the lead with a "cooling" indicator — don't delete, but move down the ranked list
- Pipeline dashboard: show funnel metrics (profiled → contacted → responded → converting) — Ivan can calibrate his own success rate
- Lead quality feedback loop: Ivan's contact/pass/response data should feed back to the Profiling AI OS to improve future ranking accuracy

> **MOMENT OF TRUTH #3**: When a contacted athlete responds positively and Ivan can see the platform's profiling was accurate — "this kid really does care about academic-athletic balance" — the system earns Ivan's long-term trust. This is the validation loop that makes Ivan a power user.

---

## Moments of Truth
| # | Stage | Moment | Why Critical | Design Implication |
|---|-------|--------|-------------|-------------------|
| 1 | Pipeline Review | **First Scout Sheet review** | Sets trust level for the platform. If top leads are credible, Ivan is in. One bad lead in top 3 = doubt. | Ranked list with plain-language summaries, confidence indicators, and clear tier separation. |
| 2 | Outreach Decision | **First outreach message sent** | The conversion point. If the message resonates because it references real athlete signals, the core thesis is validated. | Pre-populated outreach templates with signal-derived language. One-click channel launch. |
| 3 | Follow-Up | **First positive response from a contacted athlete** | Closes the trust loop. Ivan sees the profiling was accurate. He becomes a repeat user. | Response tracking + pipeline view that makes successful matches visible and celebratory. |

## Emotional Arc Summary
| Stage | Emotion | Intensity | Trend |
|-------|---------|-----------|-------|
| Pre-Review Prep | Mildly anticipatory | Low | — |
| Pipeline Review | Focused, scanning | Medium | ↑ |
| Profile Deep-Dive | Evaluative, pattern-matching | Medium → High | ↑ |
| Outreach Decision | Decisive, energized | High | ↑ |
| Follow-Up & Monitoring | Satisfied, vigilant | Medium | → |

## Drop-Off Risks
| Stage | Risk | Probability | Mitigation |
|-------|------|-------------|------------|
| Pipeline Review | False positives pollute ranked list → trust erodes in 2-3 cycles | **High** | Anti-persona filtering in Profiling AI OS; confidence thresholds; "New" badges to focus attention |
| Pipeline Review | Too many leads without tier separation → cognitive overload | **Medium** | Hard tier cutoffs (High/Medium/Low); default view shows High only; expand to see more |
| Profile Deep-Dive | LLM-generated summary fails or reads like algorithm output | **Medium** | Fallback to structured data display (hypothesis list with evidence); LLM retry with different prompt |
| Outreach Decision | Multi-tool workflow slows outreach → competitor reaches athlete first | **Medium** | One-click outreach from profile; pre-drafted message in preferred channel |
| Follow-Up | Disengaged leads not auto-deprioritized → Ivan wastes follow-up | **Low** | Engagement decay detection in Profiling AI OS; auto-flagging with "cooling" indicator |

## Signals for Other Agents
| Agent | Signal |
|-------|--------|
| **PO** | Write epics for: (1) Ranked Scout Sheet with plain-language summaries + confidence indicators, (2) Profile deep-dive with "because" evidence trails, (3) One-click outreach initiation with pre-populated templates, (4) Lead status pipeline (contacted/responded/converting), (5) Auto-deprioritization of cooling leads, (6) Pass-with-reason feedback loop. Stories per stage — Prep: smart notification with lead count. Review: tier filtering, "new" badges. Deep-Dive: evidence expansion, completeness indicator. Outreach: channel selection, template pre-fill. Follow-Up: response logging, pipeline health dashboard. |
| **Architect** | Scout Sheet is **read-heavy, low-write** — generate asynchronously from Profiling AI OS, cache aggressively. LLM call for plain-language summaries must be fault-tolerant with structured-data fallback. PDF export needed for offline review and university coach sharing. **Lead quality feedback loop** from Ivan's contact/pass decisions back to Profiling AI OS is a critical data pipeline — requires async processing, not blocking. WhatsApp API integration for outreach is a third-party dependency with rate limits and compliance requirements. Desktop-first layout, mobile-responsive for travel. |
| **PjM** | **Validation-critical**: (1) 2-3 structured interviews with Ivan (or equivalent scouts) before Scout Sheet UI design begins — his definition of "Reality Match" calibrates the entire Profiling AI OS threshold. (2) First Scout Sheet demo with real(ish) data is the highest-risk demo in the MVP — prepare synthetic but realistic profiles. (3) LLM reliability for summary generation: define fallback behavior and acceptable quality threshold before build. (4) WhatsApp integration timeline and compliance review — if outreach channel is not ready at launch, the conversion path breaks. |

---

*Validated against Journey Validation Checklist: PASS — Persona-grounded, stage-sequenced, touchpoint-specific, emotionally-arced, pain-mapped to opportunities, transition-defined, 3 moments of truth identified, channel-aware.*
