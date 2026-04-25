using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Events;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Core.Agent.Steps.CODESteps;

namespace AgentFramework.Core.Agent;

public class AgentAggregate<TId> :
    ISessionWriter,
    ISessionObjectiveWriter,
    ISessionBacklogWriter,
    IQuestionWriter,
    ITokenConsumptionWriter,
    IDomainEventPublisher
{
    public TId Id { get; protected set; } = default!;
    public Role Role { get; protected set; } = default!;
    public AgentSession? Session { get; private set; }

    private readonly ConversationHistory _conversation = new();
    public IReadOnlyList<ChatMessage> ConversationMessages => _conversation.Messages;

    private readonly List<DomainEvent> _domainEvents = [];
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public StepPipeline Pipeline { get; protected set; } = default!;

    public IReadOnlyList<AgentStep> Steps => Pipeline.Steps;
    public bool IsCompleted => Pipeline.IsCompleted;

    private readonly AgentStepExecutor _executor;

    protected AgentAggregate()
    {
        _executor = new AgentStepExecutor(_conversation, this);
    }

    public AgentAggregate(TId id, Role role)
    {
        Id = id;
        Role = role;
        _executor = new AgentStepExecutor(_conversation, this);
    }

    public AgentSession StartSession(string sessionObjective, int sessionIteration = 1)
    {
        Session = new AgentSession(sessionObjective, sessionIteration);
        return Session;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    // --- Question Queries ---

    public IReadOnlyList<Question> GetQuestions()
        => Session?.Questions ?? [];

    public IReadOnlyList<Question> GetQuestions(QuestionStatus status)
        => Session?.Questions.Where(q => q.Status == status).ToList().AsReadOnly()
           ?? [];

    public IReadOnlyList<Question> GetOpenQuestions()
        => GetQuestions(QuestionStatus.Open);

    public IReadOnlyList<Question> GetPendingReviewQuestions()
        => GetQuestions(QuestionStatus.Answered);

    // --- Supply Answers (between sessions) ---

    public void SupplyAnswers(IReadOnlyList<(string QuestionId, string Answer, string AnswerSource)> answers)
    {
        if (Session is null)
            throw new InvalidOperationException("No active session. Start a session before supplying answers.");

        foreach (var (questionId, answer, answerSource) in answers)
        {
            var question = Session.FindQuestion(questionId)
                ?? throw new InvalidOperationException($"Question '{questionId}' not found.");
            question.SetAnswer(answer, answerSource);
        }
    }

    public async Task<StepResult> ExecuteNextStepAsync(
        IStepMessageBuilder messageBuilder,
        IChatClient chatClient,
        CancellationToken ct = default)
    {
        if (IsCompleted)
            throw new InvalidOperationException("All steps have been completed.");

        if (!Pipeline.TryGetCurrentStep(out var step))
            throw new InvalidOperationException("No current step available.");

        var result = await _executor.ExecuteStepAsync(step!, Role, Session, messageBuilder, chatClient, ct);

        if (result.GateSatisfied)
            Pipeline.Advance();

        return result;
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

    public async Task<AgentRunResult> RunAsync(
        string objective,
        IChatClient chatClient,
        ISkillProvider skillProvider,
        IDeliverableWriter deliverableWriter,
        IPipelineFactory pipelineFactory,
        IStepMessageBuilder messageBuilder,
        CancellationToken ct = default)
    {
        ClearDomainEvents();

        Pipeline = await pipelineFactory.CreatePipelineAsync(Role, skillProvider, ct);
        StartSession(objective);

        var results = new List<StepResult>();

        while (!IsCompleted)
        {
            var result = await ExecuteNextStepAsync(messageBuilder, chatClient, ct);
            results.Add(result);

            if (!result.GateSatisfied)
                break;
        }

        await deliverableWriter.WriteAsync(Session!, results.AsReadOnly(), ct);

        return new AgentRunResult(
            Session!,
            results.AsReadOnly(),
            DomainEvents,
            IsCompleted);
    }

    // ==========================================================
    // IDomainEventPublisher implementation
    // ==========================================================

    void IDomainEventPublisher.Publish(DomainEvent @event) => _domainEvents.Add(@event);

    // ==========================================================
    // Writer interface implementations
    // ==========================================================

    void ISessionWriter.UpdateObjective(string sessionObjective)
    {
        Session!.UpdateObjective(sessionObjective);
    }

    void ISessionWriter.SetCapturedIslands(IReadOnlyList<CapturedIsland> islands)
    {
        Session!.Backlog.SetCaptured(islands);
    }

    void ISessionWriter.ApplyOrganization(IReadOnlyList<IslandOrganization> organizations, IReadOnlyList<DecisionRecord> decisions)
    {
        Session!.Backlog.ApplyOrganization(organizations);

        foreach (var dec in decisions)
            Session.RecordDecision(dec.Id, dec.Description, dec.Impact);
    }

    void ISessionWriter.ApplyDistillation(IReadOnlyList<IslandDistillation> distillations, IReadOnlyList<DeliverableRecord> deliverables)
    {
        Session!.Backlog.ApplyDistillation(distillations);

        foreach (var del in deliverables)
            Session.RecordDeliverable(del.DeliverableId, del.Path, del.Status);
    }

    void ISessionWriter.UpdateTokenConsumption(int inputTokens, int outputTokens)
    {
        Session!.UpdateTokenConsumption(inputTokens, outputTokens);
    }

    void ISessionWriter.RaiseQuestion(string id, string text, string source)
    {
        if (Session!.FindQuestion(id) is not null)
            throw new InvalidOperationException($"Question '{id}' already exists in this session.");

        Session.RaiseQuestion(id, text, source);
    }

    void ISessionWriter.ReviewQuestion(string id, QuestionStatus newStatus)
    {
        var existing = Session!.FindQuestion(id);
        if (existing is not null)
        {
            Session.ApplyQuestionReview(id, newStatus);
        }
        else if (newStatus == QuestionStatus.Open)
        {
            Session.RaiseQuestion(id, string.Empty, "express-relay");
        }
    }

    void ISessionObjectiveWriter.UpdateObjective(string sessionObjective)
        => ((ISessionWriter)this).UpdateObjective(sessionObjective);

    void ISessionBacklogWriter.SetCapturedIslands(IReadOnlyList<CapturedIsland> islands)
        => ((ISessionWriter)this).SetCapturedIslands(islands);

    void ISessionBacklogWriter.ApplyOrganization(IReadOnlyList<IslandOrganization> organizations, IReadOnlyList<DecisionRecord> decisions)
        => ((ISessionWriter)this).ApplyOrganization(organizations, decisions);

    void ISessionBacklogWriter.ApplyDistillation(IReadOnlyList<IslandDistillation> distillations, IReadOnlyList<DeliverableRecord> deliverables)
        => ((ISessionWriter)this).ApplyDistillation(distillations, deliverables);

    void IQuestionWriter.RaiseQuestion(string id, string text, string source)
        => ((ISessionWriter)this).RaiseQuestion(id, text, source);

    void IQuestionWriter.ReviewQuestion(string id, QuestionStatus newStatus)
        => ((ISessionWriter)this).ReviewQuestion(id, newStatus);

    void ITokenConsumptionWriter.UpdateTokenConsumption(int inputTokens, int outputTokens)
        => ((ISessionWriter)this).UpdateTokenConsumption(inputTokens, outputTokens);
}
