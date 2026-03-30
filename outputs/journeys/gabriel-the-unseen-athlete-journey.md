# Journey Map: Gabriel Ferreira — Catch MVP Stage 1 (Home Viewer)
**Persona**: Gabriel Ferreira (Primary — PC-1)
**Created**: 2026-03-29 | **Session**: #1

## Journey Overview
| Field | Value |
|-------|-------|
| **Trigger** | Gabriel hears about Catch from a teammate's WhatsApp share, a coach mention, or a targeted social ad |
| **End State** | Gabriel is a profiled athlete with ≥3 confirmed hypotheses, visible on Ivan's Scout Sheet, and consciously open to outreach |
| **Total Stages** | 6 |
| **Primary Channel** | Mobile App (Home Viewer — vertical reel) |

---

## Stage 1: Discovery
**User Goal**: Encounter something that looks interesting enough to download
**Emotional State**: Invisible, mildly curious (Low)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **WhatsApp share from teammate** | WhatsApp → App Store | Taps link, reads app store listing, decides to download | Referral attribution tracked (if link is instrumented) |
| Coach verbal mention | In-person | Searches app store manually | None |
| Social media ad | Instagram/TikTok → App Store | Taps ad, lands on app store listing | Ad campaign targeting student-athletes in key markets |

**Pain Points**:
- **App store listing must immediately signal "this is for athletes like me"** — not a generic scholarship tool
- Download friction is a hard gate; any hint of a "platform" rather than "content" reduces conversion
- Gabriel has deleted 2-3 similar apps before — his tolerance is near zero

**Opportunities**:
- App store screenshots should show reel content featuring athletes from Gabriel's background (Latin America, West Africa)
- WhatsApp share link should deep-link to a specific video reel, not a landing page
- Keep APK size minimal — large downloads on 4G are abandoned

> **MOMENT OF TRUTH #1**: The app store listing has ~5 seconds to convince Gabriel this is different from every other scholarship app he has deleted. If it looks like a form-heavy platform, he will not download.

---

## Stage 2: First Session
**User Goal**: Scroll content that feels native and interesting — no commitment
**Emotional State**: Curious, pleasantly surprised (Low → Medium)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **App launch → immediate reel** | Home Viewer (mobile) | Opens app, sees vertical reel, starts scrolling | No registration gate. First-frame preloaded during install. |
| First 5 videos | Home Viewer | Scrolls, watches, skips — natural consumption behavior | **CSI-tagged** videos served. Engagement signals captured: watch-time %, skip speed, scroll velocity |
| Save / share action | Home Viewer + WhatsApp | Saves a video that resonates or shares to WhatsApp group | High-intent signal logged. Hypothesis validation triggered for saved content tags. |

**Pain Points**:
- **First-frame load > 3 seconds on 4G = session death** — Gabriel is conditioned to instant-play by TikTok/Reels
- If the first 5 videos do not feature athletes from non-US, non-elite backgrounds, Gabriel will not see himself → deletes app
- Any onboarding screen, tutorial, or form between download and first reel → immediate drop-off

**Opportunities**:
- Aggressive video preloading (first 3 videos cached during app startup)
- Content curation algorithm seeded with high-representation, high-hypothesis-diversity videos for cold-start users
- Zero-chrome UI: no headers, no menus, no tooltips on first session — just the reel

> **MOMENT OF TRUTH #2**: The first 5 videos are the representation test. Gabriel must see at least one athlete who looks, sounds, or comes from a background like his. Failure here = uninstall within the week.

---

## Stage 3: Habitual Engagement
**User Goal**: Keep watching content that feels increasingly relevant and personally meaningful
**Emotional State**: Engaged, occasionally excited (Medium)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Daily reel sessions** | Home Viewer (mobile) | Scrolls 2-3 hours across day — commute, post-training, weekend | Engagement events streamed async to Profiling AI OS. Watch-time > 80% → confirm hypothesis. Skip < 3s → reject. |
| Re-watch behavior | Home Viewer | Replays tactical breakdowns, day-in-the-life content | **Deep-interest signal**: re-watch logged. Hypothesis confidence elevated for re-watched content tags. |
| Save and share patterns | Home Viewer + WhatsApp | Saves aspirational content, shares with teammate group | Save = high-intent. Share = identity signal ("this is who I am"). Both logged. |
| Content personalization shift | Home Viewer | Notices (without being told) that the feed is getting "better" | **Profiling AI OS** adjusting content mix based on confirmed hypotheses. More relevant tags surfaced. |

