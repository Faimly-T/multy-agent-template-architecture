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

public class ConversationPipelineTests
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

    // --- Step 1: First interaction builds system + user messages ---

    [Fact]
    public async Task Step1_BuildsSystemPromptWithRole_OnFirstInteraction()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client);

        // System message is first in conversation
        Assert.Equal(MessageRole.System, agent.ConversationMessages[0].Role);
        Assert.Contains("Clara Mendes", agent.ConversationMessages[0].Content);
        Assert.Contains("Senior UX Researcher", agent.ConversationMessages[0].Content);
    }

    [Fact]
    public async Task Step1_BuildsUserPromptWithStepInstructions()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client);

        // KickoffStep uses a minimal tracking user message ("## Step N: Name"); chain handlers carry session input
        Assert.Equal(MessageRole.User, agent.ConversationMessages[1].Role);
        Assert.Contains("KickoffStep", agent.ConversationMessages[1].Content);
    }

    [Fact]
    public async Task Step1_AssistantResponseIsRecorded()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client);

        // Assistant response is the chain's synthesis JSON output
        Assert.Equal(MessageRole.Assistant, agent.ConversationMessages[2].Role);
        Assert.Contains("sessionObjective", agent.ConversationMessages[2].Content);
    }

    [Fact]
    public async Task Step1_MapsObjectiveToSession()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client);

        Assert.Equal("Build personas for college athletic recruiting platform", agent.Session!.CurrentCheckpoint!.SessionObjective);
    }

    // --- Step 2: Conversation accumulates ---

    [Fact]
    public async Task Step2_DoesNotDuplicateSystemPrompt()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client); // Step 1
        await agent.ExecuteNextStepAsync(client); // Step 2

        var systemMessages = agent.ConversationMessages.Where(m => m.Role == MessageRole.System).ToList();
        Assert.Single(systemMessages);
    }

    [Fact]
    public async Task Step2_ConversationHasFullHistory()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client); // Step 1: system + user + assistant
        await agent.ExecuteNextStepAsync(client); // Step 2: user + assistant

        // system(1) + user(1) + assistant(1) + user(2) + assistant(2) = 5
        Assert.Equal(5, agent.ConversationMessages.Count);
    }

    [Fact]
    public async Task Step2_SendsFullHistoryToChatClient()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client);
        await agent.ExecuteNextStepAsync(client);

        // The client received all messages accumulated so far when step 2 was called
        Assert.True(client.LastReceivedMessageCount >= 2); // system + context-injection + current-user
    }

    [Fact]
    public async Task Step2_UserPromptIncludesSessionObjective()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client); // Step 1
        await agent.ExecuteNextStepAsync(client); // Step 2

        // Step 2 user message (index 3) should reference the objective set in step 1
        var step2UserMsg = agent.ConversationMessages[3];
        Assert.Contains("Build personas for college athletic recruiting platform", step2UserMsg.Content);
    }

    // --- Gate failure ---

    [Fact]
    public async Task ConversationPipeline_GateFailed_StillRecordsAssistantMessage()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient(failGate: true);

        await agent.ExecuteNextStepAsync(client);

        // Assistant message is still recorded even on failure
        Assert.Equal(3, agent.ConversationMessages.Count);
        Assert.Equal(MessageRole.Assistant, agent.ConversationMessages[2].Role);
        Assert.Equal(0, agent.Pipeline.CurrentStepIndex); // did not advance
    }

    // --- Full pipeline ---

    [Fact]
    public async Task FullPipeline_AccumulatesConversation()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient();

        var results = await agent.ExecuteAllStepsAsync(client);

        Assert.Equal(5, results.Count);
        Assert.True(agent.IsCompleted);
        // system(1) + 5*(user + assistant) = 11
        Assert.Equal(11, agent.ConversationMessages.Count);
    }

    // --- Fake chat client ---

    private class FakeChatClient : IChatClient
    {
        private readonly bool _failGate;
        public int LastReceivedMessageCount { get; private set; }

        public FakeChatClient(bool failGate = false)
        {
            _failGate = failGate;
        }

        public Task<TResult> SendHandlerAsync<TResult>(
            IReadOnlyList<ChatMessage> messages, string jsonSchema,
            Func<JsonElement, TResult> parse, CancellationToken ct = default)
        {
            var passed = !_failGate;
            var json = jsonSchema.Contains("triaged")
                ? """{"triaged":[]}"""
                : $$"""{"sessionObjective":"Build personas for college athletic recruiting platform","narrativeBridge":"Initial session.","isInitialSession":true,"stalenessWarning":null,"gateSatisfied":{{(passed ? "true" : "false")}}}""";
            return Task.FromResult(parse(JsonDocument.Parse(json).RootElement));
        }

        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            LastReceivedMessageCount = messages.Count;

            StepResult result = step.StepNumber switch
            {
                1 => new KickoffResult(
                    Output: "Objective defined",
                    GateSatisfied: !_failGate,
                    SessionObjective: "Build personas for college athletic recruiting platform"),

                2 => new CaptureResult(
                    Output: "Islands captured",
                    GateSatisfied: !_failGate,
                    Islands:
                    [
                        new("ISL-001", IslandType.UserType, "Student athlete", "product desc"),
                        new("ISL-002", IslandType.Stakeholder, "College coach", "product desc"),
                        new("ISL-003", IslandType.PainPoint, "No visibility", "interview", "ISL-001"),
                    ]),

                3 => new OrganizeResult(
                    Output: "Islands organized",
                    GateSatisfied: !_failGate,
                    OrganizedIslands:
                    [
                        new("ISL-001", IslandStatus.Organized),
                        new("ISL-002", IslandStatus.Organized),
                        new("ISL-003", IslandStatus.Discarded),
                    ],
                    Decisions: [new("DEC-001", "Merge pain into athlete persona", "Cleaner model")]),

                4 => new DistillResult(
                    Output: "Persona cards produced",
                    GateSatisfied: !_failGate,
                    DistilledIslands:
                    [
                        new("ISL-001", IslandStatus.Distilled),
                        new("ISL-002", IslandStatus.Distilled),
                    ],
                    Deliverables: [new("DEL-001", "outputs/personas/01-athlete.md", DeliverableStatus.Complete)]),

                5 => new ExpressResult(
                    Output: "Relay emitted",
                    GateSatisfied: !_failGate,
                    InputTokens: 2000,
                    OutputTokens: 5000,
                    Questions: []),

                _ => new StepResult($"Step {step.StepNumber}", !_failGate),
            };

            return Task.FromResult(result);
        }
    }
}
