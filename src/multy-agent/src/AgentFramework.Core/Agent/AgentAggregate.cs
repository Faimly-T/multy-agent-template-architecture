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
    IDomainEventPublisher,
    IAgentRunContext
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

    // --- Domain outputs (moved from AgentSession) ---

    private readonly List<Question> _questions = [];
    private readonly List<Decision> _decisions = [];
    private readonly List<Deliverable> _deliverables = [];

    public IReadOnlyList<Question> Questions => _questions.AsReadOnly();
    public IReadOnlyList<Decision> Decisions => _decisions.AsReadOnly();
    public IReadOnlyList<Deliverable> Deliverables => _deliverables.AsReadOnly();

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

    // --- Session lifecycle ---

    public AgentSession OpenSession(string projectId, SessionMarkFilePaths markFilePaths, string initialObjective = "")
    {
        Session = new AgentSession(projectId, markFilePaths);
        if (!string.IsNullOrEmpty(initialObjective))
            Session.BeginIteration(initialObjective);
        return Session;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    // --- Question Queries ---

    public IReadOnlyList<Question> GetQuestions()
        => _questions.AsReadOnly();

    public IReadOnlyList<Question> GetQuestions(QuestionStatus status)
        => _questions.Where(q => q.Status == status).ToList().AsReadOnly();

    public IReadOnlyList<Question> GetOpenQuestions()
        => GetQuestions(QuestionStatus.Open);

    public IReadOnlyList<Question> GetPendingReviewQuestions()
        => GetQuestions(QuestionStatus.Answered);

    public Question? FindQuestion(string id) => _questions.Find(q => q.Id == id);

    public void RaiseQuestion(string id, string text, string source)
        => ((ISessionWriter)this).RaiseQuestion(id, text, source);

    public void ApplyQuestionReview(string id, QuestionStatus newStatus)
        => ((ISessionWriter)this).ReviewQuestion(id, newStatus);

    // --- Supply Answers (between sessions) ---

    public void SupplyAnswers(IReadOnlyList<(string QuestionId, string Answer, string AnswerSource)> answers)
    {
        if (Session is null)
            throw new InvalidOperationException("No active session. Open a session before supplying answers.");

        foreach (var (questionId, answer, answerSource) in answers)
        {
            var question = FindQuestion(questionId)
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

        var result = await _executor.ExecuteStepAsync(step!, Role, this, this, messageBuilder, chatClient, ct);

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
        if (Session is null)
            throw new InvalidOperationException("Call OpenSession before RunAsync.");

        ClearDomainEvents();

        Pipeline = await pipelineFactory.CreatePipelineAsync(Role, skillProvider, ct);
        Session.BeginIteration(objective);

        var results = new List<StepResult>();

        while (!IsCompleted)
        {
            var result = await ExecuteNextStepAsync(messageBuilder, chatClient, ct);
            results.Add(result);

            if (!result.GateSatisfied)
                break;
        }

        await deliverableWriter.WriteAsync(this, results.AsReadOnly(), ct);

        return new AgentRunResult(
            Session!,
            results.AsReadOnly(),
            DomainEvents,
            IsCompleted,
            Questions,
            Decisions,
            Deliverables);
    }

    // ==========================================================
    // IAgentRunContext
    // ==========================================================

    AgentSession? IAgentRunContext.Session => Session;
    IReadOnlyList<Question> IAgentRunContext.Questions => _questions.AsReadOnly();
    IReadOnlyList<Decision> IAgentRunContext.Decisions => _decisions.AsReadOnly();
    IReadOnlyList<Deliverable> IAgentRunContext.Deliverables => _deliverables.AsReadOnly();

    // ==========================================================
    // IDomainEventPublisher
    // ==========================================================

    void IDomainEventPublisher.Publish(DomainEvent @event) => _domainEvents.Add(@event);

    // ==========================================================
    // ISessionWriter
    // ==========================================================

    void ISessionWriter.UpdateObjective(string sessionObjective)
    {
        Session?.UpdateObjective(sessionObjective);
    }

    void ISessionWriter.SetCapturedIslands(IReadOnlyList<CapturedIsland> islands)
    {
        Session?.Backlog.SetCaptured(islands);
    }

    void ISessionWriter.ApplyOrganization(IReadOnlyList<IslandOrganization> organizations, IReadOnlyList<DecisionRecord> decisions)
    {
        Session?.Backlog.ApplyOrganization(organizations);

        foreach (var dec in decisions)
            _decisions.Add(new Decision(dec.Id, dec.Description, dec.Impact));
    }

    void ISessionWriter.ApplyDistillation(IReadOnlyList<IslandDistillation> distillations, IReadOnlyList<DeliverableRecord> deliverables)
    {
        Session?.Backlog.ApplyDistillation(distillations);

        foreach (var del in deliverables)
            _deliverables.Add(new Deliverable(del.DeliverableId, del.Path, del.Status));
    }

    void ISessionWriter.UpdateTokenConsumption(int inputTokens, int outputTokens)
    {
        Session?.UpdateTokenConsumption(inputTokens, outputTokens);
    }

    // ==========================================================
    // IQuestionWriter
    // ==========================================================

    void IQuestionWriter.RaiseQuestion(string id, string text, string source)
    {
        if (FindQuestion(id) is not null)
            throw new InvalidOperationException($"Question '{id}' already exists.");

        _questions.Add(new Question(id, text, source));
    }

    void IQuestionWriter.ReviewQuestion(string id, QuestionStatus newStatus)
    {
        var existing = FindQuestion(id);
        if (existing is not null)
        {
            switch (newStatus)
            {
                case QuestionStatus.Reviewed: existing.MarkReviewed(); break;
                case QuestionStatus.Obsolete: existing.MarkObsolete(); break;
            }
        }
        else if (newStatus == QuestionStatus.Open)
        {
            _questions.Add(new Question(id, string.Empty, "express-relay"));
        }
    }

    void IQuestionWriter.RestoreQuestion(string id, string text, string source, QuestionStatus status,
        string? answer, string? answerSource)
    {
        if (FindQuestion(id) is not null) return;

        var question = new Question(id, text, source);
        _questions.Add(question);

        switch (status)
        {
            case QuestionStatus.Answered:
                question.SetAnswer(answer ?? "restored", answerSource ?? "restored");
                break;
            case QuestionStatus.Reviewed:
                question.SetAnswer(answer ?? "restored", answerSource ?? "restored");
                question.MarkReviewed();
                break;
            case QuestionStatus.Obsolete:
                question.MarkObsolete();
                break;
        }
    }

    void IQuestionWriter.AnswerQuestion(string id, string answer, string answerSource)
    {
        var question = FindQuestion(id);
        if (question?.Status == QuestionStatus.Open)
            question.SetAnswer(answer, answerSource);
    }

    // ==========================================================
    // Sub-interface forwarding
    // ==========================================================

    void ISessionObjectiveWriter.UpdateObjective(string sessionObjective)
        => ((ISessionWriter)this).UpdateObjective(sessionObjective);

    void ISessionBacklogWriter.SetCapturedIslands(IReadOnlyList<CapturedIsland> islands)
        => ((ISessionWriter)this).SetCapturedIslands(islands);

    void ISessionBacklogWriter.ApplyOrganization(IReadOnlyList<IslandOrganization> organizations, IReadOnlyList<DecisionRecord> decisions)
        => ((ISessionWriter)this).ApplyOrganization(organizations, decisions);

    void ISessionBacklogWriter.ApplyDistillation(IReadOnlyList<IslandDistillation> distillations, IReadOnlyList<DeliverableRecord> deliverables)
        => ((ISessionWriter)this).ApplyDistillation(distillations, deliverables);

    void ITokenConsumptionWriter.UpdateTokenConsumption(int inputTokens, int outputTokens)
        => ((ISessionWriter)this).UpdateTokenConsumption(inputTokens, outputTokens);
}
