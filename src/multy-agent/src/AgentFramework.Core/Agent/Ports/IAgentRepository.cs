namespace AgentFramework.Core.Agent.Ports;

/// <summary>
/// Generic port for loading agent configuration and resolving handler instruction overrides.
///
/// Implement in infrastructure (JSON file, CosmosDB, etc.) to supply both the serialized
/// <typeparamref name="TConfig"/> record and per-handler prompt overrides that can be edited
/// at runtime without recompiling.
///
/// <b>Pattern:</b>
/// <list type="bullet">
///   <item>Domain agents define a specific binding: <c>IUxAgentRepository : IAgentRepository&lt;AgentConfig&gt;</c></item>
///   <item>Infrastructure implements the binding, reading from the persistence layer.</item>
///   <item><see cref="GetHandlerInstruction"/> is synchronous — it is called per-handler inside
///     <c>SequentialCommandHandlerChain.RunAsync</c> where awaiting is not viable.</item>
/// </list>
/// </summary>
public interface IAgentRepository<TConfig>
{
    /// <summary>Loads the full agent configuration record.</summary>
    Task<TConfig?> GetConfigAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns a runtime instruction override for the named handler, or <c>null</c> when no
    /// override is configured and the handler should use its compiled-in default.
    /// </summary>
    string? GetHandlerInstruction(string handlerName);
}
