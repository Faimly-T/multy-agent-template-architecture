using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps.CODESteps;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

public class SessionTests
{
    private const string TestDataPath = "TestData/UxPersonaRole.md";
    private static readonly SessionMarkFilePaths TestMarkFilePaths = new("UX", "outputs/contextAgent");

    private static UxPersona CreateAgent()
    {
        var markdown = File.ReadAllText(TestDataPath);
        return new UxPersona(RoleParser.ParseFromMarkdown(markdown), TestSteps.DefaultSteps(), TestSteps.DefaultSkills());
    }

    // --- Checkpoint ---

    [Fact]
    public void OpenSession_CreatesSessionWithObjective()
    {
        var agent = CreateAgent();
        var session = agent.OpenSession("test-proj", TestMarkFilePaths, "Define personas for recruiting platform");

        Assert.NotNull(session);
        Assert.Equal("Define personas for recruiting platform", session.CurrentCheckpoint!.SessionObjective);
        Assert.Equal(1, session.CurrentCheckpoint!.SessionIteration);
    }

    [Fact]
    public void OpenSession_SetsDateToUtcNow()
    {
        var agent = CreateAgent();
        var before = DateTime.UtcNow;
        var session = agent.OpenSession("test-proj", TestMarkFilePaths, "objective");
        var after = DateTime.UtcNow;

        Assert.InRange(session.CurrentCheckpoint!.Date, before, after);
    }

    [Fact]
    public void OpenSession_InitializesTokenConsumptionToZero()
    {
        var agent = CreateAgent();
        var session = agent.OpenSession("test-proj", TestMarkFilePaths, "objective");

        Assert.Equal(0, session.CurrentCheckpoint!.TokensConsumption.InputTokens);
        Assert.Equal(0, session.CurrentCheckpoint!.TokensConsumption.OutputTokens);
        Assert.Equal(0, session.CurrentCheckpoint!.TokensConsumption.TotalTokens);
    }

    [Fact]
    public void UpdateTokenConsumption_UpdatesCheckpoint()
    {
        var agent = CreateAgent();
        var session = agent.OpenSession("test-proj", TestMarkFilePaths, "objective");

        ((ISessionWriter)agent).UpdateTokenConsumption(1500, 3000);

        Assert.Equal(1500, session.CurrentCheckpoint!.TokensConsumption.InputTokens);
        Assert.Equal(3000, session.CurrentCheckpoint!.TokensConsumption.OutputTokens);
        Assert.Equal(4500, session.CurrentCheckpoint!.TokensConsumption.TotalTokens);
    }

    [Fact]
    public void OpenSession_IterationStartsAt1()
    {
        var agent = CreateAgent();
        var session = agent.OpenSession("test-proj", TestMarkFilePaths, "Continue personas");

        Assert.Equal(1, session.CurrentCheckpoint!.SessionIteration);
    }

    // --- Islands (via IslandBacklog) ---

