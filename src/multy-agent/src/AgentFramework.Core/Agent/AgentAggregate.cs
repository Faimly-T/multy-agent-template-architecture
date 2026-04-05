using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Events;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Core.Agent.Steps.CODESteps;

namespace AgentFramework.Core.Agent;

public class AgentAggregate<TId>
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
    public int CurrentStepIndex => Pipeline.CurrentStepIndex;
    public bool IsCompleted => Pipeline.IsCompleted;

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

    protected void RaiseDomainEvent(DomainEvent @event) => _domainEvents.Add(@event);

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

        var step = Pipeline.CurrentStep;

        RaiseDomainEvent(new StepStarted(step.StepNumber, step.Name, step.SkillName));

        var messages = messageBuilder.BuildMessages(step, Role, Session, ConversationMessages);

        foreach (var msg in messages)
        {
            if (msg.Role == MessageRole.System)
                _conversation.AddSystemMessage(msg.Content);
            else
                _conversation.AddUserMessage(msg.Content);
        }

        var result = await chatClient.SendAsync(_conversation.Messages, step, ct);

        _conversation.AddAssistantMessage(result.Output);

        ApplyStepOutcome(step, result);

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

    private void ApplyStepOutcome(AgentStep step, StepResult result)
    {
        if (result.GateSatisfied)
        {
            Session?.Apply(result);

            if (result is ExpressResult express && express.Questions.Count > 0)
            {
                var newQuestions = express.Questions.Where(q =>
                    string.Equals(q.Status, "open", StringComparison.OrdinalIgnoreCase)).ToList();
                var reviewedCount = express.Questions.Count(q =>
                    string.Equals(q.Status, "reviewed", StringComparison.OrdinalIgnoreCase));
                RaiseDomainEvent(new QuestionsUpdated(newQuestions, reviewedCount));
            }

            RaiseDomainEvent(new StepCompleted(step.StepNumber, step.Name, result));
            Pipeline.Advance();
        }
        else
        {
            RaiseDomainEvent(new StepGateFailed(step.StepNumber, step.Name, step.Gate.Description, result));
        }
    }
}