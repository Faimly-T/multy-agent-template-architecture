using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Events;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent;

public class AgentAggregate<TId>
{
    public TId Id { get; protected set; } = default!;
    public Role Role { get; protected set; } = default!;
    public AgentSession? Session { get; private set; }
    public ConversationHistory Conversation { get; } = new();

    private readonly List<DomainEvent> _domainEvents = [];
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private readonly List<AgentStep> _steps = [];
    public IReadOnlyList<AgentStep> Steps => _steps.AsReadOnly();

    public int CurrentStepIndex { get; private set; }
    public bool IsCompleted => CurrentStepIndex >= _steps.Count;

    protected AgentAggregate() { }

    public AgentAggregate(TId id, Role role)
    {
        Id = id;
        Role = role;
    }

    public AgentSession StartSession(string sessionObjective, int sessionIteration = 1)
    {
        Session = new AgentSession(sessionObjective, sessionIteration);
        return Session;
    }

    protected void AddStep(AgentStep step) => _steps.Add(step);

    protected void ReplaceSteps(IReadOnlyList<AgentStep> steps)
    {
        _steps.Clear();
        _steps.AddRange(steps);
    }

    protected void RaiseDomainEvent(DomainEvent @event) => _domainEvents.Add(@event);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public async Task<StepResult> ExecuteNextStepAsync(IStepExecutor executor, CancellationToken ct = default)
    {
        if (IsCompleted)
            throw new InvalidOperationException("All steps have been completed.");

        var step = _steps[CurrentStepIndex];

        RaiseDomainEvent(new StepStarted(step.StepNumber, step.Name, step.SkillName));

        var result = await executor.ExecuteAsync(step, Role, ct);

        if (result.GateSatisfied)
        {
            Session?.Apply(result);
            RaiseDomainEvent(new StepCompleted(step.StepNumber, step.Name, result));
            CurrentStepIndex++;
        }
        else
        {
            RaiseDomainEvent(new StepGateFailed(step.StepNumber, step.Name, step.Gate.Description, result));
        }

        return result;
    }

    public async Task<StepResult> ExecuteNextStepAsync(
        IStepMessageBuilder messageBuilder,
        IChatClient chatClient,
        CancellationToken ct = default)
    {
        if (IsCompleted)
            throw new InvalidOperationException("All steps have been completed.");

        var step = _steps[CurrentStepIndex];

        RaiseDomainEvent(new StepStarted(step.StepNumber, step.Name, step.SkillName));

        var messages = messageBuilder.BuildMessages(step, Role, Session, Conversation);

        foreach (var msg in messages)
        {
            if (msg.Role == MessageRole.System)
                Conversation.AddSystemMessage(msg.Content);
            else
                Conversation.AddUserMessage(msg.Content);
        }

        var result = await chatClient.SendAsync(Conversation.Messages, step, ct);

        Conversation.AddAssistantMessage(result.Output);

        if (result.GateSatisfied)
        {
            Session?.Apply(result);
            RaiseDomainEvent(new StepCompleted(step.StepNumber, step.Name, result));
            CurrentStepIndex++;
        }
        else
        {
            RaiseDomainEvent(new StepGateFailed(step.StepNumber, step.Name, step.Gate.Description, result));
        }

        return result;
    }

    public async Task<IReadOnlyList<StepResult>> ExecuteAllStepsAsync(IStepExecutor executor, CancellationToken ct = default)
    {
        var results = new List<StepResult>();

        while (!IsCompleted)
        {
            var result = await ExecuteNextStepAsync(executor, ct);
            results.Add(result);

            if (!result.GateSatisfied)
                break;
        }

        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<StepResult>> ExecuteAllStepsAsync(
        IStepMessageBuilder messageBuilder,
        IChatClient chatClient,
        CancellationToken ct = default)
    {
        var results = new List<StepResult>();

        while (!IsCompleted)
        {
            var result = await ExecuteNextStepAsync(messageBuilder, chatClient, ct);
            results.Add(result);

            if (!result.GateSatisfied)
                break;
        }

        return results.AsReadOnly();
    }
}