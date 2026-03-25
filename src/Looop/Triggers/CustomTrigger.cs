namespace KatzuoOgust.Looop.Triggers;

/// <summary>Trigger backed by a user-supplied delegate.</summary>
internal sealed class CustomTrigger(Func<CancellationToken, ValueTask<DateTimeOffset?>> next) : ITrigger
{
	/// <inheritdoc/>
	public ValueTask<DateTimeOffset?> NextAsync(CancellationToken cancellationToken = default) =>
		next(cancellationToken);
}
