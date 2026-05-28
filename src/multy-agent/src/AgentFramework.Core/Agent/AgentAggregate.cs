using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
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
    IAgentRunContext
{
    public TId Id { get; protected set; } = default!;
    public Role Role { get; protected set; } = default!;
    public IStepPromptLayer promptContext { get; init; }

    public AgentSession? Session { get; private set; }

    private readonly StepConversationLog _stepConversations = new();

    public IReadOnlyList<ChatMessage> ConversationMessages
    {
        get
        {
            var result = new List<ChatMessage>();
            bool systemAdded = false;
            foreach (var step in _stepConversations.Steps)
            {
                if (!systemAdded)
                {
                    var sys = step.Messages.FirstOrDefault(m => m.Role == MessageRole.System);
                    if (sys is not null) { result.Add(sys); systemAdded = true; }
                }
                result.AddRange(step.Messages.Where(m => m.Role != MessageRole.System));
                result.Add(new ChatMessage(MessageRole.Assistant, step.Response));
            }
            return result.AsReadOnly();
        }
    }

    public StepPipeline Pipeline { get; protected set; } = default!;

    public IReadOnlyList<AgentStep> Steps => Pipeline.Steps;
    public bool IsCompleted => Pipeline.IsCompleted;

    // --- Domain outputs ---

    private readonly List<Question> _questions = [];
    private readonly List<Decision> _decisions = [];
    private readonly List<Deliverable> _deliverables = [];

    public IReadOnlyList<Question> Questions => _questions.AsReadOnly();
    public IReadOnlyList<Decision> Decisions => _decisions.AsReadOnly();
    public IReadOnlyList<Deliverable> Deliverables => _deliverables.AsReadOnly();


    protected AgentAggregate() { }

    public AgentAggregate(TId id, Role role, string projectId, SessionMarkFilePaths markFilePaths)
    {
        Id = id;
        Role = role;
        promptContext = ((IAgentPromptLayer)PromptContext.Empty).WithRole(role);
        Session = new AgentSession(projectId, markFilePaths);
    }

    public void SetUserIntent(string intent) => Session?.SetUserIntent(intent);

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
        foreach (var (questionId, answer, answerSource) in answers)
        {
            var question = FindQuestion(questionId)
                ?? throw new InvalidOperationException($"Question '{questionId}' not found.");
            question.SetAnswer(answer, answerSource);
        }
    }

    public async Task<StepResult> ExecuteNextStepAsync(
        IChatClient chatClient,
        CancellationToken ct = default)
    {
        if (IsCompleted)
            throw new InvalidOperationException("All steps have been completed.");

        if (!Pipeline.TryGetCurrentStep(out var step))
            throw new InvalidOperationException("No current step available.");

        var result = await step!.ExecuteStepAsync(this, this, chatClient, ct);
        result!.ApplyTo(this);

        if (result.GateSatisfied)
            Pipeline.Advance();

        return result;
    }

    public async Task<IReadOnlyList<StepResult>> ExecuteAllStepsAsync(
        IChatClient chatClient,
        CancellationToken ct = default)
    {
        var results = new List<StepResult>();

        while (!IsCompleted)
        {
            var result = await ExecuteNextStepAsync(chatClient, ct);
            results.Add(result);

            if (!result.GateSatisfied)
                break;
        }

        return results.AsReadOnly();
    }

    public async Task<AgentRunResult> RunAsync(
        string userIntent,
        IChatClient chatClient,
        IDeliverableWriter deliverableWriter,
        IPipelineFactory pipelineFactory,
        CancellationToken ct = default)
    {
        Pipeline = await pipelineFactory.CreatePipelineAsync(promptContext, ct);
        Session?.SetUserIntent(userIntent);

        var results = new List<StepResult>();

        while (!IsCompleted)
        {
            var result = await ExecuteNextStepAsync(chatClient, ct);
            results.Add(result);

            if (!result.GateSatisfied)
                break;
        }

        await deliverableWriter.WriteAsync(this, results.AsReadOnly(), ct);

        return new AgentRunResult(
            Session,
            results.AsReadOnly(),
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
    IReadOnlyList<StepConversation> IAgentRunContext.StepConversations => _stepConversations.Steps;

    // ==========================================================
    // ISessionWriter
    // ==========================================================

    void ISessionWriter.BeginIteration(string sessionObjective)
        => Session?.BeginIteration(sessionObjective);

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

    void ISessionWriter.RecordStepExchange(int stepNumber, string stepName,
        IReadOnlyList<ChatMessage> messages, string response)
        => _stepConversations.Record(
            new StepConversation(stepNumber, stepName, messages, response, DateTime.UtcNow));

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
