namespace Altinn.ResourceRegistry.Core.Extensions;

/// <summary>
/// Extensions for <see cref="DateTimeOffset"/>
/// </summary>
public static class DateTimeOffsetExtensions
{
    /// <summary>
    /// Rounds a <see cref="DateTimeOffset"/> down to the closest <paramref name="precision"/>.
    /// </summary>
    /// <param name="dateTimeOffset">The <see cref="DateTimeOffset"/>.</param>
    /// <param name="precision">The precision.</param>
    /// <returns>The <see cref="DateTimeOffset"/>, rounded down.</returns>
    public static DateTimeOffset RoundDown(this DateTimeOffset dateTimeOffset, TimeSpan precision)
    {
        var remainder = dateTimeOffset.Ticks % precision.Ticks;
        return new DateTimeOffset(dateTimeOffset.Ticks - remainder, dateTimeOffset.Offset);
    }
}
