using System.Text.Json;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Core.Agent.Steps.CODESteps;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

public class StepPipelineTests
{
    private const string TestDataPath = "TestData/UxPersonaRole.md";
    private static readonly SessionMarkFilePaths TestMarkFilePaths = new("UX", "outputs/contextAgent");

    private static UxPersona CreateAgent()
    {
        var markdown = File.ReadAllText(TestDataPath);
        return new UxPersona(RoleParser.ParseFromMarkdown(markdown), TestSteps.DefaultSteps(), "test-proj", TestMarkFilePaths);
    }

    [Fact]
    public void UxPersona_Has5Steps()
    {
        var agent = CreateAgent();
        Assert.Equal(5, agent.Steps.Count);
    }

    [Fact]
    public void Steps_AreNumberedSequentially()
    {
        var agent = CreateAgent();
        for (int i = 0; i < agent.Steps.Count; i++)
        {
            Assert.Equal(i + 1, agent.Steps[i].StepNumber);
        }
    }

    [Fact]
    public void Step1_UsesKickoffext()
    {
        var agent = CreateAgent();
        Assert.Equal("Kickoff-context", agent.Steps[0].SkillName);
        Assert.Equal("Objective confirmed", agent.Steps[0].Gate.Description);
    }

    [Fact]
    public void Step2_UsesAutonomousCapture()
    {
        var agent = CreateAgent();
        Assert.Equal("autonomous-capture", agent.Steps[1].SkillName);
        Assert.Equal("≥3 user-type islands", agent.Steps[1].Gate.Description);
    }

    [Fact]
    public void NewAgent_StartsAtStep0_NotCompleted()
    {
        var agent = CreateAgent();
        Assert.Equal(0, agent.Pipeline.CurrentStepIndex);
        Assert.False(agent.IsCompleted);
    }

    [Fact]
    public async Task ExecuteNextStep_AdvancesPipeline_WhenGatePasses()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient(gateSatisfied: true);

        var result = await agent.ExecuteNextStepAsync(client);

        Assert.True(result.GateSatisfied);
        Assert.Equal(1, agent.Pipeline.CurrentStepIndex);
    }

    [Fact]
    public async Task ExecuteNextStep_DoesNotAdvancePipeline_WhenGateFails()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient(gateSatisfied: false);

        var result = await agent.ExecuteNextStepAsync(client);

        Assert.False(result.GateSatisfied);
        Assert.Equal(0, agent.Pipeline.CurrentStepIndex);
    }

    [Fact]
    public async Task ExecuteAllSteps_RunsAllSteps_WhenAllGatesPass()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient(gateSatisfied: true);

        var results = await agent.ExecuteAllStepsAsync(client);

        Assert.Equal(5, results.Count);
        Assert.True(agent.IsCompleted);
        Assert.All(results, r => Assert.True(r.GateSatisfied));
    }

    [Fact]
    public async Task ExecuteAllSteps_StopsAtFailedGate()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient(failAtStep: 3);

        var results = await agent.ExecuteAllStepsAsync(client);

        Assert.Equal(3, results.Count);
        Assert.False(agent.IsCompleted);
        Assert.Equal(2, agent.Pipeline.CurrentStepIndex); // stopped at step 3 (index 2)
    }

    [Fact]
    public async Task ExecuteNextStep_ThrowsWhenAllCompleted()
    {
        var agent = CreateAgent();
        var client = new FakeChatClient(gateSatisfied: true);
        await agent.ExecuteAllStepsAsync(client);

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
            // Chain calls are always for KickoffStep (step 1)
            var passed = _failAtStep == 1 ? false : _gateSatisfied;
            var json = jsonSchema.Contains("triaged")
                ? """{"triaged":[]}"""
                : $$"""{"sessionObjective":"Build personas for recruiting platform","narrativeBridge":"Initial session.","isInitialSession":true,"stalenessWarning":null,"gateSatisfied":{{(passed ? "true" : "false")}}}""";
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
