# Persona: Luciana Ferreira
**Type**: Secondary
**Created**: 2026-03-28 | **Session**: #1

---

## Quick Profile
| Field | Value |
|-------|-------|
| **Role / Occupation** | Secondary school teacher and mother of Gabriel (PC-1), age 44 |
| **Tech Comfort** | Medium — comfortable with WhatsApp, Facebook, and mobile banking; unfamiliar with app-based data products |
| **Context of Use** | Mobile, home Wi-Fi, late evenings. First encounters Catch via a link Gabriel shares. Returns to the platform (or a landing page) to investigate legitimacy independently. |
| **Key Behavior** | Researches anything involving her son's future with high scrutiny. Asks direct questions. Requires evidence of legitimacy before granting permission. |

## Photo Prompt
> A composed, intelligent woman in her early 40s sitting at a kitchen table with her phone in hand, reading a screen with visible concentration. A school workbook is open beside her. She is not alarmed — she is assessing. Her expression is thoughtful and protective rather than hostile.

---

## Goals
1. **Primary goal**: Verify that Catch is a legitimate, safe, and beneficial pathway for her son — not a data harvesting scheme, a scam, or an unrealistic promise that will distract him from local academic commitments.
2. Understand clearly what the platform does, what data it collects about her minor child, and who has access to it.
3. If the platform is legitimate, become an active enabler — someone who encourages and facilitates Gabriel's engagement rather than blocking it.

## Pain Points
1. **Core frustration**: The platform is opaque. Gabriel is excited about it, but when she asks "what is it actually doing?", neither Gabriel nor the app can give her a satisfying answer. The implicit, zero-form design that delights Gabriel is alarming to Luciana — she cannot see what is being built.
2. She has seen other "scholarship platforms" promise opportunities that were either scams or unreachable for someone without US connections and money. Her skepticism is earned.
3. There is no parent-facing communication from the platform. She is expected to either trust it blindly or block it. Neither is acceptable to her.

## Behavioral Patterns
- When her child shows interest in a platform she does not know, she researches it independently — Googles the company name, looks for news articles, checks for reviews.
- Joins relevant Facebook groups (e.g., "Scholarships Abroad for Brazilian Students") to ask other parents about their experience.
- Makes a yes/no decision within 1-2 weeks of first exposure. If the platform has not earned her trust by then, she discourages Gabriel's continued use.
- Decision-making style: deliberate, evidence-based, high-trust threshold — but not closed-minded. She wants to be convinced, not bypassed.

## Jobs-To-Be-Done
> **When** my son shows me a new platform that is tracking his behavior to build a profile, **I want to** access a clear and honest explanation of what data is collected, who can see it, and what happens next, **so I can** make an informed decision about whether this is safe and beneficial for him.

1. JTBD-1: When I am researching whether Catch is legitimate, I want to find credible third-party validation (press coverage, university partner logos, testimonials from families who have gone through the process), so I can satisfy my skepticism with evidence rather than assumption.
2. JTBD-2: When I want to understand what the platform knows about Gabriel, I want to access a plain-language privacy summary written for parents — not a legal terms-of-service document, so I can understand the data relationship without needing a lawyer.
3. JTBD-3: When Gabriel has been active on the platform for a month, I want to receive some form of update or summary about what his profile suggests — even a general one, so I feel included in the process rather than excluded from my child's future planning.

## Emotional Journey
| Phase | Emotion | Trigger |
|-------|---------|---------|
| First exposure | Cautious, skeptical | Gabriel shows her the app. It looks like TikTok. She is immediately suspicious of why a scholarship tool looks like a social media app. |
| During independent research | Analytical, probing | She is actively looking for red flags. Every unanswered question raises the threat level. |
| If trust is earned | Relieved, supportive | She becomes an active advocate — asks Gabriel if he has been using it, reminds him to keep engaging. |
| If trust is not earned | Firm, blocking | She asks Gabriel to delete the app. His engagement drops to zero. The platform has lost its primary user. |

## Scenario
> Gabriel mentions Catch at dinner and shows his mother the reel. Luciana watches him scroll for a minute, asks "what does it do with what you watch?", and gets a shrug. That evening, she Googles "Catch scholarship app review." She finds the company's landing page and reads the About section. It mentions that student engagement data is used to build athlete profiles for university recruitment. She wants to know: who are the university partners? Is this data sold? What happens to the profile if Gabriel is not selected? She finds a FAQ page with a parent-specific section. It answers her main questions in plain language and links to a one-page privacy summary. It also lists three partner universities she has heard of. Her threat level drops. She tells Gabriel he can keep using it.

## Design Implications
- Luciana will never use the Home Viewer reel. Her touchpoint is a parent-facing landing page, FAQ, or trust-building content layer — which may be outside the MVP app itself but is critical to Gabriel's continued access.
- The product's privacy architecture must be explainable in plain language. "We track what you watch to understand what kind of university would suit you" is acceptable. "We aggregate behavioral metadata using hypothesis validation logic" is not.
- A parent notification or summary feature — even a quarterly email summary of a student's general profile direction — is a high-ROI trust feature for this persona.
- Explicit mention of data deletion or deactivation pathways reduces Luciana's perceived risk of "locking in" her son to something irrevocable.
- GDPR / COPPA / LGPD compliance signals (depending on target market) must be visible and non-buried in the trust-facing content. This persona knows how to look for them.

## Signals for Other Agents
| Agent | Signal |
|-------|--------|
| **PO** | Write user stories for: parent-facing trust content (privacy summary, FAQ, partner university list), optional parent notification / profile summary feature, explicit data deletion / deactivation pathway visible in onboarding. Note: if the student user base is under 18, COPPA (US) and LGPD (Brazil) parental consent requirements are not optional — they are legal gates. |
| **Architect** | If any user in scope is under 13 (US) or under 18 without parental consent (Brazil/EU), the data collection model must be reviewed against COPPA, GDPR-K, and LGPD requirements. This is a legal architecture question, not just a UX one. Age verification or consent flow may be required at onboarding. |
| **PjM** | Luciana's trust barrier is a silent conversion killer. If parent trust is not addressed, the platform will lose a significant portion of its primary users (minors) within the first 1-2 weeks of household exposure. Recommend: include a parent trust content sprint in the pre-launch plan and consider guerrilla research with parents in target markets to validate the trust FAQ content. |

---

*Validated against Empathy Validation Checklist: PASS — Named and vivid, goal-driven, pain-grounded, behaviorally distinct, context-rich, JTBD defined, testable, not a demographic bucket.*
