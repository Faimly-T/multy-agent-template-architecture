namespace AgentFramework.Core.Agent.Steps;

public class StepPipeline
{
    private readonly List<AgentStep> _steps;

    public StepPipeline(IEnumerable<AgentStep> steps)
    {
        _steps = [.. steps];

        if (_steps.Count == 0)
            throw new ArgumentException("At least one step is required.", nameof(steps));
    }

    public IReadOnlyList<AgentStep> Steps => _steps.AsReadOnly();

    public int CurrentStepIndex { get; private set; }

    public AgentStep CurrentStep => _steps[CurrentStepIndex];

    public bool IsCompleted => CurrentStepIndex >= _steps.Count;

    public int Count => _steps.Count;

    public AgentStep this[int index] => _steps[index];

    public bool TryGetCurrentStep(out AgentStep? step)
    {
        if (IsCompleted)
        {
            step = null;
            return false;
        }

        step = _steps[CurrentStepIndex];
        return true;
    }

    internal void Advance()
    {
        if (IsCompleted)
            throw new InvalidOperationException("Cannot advance beyond the last step.");

        CurrentStepIndex++;
    }
}
