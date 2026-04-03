namespace AgentFramework.Core.Agent.Steps;

public static class ResponseSchemas
{
    public const string Rehydrate = """
        {
          "sessionObjective": "string — the confirmed session objective",
          "gateSatisfied": true
        }
        """;

    public const string Capture = """
        {
          "islands": [
            {
              "id": "ISL-XXX",
              "type": "UserType | Goal | PainPoint | BehavioralPattern | ContextOfUse | EmotionalState | AntiUser | Stakeholder | AccessibilitySignal",
              "description": "string",
              "source": "string",
              "relatesToIslandId": "ISL-XXX or null"
            }
          ],
          "gateSatisfied": true
        }
        """;

    public const string Organize = """
        {
          "organizedIslands": [
            {
              "islandId": "ISL-XXX",
              "newStatus": "Organized | Discarded"
            }
          ],
          "decisions": [
            {
              "id": "DEC-XXX",
              "description": "string",
              "impact": "string"
            }
          ],
          "gateSatisfied": true
        }
        """;

    public const string Distill = """
        {
          "distilledIslands": [
            {
              "islandId": "ISL-XXX",
              "newStatus": "Distilled | Discarded"
            }
          ],
          "deliverables": [
            {
              "deliverableId": "DEL-XXX",
              "path": "outputs/folder/filename.md",
              "status": "Draft | Partial | Complete"
            }
          ],
          "gateSatisfied": true
        }
        """;

    public const string Relay = """
        {
          "inputTokens": 0,
          "outputTokens": 0,
          "gateSatisfied": true
        }
        """;
}
