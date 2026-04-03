using AgentFramework.Core.Agent;

namespace AgentFramework.Domain.UxAgent;

public static class UxPersonaFactory
{
    public static UxPersona Create(string roleMarkdown)
    {
        var role = Role.FromMd(roleMarkdown);
        return new UxPersona(role);
    }
}
