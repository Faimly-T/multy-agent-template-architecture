using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Events;

public record StepCompleted(int StepNumber, string StepName, StepResult Result) : DomainEvent;
