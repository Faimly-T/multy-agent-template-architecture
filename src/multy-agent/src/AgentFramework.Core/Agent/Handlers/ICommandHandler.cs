using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Handlers;

/// <summary>
/// The unit of domain reasoning inside a pipeline step.
///
/// Each handler encapsulates one focused LLM instruction (or a pure context-assembly step with
/// no LLM call). Handlers are composed sequentially inside a <see cref="Steps.IStepChain"/>;
/// each handler receives the chain journal built by its predecessors and contributes one new
/// <see cref="HandlerExchange"/> to that journal.
///
/// <b>Execution contract</b>
/// <list type="bullet">
///   <item>Read prior reasoning from <c>context</c> via <see cref="HandlerContextExtensions"/>.</item>
///   <item>Read agent state (questions, decisions, brain) from <c>contextAgent</c>.</item>
///   <item>Write urgent session mutations via <c>writer</c>; defer bulk mutations to
///     <see cref="Steps.StepResult.ApplyTo"/> after the chain finishes.</item>
///   <item>Call <c>chatClient.SendHandlerAsync</c> for structured JSON responses;
///     skip it entirely for pure context-assembly handlers.</item>
/// </list>
///
/// <b>SRP reminder:</b> one handler = one responsibility. If a handler does two distinct things, split it.
/// </summary>
public interface ICommandHandler
{
    /// <summary>Simple class name — used as the lookup key in <see cref="HandlerContextExtensions.GetOutput"/>.</summary>
    string Name => GetType().Name;

    /// <param name="context">Journal of exchanges from prior handlers. Empty on the first handler.</param>
    /// <param name="contextAgent">Read-only session state. <c>null</c> in isolated unit tests.</param>
    /// <param name="writer">Write façade for urgent session mutations (e.g. answering a question mid-chain).</param>
    /// <param name="chatClient">LLM port. <c>null</c> for non-LLM handlers.</param>
    /// <param name="ct">Propagated cancellation token.</param>
    Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext?              contextAgent,
        ISessionWriter                 writer,
        IChatClient?                   chatClient,
        CancellationToken              ct);
}
