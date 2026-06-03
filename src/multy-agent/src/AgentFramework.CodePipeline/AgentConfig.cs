using AgentFramework.Core.Agent.Prompts;

namespace AgentFramework.CodePipeline;

/// <summary>
/// Unified serializable configuration for a CODE pipeline agent.
/// Contains only data — no assembly logic, no delegates.
/// Stores cleanly as JSON or a database row; load it back and pass to
/// <see cref="AgentBuilder.FromConfig"/> to assemble a live pipeline.
/// </summary>
public record AgentConfig(
    string         AgentId,
    string         ProjectId,
    RoleDefinition Role,
    StepConfig     Kickoff,
    StepConfig     Capture,
    StepConfig     Organize,
    StepConfig     Distill,
    StepConfig     Express,
    StepConfig?    Closed = null);
