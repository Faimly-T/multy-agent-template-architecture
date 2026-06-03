using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent;

/// <summary>
/// Read-only view of the agent's current session state, passed to every handler via
/// <see cref="Handlers.ICommandHandler.ExecuteAiCommandAsync"/>.
///
/// Handlers use this to build informed prompts without writing to the session directly.
/// All mutations go through <see cref="Session.ISessionWriter"/> (the write façade).
/// </summary>
public interface IAgentRunContext
{
    /// <summary>
    /// The full session — checkpoints, brain (islands/groups/decisions), and convenience delegates.
    /// <c>null</c> only before the very first iteration starts.
    /// </summary>
    AgentSession? Session { get; }

    /// <summary>All questions raised across the current and prior iterations, in any status.</summary>
    IReadOnlyList<Question> Questions { get; }

    /// <summary>All strategic decisions recorded by the Organize and Distill steps so far.</summary>
    IReadOnlyList<Decision> Decisions { get; }

    /// <summary>All output artifacts planned by the Distill step in the current iteration.</summary>
    IReadOnlyList<Deliverable> Deliverables { get; }

    /// <summary>
    /// The user's <see cref="AgentRequest"/> for the current iteration — set by
    /// <c>RunAsync</c> and available to all Kickoff handlers before
    /// <see cref="Session.ISessionWriter.BeginIteration"/> is called.
    /// Becomes <c>null</c> once BeginIteration consumes it into the new Checkpoint.
    /// </summary>
    AgentRequest? PendingRequest { get; }

    /// <summary>
    /// Convenience string accessor for the pending request's intent text.
    /// Returns an empty string when no request is pending.
    /// </summary>
    string PendingUserIntent => PendingRequest?.Intent ?? string.Empty;

    /// <summary>
    /// Returns the full handler journal recorded for <paramref name="stepName"/> in a prior step,
    /// or an empty list when the step has not yet run.
    /// </summary>
    IReadOnlyList<HandlerExchange> GetStepJournal(string stepName);
}
