using System.Text.Json;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps.CODESteps;

namespace AgentFramework.Core.Agent.Steps;

public abstract class AgentStep
{
    public string Name => GetType().Name;
    public int StepNumber { get; }
    public string SkillName { get; }
    public string Instructions { get; }
    public Gate Gate { get; }
    public virtual int MaxContextTokenBudget => 4096;

    public abstract string JsonResponseSchema { get; }

    protected readonly IStepPromptLayer _stepContext;

    protected AgentStep(
        IStepPromptLayer stepContext,
        int stepNumber,
        string skillName,
        string instructions,
        Gate gate)
    {
        _stepContext = stepContext;
        StepNumber   = stepNumber;
        SkillName    = skillName;
        Instructions = instructions;
        Gate         = gate;
    }

    public virtual string BuildContext(IAgentRunContext? context) => string.Empty;

    public abstract StepResult ParseResult(JsonElement root, string rawOutput, bool gateSatisfied);

    public virtual async Task<StepResult?> ExecuteStepAsync(
        IAgentRunContext? context,
        ISessionWriter writer,
        IChatClient chatClient,
        CancellationToken ct = default)
    {
        var command = new DefaultStepCommand(_stepContext, Instructions, this);
        await command.ExecuteAiCommandAsync([], context, writer, chatClient, ct);

        var result = command.LastResult!;
        writer.RecordStepExchange(StepNumber, Name, command.LastMessages, result.Output);
        return result;
    }
}
