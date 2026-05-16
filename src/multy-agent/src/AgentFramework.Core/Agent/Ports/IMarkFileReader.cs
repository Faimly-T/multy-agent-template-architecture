namespace AgentFramework.Core.Agent.Ports;

public interface IMarkFileReader
{
    Task<string?> ReadProgressSummaryAsync(CancellationToken ct = default);
    Task<string?> ReadQuestionsLogAsync(CancellationToken ct = default);
    Task<string?> ReadDistillHistoryAsync(CancellationToken ct = default);
}
