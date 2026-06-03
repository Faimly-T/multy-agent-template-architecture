namespace AgentFramework.Core.Agent.Steps;

/// <summary>
/// Declares the advancement condition for a pipeline step.
///
/// A <see cref="Gate"/> is a named contract: it describes in plain language what must be true
/// before the pipeline advances to the next step. The actual evaluation logic lives in the step
/// itself via <see cref="AgentStep.EvaluateGate"/> — override that method to base the decision
/// on parsed domain data (e.g. island count, group readiness) rather than a generic JSON flag.
///
/// Gate semantics:
/// <list type="bullet">
///   <item>When <see cref="AgentStep.EvaluateGate"/> returns <c>true</c> → the step's
///     <see cref="StepResult.GateSatisfied"/> is <c>true</c> → the pipeline advances.</item>
///   <item>When it returns <c>false</c> → the pipeline halts at this step and the caller
///     receives the partial result for error handling or retry logic.</item>
/// </list>
/// </summary>
public record Gate(string Description);
