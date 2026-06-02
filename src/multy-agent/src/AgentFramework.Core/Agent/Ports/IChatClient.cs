using System.Text.Json;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Ports;

/// <summary>
/// Port (abstraction) for all outbound LLM calls.
///
/// Two call surfaces, each with a distinct contract:
/// <list type="bullet">
///   <item>
///     <b><see cref="SendAsync"/></b> — step-level call. Used by the single-LLM fallback path in
///     <see cref="AgentStep.ExecuteStepAsync"/> when no <see cref="IStepChain"/> is wired.
///     The step provides its JSON schema; the client parses and returns a typed <see cref="StepResult"/>.
///   </item>
///   <item>
///     <b><see cref="SendHandlerAsync{TResult}"/></b> — handler-level call. Used by every
///     <see cref="Handlers.ICommandHandler"/> that needs a focused, single-purpose LLM call.
///     The caller supplies the JSON schema and a parse delegate; the implementation handles
///     retries, extraction, and deserialization.
///   </item>
/// </list>
/// </summary>
public interface IChatClient
{
    /// <summary>
    /// Executes a step-level LLM call. Used by the default (non-chain) step path.
    /// </summary>
    Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default);

    /// <summary>
    /// Executes a handler-level LLM call with structured JSON output.
    /// <paramref name="jsonSchema"/> is injected into the system prompt so the model knows
    /// the exact shape expected. <paramref name="parse"/> converts the root JSON element into
    /// the strongly-typed result.
    /// Default implementation throws <see cref="NotSupportedException"/> — override in any
    /// <see cref="IChatClient"/> that supports the Kickoff-chain handler pattern.
    /// </summary>
    Task<TResult> SendHandlerAsync<TResult>(
        IReadOnlyList<ChatMessage> messages,
        string jsonSchema,
        Func<JsonElement, TResult> parse,
        CancellationToken ct = default)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement SendHandlerAsync. " +
            "Override this method to support handler-level LLM calls.");
}
