namespace AgentFramework.Core.Agent.Session;

public interface IQuestionWriter
{
    void RaiseQuestion(string id, string text, string source);
    void ReviewQuestion(string id, QuestionStatus newStatus);
    void RestoreQuestion(string id, string text, string source, QuestionStatus status,
        string? answer = null, string? answerSource = null);
    void AnswerQuestion(string id, string answer, string answerSource);
}
