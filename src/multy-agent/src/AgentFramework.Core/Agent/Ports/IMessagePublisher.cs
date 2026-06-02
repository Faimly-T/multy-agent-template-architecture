namespace AgentFramework.Core.Agent.Ports;

/// <summary>
/// Facade for publishing agent messages to a service bus or message queue.
/// Implementations can target Azure Service Bus, RabbitMQ, AWS SQS, or an in-memory mock.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message to the specified topic/queue.
    /// </summary>
    /// <typeparam name="T">The message payload type.</typeparam>
    /// <param name="topic">The destination topic, queue, or channel name.</param>
    /// <param name="message">The message payload to publish.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PublishAsync<T>(string topic, T message, CancellationToken ct = default);
}
