---
name: interview-script
description: Skill for generating structured user interview scripts that validate buyer personas in the real world.
---

## Interview Design Philosophy
A validation interview is NOT a feature request session. The goal is to confirm or deny the assumptions baked into each persona. Every question must map to a specific hypothesis.

## Interview Structure
1. **Warm-Up (5 min)**: Build rapport, understand their world — no product talk
2. **Core Exploration (20-30 min)**: Probe the behaviors and pains we captured
3. **Validation (10-15 min)**: Present specific persona assumptions and ask for reaction
4. **Closing (5 min)**: Meta-questions and referrals

## Question Design Rules
- **Warm-Up**: Open, past-tense, behavioral ("Tell me about the last time you...")
- **Core**: Start broad, then narrow ("What does your typical process look like?" → "Where does it break down?")
- **Validation**: Show, don't tell ("We think [X] is a problem for people like you — is that true for you?")
- **NEVER** ask leading questions ("Don't you find it frustrating when...?")
- **NEVER** ask about future behavior ("Would you use...?") — ask about past behavior instead

## What We Are Validating
For each question, the interviewer must know:
- The hypothesis being tested
- What a CONFIRMING answer looks like
- What a DISCONFIRMING answer looks like

## Scoring Guidance
After each interview session, score each hypothesis:
- ✅ Confirmed: interviewee independently raised the issue
- ⚠️ Weak signal: acknowledged when prompted
- ❌ Disconfirmed: directly contradicted or irrelevant

## Output Format
Respond with valid JSON only (```json code fences).
Include one interview section per persona, plus shared interviewer guidelines and a scoring rubric.
