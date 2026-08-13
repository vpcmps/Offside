namespace Offside.Tool;

public sealed class SkillInstaller
{
    public const string CursorSkills = ".cursor/skills";
    public const string AgentsSkills = ".agents/skills";
    public const string ClaudeSkills = ".claude/skills";

    private static readonly string[] SkillNames =
    [
        "offside-setup",
        "offside-domain",
        "offside-aspnet"
    ];

    private static readonly string[] AgentRoots =
    [
        CursorSkills,
        AgentsSkills,
        ClaudeSkills
    ];

    private readonly string _skillsSource;

    public SkillInstaller(string skillsSource)
    {
        _skillsSource = skillsSource;
    }

    public static SkillInstaller FromToolLocation()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "skills");
        return new SkillInstaller(source);
    }

    public IReadOnlyList<string> Install(string projectRoot, bool force)
    {
        if (!Directory.Exists(_skillsSource))
            throw new DirectoryNotFoundException($"Offside skills not found at '{_skillsSource}'.");

        Directory.CreateDirectory(projectRoot);
        var written = new List<string>();

        foreach (var agentRoot in AgentRoots)
        {
            foreach (var skill in SkillNames)
            {
                var from = Path.Combine(_skillsSource, skill);
                if (!Directory.Exists(from))
                    throw new DirectoryNotFoundException($"Skill '{skill}' missing at '{from}'.");

                var to = Path.Combine(projectRoot, agentRoot, skill);
                written.AddRange(CopyDirectory(from, to, force));
            }
        }

        written.AddRange(CopyTemplates(projectRoot, force));
        return written;
    }

    private IEnumerable<string> CopyTemplates(string projectRoot, bool force)
    {
        var templates = Path.Combine(_skillsSource, "templates");
        if (!Directory.Exists(templates))
            yield break;

        var errorsDir = Path.Combine(projectRoot, "errors");
        Directory.CreateDirectory(errorsDir);

        foreach (var file in Directory.GetFiles(templates))
        {
            var dest = Path.Combine(errorsDir, Path.GetFileName(file));
            if (File.Exists(dest) && !force)
                continue;

            File.Copy(file, dest, overwrite: force);
            yield return dest;
        }
    }

    private static IEnumerable<string> CopyDirectory(string from, string to, bool force)
    {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(from, file);
            var dest = Path.Combine(to, relative);
            var destDir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(destDir))
                Directory.CreateDirectory(destDir);

            if (File.Exists(dest) && !force)
                continue;

            File.Copy(file, dest, overwrite: force);
            yield return dest;
        }
    }
}
