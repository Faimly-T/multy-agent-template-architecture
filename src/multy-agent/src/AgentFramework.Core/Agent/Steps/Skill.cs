namespace AgentFramework.Core.Agent.Steps;

public record Skill(string Name, string Description, string Instructions)
{
    public static Skill FromMd(string markdown)
    {
        var name = ExtractFrontmatter(markdown, "name");
        var description = ExtractFrontmatter(markdown, "description");
        var instructions = ExtractBody(markdown);
        return new Skill(name, description, instructions);
    }

    private static string ExtractFrontmatter(string md, string field)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            md, $@"^{field}:\s*(.+)$", System.Text.RegularExpressions.RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static string ExtractBody(string md)
    {
        var match = System.Text.RegularExpressions.Regex.Match(md, @"---\s*\n[\s\S]*?---\s*\n(.+)", System.Text.RegularExpressions.RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : md.Trim();
    }
}
