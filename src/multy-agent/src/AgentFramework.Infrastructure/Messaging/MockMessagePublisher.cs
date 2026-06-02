using AgentFramework.Core.Agent.Ports;

namespace AgentFramework.Infrastructure.Messaging;

/// <summary>
/// In-memory mock of IMessagePublisher.
/// Simulates publishing to a service bus or message queue by recording all published messages
/// in memory. Use in tests or local development to verify what would be sent to the bus.
///
/// In production, replace with a real implementation targeting Azure Service Bus,
/// RabbitMQ, AWS SQS, or any AMQP-compatible broker.
/// </summary>
public sealed class MockMessagePublisher : IMessagePublisher
{
    private readonly List<PublishedMessage> _published = [];

    /// <summary>All messages published to any topic since construction.</summary>
    public IReadOnlyList<PublishedMessage> Published => _published.AsReadOnly();

    public Task PublishAsync<T>(string topic, T message, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var entry = new PublishedMessage(
            Topic:       topic,
            PayloadType: typeof(T).Name,
            Payload:     message!,
            PublishedAt: DateTime.UtcNow);

        _published.Add(entry);

        // Simulate the side-effect that would normally be a network call
        Console.WriteLine(
            $"[MockServiceBus] → topic:{topic} type:{typeof(T).Name} at {entry.PublishedAt:O}");

        return Task.CompletedTask;
    }

    /// <summary>Returns all messages published to the given topic.</summary>
    public IReadOnlyList<PublishedMessage> GetByTopic(string topic)
        => _published.Where(m => m.Topic == topic).ToList().AsReadOnly();

    /// <summary>Clears all recorded messages (useful between test cases).</summary>
    public void Clear() => _published.Clear();
}

/// <summary>A single message recorded by MockMessagePublisher.</summary>
public sealed record PublishedMessage(
    string   Topic,
    string   PayloadType,
    object   Payload,
    DateTime PublishedAt);
