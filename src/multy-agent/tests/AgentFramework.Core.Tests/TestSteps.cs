using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Core.Agent.Steps.CODESteps;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

internal static class TestSteps
{
    public static StepPipeline DefaultPipeline(IStepPromptLayer? stepContext = null) =>
        UxStepBuilder.Create().WithSteps(DefaultSteps(stepContext)).Build();

    public static AgentStep[] DefaultSteps(IStepPromptLayer? stepContext = null)
    {
        var ctx = stepContext ?? PromptContext.Empty;
        return
        [
            new KickoffStep(
                stepContext: ctx,
                stepNumber: 1,
                instructions: "Session Objective. Parse product description.",
                gate: new Gate("Objective confirmed")),

            new CaptureStep(
                stepContext: ctx,
                stepNumber: 2,
                instructions: "Hunt for: user types (direct + indirect) · goals & motivations · pain points · behavioral patterns · context of use (where/when/device) · emotional states · anti-users · stakeholders · accessibility signals.",
                gate: new Gate("≥3 user-type islands")),

            new OrganizeStep(
                stepContext: ctx,
                stepNumber: 3,
                instructions: "Cluster by person → proto-persona. Within each: goals > pains > behaviors > context. Merge clusters yielding identical design decisions. Classify: Primary / Secondary / Anti-persona.",
                gate: new Gate("2-5 ranked candidates")),

            new DistillStep(
                stepContext: ctx,
                stepNumber: 4,
                instructions: "Produce Persona Cards per agent's configured template. Behavioral over demographic. JTBD: \"When [situation], I want to [motivation], so I can [outcome]\". Each persona ≥1 usage scenario. Progressive Summarization — scannable in 30s.",
                gate: new Gate("All → Card or Concern")),

            new ExpressStep(
                stepContext: ctx,
                stepNumber: 5,
                instructions: "Write cards to agent's configured output folder. Emit relay. Record token usage in session checkpoint.",
                gate: new Gate("Session + Cards + Relay + Token Usage logged"))
        ];
    }
}
