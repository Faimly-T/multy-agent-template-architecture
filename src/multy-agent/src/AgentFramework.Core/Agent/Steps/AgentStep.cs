using System.Text.Json;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps;

public abstract class AgentStep
{
    public int StepNumber { get; }
    public string Name { get; }
    public string SkillName { get; }
    public string Instructions { get; }
    public Gate Gate { get; }

    public Skill? Skill { get; private set; }

    public abstract string JsonResponseSchema { get; }

    protected AgentStep(int stepNumber, string name, string skillName, string instructions, Gate gate)
    {
        StepNumber = stepNumber;
        Name = name;
        SkillName = skillName;
        Instructions = instructions;
        Gate = gate;
    }

    public virtual string BuildContext(IAgentRunContext? context) => string.Empty;

    public abstract StepResult ParseResult(JsonElement root, string rawOutput, bool gateSatisfied);

    internal void AttachSkill(Skill skill) => Skill = skill;

    public virtual Task<StepResult?> ExecuteChainAsync(
        IAgentRunContext? context,
        ISessionWriter writer,
        IChatClient chatClient,
        IDomainEventPublisher? publisher = null,
        Role? role = null,
        CancellationToken ct = default) => Task.FromResult<StepResult?>(null);
}
