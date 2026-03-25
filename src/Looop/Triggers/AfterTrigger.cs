namespace KatzuoOgust.Looop.Triggers;

/// <summary>Waits a fixed delay then fires once and stops.</summary>
internal sealed class AfterTrigger(TimeSpan delay) : ITrigger
{
	private int _fired;

	/// <inheritdoc/>
	public ValueTask<DateTimeOffset?> NextAsync(CancellationToken cancellationToken = default)
	{
		if (Interlocked.CompareExchange(ref _fired, 1, 0) != 0)
			return new ValueTask<DateTimeOffset?>((DateTimeOffset?)null);

		return new ValueTask<DateTimeOffset?>(DateTimeOffset.UtcNow + delay);
	}
}
