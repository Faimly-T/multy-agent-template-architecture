using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.CodePipeline;

/// <summary>
/// Runs a fixed sequence of <see cref="ICommandHandler"/> instances, passing each handler's
/// output into the next handler's context journal.
///
/// This is the single chain implementation for the whole pipeline.
/// Domain projects compose their step handlers inside a <c>ChainFactory</c> lambda in
/// <see cref="StepSkillConfig"/> and wrap them with this class — no separate chain class needed.
///
/// Usage in UxAgentDefaults + UxAgent.BuildAsync:
/// <code>
/// ChainFactory: ctx => new SequentialCommandHandlerChain(
///     new CheckpointValidatorCommand(ctx),
///     new DistillObjectiveHandler(),
///     new QuestionTriageHandler(ctx),
///     new ObjectiveSynthesisHandler(ctx))
/// </code>
/// </summary>
public sealed class SequentialCommandHandlerChain(params ICommandHandler[] handlers) : IStepChain
{
    public async Task<(string FinalJson, IReadOnlyList<HandlerExchange> Journal)> RunAsync(
        IAgentRunContext? context,
        ISessionWriter    writer,
        IChatClient       chatClient,
        CancellationToken ct)
    {
        var journal = new List<HandlerExchange>();

        foreach (var handler in handlers)
        {
            var exchange = await handler.ExecuteAiCommandAsync(
                journal.AsReadOnly(), context, writer, chatClient, ct);
            journal.Add(exchange);
        }

        return journal.Count > 0
            ? (journal[^1].Content.Output, journal.AsReadOnly())
            : throw new InvalidOperationException(
                $"{nameof(SequentialCommandHandlerChain)} completed without producing output.");
    }
}
