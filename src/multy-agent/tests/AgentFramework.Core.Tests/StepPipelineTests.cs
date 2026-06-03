using System.Text.Json;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.CodePipeline;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

public class StepPipelineTests
{
    private const string TestDataPath = "TestData/UxAgentRole.md";

    private static async Task<UxAgent> CreateAgentAsync()
    {
        return await UxAgent.BuildAsync(UxAgentDefaults.Config(TestSteps.DefaultRole()), TestSteps.DefaultResolver());
    }

    [Fact]
    public async Task UxAgent_Has6Steps()
    {
        var agent = await CreateAgentAsync();
        Assert.Equal(6, agent.Steps.Count);
    }

    [Fact]
    public async Task Steps_AreNumberedSequentially()
    {
        var agent = await CreateAgentAsync();
        for (int i = 0; i < agent.Steps.Count; i++)
        {
            Assert.Equal(i + 1, agent.Steps[i].StepNumber);
        }
    }

    [Fact]
    public async Task Step1_UsesKickoffContext()
    {
        var agent = await CreateAgentAsync();
        Assert.Contains("Kickoff-context", agent.Steps[0].SkillNames);
        Assert.Equal("Objective confirmed", agent.Steps[0].Gate.Description);
    }

    [Fact]
    public async Task Step2_UsesSixThinkingHatsAndCaptureStrict()
    {
        var agent = await CreateAgentAsync();
        Assert.Contains("six-thinking-hats",     agent.Steps[1].SkillNames);
        Assert.Contains("capture-strict-islands", agent.Steps[1].SkillNames);
        Assert.Equal("≥3 user-type islands", agent.Steps[1].Gate.Description);
    }

    [Fact]
    public async Task NewAgent_StartsAtStep0_NotCompleted()
    {
        var agent = await CreateAgentAsync();
        Assert.Equal(0, agent.Pipeline.CurrentStepIndex);
        Assert.False(agent.IsCompleted);
    }

    [Fact]
    public async Task ExecuteNextStep_AdvancesPipeline_WhenGatePasses()
    {
        var agent = await CreateAgentAsync();
        var client = new FakeChatClient(gateSatisfied: true);

        var result = await agent.ExecuteNextStepAsync(client);

        Assert.True(result.GateSatisfied);
        Assert.Equal(1, agent.Pipeline.CurrentStepIndex);
    }

    [Fact]
    public async Task ExecuteNextStep_DoesNotAdvancePipeline_WhenGateFails()
    {
        var agent = await CreateAgentAsync();
        var client = new FakeChatClient(gateSatisfied: false);

        var result = await agent.ExecuteNextStepAsync(client);

        Assert.False(result.GateSatisfied);
        Assert.Equal(0, agent.Pipeline.CurrentStepIndex);
    }

    [Fact]
    public async Task ExecuteAllSteps_RunsAllSteps_WhenAllGatesPass()
    {
        var agent = await CreateAgentAsync();
        var client = new FakeChatClient(gateSatisfied: true);

        var results = await agent.ExecuteAllStepsAsync(TestSteps.DefaultIntent, client);

        Assert.Equal(6, results.Count);
        Assert.True(agent.IsCompleted);
        Assert.All(results, r => Assert.True(r.GateSatisfied));
    }

    [Fact]
    public async Task ExecuteAllSteps_StopsAtFailedGate()
    {
        var agent = await CreateAgentAsync();
        var client = new FakeChatClient(failAtStep: 3);

        var results = await agent.ExecuteAllStepsAsync(TestSteps.DefaultIntent, client);

        Assert.Equal(3, results.Count);
        Assert.False(agent.IsCompleted);
        Assert.Equal(2, agent.Pipeline.CurrentStepIndex);
    }

