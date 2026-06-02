namespace AgentFramework.Core.Agent.Session;

/// <summary>Write operations for the <see cref="Question"/> lifecycle.</summary>
public interface IQuestionWriter
{
    /// <summary>
    /// Creates a new <see cref="Question"/> in <see cref="QuestionStatus.Open"/> status.
    /// Throws if a question with the same <paramref name="id"/> already exists.
    /// Called by <c>DistillResult.ApplyTo</c> when the Distill step surfaces new questions.
    /// </summary>
    void RaiseQuestion(string id, string text, string source);

    /// <summary>
    /// Transitions an existing question to <paramref name="newStatus"/>.
    /// If the question does not exist and <paramref name="newStatus"/> is
    /// <see cref="QuestionStatus.Open"/>, a new question is created using <paramref name="text"/>.
    /// Called by <c>ExpressResult.ApplyTo</c> when the LLM reviews question status.
    /// </summary>
    void ReviewQuestion(string id, QuestionStatus newStatus, string text = "");

    /// <summary>
    /// Reconstructs a question with a known history (used when deserialising a persisted session).
    /// No-op if the question already exists.
    /// </summary>
    void RestoreQuestion(string id, string text, string source, QuestionStatus status,
        string? answer = null, string? answerSource = null);

    /// <summary>
    /// Supplies an answer to an <see cref="QuestionStatus.Open"/> question and transitions it to
    /// <see cref="QuestionStatus.Answered"/>. Called by the Kickoff triage handler when a prior
    /// session's context resolves an open question.
    /// </summary>
    void AnswerQuestion(string id, string answer, string answerSource);
}
