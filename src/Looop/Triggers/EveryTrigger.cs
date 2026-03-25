namespace KatzuoOgust.Looop.Triggers;

/// <summary>Fires repeatedly on a fixed interval.</summary>
internal sealed class EveryTrigger(TimeSpan interval) : ITrigger
{
	/// <inheritdoc/>
	public ValueTask<DateTimeOffset?> NextAsync(DateTimeOffset? lastRunAt = null, CancellationToken cancellationToken = default) =>
		new((lastRunAt ?? DateTimeOffset.UtcNow) + interval);
}
