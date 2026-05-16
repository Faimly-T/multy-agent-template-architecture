using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Ports;

public interface IStepMessageBuilder
{
    IReadOnlyList<ChatMessage> BuildMessages(AgentStep step, Role role, IAgentRunContext? context, IReadOnlyList<ChatMessage> conversationHistory);
}
