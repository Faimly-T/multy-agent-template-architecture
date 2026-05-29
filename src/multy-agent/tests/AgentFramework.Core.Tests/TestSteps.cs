using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Core.Agent.Steps.CODESteps;
using AgentFramework.Core.Tests.TestHelpers;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

internal static class TestSteps
{
    private const string SkillsBasePath = "TestData/Skills";

    // -------------------------------------------------------
    // Raw step construction (for unit tests that test steps directly)
    // -------------------------------------------------------

    public static AgentStep[] DefaultSteps(IStepPromptLayer? stepContext = null)
    {
        var ctx = stepContext ?? PromptContext.Empty;
        return
        [
            new KickoffStep(
                stepContext:  ctx,
                stepNumber:   1,
                instructions: "Session Objective. Parse product description.",
                gate:         new Gate("Objective confirmed")),

            new CaptureStep(
                stepContext:  ctx,
                stepNumber:   2,
                instructions: "Hunt for user types · goals · pain points · behavioral patterns.",
                gate:         new Gate("≥3 user-type islands")),

            new OrganizeStep(
                stepContext:  ctx,
                stepNumber:   3,
                instructions: "Cluster by person → proto-persona. Merge overlapping clusters.",
                gate:         new Gate("2-5 ranked candidates")),

            new DistillStep(
                stepContext:  ctx,
                stepNumber:   4,
                instructions: "Produce Persona Cards per template. JTBD for each.",
                gate:         new Gate("All → Card or Concern")),

            new ExpressStep(
                stepContext:  ctx,
                stepNumber:   5,
                instructions: "Write cards. Emit relay. Record token usage.",
                gate:         new Gate("Session + Cards + Relay + Token Usage logged")),
        ];
    }

    public static StepPipeline DefaultPipeline(IStepPromptLayer? stepContext = null) =>
        new StepPipeline(DefaultSteps(stepContext));

    // -------------------------------------------------------
    // Config-based construction (for agent-level tests)
    // -------------------------------------------------------

    public static CodePipelineConfig DefaultConfig() => new(
        Kickoff:  new StepSkillConfig(
            Instructions: "Session Objective. Parse product description.",
            Gate:         new Gate("Objective confirmed"),
            SkillNames:   ["Kickoff-context", "okr-kickoff-strategy"]),

        Capture:  new StepSkillConfig(
            Instructions: "Hunt for user types · goals · pain points · behavioral patterns.",
            Gate:         new Gate("≥3 user-type islands"),
            SkillNames:   ["autonomous-capture"]),

        Organize: new StepSkillConfig(
            Instructions: "Cluster by person → proto-persona. Merge overlapping clusters.",
            Gate:         new Gate("2-5 ranked candidates"),
            SkillNames:   ["strategic-organize"]),

        Distill:  new StepSkillConfig(
            Instructions: "Produce Persona Cards per template. JTBD for each.",
            Gate:         new Gate("All → Card or Concern"),
            SkillNames:   ["expert-distill"]),

        Express:  new StepSkillConfig(
            Instructions: "Write cards. Emit relay. Record token usage.",
            Gate:         new Gate("Session + Cards + Relay + Token Usage logged"),
            SkillNames:   ["express-relay"]));

    public static PipelineCode DefaultPipelineCode() =>
        new(new FlatFileSkillResolver(SkillsBasePath));

    public static UxPersonaConfig DefaultUxConfig(RoleDefinition role) => new(
        AgentId:       "ux-persona-test",
        ProjectId:     "test-proj",
        MarkFilePaths: new SessionMarkFilePaths("UX", "outputs/contextAgent"),
        Role:          role,
        Pipeline:      DefaultConfig());
}
