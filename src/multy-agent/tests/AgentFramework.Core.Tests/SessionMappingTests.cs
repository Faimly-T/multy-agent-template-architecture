using System.Text.Json;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.CodePipeline;
using AgentFramework.Core.Agent;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

public class SessionMappingTests
{
    private const string TestDataPath = "TestData/UxAgentRole.md";

    private static async Task<UxAgent> CreateAgentAsync()
    {
        return await UxAgent.BuildAsync(UxAgentDefaults.Config(TestSteps.DefaultRole()), TestSteps.DefaultResolver());
    }

    // --- Step 1: Kickoff → maps Session Objective ---

    [Fact]
    public async Task Step1_KickoffResult_MapsSessionObjective()
    {
        var agent = await CreateAgentAsync();
        var client = new PhaseAwareChatClient();

        await agent.ExecuteNextStepAsync(client);

        Assert.Equal("Build personas for a college athletic recruiting platform", agent.Session!.CurrentCheckpoint!.SessionObjective);
    }

    // --- Step 2: Capture → maps Islands ---

    [Fact]
    public async Task Step2_CaptureResult_MapsIslandsToSession()
    {
        var agent = await CreateAgentAsync();
        var client = new PhaseAwareChatClient();

        await agent.ExecuteNextStepAsync(client);
        await agent.ExecuteNextStepAsync(client);

        Assert.Equal(3, agent.Brain.Islands.Count);
        Assert.Equal("ISL-001", agent.Brain.Islands[0].Id);
        Assert.Equal(IslandType.UserType, agent.Brain.Islands[0].Type);
        Assert.Equal(IslandStatus.Captured, agent.Brain.Islands[0].Status);
    }

    [Fact]
    public async Task Step2_CaptureResult_MapsIslandRelations()
    {
        var agent = await CreateAgentAsync();
        var client = new PhaseAwareChatClient();

        await agent.ExecuteNextStepAsync(client);
        await agent.ExecuteNextStepAsync(client);

        Assert.Null(agent.Brain.Islands[0].RelatesToIslandId);
        Assert.Equal("ISL-001", agent.Brain.Islands[2].RelatesToIslandId);
    }

    // --- Step 3: Organize → maps decisions + island status ---

    [Fact]
    public async Task Step3_OrganizeResult_UpdatesIslandStatuses()
    {
        var agent = await CreateAgentAsync();
        var client = new PhaseAwareChatClient();

        await agent.ExecuteNextStepAsync(client); // Step 1
        await agent.ExecuteNextStepAsync(client); // Step 2
        await agent.ExecuteNextStepAsync(client); // Step 3

        Assert.Equal(IslandStatus.Organized, agent.Brain.Islands[0].Status);
        Assert.Equal(IslandStatus.Organized, agent.Brain.Islands[1].Status);
        Assert.Equal(IslandStatus.Discarded, agent.Brain.Islands[2].Status);
    }

    [Fact]
    public async Task Step3_OrganizeResult_RecordsDecisions()
    {
        // Decisions are now produced by the Distill step (step 4) via group distillations.
        var agent = await CreateAgentAsync();
        var client = new PhaseAwareChatClient();

        await agent.ExecuteNextStepAsync(client); // step 1: kickoff
        await agent.ExecuteNextStepAsync(client); // step 2: capture
        await agent.ExecuteNextStepAsync(client); // step 3: organize — no decisions yet
        Assert.Empty(agent.Decisions);

        await agent.ExecuteNextStepAsync(client); // step 4: distill — decisions appear here
        Assert.Single(agent.Decisions);
        Assert.Equal("DEC-001", agent.Decisions[0].Id);
        Assert.Equal("Merge pain-point island into athlete persona", agent.Decisions[0].Description);
    }

    // --- Step 4: Distill → maps deliverables + island status ---

    [Fact]
    public async Task Step4_DistillResult_MapsDeliverablesAndIslandStatus()
    {
        var agent = await CreateAgentAsync();
        var client = new PhaseAwareChatClient();

        await agent.ExecuteNextStepAsync(client); // 1
        await agent.ExecuteNextStepAsync(client); // 2
        await agent.ExecuteNextStepAsync(client); // 3
        await agent.ExecuteNextStepAsync(client); // 4

        Assert.Equal(IslandStatus.Distilled, agent.Brain.Islands[0].Status);
        Assert.Equal(IslandStatus.Distilled, agent.Brain.Islands[1].Status);

        Assert.Single(agent.Deliverables);
        Assert.Equal("DEL-001", agent.Deliverables[0].DeliverableId);
        Assert.Equal(DeliverableStatus.Complete, agent.Deliverables[0].Status);
    }

    // --- Step 5: Express → maps token consumption ---

    [Fact]
    public async Task Step5_ExpressResult_MapsTokenConsumption()
    {
        var agent = await CreateAgentAsync();
        var client = new PhaseAwareChatClient();

        await agent.ExecuteAllStepsAsync(TestSteps.DefaultIntent, client);

        Assert.True(agent.IsCompleted);
        Assert.Equal(2000, agent.Session!.CurrentCheckpoint!.TokensConsumption.InputTokens);
        Assert.Equal(5000, agent.Session.CurrentCheckpoint.TokensConsumption.OutputTokens);
        Assert.Equal(7000, agent.Session.CurrentCheckpoint.TokensConsumption.TotalTokens);
    }

    // --- Full pipeline ---

