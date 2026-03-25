namespace KatzuoOgust.Looop;

/// <summary>
/// Determines when the loop should fire next.
/// Return <c>null</c> to signal that the loop should stop.
/// </summary>
public interface ITrigger
{
	ValueTask<DateTimeOffset?> NextAsync(DateTimeOffset? lastRunAt = null, CancellationToken cancellationToken = default);
}
