# Journey Map: Luciana Ferreira — Catch MVP Stage 1 (Trust Gate)
**Persona**: Luciana Ferreira (Secondary — PC-4)
**Created**: 2026-03-29 | **Session**: #1

## Journey Overview
| Field | Value |
|-------|-------|
| **Trigger** | Gabriel shows Luciana the Catch app during a household interaction (dinner, after school) |
| **End State** | Luciana makes a binary trust decision: ALLOW (becomes enabler) or BLOCK (Gabriel stops using, primary user lost) |
| **Total Stages** | 5 |
| **Primary Channel** | Mobile Web (landing page, FAQ, Google search results) — not the app itself |

---

## Stage 1: First Exposure
**User Goal**: Understand what Gabriel is using and form an initial impression
**Emotional State**: Cautious, skeptical (Medium)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Gabriel shows the reel** | In-person (physical) | Luciana watches Gabriel scroll the reel for 30-60 seconds. Sees a TikTok-like interface. | None — this is a household interaction |
| **"What does it do?"** | In-person | Luciana asks Gabriel what the app does with what he watches. Gabriel shrugs. | None — but the fact that Gabriel cannot explain the value proposition is itself a design problem |
| **Mental threat assessment** | Luciana's cognition | She categorizes the app: "looks like social media" + "claims to be educational/career" = suspicious | None — but this cognitive framing drives every subsequent action |

**Pain Points**:
- **The zero-form, implicit design that delights Gabriel is alarming to Luciana** — she sees a tracking app disguised as entertainment
- Gabriel cannot explain what the platform does because the platform has never explained it to him — there is no "what this is" messaging in the zero-friction UX
- Luciana's first impression is formed in ~60 seconds and is very difficult to reverse

