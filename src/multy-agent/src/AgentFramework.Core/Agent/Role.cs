using System.Text;

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