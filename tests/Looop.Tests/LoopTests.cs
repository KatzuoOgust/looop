using System.Runtime.ExceptionServices;
using KatzuoOgust.Looop;

namespace KatzuoOgust.Looop;

public class LoopTests
{

	[Fact]
	public async Task RunAsync_Once_ExecutesExactlyOnce()
	{
		int count = 0;
		await Loop.RunAsync(_ => { count++; return Task.CompletedTask; }, Trigger.Once());
		Assert.Equal(1, count);
	}

	[Fact]
	public async Task RunAsync_After_ExecutesOnceAfterDelay()
	{
		int count = 0;
		var sw = System.Diagnostics.Stopwatch.StartNew();
		await Loop.RunAsync(_ => { count++; return Task.CompletedTask; },
			Trigger.After(TimeSpan.FromMilliseconds(50)));
		sw.Stop();

		Assert.Equal(1, count);
		Assert.True(sw.ElapsedMilliseconds >= 40);
	}

	[Fact]
	public async Task RunAsync_Every_StopsOnCancellation()
	{
		int count = 0;
		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

		await Loop.RunAsync(
			_ => { count++; return Task.CompletedTask; },
			Trigger.Every(TimeSpan.FromMilliseconds(50)),
			cancellationToken: cts.Token);

		Assert.True(count >= 3, $"Expected at least 3 executions, got {count}");
	}


	[Fact]
	public async Task RunAsync_CancellationBeforeStart_DoesNotExecute()
	{
		int count = 0;
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		await Loop.RunAsync(_ => { count++; return Task.CompletedTask; },
			Trigger.Every(TimeSpan.FromMilliseconds(10)),
			cancellationToken: cts.Token);

		Assert.Equal(0, count);
	}

	[Fact]
	public async Task RunAsync_CancellationDuringDelay_StopsCleanly()
	{
		int count = 0;
		using var cts = new CancellationTokenSource();

		var task = Loop.RunAsync(
			_ => { count++; return Task.CompletedTask; },
			Trigger.After(TimeSpan.FromSeconds(60)),
			cancellationToken: cts.Token);

		await Task.Delay(20);
		await cts.CancelAsync();
		await task; // should complete without throwing

		Assert.Equal(0, count);
	}


	[Fact]
	public async Task RunAsync_ErrorPolicyStop_ThrowsAndStops()
	{
		int count = 0;

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			Loop.RunAsync(_ =>
			{
				count++;
				throw new InvalidOperationException("boom");
			}, Trigger.Every(TimeSpan.FromMilliseconds(10)), ErrorPolicy.Stop));

		Assert.Equal(1, count);
	}


	[Fact]
	public async Task RunAsync_ErrorPolicyContinue_KeepsLooping()
	{
		int count = 0;
		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

		await Loop.RunAsync(_ =>
		{
			count++;
			throw new Exception("ignored");
		}, Trigger.Every(TimeSpan.FromMilliseconds(30)), ErrorPolicy.Continue, cts.Token);

		Assert.True(count >= 3, $"Expected at least 3 executions, got {count}");
	}


	[Fact]
	public async Task RunAsync_CustomOnError_CalledWithException()
	{
		var errors = new List<Exception>();
		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));

		await Loop.RunAsync(_ =>
		{
			throw new InvalidOperationException("custom");
		}, Trigger.Every(TimeSpan.FromMilliseconds(30)),
			ErrorPolicy.Custom((ex, _) => { errors.Add(ex); return ValueTask.CompletedTask; }),
			cts.Token);

		Assert.NotEmpty(errors);
		Assert.All(errors, e => Assert.IsType<InvalidOperationException>(e));
	}

	[Fact]
	public async Task RunAsync_CustomOnError_CanStopByThrowing()
	{
		int count = 0;

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			Loop.RunAsync(_ =>
			{
				count++;
				throw new InvalidOperationException("stop me");
			}, Trigger.Every(TimeSpan.FromMilliseconds(10)),
				ErrorPolicy.Custom((ex, _) =>
				{
					ExceptionDispatchInfo.Capture(ex).Throw();
					return ValueTask.CompletedTask;
				})));

		Assert.Equal(1, count);
	}


	[Fact]
	public async Task RunAsync_WhenAny_ExecutesForEachEarliestTick()
	{
		int count = 0;
		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

		await Loop.RunAsync(
			_ => { count++; return Task.CompletedTask; },
			Trigger.WhenAny(
				Trigger.Every(TimeSpan.FromMilliseconds(50)),
				Trigger.Every(TimeSpan.FromMilliseconds(200))),
			cancellationToken: cts.Token);

		Assert.True(count >= 4, $"Expected at least 4 executions, got {count}");
	}
}
