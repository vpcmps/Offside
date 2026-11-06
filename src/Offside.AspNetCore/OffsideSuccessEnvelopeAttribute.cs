namespace Offside.AspNetCore;

/// <summary>Overrides the host's success envelope setting for an endpoint.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class OffsideSuccessEnvelopeAttribute(bool enabled) : Attribute
{
    /// <summary>Whether the endpoint uses the success envelope.</summary>
    public bool Enabled { get; } = enabled;
}
