using System.Text;
using System.Text.RegularExpressions;

namespace AgentFramework.Core.Agent;

public class Role
{
    public string Name { get; }
    public string Description { get; }
    public Identity Identity { get; }
    public string Mandate { get; }
    public IReadOnlyList<string> FactsAndDirectives { get; }

    public Role(
        string name,
        string description,
        Identity identity,
        string mandate,
        IReadOnlyList<string> factsAndDirectives)
    {
        Name = name;
        Description = description;
        Identity = identity;
        Mandate = mandate;
        FactsAndDirectives = factsAndDirectives;
    }

    public static Role FromMd(string markdown)
    {
        var name = ExtractFrontmatterField(markdown, "name");
        var description = ExtractFrontmatterField(markdown, "description");
        var identity = ParseIdentity(markdown);
        var mandate = ParseMandate(markdown);
        var directives = ParseFactsAndDirectives(markdown);

        return new Role(name, description, identity, mandate, directives);
    }

    private static string ExtractFrontmatterField(string md, string field)
    {
        var match = Regex.Match(md, $@"^{field}:\s*(.+)$", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static Identity ParseIdentity(string md)
    {
        string Extract(string label)
        {
            var pattern = $@"\|\s*\*\*{label}\*\*\s*\|\s*(.+?)\s*\|";
            var match = Regex.Match(md, pattern);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        return new Identity(
            role: Extract("Role"),
            persona: Extract("Persona"),
            authority: Extract("Authority"),
            boundary: Extract("Boundary"));
    }

    private static string ParseMandate(string md)
    {
        var match = Regex.Match(md, @"##\s*Mandate\s*\n>\s*(.+)", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static List<string> ParseFactsAndDirectives(string md)
    {
        var directives = new List<string>();
        var sectionMatch = Regex.Match(md, @"##\s*Facts\s*&\s*Directives\s*\n([\s\S]+?)(?=\n##|\z)");
        if (!sectionMatch.Success) return directives;

        var lines = sectionMatch.Groups[1].Value.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("- "))
            {
                directives.Add(trimmed[2..]);
            }
        }

        return directives;
    }

    public string ToMd()
    {
        var sb = new StringBuilder();

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
        {
            sb.AppendLine($"- {directive}");
        }

        return sb.ToString();
    }
}

public class Identity
{
    public string Role { get; }
    public string Persona { get; }
    public string Authority { get; }
    public string Boundary { get; }

    public Identity(string role, string persona, string authority, string boundary)
    {
        Role = role;
        Persona = persona;
        Authority = authority;
        Boundary = boundary;
    }
}