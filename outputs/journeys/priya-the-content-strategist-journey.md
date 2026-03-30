# Journey Map: Priya Nkosi — Catch MVP Stage 1 (CSI Dashboard)
**Persona**: Priya Nkosi (Secondary — PC-3)
**Created**: 2026-03-29 | **Session**: #1

## Journey Overview
| Field | Value |
|-------|-------|
| **Trigger** | Weekly content ingestion sprint begins — new batch of sourced videos enters the CSI queue |
| **End State** | All videos in the batch are tagged, reviewed, and published to the Home Viewer reel with high-quality hypothesis metadata |
| **Total Stages** | 5 |
| **Primary Channel** | Desktop Browser (CSI Dashboard) |

---

## Stage 1: Content Sourcing
**User Goal**: Identify 20-30 high-potential videos from external platforms for this sprint's ingestion
**Emotional State**: Organized, task-oriented (Low → Medium)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **External platform browse** | YouTube / TikTok / Instagram / Facebook | Priya searches for student-athlete, scholarship, and football content using her sourcing criteria | None — manual search on external platforms |
| **Hypothesis coverage gap report** | CSI Dashboard | Priya checks which hypothesis categories are underrepresented in the current library | Dashboard queries video-hypothesis distribution and highlights gaps: "Only 3 videos tagged for 'Rural Campus Preference' — sourcing target needed" |
| **Video URL submission** | CSI Dashboard | Priya pastes video URLs into the ingestion queue | **Content Aggregator** validates URL format, checks for duplicates, extracts embed-compatible metadata from social API (thumbnail, duration, source platform) |

**Pain Points**:
- Sourcing without a coverage gap report = guesswork — Priya risks reinforcing existing content biases
- Social media platform search is manual and time-consuming — no integrated discovery tool in Stage 1
- Duplicate video detection must happen at submission, not after tagging work is wasted

**Opportunities**:
- Hypothesis coverage dashboard as a sourcing compass: "You need 8 more videos in the 'Academic-Athletic Balance' category to reach balanced coverage"
- URL validation with instant feedback: "This video is already in the library" / "This URL format is not supported"
- Optional: RSS or playlist import for batch URL submission (reduces manual paste-per-video friction)

---

## Stage 2: AI Review
**User Goal**: Evaluate AI-proposed hypothesis tags quickly and confidently for each video
**Emotional State**: Focused, wary (Medium) — expects some misfires

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Video queue with AI proposals** | CSI Dashboard | Priya opens the tagged queue. Each video shows: thumbnail, duration, AI-proposed hypotheses, confidence score, confidence rationale | **AI Tagging Connector**: LLM (Gemini/GPT-4) has analyzed video metadata (title, description, transcript if available, visual cues) and proposed hypothesis tags with structured JSON: `{proposed_hypothesis, confidence_score, confidence_rationale}` |
| **Video preview** | CSI Dashboard (embedded player) | Priya watches the video (or samples key segments) to verify AI proposals | Video plays inline from social embed API |
| **Tag approval** | CSI Dashboard | Priya approves accurate tags with one click | Approved tags stored in Strategic Storage: Video ID → Hypothesis mapping |

**Pain Points**:
- **AI confidence rationale is essential** — a tag without a reason is not reviewable. "Tagged 'NCAA Eligibility' because…" vs. just "NCAA Eligibility" is the difference between a 12-second approval and a 4-minute investigation
- If the AI consistently misreads a content type (e.g., training montages), Priya wastes time on the same category of error every sprint
- Video viewing is the time bottleneck — Priya needs to know if she must watch the full video or if the AI rationale is sufficient for approval

**Opportunities**:
- Confidence-tiered queue: High-confidence tags at the top (likely auto-approvable), low-confidence at the bottom (need full review)
- "Quick approve" mode for high-confidence batch: select all >90% confidence → approve in one click, review exceptions manually
- AI misfire pattern detection: "The AI has misclassified 4 training montages this sprint as 'Lifestyle Content' — consider prompt adjustment"

> **MOMENT OF TRUTH #1**: The AI's accuracy rate on the first sprint sets Priya's trust calibration. If accuracy is >80%, she works efficiently and trusts the system. If <70%, she enters distrust mode and double-checks everything — her throughput collapses.

---

