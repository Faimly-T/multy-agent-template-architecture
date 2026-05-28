namespace AgentFramework.Core.Agent.Prompts;

public interface IAgentPromptLayer
{
    static IAgentPromptLayer Empty => PromptContext.Empty;
    IStepPromptLayer WithRole(Role role);
}
