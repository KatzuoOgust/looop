using KatzuoOgust.Looop.Triggers;

namespace KatzuoOgust.Looop;

/// <summary>Factory for all built-in triggers.</summary>
public static class Trigger
{
	/// <summary>Fire immediately once, then stop.</summary>
	public static ITrigger Once() => new OnceTrigger();

	/// <summary>Wait <paramref name="delay"/>, fire once, then stop.</summary>
	public static ITrigger After(TimeSpan delay) => new ShiftTrigger(new OnceTrigger(), delay);

	/// <summary>Fire repeatedly every <paramref name="interval"/>.</summary>
	public static ITrigger Every(TimeSpan interval) => new EveryTrigger(interval);

	/// <summary>
	/// Fire <paramref name="leadTime"/> before each tick of <paramref name="inner"/>.
	/// </summary>
	public static ITrigger Before(ITrigger inner, TimeSpan leadTime) =>
		new ShiftTrigger(inner, -leadTime);

	/// <summary>
	/// Fire <paramref name="delay"/> after each tick of <paramref name="inner"/>.
	/// </summary>
	public static ITrigger After(ITrigger inner, TimeSpan delay) =>
		new ShiftTrigger(inner, delay);

	/// <summary>
	/// Fire on a cron schedule. Supports full 5-field expressions (<c>* * * * *</c>)
	/// and named macros: <c>@yearly</c>, <c>@monthly</c>, <c>@weekly</c>,
	/// <c>@daily</c>, <c>@hourly</c>, <c>@minutely</c>.
	/// </summary>
	public static ITrigger Cron(string expression) => new CronTrigger(expression);

	/// <summary>
	/// Fire at the earliest of the given triggers.
	/// Stops when all children are exhausted.
	/// </summary>
	public static ITrigger WhenAny(params ITrigger[] triggers) => new WhenAnyTrigger(triggers);

	/// <summary>
	/// Fire when all given triggers would have fired (latest wins per iteration).
	/// Stops when any child is exhausted.
	/// </summary>
	public static ITrigger WhenAll(params ITrigger[] triggers) => new WhenAllTrigger(triggers);

	/// <summary>
	/// Build a trigger from a delegate that returns the next fire time on each call.
	/// Return <c>null</c> from the delegate to stop the loop.
	/// </summary>
	public static ITrigger Custom(Func<DateTimeOffset?, CancellationToken, ValueTask<DateTimeOffset?>> next) =>
		new CustomTrigger(next);
}
