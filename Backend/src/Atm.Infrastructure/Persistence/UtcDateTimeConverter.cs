using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Atm.Infrastructure.Persistence;

/// <summary>
/// All timestamps are stored in UTC, but providers return <see cref="DateTimeKind.Unspecified"/>.
/// Re-tagging them as UTC makes them serialize with a trailing "Z" so clients don't read them as local time.
/// </summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    toDb => toDb.Kind == DateTimeKind.Utc ? toDb : toDb.ToUniversalTime(),
    fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc));
