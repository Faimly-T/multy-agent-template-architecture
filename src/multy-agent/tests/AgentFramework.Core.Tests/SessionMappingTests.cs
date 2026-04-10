using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Core.Agent.Steps.CODESteps;
using AgentFramework.Core.Agent;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

public class SessionMappingTests
{
    private const string TestDataPath = "TestData/UxPersonaRole.md";

    private static UxPersona CreateAgent()
    {
        var markdown = File.ReadAllText(TestDataPath);
        return new UxPersona(Role.FromMd(markdown), TestSteps.DefaultSteps(), TestSteps.DefaultSkills());
    }

    // --- Step 1: Rehydrate → maps Session Objective ---

    [Fact]
    public async Task Step1_RehydrateResult_MapsSessionObjective()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new PhaseAwareChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        Assert.Equal("Build personas for a college athletic recruiting platform", agent.Session!.Checkpoint.SessionObjective);
    }

    // --- Step 2: Capture → maps Islands ---

    [Fact]
    public async Task Step2_CaptureResult_MapsIslandsToSession()
    {
        var agent = CreateAgent();
        agent.StartSession("objective");
        var builder = new UxStepMessageBuilder();
        var client = new PhaseAwareChatClient();

        // Step 1
        await agent.ExecuteNextStepAsync(builder, client);
        // Step 2
        await agent.ExecuteNextStepAsync(builder, client);

        Assert.Equal(3, agent.Session!.Islands.Count);
        Assert.Equal("ISL-001", agent.Session.Islands[0].Id);
        Assert.Equal(IslandType.UserType, agent.Session.Islands[0].Type);
        Assert.Equal(IslandStatus.Captured, agent.Session.Islands[0].Status);
    }

    [Fact]
    public async Task Step2_CaptureResult_MapsIslandRelations()
    {
        var agent = CreateAgent();
        agent.StartSession("objective");
        var builder = new UxStepMessageBuilder();
        var client = new PhaseAwareChatClient();

        await agent.ExecuteNextStepAsync(builder, client);
        await agent.ExecuteNextStepAsync(builder, client);

        Assert.Null(agent.Session!.Islands[0].RelatesToIslandId);
        Assert.Equal("ISL-001", agent.Session.Islands[2].RelatesToIslandId);
    }

    // --- Step 3: Organize → maps decisions + island status ---

    [Fact]
    public async Task Step3_OrganizeResult_UpdatesIslandStatuses()
    {
        var agent = CreateAgent();
        agent.StartSession("objective");
        var builder = new UxStepMessageBuilder();
        var client = new PhaseAwareChatClient();

        await agent.ExecuteNextStepAsync(builder, client); // Step 1
        await agent.ExecuteNextStepAsync(builder, client); // Step 2
        await agent.ExecuteNextStepAsync(builder, client); // Step 3

        Assert.Equal(IslandStatus.Organized, agent.Session!.Islands[0].Status);
        Assert.Equal(IslandStatus.Organized, agent.Session.Islands[1].Status);
        Assert.Equal(IslandStatus.Discarded, agent.Session.Islands[2].Status);
    }

    [Fact]
    public async Task Step3_OrganizeResult_RecordsDecisions()
    {
        var agent = CreateAgent();
        agent.StartSession("objective");
        var builder = new UxStepMessageBuilder();
        var client = new PhaseAwareChatClient();

        await agent.ExecuteNextStepAsync(builder, client);
        await agent.ExecuteNextStepAsync(builder, client);
        await agent.ExecuteNextStepAsync(builder, client);

        Assert.Single(agent.Session!.Decisions);
        Assert.Equal("DEC-001", agent.Session.Decisions[0].Id);
        Assert.Equal("Merge pain-point island into athlete persona", agent.Session.Decisions[0].Description);
    }

    // --- Step 4: Distill → maps deliverables + island status ---

    [Fact]
    public async Task Step4_DistillResult_MapsDeliverablesAndIslandStatus()
    {
        var agent = CreateAgent();
        agent.StartSession("objective");
        var builder = new UxStepMessageBuilder();
        var client = new PhaseAwareChatClient();

        await agent.ExecuteNextStepAsync(builder, client); // 1
        await agent.ExecuteNextStepAsync(builder, client); // 2
        await agent.ExecuteNextStepAsync(builder, client); // 3
        await agent.ExecuteNextStepAsync(builder, client); // 4

        Assert.Equal(IslandStatus.Distilled, agent.Session!.Islands[0].Status);
        Assert.Equal(IslandStatus.Distilled, agent.Session.Islands[1].Status);

        Assert.Single(agent.Session.Deliverables);
        Assert.Equal("DEL-001", agent.Session.Deliverables[0].DeliverableId);
        Assert.Equal(DeliverableStatus.Complete, agent.Session.Deliverables[0].Status);
    }

    // --- Step 5: Express → maps token consumption ---

    [Fact]
    public async Task Step5_ExpressResult_MapsTokenConsumption()
    {
        var agent = CreateAgent();
        agent.StartSession("objective");
        var builder = new UxStepMessageBuilder();
        var client = new PhaseAwareChatClient();

        await agent.ExecuteAllStepsAsync(builder, client);

        Assert.True(agent.IsCompleted);
        Assert.Equal(2000, agent.Session!.Checkpoint.TokensConsumption.InputTokens);
        Assert.Equal(5000, agent.Session.Checkpoint.TokensConsumption.OutputTokens);
        Assert.Equal(7000, agent.Session.Checkpoint.TokensConsumption.TotalTokens);
    }

    // --- Full pipeline ---

    [Fact]
    public async Task FullPipeline_EmptySession_MapsAllSteps()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new PhaseAwareChatClient();

        var results = await agent.ExecuteAllStepsAsync(builder, client);

        Assert.Equal(5, results.Count);
        Assert.True(agent.IsCompleted);

        // Objective updated by step 1
        Assert.Equal("Build personas for a college athletic recruiting platform", agent.Session!.Checkpoint.SessionObjective);
        // Islands from step 2
        Assert.Equal(3, agent.Session.Islands.Count);
        // Decisions from step 3
        Assert.Single(agent.Session.Decisions);
        // Deliverables from step 4
        Assert.Single(agent.Session.Deliverables);
        // Tokens from step 5
        Assert.Equal(7000, agent.Session.Checkpoint.TokensConsumption.TotalTokens);
    }

    [Fact]
    public async Task NoSession_DoesNotThrow_WhenApplySkipped()
    {
        var agent = CreateAgent();
        // No StartSession call
        var builder = new UxStepMessageBuilder();
        var client = new PhaseAwareChatClient();

        var result = await agent.ExecuteNextStepAsync(builder, client);

        Assert.True(result.GateSatisfied);
        Assert.Null(agent.Session);
    }

    // --- Fake chat client that returns typed results per step ---

    private class PhaseAwareChatClient : IChatClient
    {
        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            StepResult result = step.StepNumber switch
            {
                1 => new RehydrateResult(
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
