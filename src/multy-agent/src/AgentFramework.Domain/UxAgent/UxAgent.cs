using AgentFramework.CodePipeline;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Domain.Capture.Handlers;
using AgentFramework.Domain.Distill.Handlers;
using AgentFramework.Domain.Kickoff.Handlers;
using AgentFramework.Domain.Organize.Handlers;
using AgentFramework.Domain.UxAgent.Express;
using AgentFramework.Domain.UxAgent.Express.Handlers;

namespace AgentFramework.Domain.UxAgent;

/// <summary>
/// The UX research agent — runs the 6-step CODE pipeline to produce persona deliverables.
///
/// ── How to build ──────────────────────────────────────────────────────────────
///
/// <code>
/// // Use defaults:
/// var config = UxAgentDefaults.Config(role);
/// var agent  = await UxAgent.BuildAsync(config, resolver);
///
/// // Or customise any step field before building:
/// var config = UxAgentDefaults.Config(role) with
/// {
///     Express = myConfig.Express with { Instructions = "Custom express instructions." }
/// };
/// var agent = await UxAgent.BuildAsync(config, resolver);
/// </code>
///
/// ── Multi-iteration ───────────────────────────────────────────────────────────
/// <code>
/// var r1 = await agent.RunAsync(intent1, chatClient, writer, ct);
/// agent.PrepareNextIteration(answers);
/// var r2 = await agent.RunAsync(intent2, chatClient, writer, ct);
/// </code>
/// </summary>
public class UxAgent : AgentAggregate<string>
{
    protected UxAgent(
        string           agentId,
        string           projectId,
        IStepPromptLayer agentPrompt,
        StepPipeline     pipeline)
        : base(agentId, projectId, agentPrompt)
    {
        Pipeline = pipeline;
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// The single factory method for building a <see cref="UxAgent"/>.
    /// Supply an <see cref="AgentConfig"/> — use <see cref="UxAgentDefaults.Config"/> for defaults
    /// or construct a custom <see cref="AgentConfig"/> to override any step.
    /// </summary>
    public static async Task<UxAgent> BuildAsync(
        AgentConfig          config,
        ISkillResolver       resolver,
        IUxAgentRepository?  repo = null,
        CancellationToken    ct   = default)
    {
        var agentCtx = PromptContext.ForRole(config.Role);

        var pipeline = await AgentBuilder
            .FromConfig(config)
            .WithResolver(resolver)
            .WithInstructionLookup(repo is not null ? repo.GetHandlerInstruction : null)

            .ForStep(PipelineStep.Kickoff, ctx => [
                new CheckpointValidatorCommand(ctx),
                new QuestionTriageHandler(ctx),
                new ObjectiveSynthesisHandler(ctx)])

            .ForStep(PipelineStep.Capture, ctx => [
                new CaptureObjectiveContextCommand(),
                new IslandFromObjectiveHandler(ctx),
                new IslandFromDeliverablesHandler(ctx),
                new IslandFromQuestionsHandler(ctx)])

            .ForStep(PipelineStep.Organize, ctx => [
                new IslandGroupingHandler(ctx),
                new GroupReadinessHandler(ctx),
                new OrganizeSynthesisHandler()])

            .ForStep(PipelineStep.Distill, ctx => [
                new GroupDistillationHandler(ctx)])

            .ForStep(PipelineStep.Express,
                handlers: ctx => [
                    new UxSessionSummaryHandler(ctx),
                    new BuyerPersonaDocumentHandler(ctx),
                    new ResearchValidationGuideHandler(ctx),
                    new PersonaInterviewScriptHandler(ctx),
                    new NextSessionGuideHandler(ctx),
                    new UxQuestionReviewHandler(ctx)],
                step: (ctx, num, cfg, chain) =>
                    new UxExpressStep(ctx, num, cfg.Instructions, cfg.Gate, chain,
                        repo is not null ? repo.GetHandlerInstruction : null))

            .BuildPipelineAsync(ct);

        return new UxAgent(config.AgentId, config.ProjectId, agentCtx, pipeline);
    }
}
