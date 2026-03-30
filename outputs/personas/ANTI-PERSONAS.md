# Anti-Personas: Catch Football Scholarship Pathway MVP Stage 1
**Created**: 2026-03-28 | **Session**: #1

---

## Purpose

Anti-personas define who the product is NOT for. They are as important as persona definitions because they:
1. Prevent scope creep driven by edge-case users whose needs would distort the product.
2. Give the team permission to say "no" to feature requests that serve these users.
3. Sharpen the product's value proposition by clarifying its boundaries.

---

## Anti-Persona 1: The Casual Sports Fan

**Who they are**: A 19-25 year old who downloaded the app because the content looked interesting but has no athletic career, no scholarship intent, and no connection to the football recruitment ecosystem.

**Why they are NOT a persona for this product**: Their engagement data (high watch-time on entertainment content) would corrupt the hypothesis validation model — the Profiling AI OS would generate false-positive athlete profiles. Their presence in the dataset degrades the Scout Sheet quality for Ivan.

**Design boundary**: The product should not optimize for discoverability or virality to general audiences. Content strategy should be hypothesis-first, not engagement-maximization. If a video performs well with casual fans but poorly with genuine prospects, it is a bad video for this system.

**If we design for them anyway**: Ivan's Scout Sheet fills with unqualified leads. His trust in the platform collapses within 60 days. The core value proposition is destroyed.

---

## Anti-Persona 2: The Already-Committed Athlete

**Who they are**: A student-athlete who has already signed a National Letter of Intent or committed to a university program — domestic or international.

**Why they are NOT a persona for this product**: Their engagement behavior signals post-decision content consumption, not pre-decision exploration. Their hypothesis confirmations would reflect past choices, not future match potential. They consume without intent to convert.

**Design boundary**: The 3-to-5 year recruitment window definition (from the brief) implicitly excludes this persona. Content and hypothesis frameworks should target the exploration and consideration phases — not confirmation or post-decision stages.

---

## Anti-Persona 3: The Professional or Semi-Professional Athlete

**Who they are**: An athlete 19+ who has played professionally or semi-professionally and is beyond the NCAA eligibility window.

**Why they are NOT a persona for this product**: NCAA eligibility rules create hard cutoffs around professional play and age. A profiling system built around NCAA pathways cannot serve this user — and attempting to do so would produce misleading match outputs.

**Design boundary**: If age or professional history signals are available, the Profiling AI OS should exclude or deprioritize these profiles from the Scout Sheet. Ivan should never receive a "Reality Match" for an ineligible athlete.

---

## Anti-Persona 4: The Recruiter Who Needs Video Production Tools

**Who they are**: A coach or scout who came to the platform expecting to host, produce, or distribute their own recruiting videos or send content directly to student-athletes.

**Why they are NOT a persona for this product**: Stage 1 is a passive profiling engine, not a two-way communication platform or video hosting service. The CSI ingests external content; it does not enable user-generated content from recruiters.

**Design boundary**: Do not build inbound recruiter content submission, two-way messaging, or coach-to-student outreach tools in Stage 1. These are Stage 2+ features. Designing for this persona now would bloat the architecture and distract from the core "Handshake" logic.

---

## Anti-Persona 5: The Academic-Only Student (No Athletic Component)

**Who they are**: A high-achieving student with no meaningful athletic involvement who is interested in US academic scholarships via merit or financial need pathways.

**Why they are NOT a persona for this product**: The Profiling AI OS is built around athletic hypothesis validation. Academic-only scholarships operate through entirely different pathways (SAT/ACT scores, GPA, essays, financial documentation) that are outside the scope of the CSI hypothesis taxonomy and the Scout Sheet output format.

**Design boundary**: Do not expand the hypothesis taxonomy to cover academic-only scholarship types in Stage 1. Doing so would require a fundamentally different profiling model and a different recruiter persona (Ivan is an athletic scout, not an academic admissions officer).

---

*Note: Anti-persona definitions should be reviewed at the start of each major feature sprint to ensure scope discipline is maintained.*
