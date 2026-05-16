namespace AgentFramework.Core.Agent.Session;

public record SessionMarkFilePaths(string AgentPrefix, string BasePath)
{
    public string ProgressSummary => $"{BasePath}/{AgentPrefix}_Progress_Summary_MARK.md";
    public string QuestionsLog    => $"{BasePath}/{AgentPrefix}_Questions_Log_MARK.md";
    public string DistillHistory  => $"{BasePath}/{AgentPrefix}_Distill_History_MARK.md";
}
