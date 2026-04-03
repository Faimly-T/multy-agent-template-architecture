
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Domain.UxAgent;

public class UxPersona : AgentAggregate<string>
{
    public UxPersona(Role role) : base("ux-persona-architect", role)
    {
        DefineSteps();
    }

    public async Task LoadSkillsAsync(ISkillProvider skillProvider, CancellationToken ct = default)
    {
        var loaded = new List<AgentStep>();
        foreach (var step in Steps)
        {
            var skill = await skillProvider.LoadAsync(step.SkillName, ct);
            loaded.Add(step.WithSkill(skill));
        }

        ReplaceSteps(loaded);
    }

    private void DefineSteps()
    {
        AddStep(new AgentStep(
            StepNumber: 1,
            Name: "Define objective for agent",
            SkillName: "rehydrate-context",
            Instructions: "Session Objective. Parse product description.",
            Gate: new Gate("Objective confirmed"),
            JsonResponseSchema: ResponseSchemas.Rehydrate));

        AddStep(new AgentStep(
            StepNumber: 2,
            Name: "Generate unfiltered Island Backlog",
            SkillName: "autonomous-capture",
            Instructions: "Hunt for: user types (direct + indirect) · goals & motivations · pain points · behavioral patterns · context of use (where/when/device) · emotional states · anti-users · stakeholders · accessibility signals.",
            Gate: new Gate("≥3 user-type islands"),
            JsonResponseSchema: ResponseSchemas.Capture));

        AddStep(new AgentStep(
            StepNumber: 3,
            Name: "Map, group, and sequence the Island Backlog",
            SkillName: "strategic-organize",
            Instructions: "Cluster by person → proto-persona. Within each: goals > pains > behaviors > context. Merge clusters yielding identical design decisions. Classify: Primary / Secondary / Anti-persona.",
            Gate: new Gate("2-5 ranked candidates"),
            JsonResponseSchema: ResponseSchemas.Organize));

        AddStep(new AgentStep(
            StepNumber: 4,
            Name: "Distill each island into a concrete result",
            SkillName: "expert-distill",
            Instructions: "Produce Persona Cards per agent's configured template. Behavioral over demographic. JTBD: \"When [situation], I want to [motivation], so I can [outcome]\". Each persona ≥1 usage scenario. Progressive Summarization — scannable in 30s.",
            Gate: new Gate("All → Card or Concern"),
            JsonResponseSchema: ResponseSchemas.Distill));

        AddStep(new AgentStep(
            StepNumber: 5,
            Name: "Update MARK files and emit",
            SkillName: "express-relay",
            Instructions: "Write cards to agent's configured output folder. Emit relay. Record token usage in Progress Summary MARK.",
            Gate: new Gate("MARKs + Cards + Relay + Token Usage logged"),
            JsonResponseSchema: ResponseSchemas.Relay));
    }
}