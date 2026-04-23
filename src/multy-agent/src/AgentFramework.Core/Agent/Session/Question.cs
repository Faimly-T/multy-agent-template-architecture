 namespace AgentFramework.Core.Agent.Session;

public sealed class Question
{
    public string Id { get; }
    public string Text { get; }
    public string Source { get; }
    public QuestionStatus Status { get; private set; }
    public string? Answer { get; private set; }
    public string? AnswerSource { get; private set; }
    public DateTime? ResolvedDate { get; private set; }

    public Question(string id, string text, string source)
    {
        Id = id;
        Text = text;
        Source = source;
        Status = QuestionStatus.Open;
    }

    internal void SetAnswer(string answer, string answerSource)
    {
        if (Status is not QuestionStatus.Open)
            throw new InvalidOperationException(
                $"Cannot answer question '{Id}' in status '{Status}'. Only Open questions can be answered.");

        Answer = answer;
        AnswerSource = answerSource;
        Status = QuestionStatus.Answered;
    }

    internal void MarkReviewed()
    {
        if (Status is not QuestionStatus.Answered)
            throw new InvalidOperationException(
                $"Cannot review question '{Id}' in status '{Status}'. Only Answered questions can be reviewed.");

        Status = QuestionStatus.Reviewed;
        ResolvedDate = DateTime.UtcNow;
    }

    internal void MarkObsolete()
    {
        if (Status is QuestionStatus.Obsolete)
            throw new InvalidOperationException(
                $"Question '{Id}' is already obsolete.");

        Status = QuestionStatus.Obsolete;
        ResolvedDate = DateTime.UtcNow;
    }
}
