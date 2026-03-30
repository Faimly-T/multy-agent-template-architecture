# Persona: Gabriel Ferreira
**Type**: Primary
**Created**: 2026-03-28 | **Session**: #1

---

## Quick Profile
| Field | Value |
|-------|-------|
| **Role / Occupation** | Student-Athlete, Year 11 (age 16), central midfielder |
| **Tech Comfort** | High — native mobile user, heavy social media consumer |
| **Context of Use** | Evening after training, commute, weekend downtime. Older mid-range Android phone (Samsung A-series). Often on 4G, occasionally Wi-Fi. |
| **Key Behavior** | Scrolls vertical video for 2-3 hours daily. Re-watches tactical and highlight content. Shares videos that mirror his identity and ambitions. |

## Photo Prompt
> A lean, focused teenage boy in training kit sitting on stadium bleachers after practice, phone in both hands, headphones around his neck, scrolling with his thumb — the glow of the screen reflected in his eyes. The background is a football pitch at dusk in a mid-sized Latin American or African city.

---

## Goals
1. **Primary goal**: Get discovered by a US university program that values both his athletic ability and his academic potential — without having to navigate a system he was never taught.
2. Find content that helps him understand what American college football culture actually looks, feels, and sounds like so he can picture himself in it.
3. Receive implicit validation that he is on the right pathway — through increasingly relevant content that mirrors his profile.

## Pain Points
1. **Core frustration**: The US scholarship world is an insider's game. He has no agent, no network, and no one in his school has ever gone through it. The process feels like it was designed for someone else.
2. Every "scholarship platform" he has tried demands forms, CVs, and video submissions upfront — before he even knows if the opportunity is real. The barrier feels humiliating.
3. He cannot calibrate his own market value. He does not know if he is a Division I, Division II, or NAIA prospect. No one has ever told him.

## Behavioral Patterns
- Spends 2-3 hours per day on TikTok and Instagram consuming sport, lifestyle, and motivation content. This is habituated, reflexive behavior — not intentional research.
- Re-watches position-specific tactical breakdowns and college athlete day-in-the-life content multiple times if it resonates.
- Shares content to his WhatsApp group with teammates when it speaks to a shared aspiration ("this could be us").
- Has never filled out a form for a scholarship platform. Has downloaded 2-3 apps and deleted them within a week due to onboarding friction.
- Decision-making style: intuitive and identity-driven — he gravitates toward content that confirms who he believes he is becoming.

## Jobs-To-Be-Done
> **When** I am unwinding after training and scrolling my phone, **I want to** encounter content that feels like it was made for someone exactly like me, **so I can** stay engaged long enough to discover that a US scholarship pathway is something I could actually pursue.

1. JTBD-1: When I am unsure whether I am good enough for a US program, I want to watch content about athletes from my background who succeeded, so I can build the belief that this is possible for me.
2. JTBD-2: When I watch a video about a specific university's culture or football program, I want the platform to silently learn what kinds of environments appeal to me, so I can eventually be matched to options that actually fit — without filling out a single form.
3. JTBD-3: When I re-watch a tactical video about a specific playing style, I want that signal to communicate my position and football intelligence to whoever is on the other side of this platform, so I can be found by the right coach without having to self-promote.

## Emotional Journey
| Phase | Emotion | Trigger |
|-------|---------|---------|
| Before use | Invisible and uncertain | No one in his network has a US scholarship roadmap. He does not know if he is on anyone's radar. |
| During use (first session) | Mildly curious, pleasantly surprised | The content looks and feels like content he already watches. No form, no friction. He keeps scrolling. |
| During use (week 2-3) | Engaged, occasionally excited | The reel starts reflecting his interests back at him — tactical content, D1 environments, academic campus life. |
| After use (month 1+) | Quietly hopeful | The platform has become part of his routine. He senses — without being told — that something is being built on his behalf. |

## Scenario
> It is 9:30 PM on a Thursday. Gabriel has just showered after a 90-minute training session. He is lying on his bed, phone propped on his chest, scrolling through the Catch Home Viewer reel. A 58-second video appears — a day in the life of a Colombian central midfielder on a full athletic scholarship at a mid-sized university in North Carolina. Gabriel watches it all the way through twice. He saves it. He screenshots the university name and pastes it into Google. He does not know that three signals — 100% watch time, a re-watch, and a save — have just confirmed three strategic hypotheses about his academic preference tier, geographic openness, and position identity. The Profiling AI OS has updated his Full Athlete Profile. Ivan will see him in the Scout Sheet by Monday morning.

## Design Implications
- The Home Viewer must feel indistinguishable from TikTok or Instagram Reels in interaction paradigm — any deviation from the native vertical scroll pattern will read as "wrong" and reduce session length.
- Zero onboarding forms. Any screen that asks for data before the reel starts is a drop-off event for this persona.
- Video buffering and load time are critical. Gabriel is on 4G and an older Android device. The player must preload aggressively and degrade gracefully on slow connections.
- Content must include representation signals: athletes from non-US backgrounds, non-English-speaking home countries, and non-elite feeder schools. Gabriel will not see himself in content about prep school athletes from California.
- The platform must never make its profiling mechanic visible to Gabriel. Transparency about data collection could feel invasive and break trust. The implicit contract must be: "we show you great content" — full stop.

## Signals for Other Agents
| Agent | Signal |
|-------|--------|
| **PO** | Write user stories for: zero-friction onboarding (no form before first reel), high-performance video playback on mid-range Android, content representation diversity as a content acquisition KPI, save/share as first-class engagement actions. |
| **Architect** | Gabriel's device profile demands: adaptive bitrate streaming, <3s first-frame load on 4G, minimal APK size, offline-capable save function. API payloads for engagement events must be lightweight and async — do not block the UI thread for analytics writes. |
| **PjM** | This persona's behavioral assumptions (2-3h daily scroll, re-watch behavior, zero-form tolerance) are HIGH-RISK assumptions if unvalidated. Recommend: intercept interviews with 5-8 student-athletes in target markets (Brazil, Nigeria, Colombia, Mexico) before build freeze. |

---

*Validated against Empathy Validation Checklist: PASS — Named and vivid, goal-driven, pain-grounded, behaviorally distinct, context-rich, JTBD defined, testable, not a demographic bucket.*
