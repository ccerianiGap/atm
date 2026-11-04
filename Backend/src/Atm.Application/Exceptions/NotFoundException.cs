namespace Atm.Application.Exceptions;

public sealed class NotFoundException(string resource, object key)
    : Exception($"{resource} '{key}' was not found.")
{
    public string Resource { get; } = resource;
    public object Key { get; } = key;

    public static NotFoundException Account(Guid id) => new("Account", id);
}
