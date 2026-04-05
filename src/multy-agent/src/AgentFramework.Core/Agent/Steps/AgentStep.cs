using System.Text.Json;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps;

//Note for my future me: I can organize the steps and the context and the way we build the message for the llm using the SRL context engineering to format better and achieve the best possible result that I want. The steps are the core of the agent's behavior, and they should be designed to be as clear and specific as possible to guide the LLM towards the desired outcome. Each step should have a clear purpose, and the instructions should be detailed enough to ensure that the LLM understands what is expected of it. Additionally, the context provided to the LLM should be relevant and informative, helping it to generate responses that are aligned with the agent's goals. By carefully designing the steps and the context, I can create an agent that is more effective and efficient in achieving its objectives.

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

    public abstract string BuildContext(AgentSession? session);

    public abstract StepResult ParseResult(JsonElement root, string rawOutput, bool gateSatisfied);

    internal void AttachSkill(Skill skill) => Skill = skill;
}
