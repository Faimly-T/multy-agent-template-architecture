using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent;

/// <summary>
/// The complete output of a single <see cref="AgentAggregate{TId}.RunAsync"/> call.
///
/// This is the value the caller inspects after the pipeline finishes — or halts early because
/// a gate was not satisfied. <see cref="Completed"/> distinguishes the two cases.
/// </summary>
public record AgentRunResult(
    /// <summary>Full session state: checkpoints, brain (islands/groups/decisions), and iteration history.</summary>
    AgentSession Session,
    /// <summary>Ordered list of each step's result, including the raw LLM output and gate outcome.</summary>
    IReadOnlyList<StepResult> StepResults,
    /// <summary><c>true</c> when all steps ran and all gates were satisfied; <c>false</c> on early halt.</summary>
    bool Completed,
    /// <summary>All questions — Open, Answered, Reviewed, and Obsolete — from this iteration.</summary>
    IReadOnlyList<Question> Questions,
    /// <summary>All strategic decisions recorded by Organize and Distill steps.</summary>
    IReadOnlyList<Decision> Decisions,
    /// <summary>All output artifacts planned by the Distill step.</summary>
    IReadOnlyList<Deliverable> Deliverables);
