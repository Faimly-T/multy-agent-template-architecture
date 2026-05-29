using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps.CODESteps;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

public class SessionTests
{
    private const string TestDataPath = "TestData/UxPersonaRole.md";
    private static readonly SessionMarkFilePaths TestMarkFilePaths = new("UX", "outputs/contextAgent");

    private static async Task<UxPersona> CreateAgentAsync()
    {
        var markdown = File.ReadAllText(TestDataPath);
        var role = RoleParser.ParseFromMarkdown(markdown);
        var config = TestSteps.DefaultUxConfig(role);
        return await UxPersona.BuildAsync(config, TestSteps.DefaultPipelineCode());
    }

    // --- Checkpoint ---

    [Fact]
    public async Task BeginIteration_CreatesCheckpointWithObjective()
    {
        var agent = await CreateAgentAsync();
        ((ISessionWriter)agent).BeginIteration("Define personas for recruiting platform");

        Assert.NotNull(agent.Session!.CurrentCheckpoint);
        Assert.Equal("Define personas for recruiting platform", agent.Session.CurrentCheckpoint!.SessionObjective);
        Assert.Equal(1, agent.Session.CurrentCheckpoint!.SessionIteration);
    }

    [Fact]
    public async Task BeginIteration_SetsDateToUtcNow()
    {
        var agent = await CreateAgentAsync();
        var before = DateTime.UtcNow;
        ((ISessionWriter)agent).BeginIteration("objective");
        var after = DateTime.UtcNow;

        Assert.InRange(agent.Session!.CurrentCheckpoint!.CreatedAt, before, after);
    }

    [Fact]
    public async Task BeginIteration_InitializesTokenConsumptionToZero()
    {
        var agent = await CreateAgentAsync();
        ((ISessionWriter)agent).BeginIteration("objective");

        Assert.Equal(0, agent.Session!.CurrentCheckpoint!.TokensConsumption.InputTokens);
        Assert.Equal(0, agent.Session.CurrentCheckpoint!.TokensConsumption.OutputTokens);
        Assert.Equal(0, agent.Session.CurrentCheckpoint!.TokensConsumption.TotalTokens);
    }

    [Fact]
    public async Task UpdateTokenConsumption_UpdatesCheckpoint()
    {
        var agent = await CreateAgentAsync();
        ((ISessionWriter)agent).BeginIteration("objective");

        ((ISessionWriter)agent).UpdateTokenConsumption(1500, 3000);

        Assert.Equal(1500, agent.Session!.CurrentCheckpoint!.TokensConsumption.InputTokens);
        Assert.Equal(3000, agent.Session.CurrentCheckpoint!.TokensConsumption.OutputTokens);
        Assert.Equal(4500, agent.Session.CurrentCheckpoint!.TokensConsumption.TotalTokens);
    }

    [Fact]
    public async Task BeginIteration_IterationStartsAt1()
    {
        var agent = await CreateAgentAsync();
        ((ISessionWriter)agent).BeginIteration("Continue personas");

        Assert.Equal(1, agent.Session!.CurrentCheckpoint!.SessionIteration);
    }

    // --- Islands (via IslandBacklog) ---

    [Fact]
    public async Task SetCaptured_AddsIslandsWithCapturedStatus()
    {
        var agent = await CreateAgentAsync();
        var session = agent.Session!;

        session.Backlog.SetCaptured([
            new CapturedIsland("ISL-001", IslandType.UserType, "Student athlete seeking recruitment", "product description")
        ]);

        Assert.Single(session.Backlog.All);
        var island = session.Backlog.All[0];
        Assert.Equal("ISL-001", island.Id);
        Assert.Equal(IslandType.UserType, island.Type);
        Assert.Equal("Student athlete seeking recruitment", island.Description);
        Assert.Equal("product description", island.Source);
        Assert.Equal(IslandStatus.Captured, island.Status);
    }

    [Fact]
    public async Task SetCaptured_PreservesRelatesToIslandId()
    {
        var agent = await CreateAgentAsync();
        var session = agent.Session!;

        session.Backlog.SetCaptured([
            new CapturedIsland("ISL-001", IslandType.UserType, "Athlete", "desc"),
            new CapturedIsland("ISL-002", IslandType.Goal, "Get recruited", "desc", "ISL-001")
        ]);

        Assert.Null(session.Backlog.All[0].RelatesToIslandId);
        Assert.Equal("ISL-001", session.Backlog.All[1].RelatesToIslandId);
    }

