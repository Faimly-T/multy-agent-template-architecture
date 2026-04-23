namespace AgentFramework.Core.Agent.Session;

public class AgentSession
{
    public Checkpoint Checkpoint { get; private set; }

    public IslandBacklog Backlog { get; } = new();

    // Convenience accessor — delegates to Backlog for backward-compatible reads
    public IReadOnlyList<Island> Islands => Backlog.All;

    private readonly List<Deliverable> _deliverables = [];
    public IReadOnlyList<Deliverable> Deliverables => _deliverables.AsReadOnly();

    private readonly List<Decision> _decisions = [];
    public IReadOnlyList<Decision> Decisions => _decisions.AsReadOnly();

    private readonly List<Question> _questions = [];
    public IReadOnlyList<Question> Questions => _questions.AsReadOnly();

    public AgentSession(string sessionObjective, int sessionIteration = 1)
    {
        Checkpoint = new Checkpoint(
            Date: DateTime.UtcNow,
            SessionIteration: sessionIteration,
            SessionObjective: sessionObjective,
            TokensConsumption: new TokenConsumption(0, 0));
    }

    // --- Checkpoint ---

    internal void UpdateObjective(string sessionObjective)
    {
        Checkpoint = Checkpoint with { SessionObjective = sessionObjective };
    }

    internal void UpdateTokenConsumption(int inputTokens, int outputTokens)
    {
        Checkpoint = Checkpoint with
        {
            TokensConsumption = new TokenConsumption(inputTokens, outputTokens)
        };
    }

    // --- Deliverables ---

    internal void RecordDeliverable(string deliverableId, string path, DeliverableStatus status)
    {
        _deliverables.Add(new Deliverable(deliverableId, path, status));
    }

    // --- Decisions ---

    internal void RecordDecision(string id, string description, string impact)
    {
        _decisions.Add(new Decision(id, description, impact));
    }

    // --- Questions ---

    internal Question RaiseQuestion(string id, string text, string source)
    {
        var question = new Question(id, text, source);
        _questions.Add(question);
        return question;
    }

    internal void ApplyQuestionReview(string id, QuestionStatus newStatus)
    {
        var question = FindQuestion(id);
        if (question is null) return;

        switch (newStatus)
        {
            case QuestionStatus.Reviewed: question.MarkReviewed(); break;
            case QuestionStatus.Obsolete: question.MarkObsolete(); break;
        }
    }

    internal Question? FindQuestion(string id) => _questions.Find(q => q.Id == id);
}
