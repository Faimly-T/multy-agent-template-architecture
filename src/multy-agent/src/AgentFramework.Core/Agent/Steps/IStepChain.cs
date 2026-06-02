using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps;

/// <summary>
/// Runs a sequence of <see cref="ICommandHandler"/> instances and returns their combined output.
///
/// A chain is the multi-handler execution path inside a step (contrast with the single-LLM
/// fallback used when no chain is configured). The sole concrete implementation is
/// <c>SequentialCommandHandlerChain</c>, which threads each handler's output as the next
/// handler's context seed.
/// </summary>
public interface IStepChain
{
    /// <param name="context">Agent session state, passed to every handler in the chain.</param>
    /// <param name="writer">Write façade for mid-chain session mutations.</param>
    /// <param name="chatClient">LLM port injected into every handler.</param>
    /// <param name="ct">Propagated cancellation token.</param>
    /// <returns>
    /// <b>FinalJson</b> — the last handler's output string, expected to be valid JSON that
    /// <see cref="AgentStep.ParseResult"/> can deserialize into a <see cref="StepResult"/>.
    /// <b>Journal</b> — the full ordered list of all handler exchanges, used for tracing
    /// and stored in the session via <see cref="Session.ISessionWriter.RecordStepJournal"/>.
    /// </returns>
    Task<(string FinalJson, IReadOnlyList<HandlerExchange> Journal)> RunAsync(
        IAgentRunContext? context,
        ISessionWriter    writer,
        IChatClient       chatClient,
        CancellationToken ct);
}