    [Fact]
    public async Task Backlog_StatusTransitions_CapturedToOrganizedToDistilled()
    {
        var agent = await CreateAgentAsync();
        var session = agent.Session!;
        session.Backlog.SetCaptured([
            new CapturedIsland("ISL-001", IslandType.PainPoint, "No visibility", "interview")
        ]);

        Assert.Equal(IslandStatus.Captured, session.Backlog.All[0].Status);

        session.Backlog.ApplyOrganization([new IslandOrganization("ISL-001", IslandStatus.Organized)]);
        Assert.Equal(IslandStatus.Organized, session.Backlog.All[0].Status);

        session.Backlog.ApplyDistillation([new IslandDistillation("ISL-001", IslandStatus.Distilled)]);
        Assert.Equal(IslandStatus.Distilled, session.Backlog.All[0].Status);
    }

    [Fact]
    public async Task Backlog_CanDiscardDuringOrganize()
    {
        var agent = await CreateAgentAsync();
        var session = agent.Session!;
        session.Backlog.SetCaptured([
            new CapturedIsland("ISL-001", IslandType.AntiUser, "Tourist", "capture"),
            new CapturedIsland("ISL-002", IslandType.UserType, "Athlete", "capture")
        ]);

        session.Backlog.ApplyOrganization([
            new IslandOrganization("ISL-001", IslandStatus.Discarded),
            new IslandOrganization("ISL-002", IslandStatus.Organized)
        ]);

        Assert.Equal(IslandStatus.Discarded, session.Backlog.All[0].Status);
        Assert.Equal(IslandStatus.Organized, session.Backlog.All[1].Status);
    }

    // --- IslandBacklog Guard Tests ---

    [Fact]
    public async Task Backlog_ApplyOrganization_ThrowsOnInvalidTransition_OrganizedToOrganized()
    {
        var agent = await CreateAgentAsync();
        var session = agent.Session!;
        session.Backlog.SetCaptured([
            new CapturedIsland("ISL-001", IslandType.UserType, "Athlete", "desc")
        ]);
        session.Backlog.ApplyOrganization([new IslandOrganization("ISL-001", IslandStatus.Organized)]);

        Assert.Throws<InvalidOperationException>(() =>
            session.Backlog.ApplyOrganization([new IslandOrganization("ISL-001", IslandStatus.Organized)]));
    }

    [Fact]
    public async Task Backlog_ApplyDistillation_ThrowsWhenNotOrganized()
    {
        var agent = await CreateAgentAsync();
        var session = agent.Session!;
        session.Backlog.SetCaptured([
            new CapturedIsland("ISL-001", IslandType.UserType, "Athlete", "desc")
        ]);

        Assert.Throws<InvalidOperationException>(() =>
            session.Backlog.ApplyDistillation([new IslandDistillation("ISL-001", IslandStatus.Distilled)]));
    }

    [Fact]
    public async Task Backlog_ApplyOrganization_ThrowsWhenAlreadyDiscarded()
    {
        var agent = await CreateAgentAsync();
        var session = agent.Session!;
        session.Backlog.SetCaptured([
            new CapturedIsland("ISL-001", IslandType.UserType, "Athlete", "desc"),
            new CapturedIsland("ISL-002", IslandType.Goal, "Keep", "desc")
        ]);
        session.Backlog.ApplyOrganization([
            new IslandOrganization("ISL-001", IslandStatus.Discarded),
            new IslandOrganization("ISL-002", IslandStatus.Organized)
        ]);

        Assert.Throws<InvalidOperationException>(() =>
            session.Backlog.ApplyOrganization([new IslandOrganization("ISL-001", IslandStatus.Organized)]));
    }

    [Fact]
    public async Task Backlog_ApplyDistillation_ThrowsWhenAlreadyDistilled()
    {
        var agent = await CreateAgentAsync();
        var session = agent.Session!;
        session.Backlog.SetCaptured([
            new CapturedIsland("ISL-001", IslandType.UserType, "Athlete", "desc")
        ]);
        session.Backlog.ApplyOrganization([new IslandOrganization("ISL-001", IslandStatus.Organized)]);
        session.Backlog.ApplyDistillation([new IslandDistillation("ISL-001", IslandStatus.Distilled)]);

        Assert.Throws<InvalidOperationException>(() =>
            session.Backlog.ApplyDistillation([new IslandDistillation("ISL-001", IslandStatus.Discarded)]));
    }

    // --- ISessionWriter Invariant Tests ---

    [Fact]
    public async Task SessionWriter_SetCapturedIslands_ThrowsOnDuplicateIds()
    {
        var agent = await CreateAgentAsync();
        var writer = (ISessionWriter)agent;

        Assert.Throws<InvalidOperationException>(() =>
            writer.SetCapturedIslands([
                new CapturedIsland("ISL-001", IslandType.UserType, "Athlete", "desc"),
                new CapturedIsland("ISL-001", IslandType.Goal, "Duplicate", "desc")
            ]));
    }

