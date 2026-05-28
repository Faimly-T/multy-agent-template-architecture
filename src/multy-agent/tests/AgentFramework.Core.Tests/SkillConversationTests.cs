using System.Text.Json;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Core.Agent.Steps.CODESteps;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

public class SkillConversationTests
{
    private const string TestDataPath = "TestData/UxPersonaRole.md";
    private static readonly SessionMarkFilePaths TestMarkFilePaths = new("UX", "outputs/contextAgent");

    private static UxPersona CreateAgent()
    {
        var markdown = File.ReadAllText(TestDataPath);
        var role = RoleParser.ParseFromMarkdown(markdown);
        IStepPromptLayer ctx = ((IAgentPromptLayer)PromptContext.Empty).WithRole(role);
        return new UxPersona(role, TestSteps.DefaultSteps(ctx), "test-proj", TestMarkFilePaths);
    }

    // --- Skill loading ---

    [Fact]
    public void Skill_FromMd_ParsesNameAndDescription()
    {
        var md = File.ReadAllText("TestData/Skills/Kickoff-context.md");

        var skill = SkillParser.ParseFromMarkdown(md);

        Assert.Equal("Kickoff-context", skill.Name);
        Assert.Equal("Define objective for agent and reconstruct session from prior state.", skill.Description);
        Assert.Contains("session checkpoint", skill.Instructions);
    }

    // --- JSON schema in messages ---

    [Fact]
    public async Task Step1_MessageContainsJsonSchema()
    {
        // Chain uses handler-level calls; schema is internal. Assistant message has synthesis JSON output.
        var agent = CreateAgent();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client);

        var assistantMsg = agent.ConversationMessages.First(m => m.Role == MessageRole.Assistant);
        Assert.Contains("sessionObjective", assistantMsg.Content);
        Assert.Contains("gateSatisfied", assistantMsg.Content);
    }

    [Fact]
    public async Task Step2_MessageContainsIslandsSchema()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client); // step 1
        await agent.ExecuteNextStepAsync(client); // step 2

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
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client);
        await agent.ExecuteNextStepAsync(client);
        await agent.ExecuteNextStepAsync(client);

        var step3Msg = agent.ConversationMessages
            .Where(m => m.Role == MessageRole.User)
            .Skip(2).First();
        Assert.Contains("organizedIslands", step3Msg.Content);
        Assert.Contains("decisions", step3Msg.Content);
    }

    // --- Step 1 tracking message ---

    [Fact]
    public async Task Step1_UserMessageContainsStepNumber()
    {
        // KickoffStep records a minimal tracking message: "## Step 1: KickoffStep"
        var agent = CreateAgent();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client);

        var userMsg = agent.ConversationMessages.First(m => m.Role == MessageRole.User);
        Assert.Contains("Step 1", userMsg.Content);
    }

    // --- JSON schema in user content for non-kickoff steps ---

    [Fact]
    public async Task Step2_UserMessageContainsJsonSchema()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client); // step 1
        await agent.ExecuteNextStepAsync(client); // step 2

        var step2Msg = agent.ConversationMessages
            .Where(m => m.Role == MessageRole.User)
            .Skip(1).First();
        Assert.Contains("Respond ONLY", step2Msg.Content);
    }

    // --- Session state flows through conversation ---

    [Fact]
    public async Task Step2_MessageContainsObjectiveFromStep1()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client);
        await agent.ExecuteNextStepAsync(client);

        var step2Msg = agent.ConversationMessages
            .Where(m => m.Role == MessageRole.User)
            .Skip(1).First();
        Assert.Contains("Build personas for college athletic recruiting platform", step2Msg.Content);
    }

    [Fact]
    public async Task Step3_MessageContainsIslandsFromStep2()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client);
        await agent.ExecuteNextStepAsync(client);
        await agent.ExecuteNextStepAsync(client);

        var step3Msg = agent.ConversationMessages
            .Where(m => m.Role == MessageRole.User)
            .Skip(2).First();
        Assert.Contains("ISL-001", step3Msg.Content);
        Assert.Contains("Student athlete", step3Msg.Content);
    }

    // --- System prompt includes role identity ---

    [Fact]
    public async Task SystemPrompt_ContainsRoleIdentity()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client);

        var sysMsg = agent.ConversationMessages.First(m => m.Role == MessageRole.System);
        Assert.Contains("Clara Mendes", sysMsg.Content);
        Assert.Contains("Senior UX Researcher", sysMsg.Content);
    }

    // --- Full pipeline ---

    [Fact]
    public async Task FullPipeline_WithSkills_MapsAllStepsToSession()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient();

        var results = await agent.ExecuteAllStepsAsync(client);

        Assert.Equal(5, results.Count);
        Assert.True(agent.IsCompleted);
        Assert.Equal("Build personas for college athletic recruiting platform", agent.Session!.CurrentCheckpoint!.SessionObjective);
        Assert.Equal(3, agent.Session.Islands.Count);
        Assert.Single(agent.Decisions);
        Assert.Single(agent.Deliverables);
        Assert.Equal(7000, agent.Session.CurrentCheckpoint!.TokensConsumption.TotalTokens);
    }

    // --- Fakes ---

    private class FakeChatClient : IChatClient
    {
        public Task<TResult> SendHandlerAsync<TResult>(
            IReadOnlyList<ChatMessage> messages, string jsonSchema,
            Func<JsonElement, TResult> parse, CancellationToken ct = default)
        {
            var json = jsonSchema.Contains("triaged")
                ? """{"triaged":[]}"""
                : """{"sessionObjective":"Build personas for college athletic recruiting platform","narrativeBridge":"Initial session — no prior context.","isInitialSession":true,"stalenessWarning":null,"gateSatisfied":true}""";
            return Task.FromResult(parse(JsonDocument.Parse(json).RootElement));
        }

        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            StepResult result = step.StepNumber switch
            {
                1 => new KickoffResult(
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
