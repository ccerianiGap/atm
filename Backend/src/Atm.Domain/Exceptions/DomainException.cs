namespace Atm.Domain.Exceptions;

/// <summary>
/// Base type for business-rule violations. <see cref="Code"/> is a stable, machine-readable
/// identifier that clients can rely on (the message is for humans and may change).
/// </summary>
public abstract class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
