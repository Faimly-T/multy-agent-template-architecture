namespace AgentFramework.Core.Agent;

/// <summary>
/// A user's instruction to the agent for a single pipeline iteration.
///
/// This is the <b>Command</b> (DDD) that triggers
/// <see cref="AgentAggregate{TId}.RunAsync"/>. The raw intent text seeds the Kickoff
/// step's <c>ObjectiveSynthesisHandler</c>, which refines it into a focused
/// <see cref="Session.Checkpoint.SessionObjective"/>.
///
/// Usage:
/// <code>
/// // Explicit (recommended for clarity):
/// await agent.RunAsync(
///     new AgentRequest("I want that you build the UX for a system that profiles student athletes as leads."),
///     chatClient, writer);
///
/// // Implicit from string (still valid):
/// await agent.RunAsync("I want that you build the UX for ...", chatClient, writer);
/// </code>
/// </summary>
public sealed record AgentRequest
{
    /// <summary>The natural-language instruction the user wants the agent to act on this iteration.</summary>
    public string Intent { get; }

    public AgentRequest(string intent)
    {
        if (string.IsNullOrWhiteSpace(intent))
            throw new ArgumentException("Agent request intent cannot be empty.", nameof(intent));
        Intent = intent.Trim();
    }

    /// <summary>
    /// Implicit conversion from <see cref="string"/> so existing callers do not need to change:
    /// <c>await agent.RunAsync("I want...", chatClient, writer)</c> continues to compile.
    /// </summary>
    public static implicit operator AgentRequest(string intent) => new(intent);

    public override string ToString() => Intent;
}
