namespace KatzuoOgust.Looop.Triggers;

/// <summary>
/// Fires when the <em>earliest</em> of the given triggers would fire.
/// Once a child trigger is exhausted it is excluded from future races.
/// Stops when all children are exhausted.
/// </summary>
internal sealed class WhenAnyTrigger(ITrigger[] triggers) : ITrigger
{
	private readonly List<ITrigger> _active = [.. triggers];

	/// <inheritdoc/>
	public async ValueTask<DateTimeOffset?> NextAsync(DateTimeOffset? lastRunAt = null, CancellationToken cancellationToken = default)
	{
		if (_active.Count == 0) return null;

		var tasks = _active.Select(t => t.NextAsync(lastRunAt, cancellationToken).AsTask()).ToList();
		var results = await Task.WhenAll(tasks).ConfigureAwait(false);

		DateTimeOffset? earliest = null;
		for (int i = results.Length - 1; i >= 0; i--)
		{
			if (results[i] is null)
			{
				_active.RemoveAt(i);
				continue;
			}
			if (earliest is null || results[i]!.Value < earliest.Value)
				earliest = results[i];
		}

		return earliest;
	}
}
