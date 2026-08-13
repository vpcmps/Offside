using Offside.Tool;

if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    PrintHelp();
    return 0;
}

if (args[0] != "init")
{
    Console.Error.WriteLine($"Unknown command '{args[0]}'.");
    PrintHelp();
    return 1;
}

var force = args.Contains("--force", StringComparer.OrdinalIgnoreCase);
var dirIndex = Array.FindIndex(args, a => a.Equals("--dir", StringComparison.OrdinalIgnoreCase));
var projectRoot = dirIndex >= 0 && dirIndex + 1 < args.Length
    ? Path.GetFullPath(args[dirIndex + 1])
    : Directory.GetCurrentDirectory();

try
{
    var installer = SkillInstaller.FromToolLocation();
    var written = installer.Install(projectRoot, force);
    foreach (var path in written)
        Console.WriteLine($"wrote {path}");

    Console.WriteLine();
    Console.WriteLine("Next:");
    Console.WriteLine("  dotnet add package Offside");
    Console.WriteLine("  dotnet add package Offside.AspNetCore   # ASP.NET hosts only");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static void PrintHelp()
{
    Console.WriteLine("Offside — the domain called offside");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  offside init [--dir <path>] [--force]");
    Console.WriteLine();
    Console.WriteLine("Copies agent skills (.cursor, .agents, .claude) and error catalog templates into a project.");
}
