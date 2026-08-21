namespace Offside.AspNetCore.Tests;

internal sealed class ProblemPayload
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public int Status { get; set; }
    public string? Detail { get; set; }
    public string? TraceId { get; set; }
    public string? ErrorCode { get; set; }
    public string? Debug { get; set; }
    public List<ProblemErrorPayload> Errors { get; set; } = [];
}
