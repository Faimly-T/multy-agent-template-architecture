using AgentFramework.Core.Agent;

namespace AgentFramework.Core.Tests;

public class RoleTests
{
    private const string TestDataPath = "TestData/UxPersonaRole.md";

    private static Role LoadRole()
    {
        var markdown = File.ReadAllText(TestDataPath);
        return RoleParser.ParseFromMarkdown(markdown);
    }

    [Fact]
    public void FromMd_ParsesName()
    {
        var role = LoadRole();
        Assert.Equal("ux-persona-architect", role.Name);
    }

    [Fact]
    public void FromMd_ParsesDescription()
    {
        var role = LoadRole();
        Assert.Equal("Senior UX Researcher & Persona Architect", role.Description);
    }

    [Fact]
    public void FromMd_ParsesIdentityRole()
    {
        var role = LoadRole();
        Assert.Equal("Senior UX Researcher & Persona Architect", role.Identity.Role);
    }

    [Fact]
    public void FromMd_ParsesIdentityPersona()
    {
        var role = LoadRole();
        Assert.Contains("Clara Mendes", role.Identity.Persona);
    }

    [Fact]
    public void FromMd_ParsesIdentityAuthority()
    {
        var role = LoadRole();
        Assert.Contains("Full autonomy over persona definition", role.Identity.Authority);
    }

    [Fact]
    public void FromMd_ParsesIdentityBoundary()
    {
        var role = LoadRole();
        Assert.Contains("OWNS: personas", role.Identity.Boundary);
        Assert.Contains("DOES NOT OWN: visual design", role.Identity.Boundary);
    }

    [Fact]
    public void FromMd_ParsesMandate()
    {
        var role = LoadRole();
        Assert.StartsWith("Transform any product description into validated user personas", role.Mandate);
    }

    [Fact]
    public void FromMd_ParsesAllDirectives()
    {
        var role = LoadRole();
        Assert.Equal(7, role.FactsAndDirectives.Count);
    }

    [Fact]
    public void FromMd_FirstDirectiveContent()
    {
        var role = LoadRole();
        Assert.StartsWith("Prioritize behavioral data over demographics", role.FactsAndDirectives[0]);
    }

    [Fact]
    public void ToMd_RoundTrip_PreservesName()
    {
        var role = LoadRole();
        var exported = role.ToMd();
        var reparsed = RoleParser.ParseFromMarkdown(exported);

        Assert.Equal(role.Name, reparsed.Name);
    }

    [Fact]
    public void ToMd_RoundTrip_PreservesMandate()
    {
        var role = LoadRole();
        var exported = role.ToMd();
        var reparsed = RoleParser.ParseFromMarkdown(exported);

        Assert.Equal(role.Mandate, reparsed.Mandate);
    }

    [Fact]
    public void ToMd_RoundTrip_PreservesDirectiveCount()
    {
        var role = LoadRole();
        var exported = role.ToMd();
        var reparsed = RoleParser.ParseFromMarkdown(exported);

        Assert.Equal(role.FactsAndDirectives.Count, reparsed.FactsAndDirectives.Count);
    }
}
