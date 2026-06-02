namespace AgentFramework.Core.Agent.Session;

/// <summary>
/// Immutable record of a single pipeline iteration.
/// Both <see cref="UserIntent"/> and <see cref="SessionObjective"/> are written once
/// inside <see cref="AgentSession.BeginIteration"/> and never mutated afterwards.
/// <see cref="ClosedAt"/> is set by ClosedStep via <see cref="AgentSession.FinalizeSession"/>.
/// </summary>
public record Checkpoint(
    int              SessionIteration,
    DateTime         CreatedAt,
    string           UserIntent,        // raw request passed to RunAsync — never changes
    string           SessionObjective,  // LLM-refined objective from KickoffResult — never changes
    TokenConsumption TokensConsumption,
    DateTime?        ClosedAt = null);  // null until ClosedStep seals the iteration
