using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

public class SessionTests
{
    private const string TestDataPath = "TestData/UxPersonaRole.md";

    private static UxPersona CreateAgent()
    {
        var markdown = File.ReadAllText(TestDataPath);
        return new UxPersona(Role.FromMd(markdown), TestSteps.DefaultSteps());
    }

    // --- Checkpoint ---

    [Fact]
    public void StartSession_CreatesSessionWithObjective()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("Define personas for recruiting platform");

        Assert.NotNull(session);
        Assert.Equal("Define personas for recruiting platform", session.Checkpoint.SessionObjective);
        Assert.Equal(1, session.Checkpoint.SessionIteration);
    }

    [Fact]
    public void StartSession_SetsDateToUtcNow()
    {
        var agent = CreateAgent();
        var before = DateTime.UtcNow;
        var session = agent.StartSession("objective");
        var after = DateTime.UtcNow;

        Assert.InRange(session.Checkpoint.Date, before, after);
    }

    [Fact]
    public void StartSession_InitializesTokenConsumptionToZero()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("objective");

        Assert.Equal(0, session.Checkpoint.TokensConsumption.InputTokens);
        Assert.Equal(0, session.Checkpoint.TokensConsumption.OutputTokens);
        Assert.Equal(0, session.Checkpoint.TokensConsumption.TotalTokens);
    }

    [Fact]
    public void UpdateTokenConsumption_UpdatesCheckpoint()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("objective");

        session.UpdateTokenConsumption(1500, 3000);

        Assert.Equal(1500, session.Checkpoint.TokensConsumption.InputTokens);
        Assert.Equal(3000, session.Checkpoint.TokensConsumption.OutputTokens);
        Assert.Equal(4500, session.Checkpoint.TokensConsumption.TotalTokens);
    }

    [Fact]
    public void StartSession_WithCustomIteration()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("Continue personas", sessionIteration: 3);

        Assert.Equal(3, session.Checkpoint.SessionIteration);
    }

    // --- Islands ---

    [Fact]
    public void CaptureIsland_AddsIslandWithCapturedStatus()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("objective");

        var island = session.CaptureIsland("ISL-001", IslandType.UserType, "Student athlete seeking recruitment", "product description");

        Assert.Single(session.Islands);
        Assert.Equal("ISL-001", island.Id);
        Assert.Equal(IslandType.UserType, island.Type);
        Assert.Equal("Student athlete seeking recruitment", island.Description);
        Assert.Equal("product description", island.Source);
        Assert.Equal(IslandStatus.Captured, island.Status);
    }

    [Fact]
    public void Island_RelateTo_SetsRelation()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("objective");

        var parent = session.CaptureIsland("ISL-001", IslandType.UserType, "Athlete", "desc");
        var child = session.CaptureIsland("ISL-002", IslandType.Goal, "Get recruited", "desc");

        child.RelateTo("ISL-001");

        Assert.Equal("ISL-001", child.RelatesToIslandId);
        Assert.Null(parent.RelatesToIslandId);
    }

    [Fact]
    public void Island_StatusTransitions()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("objective");
        var island = session.CaptureIsland("ISL-001", IslandType.PainPoint, "No visibility", "interview");

        Assert.Equal(IslandStatus.Captured, island.Status);

        island.Organize();
        Assert.Equal(IslandStatus.Organized, island.Status);

        island.Distill();
        Assert.Equal(IslandStatus.Distilled, island.Status);
    }

    [Fact]
    public void Island_CanBeDiscarded()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("objective");
        var island = session.CaptureIsland("ISL-001", IslandType.AntiUser, "Tourist", "capture");

        island.Discard();

        Assert.Equal(IslandStatus.Discarded, island.Status);
    }

    [Fact]
    public void FindIsland_ReturnsCorrectIsland()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("objective");
        session.CaptureIsland("ISL-001", IslandType.UserType, "Athlete", "desc");
        session.CaptureIsland("ISL-002", IslandType.Stakeholder, "Coach", "desc");

        var found = session.FindIsland("ISL-002");

        Assert.NotNull(found);
        Assert.Equal("Coach", found.Description);
    }

    [Fact]
    public void FindIsland_ReturnsNull_WhenNotFound()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("objective");

        Assert.Null(session.FindIsland("NONEXISTENT"));
    }

    // --- Deliverables ---

    [Fact]
    public void RecordDeliverable_AddsDeliverable()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("objective");

        session.RecordDeliverable("DEL-001", "outputs/personas/01-athlete.md", DeliverableStatus.Complete);

        Assert.Single(session.Deliverables);
        var d = session.Deliverables[0];
        Assert.Equal("DEL-001", d.DeliverableId);
        Assert.Equal("outputs/personas/01-athlete.md", d.Path);
        Assert.Equal(DeliverableStatus.Complete, d.Status);
    }

    [Fact]
    public void RecordDeliverable_SupportsAllStatuses()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("objective");

        session.RecordDeliverable("D1", "path/draft.md", DeliverableStatus.Draft);
        session.RecordDeliverable("D2", "path/partial.md", DeliverableStatus.Partial);
        session.RecordDeliverable("D3", "path/done.md", DeliverableStatus.Complete);

        Assert.Equal(3, session.Deliverables.Count);
        Assert.Equal(DeliverableStatus.Draft, session.Deliverables[0].Status);
        Assert.Equal(DeliverableStatus.Partial, session.Deliverables[1].Status);
        Assert.Equal(DeliverableStatus.Complete, session.Deliverables[2].Status);
    }

    // --- Decisions ---

    [Fact]
    public void RecordDecision_AddsDecision()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("objective");

        session.RecordDecision("DEC-001", "Merge athlete and recruit into one persona", "Reduces persona count from 4 to 3");

        Assert.Single(session.Decisions);
        var dec = session.Decisions[0];
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
        agent.StartSession("objective");

        Assert.NotNull(agent.Session);
        Assert.Same(agent.Session, agent.Session);
    }

    [Fact]
    public void StartSession_ReplacesExistingSession()
    {
        var agent = CreateAgent();
        var first = agent.StartSession("first objective");
        first.CaptureIsland("ISL-001", IslandType.UserType, "Athlete", "desc");

        var second = agent.StartSession("second objective", sessionIteration: 2);

        Assert.Empty(second.Islands);
        Assert.Equal("second objective", second.Checkpoint.SessionObjective);
    }
}
