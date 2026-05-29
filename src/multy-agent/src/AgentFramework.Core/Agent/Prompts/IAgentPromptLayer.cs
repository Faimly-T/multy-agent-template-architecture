namespace AgentFramework.Core.Agent.Prompts;

public interface IAgentPromptLayer
{
    static IAgentPromptLayer Empty => PromptContext.Empty;
    RoleDefinition? Role { get; }
    IStepPromptLayer WithRole(RoleDefinition role);
}