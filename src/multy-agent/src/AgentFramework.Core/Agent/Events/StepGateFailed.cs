using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Events;

public record StepGateFailed(int StepNumber, string StepName, string GateDescription, StepResult Result) : DomainEvent;
