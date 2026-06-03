using AgentFramework.Core.Agent.Handlers;

namespace AgentFramework.Core.Agent.Session;

/// <summary>
/// Façade interface for all write operations on the agent session state.
///
/// Combines three segregated write interfaces (ISP) plus lifecycle methods:
/// <list type="bullet">
///   <item><see cref="IBrainWriter"/> — islands, groups, decisions (the reasoning brain).</item>
///   <item><see cref="IQuestionWriter"/> — question lifecycle (raise, answer, review, restore).</item>
///   <item><see cref="IDeliverableTracker"/> — output artifact tracking.</item>
/// </list>
///
/// Only <see cref="AgentAggregate{TId}"/> implements this interface.
/// <see cref="Steps.StepResult.ApplyTo"/> receives it as the sole write channel, ensuring that
/// all session mutations are routed through the aggregate's invariant-enforcement logic.
/// </summary>
public interface ISessionWriter : IBrainWriter, IQuestionWriter, IDeliverableTracker
{
    /// <summary>
    /// Opens a new iteration by creating an immutable <see cref="Checkpoint"/> that pairs the
    /// LLM-synthesised <paramref name="sessionObjective"/> with the raw user intent that was
    /// pending on the aggregate. Called by <c>KickoffResult.ApplyTo</c>.
    /// </summary>
    void BeginIteration(string sessionObjective);

    /// <summary>Stores the full <see cref="HandlerExchange"/> journal produced by a step for tracing.</summary>
    void RecordStepJournal(int stepNumber, string stepName, IReadOnlyList<HandlerExchange> journal);

    /// <summary>Accumulates token consumption into the current checkpoint. Called by <c>ExpressResult.ApplyTo</c>.</summary>
    void UpdateTokenConsumption(int inputTokens, int outputTokens);

    /// <summary>Seals the current checkpoint with a <c>ClosedAt</c> timestamp. Called by <c>ClosedStep</c>.</summary>
    void FinalizeSession(DateTime closedAt);
}
