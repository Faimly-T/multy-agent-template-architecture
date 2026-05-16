using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps;

public record StepResult(string Output, bool GateSatisfied)
{
    public virtual void ApplyTo(ISessionWriter writer) { }

    public virtual IEnumerable<DomainEvent> GetDomainEvents() => [];
}
