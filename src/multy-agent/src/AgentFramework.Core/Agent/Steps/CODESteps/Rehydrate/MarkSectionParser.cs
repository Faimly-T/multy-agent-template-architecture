namespace AgentFramework.Core.Agent.Steps.CODESteps.Rehydrate;

internal static class MarkSectionParser
{
    /// <summary>
    /// Extracts key-value pairs from a two-column markdown table under the given section header.
    /// Skips the header row and separator rows automatically.
    /// </summary>
    public static Dictionary<string, string> ExtractTableSection(string markdown, string sectionHeader)
    {
        var content = GetSectionContent(markdown, sectionHeader);
        if (content is null) return [];

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var firstDataRow = true;

        foreach (var line in content.Split('\n'))
        {
            if (!IsTableRow(line) || IsSeparatorRow(line)) continue;

            var cells = SplitCells(line);
            if (cells.Length < 2) continue;

            if (firstDataRow) { firstDataRow = false; continue; }

            var key = cells[0];
            var value = cells[1];
            if (!string.IsNullOrWhiteSpace(key))
                result[key] = value;
        }

        return result;
    }

    /// <summary>
    /// Extracts bullet list items (lines starting with "- ") from the given section.
    /// </summary>
    public static IReadOnlyList<string> ExtractBulletSection(string markdown, string sectionHeader)
    {
        var content = GetSectionContent(markdown, sectionHeader);
        if (content is null) return [];

        return content.Split('\n')
            .Where(l => l.TrimStart().StartsWith("- "))
            .Select(l => StripMarkdown(l.TrimStart()[2..].Trim()))
            .ToList();
    }

    /// <summary>
    /// Extracts all data rows from a multi-column markdown table under the given section header.
    /// Each row is returned as a string array of cell values. Header row is skipped.
    /// </summary>
    public static IReadOnlyList<string[]> ExtractTableRows(string markdown, string sectionHeader)
    {
        var content = GetSectionContent(markdown, sectionHeader);
        if (content is null) return [];

        var rows = new List<string[]>();
        var firstDataRow = true;

        foreach (var line in content.Split('\n'))
        {
            if (!IsTableRow(line) || IsSeparatorRow(line)) continue;

            var cells = SplitCells(line);
            if (firstDataRow) { firstDataRow = false; continue; }

            rows.Add(cells);
        }

        return rows;
    }

    /// <summary>
    /// Counts section headers that start with the given prefix (e.g. "Run #" to count distill runs).
    /// </summary>
    public static int CountSections(string markdown, string sectionPrefix)
        => markdown.Split('\n')
            .Count(l => l.TrimStart().StartsWith($"## {sectionPrefix}", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Returns true when the section is missing, empty, or contains only italic placeholder text (_None_).
    /// </summary>
    public static bool SectionIsEmpty(string markdown, string sectionHeader)
    {
        var content = GetSectionContent(markdown, sectionHeader);
        if (content is null) return true;
        var trimmed = content.Trim();
        return string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('_');
    }

    internal static string StripMarkdown(string text)
    {
        text = text.Replace("**", "").Replace("__", "");
        if (text.StartsWith('_') && text.EndsWith('_') && text.Length > 2)
            text = text[1..^1];
        return text.Replace("`", "").Trim();
    }

    private static string? GetSectionContent(string markdown, string sectionHeader)
    {
        var lines = markdown.Split('\n');
        var startIdx = -1;

        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith($"## {sectionHeader}", StringComparison.OrdinalIgnoreCase))
            {
                startIdx = i + 1;
                break;
            }
        }

        if (startIdx < 0) return null;

        var contentLines = new List<string>();
        for (var i = startIdx; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("## ")) break;
            contentLines.Add(lines[i]);
        }

        return string.Join('\n', contentLines);
    }

    private static bool IsTableRow(string line) => line.Trim().StartsWith('|');

    private static bool IsSeparatorRow(string line)
    {
        var inner = line.Trim().Trim('|');
        return inner.Split('|').All(cell => cell.Trim().All(c => c == '-' || c == ':' || c == ' '));
    }

    private static string[] SplitCells(string line)
        => line.Trim().Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => StripMarkdown(c.Trim()))
            .ToArray();
}
