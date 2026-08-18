using Offside.Tool;
using Xunit;

namespace Offside.Tool.Tests;

public sealed class SkillInstallerTests
{
    [Fact]
    public void Install_copies_skills_and_templates()
    {
        var source = CreateSkillTree();
        var dest = Directory.CreateTempSubdirectory().FullName;

        var written = new SkillInstaller(source).Install(dest, force: false);

        Assert.Contains(written, path => path.EndsWith(Path.Combine("offside-setup", "SKILL.md"), StringComparison.Ordinal));
        Assert.True(File.Exists(Path.Combine(dest, ".cursor", "skills", "offside-setup", "SKILL.md")));
        Assert.True(File.Exists(Path.Combine(dest, ".agents", "skills", "offside-domain", "SKILL.md")));
        Assert.True(File.Exists(Path.Combine(dest, ".claude", "skills", "offside-aspnet", "SKILL.md")));
        Assert.True(File.Exists(Path.Combine(dest, ".cursor", "skills", "offside-fluentvalidation", "SKILL.md")));
        Assert.True(File.Exists(Path.Combine(dest, ".agents", "skills", "offside-fastendpoint", "SKILL.md")));
        Assert.True(File.Exists(Path.Combine(dest, "errors", "errors.json")));
        Assert.True(File.Exists(Path.Combine(dest, "errors", "errors.pt-BR.json")));
    }

    [Fact]
    public void Install_without_force_does_not_overwrite()
    {
        var source = CreateSkillTree();
        var dest = Directory.CreateTempSubdirectory().FullName;
        var installer = new SkillInstaller(source);
        installer.Install(dest, force: false);

        var skillPath = Path.Combine(dest, ".cursor", "skills", "offside-setup", "SKILL.md");
        File.WriteAllText(skillPath, "keep-me");

        installer.Install(dest, force: false);

        Assert.Equal("keep-me", File.ReadAllText(skillPath));
    }

    [Fact]
    public void Install_with_force_overwrites()
    {
        var source = CreateSkillTree();
        var dest = Directory.CreateTempSubdirectory().FullName;
        var installer = new SkillInstaller(source);
        installer.Install(dest, force: false);

        var skillPath = Path.Combine(dest, ".cursor", "skills", "offside-setup", "SKILL.md");
        File.WriteAllText(skillPath, "keep-me");

        installer.Install(dest, force: true);

        Assert.NotEqual("keep-me", File.ReadAllText(skillPath));
        Assert.Contains("offside-setup", File.ReadAllText(skillPath), StringComparison.Ordinal);
    }

    [Fact]
    public void Install_throws_when_skills_source_missing()
    {
        var dest = Directory.CreateTempSubdirectory().FullName;

        Assert.Throws<DirectoryNotFoundException>(() =>
            new SkillInstaller(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")))
                .Install(dest, force: false));
    }

    private static string CreateSkillTree()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        WriteSkill(root, "offside-setup");
        WriteSkill(root, "offside-domain");
        WriteSkill(root, "offside-aspnet");
        WriteSkill(root, "offside-fluentvalidation");
        WriteSkill(root, "offside-fastendpoint");
        var templates = Path.Combine(root, "templates");
        Directory.CreateDirectory(templates);
        File.WriteAllText(Path.Combine(templates, "errors.json"), "{ }");
        File.WriteAllText(Path.Combine(templates, "errors.pt-BR.json"), "{ }");
        return root;
    }

    private static void WriteSkill(string root, string name)
    {
        var dir = Path.Combine(root, name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "SKILL.md"), $"---\nname: {name}\n---\n# {name}\n");
    }
}