## Stage 3: Manual Override
**User Goal**: Correct AI misfires quickly using the controlled hypothesis taxonomy
**Emotional State**: Frustrated (if misfires are high) / Efficient (if misfires are few)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Override panel** | CSI Dashboard | Priya rejects incorrect AI tag. Selects correct hypothesis from controlled taxonomy dropdown. | Override logged: `{video_id, original_tag, corrected_tag, misfire_type}`. Corrected tag replaces original in Strategic Storage. |
| **Misfire categorization** | CSI Dashboard | Priya selects a misfire type: "Wrong category," "Wrong specificity," "Missed context," "Hallucinated tag" | Misfire log feeds AI accuracy tracking and prompt improvement pipeline |
| **Taxonomy browser** | CSI Dashboard | If unsure which tag to apply, Priya browses the hypothesis taxonomy with definitions and examples | Hypothesis taxonomy served as a versioned reference dataset — same taxonomy that Profiling AI OS uses for validation |

**Pain Points**:
- **Free-text tagging must not be possible** — it will corrupt the dataset with inconsistent labels that degrade Profiling AI OS accuracy
- If the taxonomy is too large (>50 hypotheses), finding the right tag takes too long. If too small (<15), it lacks granularity
- Edge cases (video spanning multiple hypotheses) need a clear protocol — can Priya apply multiple tags? Is there a limit?

**Opportunities**:
- Controlled taxonomy with search: type-ahead with hypothesis name + definition preview
- Multi-tag support with primary/secondary designation — the video can confirm multiple hypotheses, but one is dominant
- Override reason becomes training data: systematic misfires → prompt refinement → AI accuracy improvement cycle

---

## Stage 4: Publish
**User Goal**: Approve and push tagged content to the Home Viewer reel
**Emotional State**: Relieved, task-completing (Medium)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Batch publish action** | CSI Dashboard | Priya selects all approved/overridden videos and clicks "Publish to Reel" | Videos with approved hypothesis tags are queued for Home Viewer. Content Aggregator pushes embed references (not video files) to the reel system. Tags travel with the video as hypothesis metadata. |
| **Publish confirmation** | CSI Dashboard | Dashboard confirms: "18 videos published. 6 pending override. 0 rejected." | Sprint status updated. Videos become available in Home Viewer content pool. |

**Pain Points**:
- Publish must be a batch action, not video-by-video — Priya processes 20-30 videos per sprint
- Need clarity on what happens to "pending" videos — are they invisible to Gabriel until approved, or are they in a limbo state?
- Accidental publish of untagged content would push videos without hypothesis metadata → meaningless engagement data

**Opportunities**:
- Publish gate: cannot publish a video without at least 1 approved hypothesis tag. System-enforced quality gate.
- Sprint summary at publish: "This sprint added 18 videos covering 12 hypothesis categories. Gap remains in 'Rural Campus Preference' (0 new videos added)."
- Preview of content pool balance: visual indicator of overall library health post-publish

---

## Stage 5: Quality Feedback
**User Goal**: Understand whether her tagging work is actually producing useful profile signals downstream
**Emotional State**: Curious, seeking closure (Low → Medium)

| Touchpoint | Channel | User Action | Backstage Process |
|------------|---------|-------------|-------------------|
| **Tag effectiveness dashboard** | CSI Dashboard | Priya reviews: which tags generated the most hypothesis confirmations in the last 30 days | Profiling AI OS feeds back aggregated confirmation data per hypothesis tag. Lightweight async aggregation — not real-time. |
| **AI accuracy trend** | CSI Dashboard | Priya sees AI accuracy rate trend: "This sprint: 83%. Last 4 sprints: 71%, 75%, 79%, 83%" | Override log aggregated into accuracy metric per sprint |
| **Misfire pattern report** | CSI Dashboard | Priya sees recurring misfire patterns: "Training montages are misclassified as 'Lifestyle Content' 60% of the time" | Misfire log clustered by content type × error type |

**Pain Points**:
- **Without this feedback loop, Priya is flying blind** — she tags in good faith but has no evidence her work matters. This drives long-term disengagement.
- If the feedback data is too granular (per-video metrics), it's overwhelming. If too aggregate (monthly summary), it's too late to act on.
- The feedback loop requires the Profiling AI OS to share data back to the CSI dashboard — this is a cross-module data pipeline that may not exist in MVP

**Opportunities**:
- Sprint-cadence feedback: after each sprint's videos have been live for 2 weeks, show a "Sprint Impact Report" — which tags generated confirmations, which generated nothing
- AI prompt improvement trigger: when misfire rate exceeds threshold on a content type, auto-suggest prompt adjustment to the engineering team
- **Priya's single most motivating metric**: "Videos you tagged last month generated 47 hypothesis confirmations that contributed to 6 Scout Sheet profiles." This closes the loop from her effort to Ivan's pipeline.

