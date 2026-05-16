using System.Text.Json;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Ports;

public interface IChatClient
{
    Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default);

    // Used by rehydrate chain handlers for focused, single-purpose LLM calls.
    // Default throws to preserve backward compatibility with existing implementations.
    Task<TResult> SendHandlerAsync<TResult>(
        IReadOnlyList<ChatMessage> messages,
        string jsonSchema,
        Func<JsonElement, TResult> parse,
        CancellationToken ct = default)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement SendHandlerAsync. " +
            "Override this method to support handler-level LLM calls.");
}
