using System.Text.RegularExpressions;

namespace AgentFramework.Core.Agent.Steps;

public static class SkillParser
{
    public static Skill ParseFromMarkdown(string markdown)
    {
        var name = ExtractFrontmatter(markdown, "name");
        var description = ExtractFrontmatter(markdown, "description");
        var content = ExtractBody(markdown);
        return new Skill(name, description, content);
    }

    public static SkillSummary ParseSummaryFromMarkdown(string markdown)
    {
        var name = ExtractFrontmatter(markdown, "name");
        var description = ExtractFrontmatter(markdown, "description");
        return new SkillSummary(name, description);
    }

    private static string ExtractFrontmatter(string md, string field)
    {
        var match = Regex.Match(md, $@"^{field}:\s*(.+)$", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static string ExtractBody(string md)
    {
        var match = Regex.Match(md, @"---\s*\n[\s\S]*?---\s*\n(.+)", RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : md.Trim();
    }
}
