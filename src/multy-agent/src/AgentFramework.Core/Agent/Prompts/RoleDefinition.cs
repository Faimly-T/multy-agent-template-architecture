namespace AgentFramework.Core.Agent.Prompts;

public record Identity(string Role, string Persona, string Authority, string Boundary);

public record RoleDefinition(
    string Name,
    string Description,
    Identity Identity,
    string Mandate,
    IReadOnlyList<string> FactsAndDirectives)
{
    public string ToPromptString() => $"""
        You are {Identity.Persona}.
        Role: {Identity.Role}
        Authority: {Identity.Authority}
        Boundary: {Identity.Boundary}

        Mandate: {Mandate}

        Directives:
        {string.Join("\n", FactsAndDirectives.Select(d => $"- {d}"))}
        """;

    public string ToMd()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"name: {Name}");
        sb.AppendLine($"description: {Description}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Identity");
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|-------|-------|");
        sb.AppendLine($"| **Role** | {Identity.Role} |");
        sb.AppendLine($"| **Persona** | {Identity.Persona} |");
        sb.AppendLine($"| **Authority** | {Identity.Authority} |");
        sb.AppendLine($"| **Boundary** | {Identity.Boundary} |");
        sb.AppendLine();
        sb.AppendLine("## Mandate");
        sb.AppendLine($"> {Mandate}");
        sb.AppendLine();
        sb.AppendLine("## Facts & Directives");
        foreach (var directive in FactsAndDirectives)
            sb.AppendLine($"- {directive}");
        return sb.ToString();
    }
}