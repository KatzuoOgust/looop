using Cronos;

namespace KatzuoOgust.Looop.Triggers;

/// <summary>
/// Fires on a cron schedule. Supports full cron expressions (<c>* * * * *</c>)
/// and named macros: <c>@yearly</c>, <c>@annually</c>, <c>@monthly</c>,
/// <c>@weekly</c>, <c>@daily</c>, <c>@midnight</c>, <c>@hourly</c>.
/// </summary>
internal sealed class CronTrigger : ITrigger
{
	private static readonly IReadOnlyDictionary<string, string> NamedSchedules =
		new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["@yearly"] = "0 0 1 1 *",
			["@annually"] = "0 0 1 1 *",
			["@monthly"] = "0 0 1 * *",
			["@weekly"] = "0 0 * * 0",
			["@daily"] = "0 0 * * *",
			["@midnight"] = "0 0 * * *",
			["@hourly"] = "0 * * * *",
			["@minutely"] = "* * * * *",
		};

	private readonly CronExpression _expr;

	public CronTrigger(string expression)
	{
		if (NamedSchedules.TryGetValue(expression.Trim(), out var expanded))
			expression = expanded;

		_expr = CronExpression.Parse(expression);
	}

	/// <inheritdoc/>
	public ValueTask<DateTimeOffset?> NextAsync(CancellationToken cancellationToken = default)
	{
		var next = _expr.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);
		return new ValueTask<DateTimeOffset?>(next);
	}
}
