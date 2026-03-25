namespace KatzuoOgust.Looop.Triggers;

/// <summary>Trigger backed by a user-supplied delegate.</summary>
internal sealed class CustomTrigger(Func<DateTimeOffset?, CancellationToken, ValueTask<DateTimeOffset?>> next) : ITrigger
{
	/// <inheritdoc/>
	public ValueTask<DateTimeOffset?> NextAsync(DateTimeOffset? lastRunAt = null, CancellationToken cancellationToken = default) =>
		next(lastRunAt, cancellationToken);
}
