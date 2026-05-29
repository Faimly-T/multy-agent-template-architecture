using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Domain.UxAgent;

public record UxPersonaConfig(
    string AgentId,
    string ProjectId,
    SessionMarkFilePaths MarkFilePaths,
    RoleDefinition Role,
    CodePipelineConfig Pipeline);
