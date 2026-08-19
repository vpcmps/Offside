namespace Offside.Tool;

/// <summary>
/// Copies the Offside agent skills and error-catalog templates into a project. This is the
/// engine behind <c>offside init</c>.
/// </summary>
public sealed class SkillInstaller
{
    /// <summary>The Cursor skills directory, relative to the project root.</summary>
    public const string CursorSkills = ".cursor/skills";

    /// <summary>The generic agent skills directory, relative to the project root.</summary>
    public const string AgentsSkills = ".agents/skills";

    /// <summary>The Claude Code skills directory, relative to the project root.</summary>
    public const string ClaudeSkills = ".claude/skills";

    private static readonly string[] SkillNames =
    [
        "offside-setup",
        "offside-domain",
        "offside-aspnet",
        "offside-fluentvalidation",
        "offside-fastendpoint",
        "offside-implementation",
        "offside-refactoring",
        "offside-azure-app-configuration"
    ];

    private static readonly string[] AgentRoots =
    [
        CursorSkills,
        AgentsSkills,
        ClaudeSkills
    ];

    private readonly string _skillsSource;

    /// <summary>Initializes a new installer reading from a given skills directory.</summary>
    /// <param name="skillsSource">The directory holding the <c>offside-*</c> skill folders and <c>templates</c>.</param>
    public SkillInstaller(string skillsSource)
    {
        _skillsSource = skillsSource;
    }

    /// <summary>Creates an installer reading the skills shipped alongside the tool.</summary>
    /// <returns>The installer.</returns>
    public static SkillInstaller FromToolLocation()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "skills");
        return new SkillInstaller(source);
    }

    /// <summary>
    /// Writes the Offside skills into each agent directory and the catalog templates into
    /// <c>&lt;projectRoot&gt;/errors</c>.
    /// </summary>
    /// <param name="projectRoot">The project directory. Created if it does not exist.</param>
    /// <param name="force">When <see langword="false"/>, existing files are left untouched; when <see langword="true"/>, they are overwritten.</param>
    /// <returns>The absolute path of every file written, in write order.</returns>
    /// <exception cref="DirectoryNotFoundException">The skills source directory, or one of the expected skill folders, is missing.</exception>
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