**Opportunities**:
- A lightweight, non-intrusive "What is Catch?" explainer accessible from the app (not in the onboarding flow — that would break Gabriel's zero-friction). Perhaps in a settings/about section Gabriel can show Luciana.
- The fact that Gabriel is engaged and excited is a positive signal — Luciana is skeptical but not hostile. The app has a narrow window to earn trust through external content.

---

## Stage 2: Independent Research
**User Goal**: Investigate the platform without relying on Gabriel or the app's own claims
**Emotional State**: Analytical, probing (Medium → High)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Google search** | Mobile browser | Luciana searches "Catch scholarship app" or "Catch football scholarship review" | None — but Catch's web presence (or absence) is the first data point |
| **Company landing page** | Mobile web | Reads About section, team page, mission statement | Catch website must exist and be mobile-friendly |
| **Facebook group inquiry** | Facebook | Posts in "Scholarships Abroad for Brazilian Students" or similar parent group: "Has anyone heard of Catch?" | None — but this signals the importance of external social proof |
| **Review/press search** | Mobile browser | Looks for news articles, app store reviews, third-party coverage | None — but absence of reviews is a red flag for this persona |

**Pain Points**:
- **If Luciana's Google search returns nothing meaningful** (no website, no reviews, no press), this is the strongest negative signal possible. The platform appears to not exist outside Gabriel's phone.
- App store reviews from other parents would be hugely influential — but are unlikely to exist for an MVP launch
- Luciana reads in Portuguese (or Spanish, or her local language) — English-only trust content excludes her

**Opportunities**:
- A basic, mobile-optimized Catch website with: mission statement, team bio, university partner logos, and a parent-specific FAQ section
- Pre-launch: seed 2-3 press mentions or partner announcements in markets where Luciana lives
- Localized trust content: at minimum, a Portuguese and Spanish version of the parent FAQ
- App store description should include a parent-facing sentence: "Built to connect student-athletes with US university scholarship opportunities through content they already love"

> **MOMENT OF TRUTH #1**: Luciana's Google search. If she finds a credible website with real team names, partner logos, and a clear explanation of what the platform does — her threat level drops. If she finds nothing, or a vague single-page site, the threat level escalates. This touchpoint is outside the product but is critical to the product's success.

---

## Stage 3: Trust Evaluation
**User Goal**: Find specific answers to three questions: (1) What data is collected? (2) Who sees it? (3) What happens if we want out?
**Emotional State**: Deliberate, evidence-seeking (High)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Parent FAQ page** | Mobile web | Luciana finds (or fails to find) a parent-specific FAQ that addresses her core concerns | FAQ content must exist, be findable, and be written at a non-technical reading level in her language |
| **Privacy summary** | Mobile web | Reads a plain-language privacy summary written for parents — NOT the legal terms of service | Privacy summary translates data collection to human language: "We track which videos Gabriel watches to understand what kind of university would suit him. We do not sell this data." |
| **Partner university list** | Mobile web | Scans for recognizable university names — this is a credibility proxy | Partner list must include real, verifiable universities |
| **Data deletion / deactivation info** | Mobile web | Looks for: "Can I delete my child's data?" / "Can we stop at any time?" | Deactivation/deletion pathway must be documented and accessible |

**Pain Points**:
- **If the privacy summary is buried in legal jargon**, Luciana cannot evaluate it. She is looking for human-readable language, not a terms-of-service document
- If there is no parent-specific section (FAQ, privacy, partner list all mixed into a general site), Luciana has to work too hard to find what matters to her
- The absence of a data deletion pathway is a hard fail — Luciana needs to know this is not irrevocable

**Opportunities**:
- **One-page parent trust center**: FAQ + privacy summary + partner list + deactivation info. All on one easily accessible mobile web page. Target: a parent can read it in under 3 minutes.
- COPPA / LGPD compliance badges visible on the trust page — Luciana recognizes these as authority signals
- Testimonial from another parent (even one) who has allowed their child to use the platform → social proof that bypasses institutional skepticism
- Explicit statement: "We track what videos your child watches. We use this to understand their academic and athletic interests. No data is sold. You can request deletion at any time."

> **MOMENT OF TRUTH #2**: The parent FAQ and privacy summary. If it answers Luciana's three core questions (what data, who sees it, can we leave) in plain language, her trust threshold is met. If any of the three are unanswered, she defaults to blocking access.

---

## Stage 4: Trust Decision (Gate)
**User Goal**: Make a binary allow/block decision
**Emotional State**: Resolved — either relieved (allow) or firm (block)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Allow path** | Household conversation | Luciana tells Gabriel he can keep using the app. May set verbal conditions: "Show me sometimes what it's recommending." | No system involvement — but this decision unlocks Gabriel's continued engagement |
| **Block path** | Household conversation | Luciana asks Gabriel to delete the app. May explain why: "I don't trust what they're doing with your data." | **CRITICAL**: Gabriel's engagement drops to zero. Primary user lost. Profiling AI OS loses a prospect. Ivan will never see this athlete. |

**Pain Points**:
- **This is a binary gate with no middle ground in Stage 1.** There is no "try it for a week with parental controls" option. Luciana either trusts enough to allow, or doesn't.
- The decision is made within 1-2 weeks of first exposure. If trust content isn't ready at launch, it's too late.
- Luciana's decision affects not just Gabriel but potentially his entire teammate group — she is an influential voice in the parent network

**Opportunities**:
- A "parent notification" feature — even a simple monthly email summary: "Gabriel has been active on Catch. His profile suggests interest in programs that balance academics and athletics." This gives Luciana ongoing visibility without changing Gabriel's UX
- Parental consent flow (if legally required by COPPA/LGPD) can be designed as a trust builder rather than a friction point — "We ask for your permission because we take your child's data seriously"
- Post-decision nurture: if Luciana allows, send a welcome email acknowledging her role and providing her a privacy point-of-contact

---

## Stage 5: Active Enablement (Allow Path Only)
**User Goal**: Support Gabriel's continued use and feel included in the process
**Emotional State**: Supportive, occasionally checking in (Low → Medium)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Periodic check-in with Gabriel** | In-person | Luciana asks: "Have you been using that scholarship app?" — nudges continued engagement | None |
| **Parent notification/summary (if built)** | Email | Luciana receives a quarterly summary: "Gabriel's profile is developing. He appears interested in mid-sized universities with strong academic-athletic balance programs." | Profiling AI OS generates parent-safe summary. No raw data, no algorithm language. Opt-in only. |
| **Word-of-mouth referral** | In-person / WhatsApp parent groups | Luciana mentions Catch to other parents: "There's this app Gabriel uses for scholarships. It seems legitimate." | Organic referral drives new Gabriel-type users to Discovery |

**Pain Points**:
- Without any periodic update, Luciana's trust may erode over time — "I haven't heard anything about this in months. Is it even doing anything?"
- The parent summary must be general enough to not feel surveillance-like but specific enough to feel educational
- If Luciana hears negative information about the platform after allowing (news, gossip, data breach), she will re-evaluate immediately

**Opportunities**:
- **Parent summary as a high-ROI trust maintenance feature**: low-cost to build (one LLM summary call per quarter), high impact on retention of minor users
- Referral mechanics: give Luciana a sharable link or one-page explainer she can forward in her parent WhatsApp groups
- Trust maintenance: annual privacy report or "year in review" personalizing what the platform has done for Gabriel's pathway

---

## Moments of Truth
| # | Stage | Moment | Why Critical | Design Implication |
|---|-------|--------|-------------|-------------------|
| 1 | Independent Research | **Google search results for "Catch scholarship app"** | If Luciana finds nothing credible, trust evaluation fails before it starts. The platform doesn't exist in her information space. | Catch must have a live, mobile-optimized, multi-language website with team, mission, and partners before MVP launch. |
| 2 | Trust Evaluation | **Parent FAQ answers the 3 core questions** | What data, who sees it, can we leave. If any of the 3 are unanswered in plain language, Luciana defaults to blocking. | One-page parent trust center with FAQ + privacy + partners + deactivation. All in plain language. Localized. |

## Emotional Arc Summary
| Stage | Emotion | Intensity | Trend |
|-------|---------|-----------|-------|
| First Exposure | Cautious, skeptical | Medium | — |
| Independent Research | Analytical, probing | Medium → High | ↑ |
| Trust Evaluation | Deliberate, evidence-seeking | High | → |
| Trust Decision | Resolved (relieved or firm) | High | ↓ or → |
| Active Enablement | Supportive, checking in | Low → Med | → |

## Drop-Off Risks
| Stage | Risk | Probability | Mitigation |
|-------|------|-------------|------------|
| Independent Research | No web presence → platform appears non-existent → trust fails instantly | **High** | Basic website with team, mission, partners, FAQ — live before MVP launch |
| Independent Research | English-only trust content → excluded from non-English parent markets | **High** | Localized FAQ in Portuguese, Spanish at minimum. Consider French for West African markets. |
| Trust Evaluation | Privacy summary in legal jargon → Luciana can't evaluate | **High** | Plain-language parent privacy summary. One page. "We track what they watch. We don't sell data. You can delete any time." |
| Trust Evaluation | No data deletion pathway documented → feels irrevocable | **Medium** | Explicit deletion instructions on trust page + email point of contact |
| Trust Decision | No parental controls or visibility → all-or-nothing decision | **Medium** | Optional parent notification/summary feature. Low cost, high trust leverage. |
| Active Enablement | No updates for months → trust erodes → retroactive block | **Low** | Quarterly parent summary email (opt-in). Annual privacy report. |

## Signals for Other Agents
| Agent | Signal |
|-------|--------|
| **PO** | Write epics for: (1) Parent trust center — one-page: FAQ + privacy + partners + deactivation (mobile web, localized), (2) Optional parent notification/summary feature (quarterly email, opt-in), (3) App-internal "What is Catch?" explainer in settings (not onboarding), (4) Data deletion / account deactivation pathway. Note: if users are under 18, COPPA (US) and LGPD (Brazil) parental consent may be a LEGAL REQUIREMENT — not optional. Consent flow design should be trust-building, not pure compliance. |
| **Architect** | Parent trust center is a **static or near-static web page** — can be hosted independently from the app backend (simple CMS or static site). Localization framework needed (Portuguese, Spanish minimum). Parent notification email requires: Profiling AI OS summary generation → email service integration → opt-in/opt-out management. Data deletion pathway: technical implementation required for COPPA/LGPD compliance — must be able to purge all engagement data and profile data for a specific user on request. Age verification or parental consent flow architecture: depends on legal review outcome. |
| **PjM** | **CRITICAL pre-launch dependency**: Luciana's journey starts OUTSIDE the app. If the website, FAQ, and privacy summary are not live at launch, every parent who Googles "Catch scholarship app" will find nothing → trust fails → their child stops using the app. This is not a "nice-to-have" — it's a silent acquisition killer. Sprint plan must include: (1) Trust content creation sprint before launch, (2) Localization sprint for top markets, (3) Legal compliance review for COPPA/LGPD. Timeline: trust content must be QA'd and live BEFORE Gabriel's first marketing push. |

---

*Validated against Journey Validation Checklist: PASS — Persona-grounded, stage-sequenced, touchpoint-specific, emotionally-arced, pain-mapped to opportunities, transition-defined, 2 moments of truth identified, channel-aware.*
