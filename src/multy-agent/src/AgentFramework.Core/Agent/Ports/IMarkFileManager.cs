namespace AgentFramework.Core.Agent.Ports;

public interface IMarkFileManager : IMarkFileReader
{
    Task WriteProgressSummaryAsync(string content, CancellationToken ct = default);
    Task WriteQuestionsLogAsync(string content, CancellationToken ct = default);
    Task AppendDistillHistoryAsync(string entry, CancellationToken ct = default);
}