    [Fact]
    public async Task FullPipeline_EmptySession_MapsAllSteps()
    {
        var agent = await CreateAgentAsync();
        var client = new PhaseAwareChatClient();

        var results = await agent.ExecuteAllStepsAsync(TestSteps.DefaultIntent, client);

        Assert.Equal(6, results.Count);
        Assert.True(agent.IsCompleted);

        Assert.Equal("Build personas for a college athletic recruiting platform", agent.Session!.CurrentCheckpoint!.SessionObjective);
        Assert.Equal(3, agent.Brain.Islands.Count);
        Assert.Single(agent.Decisions);
        Assert.Single(agent.Deliverables);
        Assert.Equal(7000, agent.Session.CurrentCheckpoint.TokensConsumption.TotalTokens);
    }

    [Fact]
    public async Task BeforeKickoff_CheckpointIsNull()
    {
        var agent = await CreateAgentAsync();

        Assert.NotNull(agent.Session);
        Assert.Null(agent.Session.CurrentCheckpoint);

        var client = new PhaseAwareChatClient();
        var result = await agent.ExecuteNextStepAsync(client);

        Assert.True(result.GateSatisfied);
        Assert.NotNull(agent.Session.CurrentCheckpoint);
    }

    // --- Fake chat client ---

    private class PhaseAwareChatClient : IChatClient
    {
        public Task<TResult> SendHandlerAsync<TResult>(
            IReadOnlyList<ChatMessage> messages, string jsonSchema,
            Func<JsonElement, TResult> parse, CancellationToken ct = default)
        {
            string json;
            if (jsonSchema.Contains("triaged"))
                json = """{"triaged":[]}""";
            else if (jsonSchema.Contains("groupDistillations"))
                json = """{"groupDistillations":[{"groupId":"GRP-001","decisions":[{"id":"DEC-001","description":"Merge pain-point island into athlete persona","impact":"Reduces persona count"}],"deliverables":[{"deliverableId":"DEL-001","path":"outputs/personas/01-athlete.md","purpose":"Athlete persona card","status":"Complete"}],"questions":[]}],"distilledIslands":[{"islandId":"ISL-001","newStatus":"Distilled"},{"islandId":"ISL-002","newStatus":"Distilled"}],"gateSatisfied":true}""";
            else if (jsonSchema.Contains("readiness"))
                json = """{"groups":[{"id":"GRP-001","name":"Recruiting Group","islandIds":["ISL-001","ISL-002"],"whyTogether":"Both describe the recruiting relationship","readiness":"Ready","readinessNotes":null}]}""";
            else if (jsonSchema.Contains("islandIds"))
                json = """{"groups":[{"id":"GRP-001","name":"Recruiting Group","islandIds":["ISL-001","ISL-002"],"whyTogether":"Both describe the recruiting relationship"}],"ungroupedIslandIds":["ISL-003"]}""";
            else if (jsonSchema.Contains("islands"))
                json = """{"islands":[{"id":"ISL-001","type":"UserType","description":"Student athlete seeking recruitment","source":"six-hats:yellow","relatesToIslandId":null},{"id":"ISL-002","type":"Stakeholder","description":"College coach evaluating talent","source":"six-hats:white","relatesToIslandId":null},{"id":"ISL-003","type":"PainPoint","description":"No visibility into recruiting process","source":"six-hats:black","relatesToIslandId":"ISL-001"}]}""";
            else if (jsonSchema.Contains("\"html\""))
                json = """{"html":"<html><body>Test Document</body></html>"}""";
            else if (jsonSchema.Contains("inputTokens"))
                json = """{"questions":[],"inputTokens":2000,"outputTokens":5000,"gateSatisfied":true}""";
            else
                json = """{"sessionObjective":"Build personas for a college athletic recruiting platform","narrativeBridge":"Initial session.","isInitialSession":true,"stalenessWarning":null,"gateSatisfied":true}""";
            return Task.FromResult(parse(JsonDocument.Parse(json).RootElement));
        }

        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            StepResult result = step.StepNumber switch
            {
                1 => new KickoffResult(
                    Output: "Objective defined",
                    GateSatisfied: true,
                    SessionObjective: "Build personas for a college athletic recruiting platform"),

                2 => new CaptureResult(
                    Output: "Captured 3 islands",
                    GateSatisfied: true,
                    Islands: new List<CapturedIsland>
                    {
                        new("ISL-001", IslandType.UserType, "Student athlete seeking recruitment", "product description"),
                        new("ISL-002", IslandType.Stakeholder, "College coach evaluating talent", "product description"),
                        new("ISL-003", IslandType.PainPoint, "No visibility into recruiting process", "interview", RelatesToIslandId: "ISL-001"),
                    }),

                3 => new OrganizeResult(
                    Output: "Organized into 2 candidates",
                    GateSatisfied: true,
                    OrganizedIslands: new List<IslandOrganization>
                    {
                        new("ISL-001", IslandStatus.Organized),
                        new("ISL-002", IslandStatus.Organized),
                        new("ISL-003", IslandStatus.Discarded),
                    },
                    Decisions: new List<DecisionRecord>
                    {
                        new("DEC-001", "Merge pain-point island into athlete persona", "Reduces persona count, keeps pain visible in JTBD"),
                    }),

                4 => new DistillResult(
                    Output: "Distilled 2 persona cards",
                    GateSatisfied: true,
                    DistilledIslands: new List<IslandDistillation>
                    {
                        new("ISL-001", IslandStatus.Distilled),
                        new("ISL-002", IslandStatus.Distilled),
                    },
                    Deliverables: new List<DeliverableRecord>
                    {
                        new("DEL-001", "outputs/personas/01-athlete.md", DeliverableStatus.Complete),
                    }),

                5 => new ExpressResult(
                    Output: "Session state compiled, relay emitted",
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
