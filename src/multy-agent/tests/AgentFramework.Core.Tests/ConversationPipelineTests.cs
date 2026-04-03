using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Events;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

public class ConversationPipelineTests
{
    private const string TestDataPath = "TestData/UxPersonaRole.md";

    private static UxPersona CreateAgent()
    {
        var markdown = File.ReadAllText(TestDataPath);
        return UxPersonaFactory.Create(markdown);
    }

    // --- Step 1: First interaction builds system + user messages ---

    [Fact]
    public async Task Step1_BuildsSystemPromptWithRole_OnFirstInteraction()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        // System message is first in conversation
        Assert.Equal(MessageRole.System, agent.Conversation.Messages[0].Role);
        Assert.Contains("Clara Mendes", agent.Conversation.Messages[0].Content);
        Assert.Contains("Senior UX Researcher", agent.Conversation.Messages[0].Content);
    }

    [Fact]
    public async Task Step1_BuildsUserPromptWithStepInstructions()
    {
        var agent = CreateAgent();
        agent.StartSession("Build personas for recruiting app");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        // User message is second in conversation
        Assert.Equal(MessageRole.User, agent.Conversation.Messages[1].Role);
        Assert.Contains("Define objective for agent", agent.Conversation.Messages[1].Content);
        Assert.Contains("Build personas for recruiting app", agent.Conversation.Messages[1].Content);
    }

    [Fact]
    public async Task Step1_AssistantResponseIsRecorded()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        // Assistant response is third
        Assert.Equal(MessageRole.Assistant, agent.Conversation.Messages[2].Role);
        Assert.Equal("Objective defined", agent.Conversation.Messages[2].Content);
    }

    [Fact]
    public async Task Step1_MapsObjectiveToSession()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        Assert.Equal("Build personas for college athletic recruiting platform", agent.Session!.Checkpoint.SessionObjective);
    }

    // --- Step 2: Conversation accumulates ---

    [Fact]
    public async Task Step2_DoesNotDuplicateSystemPrompt()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client); // Step 1
        await agent.ExecuteNextStepAsync(builder, client); // Step 2

        var systemMessages = agent.Conversation.Messages.Where(m => m.Role == MessageRole.System).ToList();
        Assert.Single(systemMessages);
    }

    [Fact]
    public async Task Step2_ConversationHasFullHistory()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client); // Step 1: system + user + assistant
        await agent.ExecuteNextStepAsync(builder, client); // Step 2: user + assistant

        // system(1) + user(1) + assistant(1) + user(2) + assistant(2) = 5
        Assert.Equal(5, agent.Conversation.Messages.Count);
    }

    [Fact]
    public async Task Step2_SendsFullHistoryToChatClient()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);
        await agent.ExecuteNextStepAsync(builder, client);

        // The client received all messages accumulated so far when step 2 was called
        Assert.True(client.LastReceivedMessageCount >= 4); // system + user1 + assistant1 + user2
    }

    [Fact]
    public async Task Step2_UserPromptIncludesSessionObjective()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client); // Step 1
        await agent.ExecuteNextStepAsync(builder, client); // Step 2

        // Step 2 user message (index 3) should reference the objective set in step 1
        var step2UserMsg = agent.Conversation.Messages[3];
        Assert.Contains("Build personas for college athletic recruiting platform", step2UserMsg.Content);
    }

    // --- Domain events still raised ---

    [Fact]
    public async Task ConversationPipeline_RaisesDomainEvents()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        Assert.Equal(2, agent.DomainEvents.Count);
        Assert.IsType<StepStarted>(agent.DomainEvents[0]);
        Assert.IsType<StepCompleted>(agent.DomainEvents[1]);
    }

    // --- Gate failure ---

    [Fact]
    public async Task ConversationPipeline_GateFailed_StillRecordsAssistantMessage()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient(failGate: true);

        await agent.ExecuteNextStepAsync(builder, client);

        // Assistant message is still recorded even on failure
        Assert.Equal(3, agent.Conversation.Messages.Count);
        Assert.Equal(MessageRole.Assistant, agent.Conversation.Messages[2].Role);
        Assert.Equal(0, agent.CurrentStepIndex); // did not advance
    }

    // --- Full pipeline ---

    [Fact]
    public async Task FullPipeline_AccumulatesConversation()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        var results = await agent.ExecuteAllStepsAsync(builder, client);

        Assert.Equal(5, results.Count);
        Assert.True(agent.IsCompleted);
        // system(1) + 5*(user + assistant) = 11
        Assert.Equal(11, agent.Conversation.Messages.Count);
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

        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            LastReceivedMessageCount = messages.Count;

            StepResult result = step.StepNumber switch
            {
                1 => new RehydrateResult(
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

                5 => new RelayResult(
                    Output: "Relay emitted",
                    GateSatisfied: !_failGate,
                    InputTokens: 2000,
                    OutputTokens: 5000),

                _ => new StepResult($"Step {step.StepNumber}", !_failGate),
            };

            return Task.FromResult(result);
        }
    }
}
