namespace KatzuoOgust.Looop.Triggers;

/// <summary>Fires repeatedly on a fixed interval.</summary>
internal sealed class EveryTrigger(TimeSpan interval) : ITrigger
{
	private DateTimeOffset _next = DateTimeOffset.UtcNow + interval;
	private readonly Lock _lock = new();

	/// <inheritdoc/>
	public ValueTask<DateTimeOffset?> NextAsync(CancellationToken cancellationToken = default)
	{
		lock (_lock)
		{
			var fireAt = _next;
			_next = fireAt + interval;
			return new ValueTask<DateTimeOffset?>(fireAt);
		}
	}
}
