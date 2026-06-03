using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps;

/// <summary>
/// The output of a completed pipeline step, carrying the LLM's raw response and the gate decision.
///
/// Follows the <b>Command pattern</b>: each concrete result subclass (<c>KickoffResult</c>,
/// <c>CaptureResult</c>, etc.) knows how to apply its own domain mutations to the aggregate
/// by overriding <see cref="ApplyTo"/>. This keeps mutation logic co-located with the data
/// and prevents the aggregate from needing to know the shape of every step result.
///
/// Pattern summary:
/// <list type="number">
///   <item>Step runs its chain → produces a concrete <c>StepResult</c> subtype.</item>
///   <item><c>AgentAggregate</c> calls <c>result.ApplyTo(this)</c>.</item>
///   <item>The result writes itself into the session state via <see cref="ISessionWriter"/>.</item>
/// </list>
/// </summary>
public record StepResult(string Output, bool GateSatisfied)
{
    /// <summary>
    /// Applies this result's domain mutations to the aggregate via the write façade.
    /// Base implementation is a no-op — concrete result types override this to write
    /// islands, groups, decisions, deliverables, or questions into the session.
    /// </summary>
    public virtual void ApplyTo(ISessionWriter writer) { }
}
