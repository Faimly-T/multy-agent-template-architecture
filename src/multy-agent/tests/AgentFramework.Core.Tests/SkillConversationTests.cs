using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Core.Agent.Steps.CODESteps;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

public class SkillConversationTests
{
    private const string TestDataPath = "TestData/UxPersonaRole.md";

    private static UxPersona CreateAgent()
    {
        var markdown = File.ReadAllText(TestDataPath);
        var skills = TestSteps.DefaultSkills();
        return new UxPersona(RoleParser.ParseFromMarkdown(markdown), TestSteps.DefaultSteps(), skills);
    }

    // --- Skill loading ---

    [Fact]
    public void Skill_FromMd_ParsesNameAndDescription()
    {
        var md = File.ReadAllText("TestData/Skills/rehydrate-context.md");

        var skill = SkillParser.ParseFromMarkdown(md);

        Assert.Equal("rehydrate-context", skill.Name);
        Assert.Equal("Define objective for agent and reconstruct session from prior state.", skill.Description);
        Assert.Contains("session checkpoint", skill.Instructions);
    }

    [Fact]
    public void WithSkill_AttachesSkillToStep()
    {
        var agent = CreateAgent();

        Assert.All(agent.Steps, step => Assert.NotNull(step.Skill));
        Assert.Equal("rehydrate-context", agent.Steps[0].Skill!.Name);
        Assert.Equal("autonomous-capture", agent.Steps[1].Skill!.Name);
        Assert.Equal("strategic-organize", agent.Steps[2].Skill!.Name);
        Assert.Equal("expert-distill", agent.Steps[3].Skill!.Name);
        Assert.Equal("express-relay", agent.Steps[4].Skill!.Name);
    }

    [Fact]
    public void AttachSkill_PreservesStepNumbersAndGates()
    {
        var agent = CreateAgent();
        var markdownRehydrate = File.ReadAllText("TestData/Skills/rehydrate-context.md");

        // Re-attach skill via internal AttachSkill — step number and gate are preserved
        agent.Steps[0].AttachSkill(SkillParser.ParseFromMarkdown(markdownRehydrate));

        Assert.Equal(5, agent.Steps.Count);
        Assert.Equal(1, agent.Steps[0].StepNumber);
        Assert.Equal("Objective confirmed", agent.Steps[0].Gate.Description);
    }

    // --- JSON schema in messages ---

