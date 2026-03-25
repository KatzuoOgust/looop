using KatzuoOgust.Looop;

// ── Once ──────────────────────────────────────────────────────────────────────
Console.WriteLine("=== Once ===");
await Loop.RunAsync(
	_ => { Console.WriteLine("Fired once"); return Task.CompletedTask; },
	Trigger.Once());

// ── After ─────────────────────────────────────────────────────────────────────
Console.WriteLine("\n=== After 200ms ===");
await Loop.RunAsync(
	_ => { Console.WriteLine("Fired after delay"); return Task.CompletedTask; },
	Trigger.After(TimeSpan.FromMilliseconds(200)));

// ── Every (with cancellation) ─────────────────────────────────────────────────
Console.WriteLine("\n=== Every 100ms for ~400ms ===");
using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
int count = 0;
await Loop.RunAsync(
	_ => { Console.WriteLine($"  tick {++count}"); return Task.CompletedTask; },
	Trigger.Every(TimeSpan.FromMilliseconds(100)),
	cancellationToken: cts.Token);

// ── Cron ──────────────────────────────────────────────────────────────────────
Console.WriteLine("\n=== Cron: next @hourly occurrence ===");
var next = await Trigger.Cron("@hourly").NextAsync();
Console.WriteLine($"  Next @hourly tick: {next:HH:mm:ss} UTC");

// ── WhenAny ───────────────────────────────────────────────────────────────────
Console.WriteLine("\n=== WhenAny: pick earliest of 50ms / 200ms (fires 3 times) ===");
using var cts2 = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
int tick = 0;
await Loop.RunAsync(
	_ => { Console.WriteLine($"  WhenAny tick {++tick}"); return Task.CompletedTask; },
	Trigger.WhenAny(
		Trigger.Every(TimeSpan.FromMilliseconds(50)),
		Trigger.Every(TimeSpan.FromMilliseconds(200))),
	cancellationToken: cts2.Token);

// ── ErrorPolicy.Continue ──────────────────────────────────────────────────────
Console.WriteLine("\n=== ErrorPolicy.Continue: errors are swallowed ===");
using var cts3 = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
int run = 0;
await Loop.RunAsync(
	_ =>
	{
		run++;
		Console.WriteLine($"  attempt {run}");
		if (run % 2 == 0) throw new Exception("even runs fail (swallowed)");
		return Task.CompletedTask;
	},
	Trigger.Every(TimeSpan.FromMilliseconds(60)),
	ErrorPolicy.Continue,
	cts3.Token);

// ── IJob ──────────────────────────────────────────────────────────────────────
Console.WriteLine("\n=== IJob implementation ===");
await Loop.RunAsync(new CountdownJob(3));

Console.WriteLine("\nDone.");

sealed class CountdownJob(int limit) : IJob
{
	private readonly ITrigger _trigger = Trigger.Every(TimeSpan.FromMilliseconds(50));
	private int _count;

	public ValueTask<DateTimeOffset?> NextAsync(CancellationToken ct = default) =>
		_count >= limit ? new(default(DateTimeOffset?)) : _trigger.NextAsync(ct);

	public Task HandleAsync(CancellationToken ct)
	{
		Console.WriteLine($"  CountdownJob tick {++_count}/{limit}");
		return Task.CompletedTask;
	}

	public ValueTask HandleErrorAsync(Exception ex, CancellationToken ct)
	{
		Console.WriteLine($"  Error (continuing): {ex.Message}");
		return ValueTask.CompletedTask;
	}
}
