namespace KatzuoOgust.Looop.Triggers;

/// <summary>
/// Fires <paramref name="leadTime"/> before each tick of an inner trigger.
/// If the computed fire time is already in the past it fires immediately.
/// </summary>
internal sealed class BeforeTrigger(ITrigger inner, TimeSpan leadTime) : ITrigger
{
	/// <inheritdoc/>
	public async ValueTask<DateTimeOffset?> NextAsync(CancellationToken cancellationToken = default)
	{
		var next = await inner.NextAsync(cancellationToken).ConfigureAwait(false);
		if (next is null) return null;

		var fireAt = next.Value - leadTime;
		// Don't return a time earlier than now — clamp to now.
		return fireAt < DateTimeOffset.UtcNow ? DateTimeOffset.UtcNow : fireAt;
	}
}
