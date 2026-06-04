namespace AgentFramework.Core.Agent.Session;

/// <summary>
/// A strategic choice made by the agent during the Organize or Distill step.
///
/// Decisions represent the HOW layer of reasoning — once islands are grouped (the WHAT),
/// each group demands a directing choice about how to act on what was found.
/// This maps to Rumelt's "Guiding Policy" concept: a decision sets the logic that connects
/// the diagnosed problem to the planned solution.
///
/// Decisions are immutable value objects. They accumulate across iterations in
/// <see cref="AgentBrain.Decisions"/> and inform subsequent Kickoff context.
///
/// A Decision is anchored to the Island that triggered it (<see cref="IslandId"/>) and
/// optionally to the Group in which that Island lives (<see cref="GroupId"/>). This
/// Island→Decision link is the HOW layer of the lineage chain:
/// Island (WHAT) → Group (WHY) → Decision (HOW) → Deliverable (OUTPUT).
/// </summary>
public record Decision(
    string  Id,
    string  Description,
    string  Impact,
    string? GroupId  = null,
    string? IslandId = null);
