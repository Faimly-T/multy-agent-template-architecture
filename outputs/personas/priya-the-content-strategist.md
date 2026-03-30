# Persona: Priya Nkosi
**Type**: Secondary
**Created**: 2026-03-28 | **Session**: #1

---

## Quick Profile
| Field | Value |
|-------|-------|
| **Role / Occupation** | Content Operations Specialist at Catch — manages the CSI dashboard |
| **Tech Comfort** | High — comfortable with CMS platforms, tagging systems, spreadsheets, and basic API workflows. Not a developer. |
| **Context of Use** | Desktop browser, office or remote. Works in weekly ingestion sprints — 20-30 videos per cycle. Collaborative workflow with the editorial team. |
| **Key Behavior** | Reviews AI-proposed tags, applies manual overrides, approves content for publishing to the Home Viewer reel. Maintains the hypothesis library. |

## Photo Prompt
> A focused woman in her late 20s at a standing desk, two browser tabs open — one showing a video with an AI tagging panel on the right, another showing a content queue. She has a coffee, a sticky note on her monitor with a checklist, and she is hovering her cursor over a tag she is about to edit.

---

## Goals
1. **Primary goal**: Maintain a high-quality, strategically tagged content pipeline that gives the Profiling AI OS the inputs it needs to build accurate athlete profiles — without becoming a bottleneck herself.
2. Reduce the time spent correcting poor AI-generated tags so she can focus on strategic content sourcing rather than quality control.
3. Ensure hypothesis coverage is balanced across the full spectrum of student-athlete profiles — not just the obvious archetypes.

## Pain Points
1. **Core frustration**: The AI tagging system is inconsistent. Some videos get excellent hypothesis proposals; others are misread entirely (a tactical breakdown tagged as "lifestyle content"). Every misfire requires manual intervention, which compounds across 30 videos per sprint.
2. There is no feedback loop from the Profiling AI OS back to her. She tags content in good faith but has no visibility into whether those tags are actually validating hypotheses or producing useful profile signals. She is flying blind on quality.
3. The hypothesis taxonomy is not formalized. Different team members apply similar-but-not-identical tags, creating a dirty dataset that degrades profile accuracy over time.

## Behavioral Patterns
- Works through the video queue in batches of 8-10, reviewing AI proposals and applying overrides. Estimates 4-6 minutes per video including viewing and tagging.
- Keeps a personal "tag quality log" in a spreadsheet because the system does not surface misfire patterns natively.
- Flags edge-case content (e.g., a video that spans multiple hypotheses) in a shared Slack channel rather than guessing.
- Decision-making style: methodical and rule-following when rules are clear; frustrated and inconsistent when they are not.
- Advocates internally for better AI prompt tuning when she sees systematic misclassifications.

## Jobs-To-Be-Done
> **When** I am processing a weekly content batch, **I want to** review AI-generated hypothesis tags that are accurate and consistently formatted, **so I can** approve or adjust them quickly and confidently without spending half my sprint on corrections.

1. JTBD-1: When the AI proposes a hypothesis for a video, I want to see a brief confidence explanation ("tagged as NCAA Eligibility because the video discusses academic credit transfer"), so I can make an informed override decision in under 30 seconds.
2. JTBD-2: When I notice a pattern of AI misfires on a certain content type, I want to flag it with a structured report, so the team can retrain or adjust the prompt without relying on my personal spreadsheet workaround.
3. JTBD-3: When I am sourcing new content, I want to see which hypothesis categories are underrepresented in the current video library, so I can intentionally fill gaps rather than reinforcing existing biases.

## Emotional Journey
| Phase | Emotion | Trigger |
|-------|---------|---------|
| Before sprint | Organized, slightly wary | She knows the sprint will surface AI misfires. She has learned to expect imperfection and build in buffer time. |
| During tagging (good AI day) | Efficient, satisfied | When the AI is right, the queue moves fast and she feels like the system is working as intended. |
| During tagging (bad AI day) | Frustrated, manual-mode | Three misfires in a row trigger a mood shift. She starts double-checking every tag, losing speed. |
| After sprint | Relieved, mildly uncertain | Sprint is done, but she is never fully confident the hypothesis quality is high enough. No feedback loop = no closure. |

## Scenario
> Priya opens the CSI dashboard on Tuesday morning for the weekly ingestion sprint. She has 24 videos in the queue. She clicks into the first — a 90-second YouTube video of a Nigerian central defender discussing his experience at a D2 university. The AI has proposed two tags: "Urban Campus Preference" and "NCAA Academic Standards — International Track." She reads the confidence note: "tagged Urban Campus because video references on-campus social life; tagged NCAA International Track because subject discusses TOEFL and credit evaluation." Both are accurate. She approves in 12 seconds. The fifth video is a training montage with no dialogue — the AI has proposed "Position-specific IQ: Goalkeeper," but the athlete is clearly a midfielder. She selects the override, corrects it to "Position-specific IQ: Central Midfielder," and logs the misfire type in the system. By end of day she has processed 24 videos, with 4 overrides. She notes the AI had a 83% accuracy rate this sprint — up from 71% last week.

## Design Implications
- The CSI dashboard must surface AI tagging confidence explanations in plain language alongside every proposed tag. A tag without a reason is not reviewable — it is a guess.
- The override workflow must be frictionless: one click to reject, one click to select the correct hypothesis from a controlled taxonomy. Free-text tagging should not be possible — it will corrupt the dataset.
- A hypothesis coverage dashboard (which hypotheses are well-represented vs. underrepresented in the current library) is a high-value feature for this persona that prevents systemic content bias.
- A feedback loop from the Profiling AI OS — even a simple "this tag generated X hypothesis confirmations in the last 30 days" — would dramatically improve Priya's ability to calibrate her tagging quality.
- The hypothesis taxonomy must be formally defined, version-controlled, and accessible within the dashboard. No tribal knowledge.

## Signals for Other Agents
| Agent | Signal |
|-------|--------|
| **PO** | Write user stories for: AI tag proposal with confidence explanation, controlled hypothesis taxonomy (no free-text), override logging with misfire categorization, hypothesis coverage gap report, feedback loop from Profiling AI OS to CSI dashboard. |
| **Architect** | The AI tagging connector must return structured JSON with: proposed_hypothesis, confidence_score, confidence_rationale. The hypothesis taxonomy must be managed as a versioned reference dataset, not hardcoded. Feedback loop data can be a lightweight async aggregation job — does not need to be real-time. |
| **PjM** | Priya's sprint volume (20-30 videos/week) defines the throughput requirement for the CSI AI tagging pipeline. If the AI accuracy rate falls below ~75%, her manual correction load becomes the bottleneck for the entire system. This is a quality gate that needs a defined threshold and monitoring from day one. |

---

*Validated against Empathy Validation Checklist: PASS — Named and vivid, goal-driven, pain-grounded, behaviorally distinct, context-rich, JTBD defined, testable, not a demographic bucket.*
