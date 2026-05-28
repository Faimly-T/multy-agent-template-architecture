using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Domain.UxAgent;

public class UxStepBuilder
{
    private readonly List<AgentStep> _steps = [];

    public static UxStepBuilder Create() => new();

    public UxStepBuilder WithStep(AgentStep step)
    {
        _steps.Add(step);
        return this;
    }

    public UxStepBuilder WithSteps(IEnumerable<AgentStep> steps)
    {
        _steps.AddRange(steps);
        return this;
    }

    public StepPipeline Build()
    {
        if (_steps.Count == 0)
            throw new InvalidOperationException("At least one step is required.");

        return new StepPipeline(_steps);
    }
}
