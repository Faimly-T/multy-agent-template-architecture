namespace AgentFramework.Core.Agent.Handlers;

/// <summary>
/// Extension helpers for reading prior handler outputs from the chain journal.
///
/// Context-chain threading pattern:
/// Each handler in a <see cref="IStepChain"/> receives the full journal of prior exchanges.
/// Use these extensions to pull exactly the context you need into the next prompt:
/// <list type="bullet">
///   <item>
///     <b><see cref="LastOutput"/></b> — gets the most recently produced output.
///     Use this when each handler's input is always the previous handler's output
///     (strict pipeline flow, e.g. Organize chain).
///   </item>
///   <item>
///     <b><see cref="GetOutput"/></b> — gets output by the producing handler's name.
///     Use this when a handler needs a specific earlier handler's output regardless of chain position
///     (e.g. <c>IslandFromQuestionsHandler</c> always needs <c>IslandFromObjectiveHandler</c>'s output).
///   </item>
/// </list>
/// </summary>
public static class HandlerContextExtensions
{
    /// <summary>
    /// Returns the output produced by the handler named <paramref name="senderName"/>,
    /// or <c>null</c> if that handler has not yet run.
    /// </summary>
    public static string? GetOutput(this IReadOnlyList<HandlerExchange> context, string senderName)
        => context.FirstOrDefault(e => e.Sender == senderName)?.Content.Output;

    /// <summary>
    /// Returns the output of the last handler that ran, or <c>null</c> when the journal is empty
    /// (i.e. this is the first handler in the chain).
    /// </summary>
    public static string? LastOutput(this IReadOnlyList<HandlerExchange> context)
        => context.Count > 0 ? context[^1].Content.Output : null;
}
