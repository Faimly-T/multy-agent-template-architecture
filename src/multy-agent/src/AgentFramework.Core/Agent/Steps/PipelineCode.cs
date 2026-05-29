using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Steps.CODESteps;

namespace AgentFramework.Core.Agent.Steps;

public class PipelineCode
{
    private readonly ISkillResolver   _skillResolver;
    private readonly IMarkFileReader? _markFileReader;

    public PipelineCode(ISkillResolver skillResolver, IMarkFileReader? markFileReader = null)
    {
        _skillResolver  = skillResolver ?? throw new ArgumentNullException(nameof(skillResolver));
        _markFileReader = markFileReader;
    }

    public async Task<StepPipeline> BuildAsync(
        IStepPromptLayer   agentContext,
        CodePipelineConfig config,
        CancellationToken  ct = default)
    {
        var kickoffCtx  = await ResolveStepContextAsync(agentContext, config.Kickoff.SkillNames,  ct);
        var captureCtx  = await ResolveStepContextAsync(agentContext, config.Capture.SkillNames,  ct);
        var organizeCtx = await ResolveStepContextAsync(agentContext, config.Organize.SkillNames, ct);
        var distillCtx  = await ResolveStepContextAsync(agentContext, config.Distill.SkillNames,  ct);
        var expressCtx  = await ResolveStepContextAsync(agentContext, config.Express.SkillNames,  ct);

        return new StepPipeline([
            new KickoffStep (kickoffCtx,  1, config.Kickoff.Instructions,  config.Kickoff.Gate,  _markFileReader),
            new CaptureStep (captureCtx,  2, config.Capture.Instructions,  config.Capture.Gate),
            new OrganizeStep(organizeCtx, 3, config.Organize.Instructions, config.Organize.Gate),
            new DistillStep (distillCtx,  4, config.Distill.Instructions,  config.Distill.Gate),
            new ExpressStep (expressCtx,  5, config.Express.Instructions,  config.Express.Gate),
        ]);
    }

    private async Task<IStepPromptLayer> ResolveStepContextAsync(
        IStepPromptLayer      agentContext,
        IReadOnlyList<string> skillNames,
        CancellationToken     ct)
    {
        if (skillNames.Count == 0) return agentContext;
        var skills = await _skillResolver.ResolveAsync(skillNames, ct);
        return agentContext.WithSkills(skills);
    }
}
