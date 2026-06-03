 namespace AgentFramework.Core.Agent.Session;

/// <summary>
/// An open question raised by the agent during the Distill or Express step.
///
/// Questions are the agent's mechanism for surfacing uncertainty — they capture what is unknown
/// or unvalidated so a human (or a downstream agent) can supply answers between iterations.
/// Each iteration's Kickoff step triages all open questions against new context: resolved ones
/// become <see cref="QuestionStatus.Answered"/>, irrelevant ones become
/// <see cref="QuestionStatus.Obsolete"/>, and the remainder carry forward unchanged.
///
/// State machine: Open → Answered → Reviewed  (or → Obsolete from any state)
/// </summary>
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
