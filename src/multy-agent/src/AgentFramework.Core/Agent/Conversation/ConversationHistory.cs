namespace AgentFramework.Core.Agent.Conversation;

public class ConversationHistory
{
    private readonly List<ChatMessage> _messages = [];
    public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();

    public void AddSystemMessage(string content)
        => _messages.Add(new ChatMessage(MessageRole.System, content));

    public void AddUserMessage(string content)
        => _messages.Add(new ChatMessage(MessageRole.User, content));

    public void AddAssistantMessage(string content)
        => _messages.Add(new ChatMessage(MessageRole.Assistant, content));
}
