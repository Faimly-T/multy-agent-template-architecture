namespace AgentFramework.Core.Agent.Session;

/// <summary>
/// Lifecycle state of a <see cref="Question"/> raised during the pipeline.
///
/// State machine:
/// <code>
///   Open ──→ Answered ──→ Reviewed
///     │                      │
///     └──────────────────→ Obsolete
/// </code>
/// </summary>
public enum QuestionStatus
{
    /// <summary>Raised but not yet answered. Surfaces in the Kickoff triage of subsequent iterations.</summary>
    Open,

    /// <summary>An answer was supplied (via <see cref="Question.SetAnswer"/>) — awaiting Express-phase review.</summary>
    Answered,

    /// <summary>The answer was incorporated into a deliverable or decision during the Express phase.</summary>
    Reviewed,

    /// <summary>No longer relevant — superseded by new context or rendered moot by a decision.</summary>
    Obsolete
}
