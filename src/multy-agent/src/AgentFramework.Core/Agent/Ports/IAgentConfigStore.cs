namespace AgentFramework.Core.Agent.Ports;

/// <summary>
/// Port for persisting and loading agent configuration records.
///
/// Pattern: every domain agent has a serializable <typeparamref name="TConfig"/> record
/// (the descriptor) and a <c>BuildAsync(TConfig, PipelineCode, ct)</c> factory (the runtime).
/// Implement this interface in infrastructure (CosmosDB, file, in-memory) to save/restore configs.
///
/// Example domain binding:
/// <code>
/// public interface IUxAgentConfigStore : IAgentConfigStore&lt;AgentConfig&gt; { }
/// </code>
/// </summary>
public interface IAgentConfigStore<TConfig>
{
    Task<TConfig?> LoadAsync(string agentId, CancellationToken ct = default);
    Task SaveAsync(TConfig config, CancellationToken ct = default);
}
