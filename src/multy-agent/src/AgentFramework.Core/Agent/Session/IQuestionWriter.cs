namespace AgentFramework.Core.Agent.Session;

public interface IQuestionWriter
{
    void RaiseQuestion(string id, string text, string source);
    void ReviewQuestion(string id, QuestionStatus newStatus);
}
