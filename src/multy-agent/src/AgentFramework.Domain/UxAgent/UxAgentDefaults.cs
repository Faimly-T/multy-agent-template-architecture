using AgentFramework.CodePipeline;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Domain.UxAgent;

/// <summary>
/// Default <see cref="AgentConfig"/> for the UX research agent.
/// Pure data — no assembly logic. Load from here or override individual fields, then pass to
/// <see cref="UxAgent.BuildAsync"/>.
///
/// <code>
/// var agent = await UxAgent.BuildAsync(UxAgentDefaults.Config(role), resolver);
/// </code>
/// </summary>
public static class UxAgentDefaults
{
    public static AgentConfig Config(
        RoleDefinition role,
        string         agentId   = "ux-agent",
        string         projectId = "ux-proj") => new(

        AgentId:   agentId,
        ProjectId: projectId,
        Role:      role,

        Kickoff: new StepConfig(
            Instructions: "Session Objective. Parse product description.",
            Gate:         new Gate("Objective confirmed"),
            SkillNames:   ["Kickoff-context", "okr-kickoff-strategy"]),

        Capture: new StepConfig(
            Instructions: "Hunt for user types · goals · pain points · behavioral patterns.",
            Gate:         new Gate("≥3 user-type islands"),
            SkillNames:   ["six-thinking-hats", "capture-strict-islands"]),

        Organize: new StepConfig(
            Instructions: "Cluster islands into semantic groups. Identify WHY each group belongs together. Assess readiness for Distill.",
            Gate:         new Gate("≥1 non-blocked group"),
            SkillNames:   ["strategic-organize"]),

        Distill: new StepConfig(
            Instructions: "For each group: define HOW decisions, link deliverables with purpose, raise open questions.",
            Gate:         new Gate("All groups distilled"),
            SkillNames:   ["expert-distill"]),

        Express: new StepConfig(
            Instructions: """
                Generate five HTML deliverables that express the full UX research session:
                1. Session Summary — 5-step journey narrative for stakeholders
                2. Buyer Persona Document — rich persona cards per user type
                3. Research Validation Guide — LLM deep-research prompts + validation points
                4. Interview Script — structured user interview guide per persona
                5. Next Session Guide — reflection questions + next-iteration prompt template
                Review all session questions and record token usage.
                """,
            Gate:      new Gate("All five HTML documents generated and questions reviewed"),
            SkillNames: ["express-relay", "express-as-html", "persona-document",
                         "research-validation-guide", "interview-script"]),

        Closed: new StepConfig(
            Instructions: "Validate session state. Seal the checkpoint with token totals and ClosedAt timestamp.",
            Gate:         new Gate("All checks passed"),
            SkillNames:   []));
}