    [Fact]
    public async Task SessionWriter_RaiseQuestion_ThrowsOnDuplicateId()
    {
        var agent = await CreateAgentAsync();
        var writer = (ISessionWriter)agent;

        writer.RaiseQuestion("Q-001", "First", "express-relay");

        Assert.Throws<InvalidOperationException>(() =>
            writer.RaiseQuestion("Q-001", "Duplicate", "express-relay"));
    }

    [Fact]
    public async Task SessionWriter_ApplyOrganization_ThrowsOnMissingIsland()
    {
        var agent = await CreateAgentAsync();
        var writer = (ISessionWriter)agent;

        Assert.Throws<InvalidOperationException>(() =>
            writer.ApplyOrganization(
                [new IslandOrganization("MISSING", IslandStatus.Organized)],
                []));
    }

    [Fact]
    public async Task Backlog_Find_ReturnsCorrectIsland()
    {
        var agent = await CreateAgentAsync();
        var session = agent.Session!;
        session.Backlog.SetCaptured([
            new CapturedIsland("ISL-001", IslandType.UserType, "Athlete", "desc"),
            new CapturedIsland("ISL-002", IslandType.Stakeholder, "Coach", "desc")
        ]);

        var found = session.Backlog.Find("ISL-002");

        Assert.NotNull(found);
        Assert.Equal("Coach", found.Description);
    }

    [Fact]
    public async Task Backlog_Find_ReturnsNull_WhenNotFound()
    {
        var agent = await CreateAgentAsync();
        var session = agent.Session!;

        Assert.Null(session.Backlog.Find("NONEXISTENT"));
    }

    // --- Deliverables (via ISessionWriter) ---

    [Fact]
    public async Task ApplyDistillation_AddsDeliverable()
    {
        var agent = await CreateAgentAsync();

        ((ISessionWriter)agent).ApplyDistillation([], [
            new DeliverableRecord("DEL-001", "outputs/personas/01-athlete.md", DeliverableStatus.Complete)
        ]);

        Assert.Single(agent.Deliverables);
        var d = agent.Deliverables[0];
        Assert.Equal("DEL-001", d.DeliverableId);
        Assert.Equal("outputs/personas/01-athlete.md", d.Path);
        Assert.Equal(DeliverableStatus.Complete, d.Status);
    }

    [Fact]
    public async Task ApplyDistillation_SupportsAllStatuses()
    {
        var agent = await CreateAgentAsync();

        ((ISessionWriter)agent).ApplyDistillation([], [
            new DeliverableRecord("D1", "path/draft.md", DeliverableStatus.Draft),
            new DeliverableRecord("D2", "path/partial.md", DeliverableStatus.Partial),
            new DeliverableRecord("D3", "path/done.md", DeliverableStatus.Complete)
        ]);

        Assert.Equal(3, agent.Deliverables.Count);
        Assert.Equal(DeliverableStatus.Draft, agent.Deliverables[0].Status);
        Assert.Equal(DeliverableStatus.Partial, agent.Deliverables[1].Status);
        Assert.Equal(DeliverableStatus.Complete, agent.Deliverables[2].Status);
    }

    // --- Decisions (via ISessionWriter) ---

    [Fact]
    public async Task ApplyOrganization_AddsDecision()
    {
        var agent = await CreateAgentAsync();
        var session = agent.Session!;
        session.Backlog.SetCaptured([
            new CapturedIsland("ISL-001", IslandType.UserType, "Athlete", "desc")
        ]);

        ((ISessionWriter)agent).ApplyOrganization(
            [new IslandOrganization("ISL-001", IslandStatus.Organized)],
            [new DecisionRecord("DEC-001", "Merge athlete and recruit into one persona", "Reduces persona count from 4 to 3")]);

        Assert.Single(agent.Decisions);
        var dec = agent.Decisions[0];
        Assert.Equal("DEC-001", dec.Id);
        Assert.Equal("Merge athlete and recruit into one persona", dec.Description);
        Assert.Equal("Reduces persona count from 4 to 3", dec.Impact);
    }

    // --- Session attached to Agent ---

    [Fact]
    public async Task Session_IsNotNull_AfterConstruction()
    {
        var agent = await CreateAgentAsync();
        Assert.NotNull(agent.Session);
    }

    [Fact]
    public async Task Session_HasNoCheckpoint_BeforeKickoff()
    {
        var agent = await CreateAgentAsync();
        Assert.NotNull(agent.Session);
        Assert.Null(agent.Session!.CurrentCheckpoint);
    }

    [Fact]
    public async Task Session_SameInstance_AlwaysReturned()
    {
        var agent = await CreateAgentAsync();
        Assert.Same(agent.Session, agent.Session);
    }
}