    [Fact]
    public void SetCaptured_AddsIslandsWithCapturedStatus()
    {
        var agent = CreateAgent();
        var session = agent.OpenSession("test-proj", TestMarkFilePaths, "objective");

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
    public void SetCaptured_PreservesRelatesToIslandId()
    {
        var agent = CreateAgent();
        var session = agent.OpenSession("test-proj", TestMarkFilePaths, "objective");

        session.Backlog.SetCaptured([
            new CapturedIsland("ISL-001", IslandType.UserType, "Athlete", "desc"),
            new CapturedIsland("ISL-002", IslandType.Goal, "Get recruited", "desc", "ISL-001")
        ]);

        Assert.Null(session.Backlog.All[0].RelatesToIslandId);
        Assert.Equal("ISL-001", session.Backlog.All[1].RelatesToIslandId);
    }

    [Fact]
    public void Backlog_StatusTransitions_CapturedToOrganizedToDistilled()
    {
        var agent = CreateAgent();
        var session = agent.OpenSession("test-proj", TestMarkFilePaths, "objective");
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
    public void Backlog_CanDiscardDuringOrganize()
    {
        var agent = CreateAgent();
        var session = agent.OpenSession("test-proj", TestMarkFilePaths, "objective");
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
    public void Backlog_ApplyOrganization_ThrowsOnInvalidTransition_OrganizedToOrganized()
    {
        var agent = CreateAgent();
        var session = agent.OpenSession("test-proj", TestMarkFilePaths, "objective");
        session.Backlog.SetCaptured([
            new CapturedIsland("ISL-001", IslandType.UserType, "Athlete", "desc")
        ]);
        session.Backlog.ApplyOrganization([new IslandOrganization("ISL-001", IslandStatus.Organized)]);

        Assert.Throws<InvalidOperationException>(() =>
            session.Backlog.ApplyOrganization([new IslandOrganization("ISL-001", IslandStatus.Organized)]));
    }

    [Fact]
    public void Backlog_ApplyDistillation_ThrowsWhenNotOrganized()
    {
        var agent = CreateAgent();
        var session = agent.OpenSession("test-proj", TestMarkFilePaths, "objective");
        session.Backlog.SetCaptured([
            new CapturedIsland("ISL-001", IslandType.UserType, "Athlete", "desc")
        ]);

        Assert.Throws<InvalidOperationException>(() =>
            session.Backlog.ApplyDistillation([new IslandDistillation("ISL-001", IslandStatus.Distilled)]));
    }

    [Fact]
    public void Backlog_ApplyOrganization_ThrowsWhenAlreadyDiscarded()
    {
        var agent = CreateAgent();
        var session = agent.OpenSession("test-proj", TestMarkFilePaths, "objective");
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
    public void Backlog_ApplyDistillation_ThrowsWhenAlreadyDistilled()
    {
        var agent = CreateAgent();
        var session = agent.OpenSession("test-proj", TestMarkFilePaths, "objective");
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
    public void SessionWriter_SetCapturedIslands_ThrowsOnDuplicateIds()
    {
        var agent = CreateAgent();
        agent.OpenSession("test-proj", TestMarkFilePaths, "objective");
        var writer = (ISessionWriter)agent;

        Assert.Throws<InvalidOperationException>(() =>
            writer.SetCapturedIslands([
                new CapturedIsland("ISL-001", IslandType.UserType, "Athlete", "desc"),
                new CapturedIsland("ISL-001", IslandType.Goal, "Duplicate", "desc")
            ]));
    }

    [Fact]
    public void SessionWriter_RaiseQuestion_ThrowsOnDuplicateId()
    {
        var agent = CreateAgent();
        agent.OpenSession("test-proj", TestMarkFilePaths, "objective");
        var writer = (ISessionWriter)agent;

        writer.RaiseQuestion("Q-001", "First", "express-relay");

        Assert.Throws<InvalidOperationException>(() =>
            writer.RaiseQuestion("Q-001", "Duplicate", "express-relay"));
    }

    [Fact]
    public void SessionWriter_ApplyOrganization_ThrowsOnMissingIsland()
    {
        var agent = CreateAgent();
        agent.OpenSession("test-proj", TestMarkFilePaths, "objective");
        var writer = (ISessionWriter)agent;

        Assert.Throws<InvalidOperationException>(() =>
            writer.ApplyOrganization(
                [new IslandOrganization("MISSING", IslandStatus.Organized)],
                []));
    }

    [Fact]
    public void Backlog_Find_ReturnsCorrectIsland()
    {
        var agent = CreateAgent();
        var session = agent.OpenSession("test-proj", TestMarkFilePaths, "objective");
        session.Backlog.SetCaptured([
            new CapturedIsland("ISL-001", IslandType.UserType, "Athlete", "desc"),
            new CapturedIsland("ISL-002", IslandType.Stakeholder, "Coach", "desc")
        ]);

        var found = session.Backlog.Find("ISL-002");

        Assert.NotNull(found);
        Assert.Equal("Coach", found.Description);
    }

    [Fact]
    public void Backlog_Find_ReturnsNull_WhenNotFound()
    {
        var agent = CreateAgent();
        var session = agent.OpenSession("test-proj", TestMarkFilePaths, "objective");

        Assert.Null(session.Backlog.Find("NONEXISTENT"));
    }

    // --- Deliverables (via ISessionWriter) ---

    [Fact]
    public void ApplyDistillation_AddsDeliverable()
    {
        var agent = CreateAgent();
        agent.OpenSession("test-proj", TestMarkFilePaths, "objective");

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
    public void ApplyDistillation_SupportsAllStatuses()
    {
        var agent = CreateAgent();
        agent.OpenSession("test-proj", TestMarkFilePaths, "objective");

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
    public void ApplyOrganization_AddsDecision()
    {
        var agent = CreateAgent();
        var session = agent.OpenSession("test-proj", TestMarkFilePaths, "objective");
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
    public void Agent_SessionIsNull_BeforeStart()
    {
        var agent = CreateAgent();
        Assert.Null(agent.Session);
    }

    [Fact]
    public void Agent_SessionIsAccessible_AfterStart()
    {
        var agent = CreateAgent();
        agent.OpenSession("test-proj", TestMarkFilePaths, "objective");

        Assert.NotNull(agent.Session);
        Assert.Same(agent.Session, agent.Session);
    }

    [Fact]
    public void OpenSession_ReplacesExistingSession()
    {
        var agent = CreateAgent();
        var first = agent.OpenSession("test-proj", TestMarkFilePaths, "first objective");
        first.Backlog.SetCaptured([
            new CapturedIsland("ISL-001", IslandType.UserType, "Athlete", "desc")
        ]);

        var second = agent.OpenSession("test-proj", TestMarkFilePaths, "second objective");

        Assert.Empty(second.Backlog.All);
        Assert.Equal("second objective", second.CurrentCheckpoint!.SessionObjective);
    }
}
