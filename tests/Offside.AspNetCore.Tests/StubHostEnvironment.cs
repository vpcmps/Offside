using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Offside.AspNetCore.Tests;

internal sealed class StubHostEnvironment : IHostEnvironment
{
    public required string EnvironmentName { get; set; }
    public string ApplicationName { get; set; } = "Offside.Tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
