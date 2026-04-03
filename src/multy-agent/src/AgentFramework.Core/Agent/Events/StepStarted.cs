using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Events;

public record StepStarted(int StepNumber, string StepName, string SkillName) : DomainEvent;