> **MOMENT OF TRUTH #2**: The first time Priya sees that a video she tagged correctly generated a confirmed hypothesis that led to a Scout Sheet profile, the system justifies itself. If this feedback never comes, she eventually treats the role as mechanical data entry — quality and motivation decline.

---

## Moments of Truth
| # | Stage | Moment | Why Critical | Design Implication |
|---|-------|--------|-------------|-------------------|
| 1 | AI Review | **First sprint AI accuracy rate** | Sets Priya's trust level. >80% = efficient workflow. <70% = distrust mode, double-checking everything, throughput collapses. | AI confidence rationale on every tag. Confidence-tiered queue. Quick-approve for high confidence. |
| 2 | Quality Feedback | **First Sprint Impact Report** | Closes the effort → value loop. If Priya sees her tagging contributed to real Scout Sheet profiles, the role has meaning. If not, it becomes mechanical. | Cross-module feedback pipeline: Profiling AI OS → CSI Dashboard. Sprint-cadence, not real-time. |

## Emotional Arc Summary
| Stage | Emotion | Intensity | Trend |
|-------|---------|-----------|-------|
| Content Sourcing | Organized, task-oriented | Low → Med | ↑ |
| AI Review | Focused, wary | Medium | → |
| Manual Override | Frustrated or efficient (depends on AI accuracy) | Medium ± | ↕ |
| Publish | Relieved, task-completing | Medium | → |
| Quality Feedback | Curious, seeking closure | Low → Med | ↑ |

## Drop-Off Risks
| Stage | Risk | Probability | Mitigation |
|-------|------|-------------|------------|
| AI Review | AI accuracy < 75% → manual correction bottleneck → sprint overruns | **High** | AI accuracy threshold as quality gate. Prompt improvement cycle. Confidence-tiered review queue. |
| Manual Override | Informal/inconsistent taxonomy → dirty data → downstream profile degradation | **High** | Controlled taxonomy (no free-text). Versioned reference dataset. Taxonomy governance. |
| Manual Override | Bad tags ship → false hypothesis confirmations → bad Scout Sheet leads for Ivan | **High** | Publish gate requiring ≥1 approved tag. Override logging. Cross-module quality monitoring. |
| Quality Feedback | No feedback loop at all → Priya works blind → motivation and quality decline | **Medium** | Sprint Impact Report. Tag effectiveness metrics. Even a simple "X confirmations from your tags" closes the loop. |

## Signals for Other Agents
| Agent | Signal |
|-------|--------|
| **PO** | Write epics for: (1) CSI Dashboard with AI tag proposals + confidence rationale, (2) Controlled hypothesis taxonomy (no free-text, versioned, searchable), (3) Override workflow with misfire categorization, (4) Hypothesis coverage gap report, (5) Publish gate (no video without ≥1 approved tag), (6) Sprint Impact Report (feedback from Profiling AI OS). Stories per stage — Sourcing: URL submission + duplicate detection + coverage gaps. AI Review: confidence queue + quick-approve. Override: taxonomy browser + multi-tag + misfire log. Publish: batch action + sprint summary. Feedback: accuracy trend + effectiveness dashboard. |
| **Architect** | AI Tagging Connector must return structured JSON: `{proposed_hypothesis, confidence_score, confidence_rationale}`. Hypothesis taxonomy = versioned reference dataset, not hardcoded — shared between CSI and Profiling AI OS. Override log → training data pipeline for prompt improvement. **Feedback loop** from Profiling AI OS to CSI is an async aggregation job (sprint-cadence, not real-time). Social embed APIs (YT/TikTok/IG/FB) for video preview — each has different embed support and rate limits. Publish = push hypothesis metadata to content pool, not video files (content stays on source platform). |
| **PjM** | (1) Priya's sprint volume (20-30 videos/week) sets the throughput floor for the AI tagging pipeline — capacity plan accordingly. (2) AI accuracy rate is the single most critical quality metric: define acceptable threshold (≥75% recommended), monitoring dashboard, and escalation path before build. (3) Hypothesis taxonomy must be formally defined and agreed upon BEFORE CSI dashboard development begins — taxonomy governance is a pre-build dependency. (4) Feedback loop from Profiling AI OS → CSI is a cross-module dependency that needs explicit sprint allocation; don't let it slip to "nice-to-have." |

---

*Validated against Journey Validation Checklist: PASS — Persona-grounded, stage-sequenced, touchpoint-specific, emotionally-arced, pain-mapped to opportunities, transition-defined, 2 moments of truth identified, channel-aware.*
