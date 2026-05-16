using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Events;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent;

public class AgentStepExecutor
{
    private readonly ConversationHistory _conversation;
    private readonly IDomainEventPublisher _eventPublisher;

    public AgentStepExecutor(ConversationHistory conversation, IDomainEventPublisher eventPublisher)
    {
        _conversation = conversation;
        _eventPublisher = eventPublisher;
    }

    public async Task<StepResult> ExecuteStepAsync(
        AgentStep step,
        Role role,
        IAgentRunContext? context,
        ISessionWriter writer,
        IStepMessageBuilder messageBuilder,
        IChatClient chatClient,
        CancellationToken ct = default)
    {
        _eventPublisher.Publish(new StepStarted(step.StepNumber, step.Name, step.SkillName));

        var chainResult = await step.ExecuteChainAsync(context, writer, chatClient, _eventPublisher, role, ct);

        if (chainResult is not null)
        {
            var initMessages = messageBuilder.BuildMessages(step, role, context, _conversation.Messages);
            foreach (var msg in initMessages.Where(m => m.Role == MessageRole.System))
                _conversation.AddSystemMessage(msg.Content);

            _conversation.AddUserMessage($"## Step {step.StepNumber}: {step.Name}");
            _conversation.AddAssistantMessage(chainResult.Output);
            ApplyStepOutcome(step, chainResult, writer, context);
            return chainResult;
        }

        var messages = messageBuilder.BuildMessages(step, role, context, _conversation.Messages);

        foreach (var msg in messages)
        {
            if (msg.Role == MessageRole.System)
                _conversation.AddSystemMessage(msg.Content);
            else
                _conversation.AddUserMessage(msg.Content);
        }

        var result = await chatClient.SendAsync(_conversation.Messages, step, ct);

        _conversation.AddAssistantMessage(result.Output);

        ApplyStepOutcome(step, result, writer, context);

        return result;
    }

    private void ApplyStepOutcome(AgentStep step, StepResult result, ISessionWriter writer, IAgentRunContext? context)
    {
        if (!result.GateSatisfied)
        {
            _eventPublisher.Publish(new StepGateFailed(step.StepNumber, step.Name, step.Gate.Description, result));
            return;
        }

        result.ApplyTo(writer);

        if (context?.Session is not null && step.Gate.Verify is not null)
        {
            var verified = step.Gate.Verify(context.Session, result);
            if (!verified)
            {
                _eventPublisher.Publish(new StepGateFailed(step.StepNumber, step.Name, step.Gate.Description, result));
                return;
            }
        }

        foreach (var domainEvent in result.GetDomainEvents())
            _eventPublisher.Publish(domainEvent);

        _eventPublisher.Publish(new StepCompleted(step.StepNumber, step.Name, result));
    }
}

public interface IDomainEventPublisher
{
    void Publish(DomainEvent @event);
}