    [Fact]
    public async Task Step1_MessageContainsJsonSchema()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        var userMsg = agent.ConversationMessages.First(m => m.Role == MessageRole.User);
        Assert.Contains("sessionObjective", userMsg.Content);
        Assert.Contains("gateSatisfied", userMsg.Content);
        Assert.Contains("Required Response Format", userMsg.Content);
    }

    [Fact]
    public async Task Step2_MessageContainsIslandsSchema()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client); // step 1
        await agent.ExecuteNextStepAsync(builder, client); // step 2

        var step2Msg = agent.ConversationMessages
            .Where(m => m.Role == MessageRole.User)
            .Skip(1).First();
        Assert.Contains("islands", step2Msg.Content);
        Assert.Contains("relatesToIslandId", step2Msg.Content);
    }

    [Fact]
    public async Task Step3_MessageContainsOrganizeSchema()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);
        await agent.ExecuteNextStepAsync(builder, client);
        await agent.ExecuteNextStepAsync(builder, client);

        var step3Msg = agent.ConversationMessages
            .Where(m => m.Role == MessageRole.User)
            .Skip(2).First();
        Assert.Contains("organizedIslands", step3Msg.Content);
        Assert.Contains("decisions", step3Msg.Content);
    }

    // --- Skill instructions in messages ---

    [Fact]
    public async Task Step1_MessageContainsSkillInstructions()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        var userMsg = agent.ConversationMessages.First(m => m.Role == MessageRole.User);
        Assert.Contains("Skill: rehydrate-context", userMsg.Content);
        Assert.Contains("session checkpoint", userMsg.Content);
        Assert.Contains("Synthesize Session Objective", userMsg.Content);
    }

    [Fact]
    public async Task Step2_MessageContainsCaptureSkill()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");

        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);
        await agent.ExecuteNextStepAsync(builder, client);

        var step2Msg = agent.ConversationMessages
            .Where(m => m.Role == MessageRole.User)
            .Skip(1).First();
        Assert.Contains("Skill: autonomous-capture", step2Msg.Content);
        Assert.Contains("Pass 1 — Objective Decomposition", step2Msg.Content);
    }

    // --- Session state flows through conversation ---

    [Fact]
    public async Task Step2_MessageContainsObjectiveFromStep1()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);
        await agent.ExecuteNextStepAsync(builder, client);

        var step2Msg = agent.ConversationMessages
            .Where(m => m.Role == MessageRole.User)
            .Skip(1).First();
        Assert.Contains("Build personas for college athletic recruiting platform", step2Msg.Content);
    }

    [Fact]
    public async Task Step3_MessageContainsIslandsFromStep2()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);
        await agent.ExecuteNextStepAsync(builder, client);
        await agent.ExecuteNextStepAsync(builder, client);

        var step3Msg = agent.ConversationMessages
            .Where(m => m.Role == MessageRole.User)
            .Skip(2).First();
        Assert.Contains("ISL-001", step3Msg.Content);
        Assert.Contains("Student athlete", step3Msg.Content);
    }

    // --- System prompt includes JSON instruction ---

    [Fact]
    public async Task SystemPrompt_ContainsJsonInstruction()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        var sysMsg = agent.ConversationMessages.First(m => m.Role == MessageRole.System);
        Assert.Contains("valid JSON", sysMsg.Content);
    }

    // --- Full pipeline with skills ---

    [Fact]
    public async Task FullPipeline_WithSkills_MapsAllStepsToSession()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        var results = await agent.ExecuteAllStepsAsync(builder, client);

        Assert.Equal(5, results.Count);
        Assert.True(agent.IsCompleted);
        Assert.Equal("Build personas for college athletic recruiting platform", agent.Session!.Checkpoint.SessionObjective);
        Assert.Equal(3, agent.Session.Islands.Count);
        Assert.Single(agent.Session.Decisions);
        Assert.Single(agent.Session.Deliverables);
        Assert.Equal(7000, agent.Session.Checkpoint.TokensConsumption.TotalTokens);
    }

    // --- Fakes ---

    private class FakeChatClient : IChatClient
    {
        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            StepResult result = step.StepNumber switch
            {
                1 => new RehydrateResult(
                    Output: """{"sessionObjective":"Build personas for college athletic recruiting platform","narrativeBridge":"Initial session — no prior context.","isInitialSession":true,"stalenessWarning":null,"blockers":[],"gateSatisfied":true}""",
                    GateSatisfied: true,
                    SessionObjective: "Build personas for college athletic recruiting platform",
                    NarrativeBridge: "Initial session — no prior context.",
                    IsInitialSession: true),

                2 => new CaptureResult(
                    Output: """{"islands":[{"id":"ISL-001","type":"UserType","description":"Student athlete","source":"product desc"},{"id":"ISL-002","type":"Stakeholder","description":"College coach","source":"product desc"},{"id":"ISL-003","type":"PainPoint","description":"No visibility","source":"interview","relatesToIslandId":"ISL-001"}],"gateSatisfied":true}""",
                    GateSatisfied: true,
                    Islands:
                    [
                        new("ISL-001", IslandType.UserType, "Student athlete", "product desc"),
                        new("ISL-002", IslandType.Stakeholder, "College coach", "product desc"),
                        new("ISL-003", IslandType.PainPoint, "No visibility", "interview", "ISL-001"),
                    ]),

                3 => new OrganizeResult(
                    Output: """{"organizedIslands":[{"islandId":"ISL-001","newStatus":"Organized"},{"islandId":"ISL-002","newStatus":"Organized"},{"islandId":"ISL-003","newStatus":"Discarded"}],"decisions":[{"id":"DEC-001","description":"Merge pain into athlete","impact":"Cleaner model"}],"gateSatisfied":true}""",
                    GateSatisfied: true,
                    OrganizedIslands:
                    [
                        new("ISL-001", IslandStatus.Organized),
                        new("ISL-002", IslandStatus.Organized),
                        new("ISL-003", IslandStatus.Discarded),
                    ],
                    Decisions: [new("DEC-001", "Merge pain into athlete", "Cleaner model")]),

                4 => new DistillResult(
                    Output: """{"distilledIslands":[{"islandId":"ISL-001","newStatus":"Distilled"},{"islandId":"ISL-002","newStatus":"Distilled"}],"deliverables":[{"deliverableId":"DEL-001","path":"outputs/personas/01-athlete.md","status":"Complete"}],"gateSatisfied":true}""",
                    GateSatisfied: true,
                    DistilledIslands:
                    [
                        new("ISL-001", IslandStatus.Distilled),
                        new("ISL-002", IslandStatus.Distilled),
                    ],
                    Deliverables: [new("DEL-001", "outputs/personas/01-athlete.md", DeliverableStatus.Complete)]),

                5 => new ExpressResult(
                    Output: """{"inputTokens":2000,"outputTokens":5000,"gateSatisfied":true}""",
                    GateSatisfied: true,
                    InputTokens: 2000,
                    OutputTokens: 5000,
                    Questions: []),

                _ => new StepResult($"Step {step.StepNumber}", true),
            };

            return Task.FromResult(result);
        }
    }
}
