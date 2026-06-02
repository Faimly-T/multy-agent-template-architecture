using System.Text.Json;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.CodePipeline;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

public class ConversationPipelineTests
{
    private const string TestDataPath = "TestData/UxAgentRole.md";

    private static async Task<UxAgent> CreateAgentAsync()
    {
        return await UxAgent.BuildAsync(UxAgentDefaults.Config(TestSteps.DefaultRole()), TestSteps.DefaultResolver());
    }

    // --- Step 1: journal is recorded after execution ---

    [Fact]
    public async Task Step1_JournalIsRecorded()
    {
        var agent = await CreateAgentAsync();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client);

        Assert.NotEmpty(agent.GetStepJournal("KickoffStep"));
    }

    [Fact]
    public async Task Step1_JournalOutputContainsFinalJson()
    {
        var agent = await CreateAgentAsync();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client);

        var output = agent.GetStepJournal("KickoffStep")[^1].Content.Output;
        Assert.Contains("sessionObjective", output);
    }

    [Fact]
    public async Task Step1_MapsObjectiveToSession()
    {
        var agent = await CreateAgentAsync();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client);

        Assert.Equal("Build personas for college athletic recruiting platform", agent.Session!.CurrentCheckpoint!.SessionObjective);
    }

    // --- Step 2: both step journals recorded ---

    [Fact]
    public async Task Step2_BothStepJournalsRecorded()
    {
        var agent = await CreateAgentAsync();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client); // Step 1
        await agent.ExecuteNextStepAsync(client); // Step 2

        Assert.NotEmpty(agent.GetStepJournal("KickoffStep"));
        Assert.NotEmpty(agent.GetStepJournal("CaptureStep"));
    }

    [Fact]
    public async Task Step2_SendsFullHistoryToChatClient()
    {
        var agent = await CreateAgentAsync();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client);
        await agent.ExecuteNextStepAsync(client);

        Assert.True(client.LastReceivedMessageCount >= 2);
    }

    // --- Gate failure ---

    [Fact]
    public async Task GateFailed_JournalStillRecorded()
    {
        var agent = await CreateAgentAsync();
        var client = new FakeChatClient(failGate: true);

        await agent.ExecuteNextStepAsync(client);

        Assert.NotEmpty(agent.GetStepJournal("KickoffStep"));
        Assert.Equal(0, agent.Pipeline.CurrentStepIndex); // did not advance
    }

    // --- Full pipeline ---

    [Fact]
    public async Task FullPipeline_AllStepJournalsRecorded()
    {
        var agent = await CreateAgentAsync();
        var client = new FakeChatClient();

        var results = await agent.ExecuteAllStepsAsync(TestSteps.DefaultIntent, client);

        Assert.Equal(6, results.Count);
        Assert.True(agent.IsCompleted);
        Assert.NotEmpty(agent.GetStepJournal("KickoffStep"));
        Assert.NotEmpty(agent.GetStepJournal("CaptureStep"));
        Assert.NotEmpty(agent.GetStepJournal("OrganizeStep"));
        Assert.NotEmpty(agent.GetStepJournal("DistillStep"));
        // Verify all 6 step journals were recorded using the actual step names
        Assert.Equal(6, agent.JournalStepNames.Count);
        Assert.NotEmpty(agent.GetStepJournal("ClosedStep"));
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
            LastReceivedMessageCount = messages.Count;
            var passed = !_failGate;
            string json;
            if (jsonSchema.Contains("triaged"))
                json = """{"triaged":[]}""";
            else if (jsonSchema.Contains("groupDistillations"))
                json = passed
                    ? """{"groupDistillations":[{"groupId":"GRP-001","decisions":[{"id":"DEC-001","description":"Merge pain into athlete persona","impact":"Cleaner model"}],"deliverables":[{"deliverableId":"DEL-001","path":"outputs/personas/01-athlete.md","purpose":"Athlete card","status":"Complete"}],"questions":[]}],"distilledIslands":[{"islandId":"ISL-001","newStatus":"Distilled"},{"islandId":"ISL-002","newStatus":"Distilled"}],"gateSatisfied":true}"""
                    : """{"groupDistillations":[],"distilledIslands":[],"gateSatisfied":false}""";
            else if (jsonSchema.Contains("readiness"))
                json = passed
                    ? """{"groups":[{"id":"GRP-001","name":"Recruiting Group","islandIds":["ISL-001","ISL-002"],"whyTogether":"Both describe recruiting","readiness":"Ready","readinessNotes":null}]}"""
                    : """{"groups":[{"id":"GRP-001","name":"Recruiting Group","islandIds":["ISL-001","ISL-002"],"whyTogether":"Both describe recruiting","readiness":"NeedsCapture","readinessNotes":null}]}""";
            else if (jsonSchema.Contains("islandIds"))
                json = """{"groups":[{"id":"GRP-001","name":"Recruiting Group","islandIds":["ISL-001","ISL-002"],"whyTogether":"Both describe recruiting"}],"ungroupedIslandIds":["ISL-003"]}""";
            else if (jsonSchema.Contains("islands"))
                json = """{"islands":[{"id":"ISL-001","type":"UserType","description":"Student athlete seeking recruitment","source":"six-hats:yellow","relatesToIslandId":null},{"id":"ISL-002","type":"Stakeholder","description":"College coach evaluating talent","source":"six-hats:white","relatesToIslandId":null},{"id":"ISL-003","type":"PainPoint","description":"No visibility into recruiting process","source":"six-hats:black","relatesToIslandId":"ISL-001"}]}""";
            else if (jsonSchema.Contains("\"html\""))
                json = """{"html":"<html><body>Test Document</body></html>"}""";
            else if (jsonSchema.Contains("inputTokens"))
                json = """{"questions":[],"inputTokens":0,"outputTokens":0,"gateSatisfied":true}""";
            else
                json = $$"""{"sessionObjective":"Build personas for college athletic recruiting platform","narrativeBridge":"Initial session.","isInitialSession":true,"stalenessWarning":null,"gateSatisfied":{{(passed ? "true" : "false")}}}""";
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
