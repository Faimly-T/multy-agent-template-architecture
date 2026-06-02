using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.CodePipeline;

/// <summary>
/// Assembles a <see cref="StepPipeline"/> from an <see cref="AgentConfig"/>.
///
/// Two concerns are kept deliberately separate:
///   Config  — what: instructions, gates, skill names (data, serializable, loaded from DB/JSON).
///   ForStep — how: which handlers run for each step (code, declared once per agent type).
///
/// The <c>step</c> overload of <see cref="ForStep"/> lets a domain agent replace the framework's
/// default step type — the only reason to use it is when gate logic or result parsing differs
/// (e.g. <c>UxExpressStep</c> validates HTML presence instead of a JSON flag).
///
/// <code>
/// var pipeline = await AgentBuilder
///     .FromConfig(config)
///     .WithResolver(skillResolver)
///     .ForStep(PipelineStep.Kickoff, ctx => [
///         new CheckpointValidatorCommand(ctx),
///         new ObjectiveSynthesisHandler(ctx)])
///     .ForStep(PipelineStep.Express,
///         handlers: ctx => [new SummaryHandler(ctx), new HtmlHandler(ctx)],
///         step: (ctx, num, cfg, chain) => new UxExpressStep(ctx, num, cfg.Instructions, cfg.Gate, chain))
///     .BuildPipelineAsync(ct);
/// </code>
/// </summary>
public sealed class AgentBuilder
{
    private sealed record StepSlot(
        Func<IStepPromptLayer, ICommandHandler[]>                        HandlersFactory,
        Func<IStepPromptLayer, int, StepConfig, IStepChain?, AgentStep>? StepFactory = null);

    private readonly AgentConfig                        _config;
    private          ISkillResolver?                    _resolver;
    private readonly Dictionary<PipelineStep, StepSlot> _slots = new();

    private AgentBuilder(AgentConfig config) => _config = config;

    public static AgentBuilder FromConfig(AgentConfig config) => new(config);

    public AgentBuilder WithResolver(ISkillResolver resolver)
    {
        _resolver = resolver;
        return this;
    }

    /// <summary>Wire the handlers for a step using the framework's default step type for that position.</summary>
    public AgentBuilder ForStep(
        PipelineStep                           position,
        Func<IStepPromptLayer, ICommandHandler[]> handlers)
    {
        _slots[position] = new(handlers);
        return this;
    }

    /// <summary>
    /// Wire the handlers and a custom step type for a step.
    /// Use the <paramref name="step"/> factory only when the domain needs a specialized step
    /// (e.g. <c>UxExpressStep</c>) — for a different gate or result type.
    /// </summary>
    public AgentBuilder ForStep(
        PipelineStep                                                     position,
        Func<IStepPromptLayer, ICommandHandler[]>                        handlers,
        Func<IStepPromptLayer, int, StepConfig, IStepChain?, AgentStep>  step)
    {
        _slots[position] = new(handlers, step);
        return this;
    }

    /// <summary>
    /// Resolves skills for each registered step, builds handler chains, and returns a fully wired
    /// <see cref="StepPipeline"/>. A <see cref="ClosedStep"/> is always appended automatically.
    /// </summary>
    public async Task<StepPipeline> BuildPipelineAsync(CancellationToken ct = default)
    {
        var baseCtx = PromptContext.ForRole(_config.Role);
        var steps   = new List<AgentStep>();

        foreach (var position in Enum.GetValues<PipelineStep>())
        {
            if (!_slots.TryGetValue(position, out var slot)) continue;

            var cfg     = StepConfigFor(position);
            var stepNum = (int)position + 1;
            var ctx     = await ResolveSkillsAsync(baseCtx, cfg.SkillNames, ct);
            var chain   = new SequentialCommandHandlerChain(slot.HandlersFactory(ctx));
            var step    = slot.StepFactory is not null
                ? slot.StepFactory(ctx, stepNum, cfg, chain)
                : DefaultStep(position, ctx, stepNum, cfg, chain);

            steps.Add(step);
        }

        var closedCfg = _config.Closed;
        steps.Add(new ClosedStep(baseCtx, steps.Count + 1,
            closedCfg?.Instructions ?? "Validate and seal the session.",
            closedCfg?.Gate         ?? new Gate("All checks passed")));

        return new StepPipeline(steps);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private StepConfig StepConfigFor(PipelineStep position) => position switch
    {
        PipelineStep.Kickoff  => _config.Kickoff,
        PipelineStep.Capture  => _config.Capture,
        PipelineStep.Organize => _config.Organize,
        PipelineStep.Distill  => _config.Distill,
        PipelineStep.Express  => _config.Express,
        _                     => throw new ArgumentOutOfRangeException(nameof(position))
    };

    private static AgentStep DefaultStep(
        PipelineStep     position,
        IStepPromptLayer ctx,
        int              stepNum,
        StepConfig       cfg,
        IStepChain?      chain) => position switch
    {
        PipelineStep.Kickoff  => new KickoffStep (ctx, stepNum, cfg.Instructions, cfg.Gate, chain),
        PipelineStep.Capture  => new CaptureStep (ctx, stepNum, cfg.Instructions, cfg.Gate, chain),
        PipelineStep.Organize => new OrganizeStep(ctx, stepNum, cfg.Instructions, cfg.Gate, chain),
        PipelineStep.Distill  => new DistillStep (ctx, stepNum, cfg.Instructions, cfg.Gate, chain),
        PipelineStep.Express  => new ExpressStep (ctx, stepNum, cfg.Instructions, cfg.Gate, chain),
        _                     => throw new ArgumentOutOfRangeException(nameof(position))
    };

    private async Task<IStepPromptLayer> ResolveSkillsAsync(
        IStepPromptLayer      ctx,
        IReadOnlyList<string> skillNames,
        CancellationToken     ct)
    {
        if (skillNames.Count == 0) return ctx;
        if (_resolver is null)
            throw new InvalidOperationException(
                $"Skills [{string.Join(", ", skillNames)}] require a resolver. " +
                $"Call {nameof(WithResolver)} before {nameof(BuildPipelineAsync)}.");
        var skills = await _resolver.ResolveAsync(skillNames, ct);
        return ctx.WithSkills(skills);
    }
}
