namespace AgentFramework.Core.Agent.Steps;

public class StepPipeline
{
    private readonly List<AgentStep> _steps;

    public StepPipeline(IReadOnlyList<AgentStep> steps)
    {
        if (steps.Count == 0)
            throw new ArgumentException("At least one step is required.", nameof(steps));

        _steps = [.. steps];
    }

    public IReadOnlyList<AgentStep> Steps => _steps.AsReadOnly();

    public int CurrentStepIndex { get; private set; }

    public AgentStep CurrentStep => _steps[CurrentStepIndex];

    public bool IsCompleted => CurrentStepIndex >= _steps.Count;

    public int Count => _steps.Count;

    public AgentStep this[int index] => _steps[index];

    internal void Advance() => CurrentStepIndex++;
}
