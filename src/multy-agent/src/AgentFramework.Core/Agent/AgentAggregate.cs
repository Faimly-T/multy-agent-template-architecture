using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent;

/// <summary>
/// DDD Aggregate Root for a single agent instance.
///
/// <b>Architectural role (Domain-Driven Design):</b>
/// The aggregate owns all state that must change together: the reasoning brain (islands, groups,
/// decisions), the question lifecycle, the deliverable registry, and the iteration history.
/// External code interacts through three interfaces that enforce invariants:
/// <list type="bullet">
///   <item><see cref="IAgentRunContext"/> — read-only view for handlers building prompts.</item>
///   <item><see cref="ISessionWriter"/> — write façade for step results applying mutations.</item>
///   <item><c>AgentAggregate</c> public API — orchestration entry points (<see cref="RunAsync"/>, etc.).</item>
/// </list>
///
/// <b>Iteration lifecycle:</b>
/// <code>
/// 1. Construct via domain factory (e.g. UxAgent.BuildAsync)
/// 2. RunAsync(userIntent, chatClient, deliverableWriter)
///    ├─ Kickoff   → BeginIteration (creates Checkpoint, clears PendingRequest)
///    ├─ Capture   → SetCapturedIslands
///    ├─ Organize  → ApplyOrganization (groups + decisions)
///    ├─ Distill   → ApplyDistillation (decisions + deliverables + questions)
///    ├─ Express   → UpdateTokenConsumption + ReviewQuestion
///    └─ Closed    → FinalizeSession (seals Checkpoint with ClosedAt)
/// 3. PrepareNextIteration(answers?) → resets pipeline, applies answers
/// 4. RunAsync again → Kickoff triages prior questions against new context
/// </code>
///
/// <b>Interface delegation pattern:</b>
/// The aggregate implements <see cref="ISessionWriter"/> and <see cref="IAgentRunContext"/> via
/// explicit interface implementations. This hides the write façade from aggregate consumers
/// (who only see the public orchestration API) while exposing it to <see cref="Steps.StepResult.ApplyTo"/>.
/// </summary>
public class AgentAggregate<TId> :
    ISessionWriter,   // façade: IBrainWriter + IQuestionWriter + IDeliverableTracker + lifecycle
    ITokenConsumptionWriter,
    IAgentRunContext
{
    public TId Id { get; protected set; } = default!;
    protected IStepPromptLayer RolePromptAgentContext { get; private set; } = PromptContext.Empty;

    public AgentSession? Session { get; private set; }

    // Holds the AgentRequest for the current RunAsync call until BeginIteration consumes
    // it into the new Checkpoint. Cleared immediately after BeginIteration fires.
    private AgentRequest? _pendingRequest;

    private readonly Dictionary<string, IReadOnlyList<HandlerExchange>> _stepJournals = new();

    public IReadOnlyList<HandlerExchange> GetStepJournal(string stepName)
        => _stepJournals.TryGetValue(stepName, out var j) ? j : [];

    public IReadOnlyCollection<string> JournalStepNames => _stepJournals.Keys;

    public StepPipeline Pipeline { get; protected set; } = default!;

    public IReadOnlyList<AgentStep> Steps => Pipeline.Steps;
    public bool IsCompleted => Pipeline.IsCompleted;

    // ── Brain (injected — not owned, shared across all agents on the project) ──

    private BrainAggregate _brain = default!;

    /// <summary>The project's shared Brain — owns all reasoning state and deliverables.</summary>
    public BrainAggregate Brain => _brain;

    // ── Domain outputs ────────────────────────────────────────────────────────

    private readonly List<Question> _questions = [];

    public IReadOnlyList<Question>    Questions    => _questions.AsReadOnly();
    public IReadOnlyList<Deliverable> Deliverables => _brain?.Deliverables ?? [];
    public IReadOnlyList<Decision>    Decisions    => _brain?.Decisions    ?? [];

    protected AgentAggregate() { }

    public AgentAggregate(TId id, BrainAggregate brain, IStepPromptLayer agentPromptContext)
    {
        Id                     = id;
        _brain                 = brain;
        RolePromptAgentContext = agentPromptContext;
        Session                = new AgentSession(brain.Id.ProjectId);
    }

    // ── Question Queries ──────────────────────────────────────────────────────

    public IReadOnlyList<Question> GetQuestions() => _questions.AsReadOnly();

    public IReadOnlyList<Question> GetQuestions(QuestionStatus status)
        => _questions.Where(q => q.Status == status).ToList().AsReadOnly();

    public IReadOnlyList<Question> GetOpenQuestions()      => GetQuestions(QuestionStatus.Open);
    public IReadOnlyList<Question> GetPendingReviewQuestions() => GetQuestions(QuestionStatus.Answered);

    public Question? FindQuestion(string id) => _questions.Find(q => q.Id == id);

    // Convenience forwarders so subclasses can mutate questions without casting to ISessionWriter.
    public void RaiseQuestion(string id, string text, string source)
        => ((ISessionWriter)this).RaiseQuestion(id, text, source);

    public void ApplyQuestionReview(string id, QuestionStatus newStatus)
        => ((ISessionWriter)this).ReviewQuestion(id, newStatus);

    // ── Supply Answers (between iterations) ───────────────────────────────────

    public void SupplyAnswers(IReadOnlyList<(string QuestionId, string Answer, string AnswerSource)> answers)
    {
        foreach (var (questionId, answer, answerSource) in answers)
        {
            var question = FindQuestion(questionId)
                ?? throw new InvalidOperationException($"Question '{questionId}' not found.");
            question.SetAnswer(answer, answerSource);
        }
    }

    // ── Multi-iteration ───────────────────────────────────────────────────────

    /// <summary>
    /// Resets the pipeline to Step 1 so the agent can run another full 6-step cycle.
    /// Must be called explicitly between runs — <see cref="RunAsync"/> throws if the pipeline
    /// is already completed without this being called first.
    /// Optionally supply answers to open questions before the next Kickoff triages them.
    /// All prior checkpoints, brain state (islands/groups/decisions), deliverables, and
    /// questions are preserved in memory and available as context for the next Kickoff.
    /// </summary>
    public void PrepareNextIteration(
        IReadOnlyList<(string QuestionId, string Answer, string AnswerSource)>? answers = null)
    {
        if (!IsCompleted)
            throw new InvalidOperationException(
                "Cannot prepare next iteration: the current run is not yet complete.");

        if (answers is not null) SupplyAnswers(answers);
        Pipeline.Reset();
    }

    // ── Step execution ────────────────────────────────────────────────────────

    /// <summary>
    /// Executes the next single step. For multi-step test sequences pass the
    /// <see cref="AgentRequest"/> on the first call (Kickoff); subsequent calls
    /// do not need it — the intent is consumed by the Kickoff step.
    /// </summary>
    public Task<StepResult> ExecuteNextStepAsync(
        AgentRequest      request,
        IChatClient       chatClient,
        CancellationToken ct = default)
    {
        _pendingRequest = request;
        return ExecuteNextStepAsync(chatClient, ct);
    }

    /// <summary>
    /// Executes the next single step using a request already set (by <see cref="RunAsync"/>
    /// or by a prior call to the <see cref="ExecuteNextStepAsync(AgentRequest,IChatClient,CancellationToken)"/> overload).
    /// </summary>
    public Task<StepResult> ExecuteNextStepAsync(IChatClient chatClient, CancellationToken ct = default)
    {
        if (IsCompleted)
            throw new InvalidOperationException("All steps have been completed.");
        return ExecuteCurrentStepAsync(chatClient, ct);
    }

    /// <summary>
    /// Executes all pipeline steps in sequence. Use this overload in tests and non-production
    /// flows where no <see cref="IDeliverableWriter"/> is needed.
    /// Equivalent to <see cref="RunAsync"/> without the deliverable-writing side-effect.
    /// </summary>
    public Task<IReadOnlyList<StepResult>> ExecuteAllStepsAsync(
        AgentRequest      request,
        IChatClient       chatClient,
        CancellationToken ct = default)
    {
        _pendingRequest = request;
        return ExecuteAllStepsAsync(chatClient, ct);
    }

    /// <summary>
    /// Executes all steps using a request already set (by <see cref="RunAsync"/> or the
    /// <see cref="ExecuteAllStepsAsync(AgentRequest,IChatClient,CancellationToken)"/> overload).
    /// </summary>
    public async Task<IReadOnlyList<StepResult>> ExecuteAllStepsAsync(
        IChatClient       chatClient,
        CancellationToken ct = default)
    {
        var results = new List<StepResult>();
        while (!IsCompleted)
        {
            results.Add(await ExecuteCurrentStepAsync(chatClient, ct));
            if (!results[^1].GateSatisfied) break;
        }
        return results.AsReadOnly();
    }

    private async Task<StepResult> ExecuteCurrentStepAsync(IChatClient chatClient, CancellationToken ct)
    {
        if (!Pipeline.TryGetCurrentStep(out var step))
            throw new InvalidOperationException("No current step available.");

        var result = await step!.ExecuteStepAsync(this, this, chatClient, ct);
        result!.ApplyTo(this);
        if (result.GateSatisfied) Pipeline.Advance();
        return result;
    }

    public async Task<AgentRunResult> RunAsync(
        AgentRequest       request,
        IChatClient        chatClient,
        IDeliverableWriter deliverableWriter,
        CancellationToken  ct = default)
    {
        if (IsCompleted)
            throw new InvalidOperationException(
                "Pipeline is completed. Call PrepareNextIteration() before running again.");

        _pendingRequest = request;

        var results = new List<StepResult>();
        while (!IsCompleted)
        {
            var result = await ExecuteNextStepAsync(chatClient, ct);
            results.Add(result);
            if (!result.GateSatisfied) break;
        }

        await deliverableWriter.WriteAsync(this, results.AsReadOnly(), ct);

        return new AgentRunResult(
            Session!,   // Session is set in constructor and never cleared
            results.AsReadOnly(),
            IsCompleted,
            Questions,
            Decisions,
            Deliverables);
    }

    // ==========================================================
    // IAgentRunContext
    // ==========================================================

    BrainAggregate?            IAgentRunContext.Brain             => _brain;
    AgentSession?              IAgentRunContext.Session           => Session;
    IReadOnlyList<Question>    IAgentRunContext.Questions         => _questions.AsReadOnly();
    IReadOnlyList<Decision>    IAgentRunContext.Decisions         => _brain?.Decisions ?? [];
    IReadOnlyList<Deliverable> IAgentRunContext.Deliverables      => _brain?.Deliverables ?? [];
    AgentRequest?              IAgentRunContext.PendingRequest    => _pendingRequest;

    IReadOnlyList<HandlerExchange> IAgentRunContext.GetStepJournal(string stepName)
        => _stepJournals.TryGetValue(stepName, out var j) ? j : [];

    // ==========================================================
    // ISessionWriter — lifecycle methods
    // ==========================================================

    void ISessionWriter.BeginIteration(string sessionObjective)
    {
        Session?.BeginIteration(sessionObjective, _pendingRequest?.Intent ?? string.Empty);
        _brain?.BeginCheckpoint(Id!.ToString()!, sessionObjective);
        _pendingRequest = null;
    }

    void ISessionWriter.RecordStepJournal(int stepNumber, string stepName,
        IReadOnlyList<HandlerExchange> journal)
        => _stepJournals[stepName] = journal;

    void ISessionWriter.UpdateTokenConsumption(int inputTokens, int outputTokens)
    {
        Session?.UpdateTokenConsumption(inputTokens, outputTokens);
    }

    void ISessionWriter.FinalizeSession(DateTime closedAt)
    {
        Session?.FinalizeSession(closedAt);
        _brain?.FinalizeCheckpoint(closedAt);
    }

    // ==========================================================
    // IBrainWriter — delegates to BrainAggregate
    // ==========================================================

    void IBrainWriter.SetCapturedIslands(IReadOnlyList<CapturedIsland> islands)
        => _brain.SetCapturedIslands(islands);

    void IBrainWriter.ApplyOrganization(
        IReadOnlyList<IslandOrganization> organizations,
        IReadOnlyList<DecisionRecord>     decisions,
        IReadOnlyList<IslandGroup>        groups)
        => _brain.ApplyOrganization(organizations, decisions, groups);

    void IBrainWriter.ApplyDistillation(
        IReadOnlyList<IslandDistillation>       distillations,
        IReadOnlyList<GroupDistillationRecord>  groupDistillations)
        => _brain.ApplyDistillation(distillations, groupDistillations);

    // ==========================================================
    // IDeliverableTracker — delegates to BrainAggregate
    // ==========================================================

    void IDeliverableTracker.TrackDeliverables(
        IReadOnlyList<DeliverableRecord>        deliverables,
        IReadOnlyList<GroupDistillationRecord>  groupDistillations)
        => _brain.TrackDeliverables(deliverables, groupDistillations);

    // ==========================================================
    // IQuestionWriter
    // ==========================================================

    void IQuestionWriter.RaiseQuestion(string id, string text, string source)
    {
        if (FindQuestion(id) is not null)
            throw new InvalidOperationException($"Question '{id}' already exists.");
        _questions.Add(new Question(id, text, source));
    }

    void IQuestionWriter.ReviewQuestion(string id, QuestionStatus newStatus, string text)
    {
        var existing = FindQuestion(id);
        if (existing is not null)
        {
            switch (newStatus)
            {
                case QuestionStatus.Reviewed:
                    if (existing.Status == QuestionStatus.Answered) existing.MarkReviewed();
                    break;
                case QuestionStatus.Obsolete: existing.MarkObsolete(); break;
            }
        }
        else if (newStatus == QuestionStatus.Open)
        {
            _questions.Add(new Question(id, text, "express-relay"));
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
                question.SetAnswer(answer ?? "restored", answerSource ?? "restored"); break;
            case QuestionStatus.Reviewed:
                question.SetAnswer(answer ?? "restored", answerSource ?? "restored");
                question.MarkReviewed(); break;
            case QuestionStatus.Obsolete:
                question.MarkObsolete(); break;
        }
    }

    void IQuestionWriter.AnswerQuestion(string id, string answer, string answerSource)
    {
        var question = FindQuestion(id);
        if (question?.Status == QuestionStatus.Open)
            question.SetAnswer(answer, answerSource);
    }

    // ==========================================================
    // ITokenConsumptionWriter — forwarding
    // ==========================================================

    void ITokenConsumptionWriter.UpdateTokenConsumption(int inputTokens, int outputTokens)
        => ((ISessionWriter)this).UpdateTokenConsumption(inputTokens, outputTokens);
}
