namespace AgentFramework.Core.Agent.Conversation;

public record StepConversation(
    int StepNumber,
    string StepName,
    IReadOnlyList<ChatMessage> Messages,
    string Response,
    DateTime ExecutedAt);
