namespace KatzuoOgust.Looop.Triggers;

/// <summary>
/// Shifts each tick of an inner trigger by <paramref name="offset"/>.
/// A negative offset fires earlier (before the inner tick);
/// a positive offset fires later (after the inner tick).
/// If the computed fire time is already in the past it fires immediately.
/// </summary>
internal sealed class ShiftTrigger(ITrigger inner, TimeSpan offset) : ITrigger
{
	/// <inheritdoc/>
	public async ValueTask<DateTimeOffset?> NextAsync(DateTimeOffset? lastRunAt = null, CancellationToken cancellationToken = default)
	{
		var next = await inner.NextAsync(lastRunAt, cancellationToken).ConfigureAwait(false);
		if (next is null) return null;

		var fireAt = next.Value + offset;
		var now = lastRunAt ?? DateTimeOffset.UtcNow;
		// Don't return a time earlier than now — clamp to now.
		return fireAt < now ? now : fireAt;
	}
}