**Pain Points**:
- Content repetition → engagement plateau → churn. The algorithm must introduce novelty while staying hypothesis-aligned
- Gabriel has no way to calibrate his own progress — he does not know if his engagement is "building" anything
- If content quality or diversity drops (Priya's pipeline bottleneck), Gabriel's session quality degrades silently

**Opportunities**:
- Introduce subtle variety within hypothesis categories — same tag, different presentation styles (tactical, emotional, lifestyle)
- Content freshness indicator for the recommendation engine: never serve the same video twice unless explicitly re-watched
- Background progress signal (optional — e.g., "You've been here 3 weeks" — very light, non-disruptive)

---

## Stage 4: Passive Profiling
**User Goal**: (Unconscious) — Be understood by the system without having to explain himself
**Emotional State**: Quietly hopeful (Medium)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Engagement accumulation** | Home Viewer (system) | Gabriel's behavior patterns stabilize — clear signals emerge | **Profiling AI OS**: hypothesis validation logic runs. Watch > 80% = confirm. Skip < 3s = reject. Aggregation builds Full Athlete Profile. |
| Identity-confirming content | Home Viewer | Gabriel notices the reel "gets" him — tactical content, academic-athletic balance, specific university types | Profile-based content mixing begins. Tagged content matching confirmed hypotheses weighted higher. |
| Milestone: ≥3 confirmed hypotheses | System | No user-visible event | **Profile crosses Scout Sheet threshold.** Gabriel becomes visible to Ivan. This is the invisible handoff. |

**Pain Points**:
- **The profiling is invisible by design** — but if Gabriel ever discovers it feels invasive ("they're tracking what I watch"), trust collapses instantly
- Time gap: weeks of engagement before profile is rich enough. Gabriel gets nothing tangible in return during this period.
- If the hypothesis taxonomy is weak (few hypotheses, poor granularity), the profile will be shallow and Ivan will get a weak lead

**Opportunities**:
- The implicit contract must be reinforced through quality: better content = earned trust = continued engagement
- If legal review requires consent disclosure (COPPA/LGPD), integrate it as a brief, honest, mobile-native explainer — not a form
- Consider a soft, opt-in "profile peek" feature for Stage 2 (not MVP) — "Here's what we think you might like about college in the US"

> **MOMENT OF TRUTH #3**: Gabriel's profile crosses the Scout Sheet threshold without him knowing. The system has done its job. If the profile is accurate, Ivan gets a real lead. If it's distorted by poor tagging (Priya) or anti-persona noise, Ivan gets a false positive and the value chain breaks.

---

## Stage 5: Match Discovery
**User Goal**: Learn that a real opportunity exists — transition from passive consumer to conscious prospect
**Emotional State**: Surprised, cautiously excited (Medium → High)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Outreach from Ivan** | WhatsApp / Email / In-app message | Gabriel receives a personalized message from a scout referencing things he actually cares about | Ivan used Scout Sheet data to craft targeted outreach. Message references confirmed interest signals. |
| Content shift (optional) | Home Viewer | Gabriel sees content that feels more specifically directed — scholarship application timelines, eligibility info | Content mix shifts to "consideration-stage" hypothesis tags for profiled athletes. |

**Pain Points**:
- If the outreach feels generic or spam-like, Gabriel will ignore it — he has never responded to cold outreach before
- The channel of outreach matters enormously — WhatsApp > email > in-app for this persona in target markets
- Outreach timing: if it comes too early (thin profile), it feels random; too late (engagement declining), Gabriel may have churned

**Opportunities**:
- Ivan's outreach message template should reference specific content-confirmed signals ("We noticed you're interested in programs that balance academics and athletics…") — this validates Gabriel's experience
- Provide Ivan with channel preference data if available (WhatsApp activity vs. email activity from the market)
- This is where Gabriel's journey connects to Ivan's journey at Stage 4 (Outreach Decision)

---

## Stage 6: Active Consideration
**User Goal**: Consciously evaluate whether a US scholarship pathway is real and achievable for him
**Emotional State**: Hopeful, determined (High)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Self-directed research** | Google / University websites | Gabriel searches university names, programs, scholarship details | No Catch system involvement — this is Gabriel's own initiative, triggered by outreach. |
| Return to reel with intent | Home Viewer | Gabriel returns to the reel now searching for specific content — not just scrolling passively | Engagement signals now reflect consideration-stage behavior. Hypothesis confirmations strengthen. |
| Conversation with Luciana | In-person / WhatsApp | Gabriel shows his mother the outreach message — triggers Luciana's journey | No system involvement — but this is the bridge to Luciana's journey. |

**Pain Points**:
- Gabriel is now research-mode but the Home Viewer is designed for passive consumption — there may be no search, no filter, no way to find specific content intentionally
- If Gabriel's self-directed research reveals the platform is unknown, unreviewed, or untrustworthy, the Luciana trust journey fails
- The gap between "I'm interested" and "I know what to do next" is wide — Gabriel has no roadmap for the scholarship process

**Opportunities**:
- A Stage 2 feature: lightweight content search / topic browse within the reel for users who have crossed the consideration threshold
- Ensure Catch has a basic web presence (About page, FAQ, partner list) that survives Gabriel's Google search — this is shared with Luciana's trust journey
- Post-outreach nurture sequence: Ivan or the system provides Gabriel with a 3-step "what happens next" guide

---

## Moments of Truth
| # | Stage | Moment | Why Critical | Design Implication |
|---|-------|--------|-------------|-------------------|
| 1 | Discovery | **App store listing (5-second test)** | Gabriel has deleted 2-3 similar apps. If this looks like a form-heavy platform, he won't download. | Screenshots must show reel content, not dashboards. Feature athletes from his background. |
| 2 | First Session | **First 5 videos (representation test)** | If no video features an athlete from a non-US, non-elite background, Gabriel won't see himself → uninstall within a week. | Cold-start content must be high-representation, high-diversity by design — not by algorithm luck. |
| 3 | Passive Profiling | **Profile crosses Scout Sheet threshold** | The invisible handoff. If the profile is accurate, the value chain works. If distorted, the entire system fails downstream. | Hypothesis taxonomy quality + Priya's tagging accuracy are preconditions. Quality gate at CSI level. |
| 4 | Match Discovery | **First outreach message from Ivan** | If it feels generic or spammy, Gabriel ignores it and the conversion dies. | Ivan's message must reference content-confirmed signals. Template must be personalized, not mass-blast. |

## Emotional Arc Summary
| Stage | Emotion | Intensity | Trend |
|-------|---------|-----------|-------|
| Discovery | Invisible, mildly curious | Low | — |
| First Session | Curious, pleasantly surprised | Low → Med | ↑ |
| Habitual Engagement | Engaged, occasionally excited | Medium | ↑ |
| Passive Profiling | Quietly hopeful | Medium | → |
| Match Discovery | Surprised, cautiously excited | Med → High | ↑ |
| Active Consideration | Hopeful, determined | High | ↑ |

## Drop-Off Risks
| Stage | Risk | Probability | Mitigation |
|-------|------|-------------|------------|
| Discovery | App store listing fails the 5-second test | **High** | Athletes-first screenshots; minimize APK size; WhatsApp deep-link to reel |
| First Session | First-frame load > 3s on 4G | **High** | Aggressive preloading; adaptive bitrate; CDN edge caching in target markets |
| First Session | Non-representative content in first 5 videos | **High** | Curated cold-start content pool with mandatory representation diversity |
| Habitual Engagement | Content repetition → plateau → churn | **Medium** | Freshness scoring in recommendation engine; novelty injection |
| Passive Profiling | Legal consent requirement breaks zero-friction contract | **Medium** | Mobile-native, honest, ultra-brief consent flow if required by COPPA/LGPD review |
| Match Discovery | Outreach feels generic or spam-like | **Medium** | Signal-personalized outreach templates; preferred channel detection |
| Active Consideration | No web presence survives Google search → trust fails | **Medium** | Basic landing page, FAQ, and partner logos for web discoverability |

## Signals for Other Agents
| Agent | Signal |
|-------|--------|
| **PO** | Write epics for: (1) zero-friction onboarding (no screen before reel), (2) cold-start content curation with representation KPI, (3) engagement signal pipeline (watch-time, save, share, re-watch), (4) outreach channel integration (WhatsApp priority). Stories per stage — Discovery: app store optimization, deep-link referral. First Session: video preloading, cold-start pool. Habitual: freshness algorithm. Profiling: threshold configuration. Match: outreach template system. |
| **Architect** | First-frame < 3s on 4G is a hard architectural constraint → CDN, adaptive bitrate, edge cache in LATAM/West Africa. Engagement events must be async, lightweight, non-blocking. Profiling AI OS threshold (≥3 confirmed hypotheses) is a configurable parameter. WhatsApp integration for outreach is a third-party API dependency. Consent flow (if needed) must be a configurable gate, not hardcoded. |
| **PjM** | **Validation-critical**: (1) Intercept interviews with 5-8 student-athletes in Brazil, Nigeria, Colombia, Mexico — validate 2-3h scroll assumption, device profile, zero-form tolerance. (2) Cold-start content representation must be reviewed BEFORE launch — this is not an algorithm task, it's an editorial decision. (3) Legal review for COPPA/LGPD must complete before data architecture is finalized. (4) App store listing copy/screenshots need UX review — this is the #1 acquisition gate. |

---

*Validated against Journey Validation Checklist: PASS — Persona-grounded, stage-sequenced, touchpoint-specific, emotionally-arced, pain-mapped to opportunities, transition-defined, 4 moments of truth identified, channel-aware.*
