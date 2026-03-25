namespace KatzuoOgust.Looop;

/// <summary>
/// Determines when the loop should fire next.
/// Return <c>null</c> to signal that the loop should stop.
/// </summary>
public interface ITrigger
{
	/// <summary>
	/// Returns the next scheduled fire time, or <c>null</c> to stop the loop.
	/// </summary>
	/// <param name="lastRunAt">The previously scheduled fire time, or <c>null</c> on the first call.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask<DateTimeOffset?> NextAsync(DateTimeOffset? lastRunAt = null, CancellationToken cancellationToken = default);
}
