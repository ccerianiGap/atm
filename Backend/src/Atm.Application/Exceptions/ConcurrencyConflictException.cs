namespace Atm.Application.Exceptions;

public sealed class ConcurrencyConflictException(Exception? innerException = null)
    : Exception("The account was modified by another operation. Please refresh and try again.", innerException);
