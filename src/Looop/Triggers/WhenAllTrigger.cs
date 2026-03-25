namespace KatzuoOgust.Looop.Triggers;

/// <summary>
/// Fires when <em>all</em> of the given triggers would have fired — i.e. the
/// latest of their next fire times. Stops when any child is exhausted.
/// </summary>
internal sealed class WhenAllTrigger(ITrigger[] triggers) : ITrigger
{
	private readonly ITrigger[] _triggers = triggers;

	/// <inheritdoc/>
	public async ValueTask<DateTimeOffset?> NextAsync(CancellationToken cancellationToken = default)
	{
		var tasks = _triggers.Select(t => t.NextAsync(cancellationToken).AsTask()).ToList();
		var results = await Task.WhenAll(tasks).ConfigureAwait(false);

		DateTimeOffset? latest = DateTimeOffset.MinValue;
		foreach (var result in results)
		{
			if (result is null) return null; // any exhausted trigger stops the loop
			if (result.Value > latest!.Value) latest = result;
		}

		return latest == DateTimeOffset.MinValue ? null : latest;
	}
}
