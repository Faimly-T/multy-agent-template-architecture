using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Ports;

/// <summary>
/// Persistence port for <see cref="BrainAggregate"/>.
///
/// The Brain's lifecycle (load → use → save) is the caller's responsibility.
/// The typical flow in a factory method like <c>UxAgent.BuildAsync</c>:
/// <code>
/// var brain = await brainRepo?.GetAsync(config.ProjectId, ct)
///             ?? new BrainAggregate(config.ProjectId);
/// var agent = await UxAgent.BuildAsync(config, resolver, brain: brain);
/// // ... run the agent ...
/// await brainRepo?.SaveAsync(agent.Brain, ct);
/// </code>
/// </summary>
public interface IBrainRepository
{
    /// <summary>
    /// Loads the Brain for the given project.
    /// Returns <c>null</c> when no Brain has been persisted yet (first run for that project).
    /// </summary>
    Task<BrainAggregate?> GetAsync(BrainId id, CancellationToken ct = default);

    /// <summary>Persists the Brain, overwriting any previously saved state for the same project.</summary>
    Task SaveAsync(BrainAggregate brain, CancellationToken ct = default);

    /// <summary>Returns <c>true</c> when a persisted Brain exists for the given project.</summary>
    Task<bool> ExistsAsync(BrainId id, CancellationToken ct = default);
}
