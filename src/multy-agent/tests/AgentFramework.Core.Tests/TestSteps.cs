using AgentFramework.Core.Agent.Steps;
using AgentFramework.Core.Agent.Steps.CODESteps;

namespace AgentFramework.Core.Tests;

internal static class TestSteps
{
    public static StepPipeline DefaultPipeline() => new(DefaultSteps());

    public static AgentStep[] DefaultSteps() =>
    [
        new RehydrateStep(
            stepNumber: 1,
            name: "Define objective for agent",
            instructions: "Session Objective. Parse product description.",
            gate: new Gate("Objective confirmed")),

        new CaptureStep(
            stepNumber: 2,
            name: "Generate unfiltered Island Backlog",
            instructions: "Hunt for: user types (direct + indirect) · goals & motivations · pain points · behavioral patterns · context of use (where/when/device) · emotional states · anti-users · stakeholders · accessibility signals.",
            gate: new Gate("≥3 user-type islands")),

        new OrganizeStep(
            stepNumber: 3,
            name: "Map, group, and sequence the Island Backlog",
            instructions: "Cluster by person → proto-persona. Within each: goals > pains > behaviors > context. Merge clusters yielding identical design decisions. Classify: Primary / Secondary / Anti-persona.",
            gate: new Gate("2-5 ranked candidates")),

        new DistillStep(
            stepNumber: 4,
            name: "Distill each island into a concrete result",
            instructions: "Produce Persona Cards per agent's configured template. Behavioral over demographic. JTBD: \"When [situation], I want to [motivation], so I can [outcome]\". Each persona ≥1 usage scenario. Progressive Summarization — scannable in 30s.",
            gate: new Gate("All → Card or Concern")),

        new ExpressStep(
            stepNumber: 5,
            name: "Compile session state and emit",
            instructions: "Write cards to agent's configured output folder. Emit relay. Record token usage in session checkpoint.",
            gate: new Gate("Session + Cards + Relay + Token Usage logged"))
    ];
}
