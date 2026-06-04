namespace AgentFramework.Core.Agent.Session;

/// <summary>
/// Produces a full lineage summary of the Brain across all groups.
///
/// A lineage summary answers: "For every group the Brain reasoned about,
/// what Islands were captured, what Decisions were made, and what Deliverables were produced?"
///
/// The Brain owns all Deliverables directly, so no external parameter is needed.
/// Implementations may format this as markdown, JSON, or a structured in-memory list.
/// </summary>
public interface IBrainSummarizer
{
    IReadOnlyList<BrainLineage> Summarize(BrainAggregate brain);
}
