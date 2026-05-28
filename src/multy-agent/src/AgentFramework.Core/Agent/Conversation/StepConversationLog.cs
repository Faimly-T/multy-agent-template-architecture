namespace AgentFramework.Core.Agent.Conversation;

public class StepConversationLog
{
    private readonly List<StepConversation> _steps = [];
    public IReadOnlyList<StepConversation> Steps => _steps.AsReadOnly();

    public void Record(StepConversation conv) => _steps.Add(conv);

    public string? LastOutput()
        => _steps.Count > 0 ? _steps[^1].Response : null;

    public string? GetOutput(string stepClassName)
        => _steps.FirstOrDefault(s => s.StepName == stepClassName)?.Response;
}