    [Fact]
    public async Task ExecuteNextStep_ThrowsWhenAllCompleted()
    {
        var agent = await CreateAgentAsync();
        var client = new FakeChatClient(gateSatisfied: true);
        await agent.ExecuteAllStepsAsync(TestSteps.DefaultIntent, client);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => agent.ExecuteNextStepAsync(client));
    }

    private class FakeChatClient : IChatClient
    {
        private readonly bool _gateSatisfied;
        private readonly int? _failAtStep;

        public FakeChatClient(bool gateSatisfied = true, int? failAtStep = null)
        {
            _gateSatisfied = gateSatisfied;
            _failAtStep = failAtStep;
        }

        public Task<TResult> SendHandlerAsync<TResult>(
            IReadOnlyList<ChatMessage> messages, string jsonSchema,
            Func<JsonElement, TResult> parse, CancellationToken ct = default)
        {
            var passed = _failAtStep == 1 ? false : _gateSatisfied;
            string json;
            if (jsonSchema.Contains("triaged"))
                json = """{"triaged":[]}""";
            else if (jsonSchema.Contains("groupDistillations"))
            {
                var distillPassed = _failAtStep != 4 && _gateSatisfied;
                json = distillPassed
                    ? """{"groupDistillations":[{"groupId":"GRP-001","decisions":[{"id":"DEC-001","description":"Merge pain into athlete persona","impact":"Cleaner model"}],"deliverables":[{"deliverableId":"DEL-001","path":"outputs/personas/01-athlete.md","purpose":"Athlete card","status":"Complete"}],"questions":[]}],"distilledIslands":[{"islandId":"ISL-001","newStatus":"Distilled"},{"islandId":"ISL-002","newStatus":"Distilled"}],"gateSatisfied":true}"""
                    : """{"groupDistillations":[],"distilledIslands":[],"gateSatisfied":false}""";
            }
            else if (jsonSchema.Contains("readiness"))
            {
                // Fail gate for step 3 when failAtStep==3: return Blocked (gate = ≥1 non-blocked group)
                var readiness = (_failAtStep == 3 || !_gateSatisfied) ? "Blocked" : "Ready";
                json = $$"""{"groups":[{"id":"GRP-001","name":"Recruiting Group","islandIds":["ISL-001","ISL-002"],"whyTogether":"Both describe recruiting","readiness":"{{readiness}}","readinessNotes":null}]}""";
            }
            else if (jsonSchema.Contains("islandIds"))
                json = """{"groups":[{"id":"GRP-001","name":"Recruiting Group","islandIds":["ISL-001","ISL-002"],"whyTogether":"Both describe recruiting"}],"ungroupedIslandIds":["ISL-003"]}""";
            else if (jsonSchema.Contains("islands"))
                json = """{"islands":[{"id":"ISL-001","type":"UserType","description":"Student athlete","source":"six-hats:yellow","relatesToIslandId":null},{"id":"ISL-002","type":"Stakeholder","description":"College coach","source":"six-hats:white","relatesToIslandId":null},{"id":"ISL-003","type":"PainPoint","description":"No visibility","source":"six-hats:black","relatesToIslandId":"ISL-001"}]}""";
            // UxExpress content handlers — LLM returns HTML directly
            else if (jsonSchema.Contains("\"html\""))
                json = """{"html":"<html><body>Test Document</body></html>"}""";
            // UxQuestionReviewHandler — tokens + question statuses
            else if (jsonSchema.Contains("inputTokens"))
                json = """{"questions":[],"inputTokens":0,"outputTokens":0,"gateSatisfied":true}""";
            else
                json = $$"""{"sessionObjective":"Build personas for recruiting platform","narrativeBridge":"Initial session.","isInitialSession":true,"stalenessWarning":null,"gateSatisfied":{{(passed ? "true" : "false")}}}""";
            return Task.FromResult(parse(JsonDocument.Parse(json).RootElement));
        }

        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            var passed = _failAtStep.HasValue
                ? step.StepNumber != _failAtStep.Value
                : _gateSatisfied;

            StepResult result = step.StepNumber switch
            {
                1 => new KickoffResult(
                    Output: "Objective defined",
                    GateSatisfied: passed,
                    SessionObjective: "Build personas for recruiting platform"),

                2 => new CaptureResult(
                    Output: "Islands captured",
                    GateSatisfied: passed,
                    Islands:
                    [
                        new("ISL-001", IslandType.UserType, "Student athlete", "product desc"),
                        new("ISL-002", IslandType.Stakeholder, "College coach", "product desc"),
                        new("ISL-003", IslandType.PainPoint, "No visibility", "interview", "ISL-001"),
                    ]),

                3 => new OrganizeResult(
                    Output: "Islands organized",
                    GateSatisfied: passed,
                    OrganizedIslands:
                    [
                        new("ISL-001", IslandStatus.Organized),
                        new("ISL-002", IslandStatus.Organized),
                        new("ISL-003", IslandStatus.Discarded),
                    ],
                    Decisions: [new("DEC-001", "Merge pain into athlete persona", "Cleaner model")]),

                4 => new DistillResult(
                    Output: "Persona cards produced",
                    GateSatisfied: passed,
                    DistilledIslands:
                    [
                        new("ISL-001", IslandStatus.Distilled),
                        new("ISL-002", IslandStatus.Distilled),
                    ],
                    Deliverables: [new("DEL-001", "outputs/personas/01-athlete.md", DeliverableStatus.Complete)]),

                5 => new ExpressResult(
                    Output: "Relay emitted",
                    GateSatisfied: passed,
                    InputTokens: 2000,
                    OutputTokens: 5000,
                    Questions: []),

                _ => new StepResult($"Fake output for step {step.StepNumber}", passed),
            };

            return Task.FromResult(result);
        }
    }
}
