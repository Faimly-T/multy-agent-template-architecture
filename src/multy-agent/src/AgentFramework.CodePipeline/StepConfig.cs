using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.CodePipeline;

/// <summary>
/// Serializable descriptor for a single pipeline step.
/// Contains only data — no assembly logic, no Func&lt;&gt; delegates.
/// Pass a collection of these (via <see cref="AgentConfig"/>) to <see cref="AgentBuilder"/>
/// which wires the correct step type and handler chain for each position.
/// </summary>
public record StepConfig(
    string                Instructions,
    Gate                  Gate,
    IReadOnlyList<string> SkillNames);
