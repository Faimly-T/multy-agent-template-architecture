using AgentFramework.CodePipeline;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Core.Tests.TestHelpers;
using AgentFramework.Domain.UxAgent;
using AgentFramework.Infrastructure.Repositories;

namespace AgentFramework.Core.Tests;

internal static class TestSteps
{
    private const string SkillsBasePath  = "TestData/Skills";
    private const string RolePath        = "TestData/UxAgentRole.md";
    private const string AgentConfigPath = "TestData/ux-agent-config.json";

    // ── Intent constants ──────────────────────────────────────────────────────

    /// <summary>General-purpose intent for pipeline tests that don't target a specific scenario.</summary>
    public const string DefaultIntent =
        "Build buyer personas for a college athletic recruiting platform.";

    /// <summary>Scholarship scenario intent used by <c>PersonaWorkshopPipelineTests</c>.</summary>
    public const string ScholarshipIntent =
        "I want to build a system to search and profile potential leads for athlete student scholarships.";

    // ── Skill resolver ────────────────────────────────────────────────────────

    /// <summary>Flat-file skill resolver pointing at the test data Skills folder.</summary>
    public static ISkillResolver DefaultResolver() => new FlatFileSkillResolver(SkillsBasePath);

    // ── Repository ────────────────────────────────────────────────────────────

    /// <summary>
    /// JSON-file repository that loads agent config and handler instructions from
    /// <c>TestData/ux-agent-config.json</c>. Use with
    /// <c>UxAgent.BuildAsync(config, resolver, repo)</c> to exercise the full repository path.
    /// </summary>
    public static IUxAgentRepository DefaultRepository() =>
        new JsonUxAgentRepository(AgentConfigPath);

    // ── Role helper ───────────────────────────────────────────────────────────

    /// <summary>Parses the default test role from markdown.</summary>
    public static RoleDefinition DefaultRole() =>
        RoleParser.ParseFromMarkdown(File.ReadAllText(RolePath));

    // ── Config DTO (for the config-driven / integration path) ─────────────────

    public static AgentConfig DefaultUxConfig(RoleDefinition role) =>
        UxAgentDefaults.Config(role);

    // ── Raw steps (for unit tests that construct steps directly) ─────────────

    public static AgentStep[] DefaultSteps(IStepPromptLayer? stepContext = null)
    {
        var ctx = stepContext ?? PromptContext.Empty;
        return
        [
            new KickoffStep(ctx, 1, "Session Objective. Parse product description.",          new Gate("Objective confirmed")),
            new CaptureStep(ctx, 2, "Hunt for user types · goals · pain points · patterns.",  new Gate("≥3 user-type islands")),
            new OrganizeStep(ctx, 3, "Cluster by person → proto-persona.",                    new Gate("2-5 ranked candidates")),
            new DistillStep(ctx,  4, "Produce Persona Cards per template. JTBD for each.",    new Gate("All → Card or Concern")),
            new ExpressStep(ctx,  5, "Write cards. Emit relay. Record token usage.",           new Gate("Session + Cards logged")),
        ];
    }

    public static StepPipeline DefaultPipeline(IStepPromptLayer? stepContext = null) =>
        new StepPipeline(DefaultSteps(stepContext));
}
