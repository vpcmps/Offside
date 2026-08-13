namespace Offside.AspNetCore.Tests;

internal sealed class ProblemErrorPayload
{
    public string? Code { get; set; }
    public string? Kind { get; set; }
    public string? Detail { get; set; }
    public string? Field { get; set; }
}
