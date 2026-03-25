using KatzuoOgust.Looop.AspNetCore.Middlewares;
using KatzuoOgust.Looop.Pipeline.Middlewares;
using Microsoft.Extensions.Logging.Abstractions;

namespace KatzuoOgust.Looop;

public class RetryMiddlewareTests
{
	[Fact]
	public async Task Invoke_CallsNextOnce_WhenSucceedsOnFirstAttempt()
	{
		int calls = 0;
		var middleware = new RetryMiddleware(NullLogger<RetryMiddleware>.Instance);

		await middleware.InvokeAsync(CancellationToken.None, ct =>
		{
			calls++;
			return Task.CompletedTask;
		});

		Assert.Equal(1, calls);
	}

	[Fact]
	public async Task Invoke_RetriesUntilSuccess_WhenActionFailsTransiently()
	{
		int calls = 0;
		var middleware = new RetryMiddleware(NullLogger<RetryMiddleware>.Instance,
			new RetryOptions { MaxRetries = 3, InitialDelay = TimeSpan.FromMilliseconds(1) });

		await middleware.InvokeAsync(CancellationToken.None, ct =>
		{
			calls++;
			if (calls < 3) throw new InvalidOperationException("transient");
			return Task.CompletedTask;
		});

		Assert.Equal(3, calls);
	}

	[Fact]
	public async Task Invoke_ThrowsAfterMaxRetries_WhenActionAlwaysFails()
	{
		int calls = 0;
		var middleware = new RetryMiddleware(NullLogger<RetryMiddleware>.Instance,
			new RetryOptions { MaxRetries = 2, InitialDelay = TimeSpan.FromMilliseconds(1) });

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			middleware.InvokeAsync(CancellationToken.None, ct =>
			{
				calls++;
				throw new InvalidOperationException("permanent");
			}));

		Assert.Equal(3, calls); // initial + 2 retries
	}

	[Fact]
	public async Task Invoke_UsesExponentialBackoff()
	{
		var middleware = new RetryMiddleware(NullLogger<RetryMiddleware>.Instance,
			new RetryOptions
			{
				MaxRetries = 3,
				InitialDelay = TimeSpan.FromMilliseconds(50),
				BackoffMultiplier = 2.0,
				MaxDelay = TimeSpan.FromSeconds(10),
			});

		int calls = 0;
		var sw = System.Diagnostics.Stopwatch.StartNew();
		var lastTick = sw.Elapsed;
		var delays = new List<TimeSpan>();

		await Assert.ThrowsAsync<Exception>(() =>
			middleware.InvokeAsync(CancellationToken.None, ct =>
			{
				delays.Add(sw.Elapsed - lastTick);
				lastTick = sw.Elapsed;
				calls++;
				throw new Exception("fail");
			}));

		// attempt 0 runs immediately; delay before attempt 1 ≈ 50ms, before attempt 2 ≈ 100ms
		Assert.True(delays[1] >= TimeSpan.FromMilliseconds(30), $"First retry delay too short: {delays[1]}");
		Assert.True(delays[2] >= TimeSpan.FromMilliseconds(70), $"Second retry delay too short: {delays[2]}");
	}

	[Fact]
	public async Task Invoke_StopsImmediately_WhenCancelledDuringRetryDelay()
	{
		using var cts = new CancellationTokenSource();
		var middleware = new RetryMiddleware(NullLogger<RetryMiddleware>.Instance,
			new RetryOptions { MaxRetries = 5, InitialDelay = TimeSpan.FromSeconds(60) });

		int calls = 0;
		var task = middleware.InvokeAsync(cts.Token, ct =>
		{
			calls++;
			throw new InvalidOperationException("fail");
		});

		await Task.Delay(20);
		await cts.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
		Assert.Equal(1, calls);
	}

	[Fact]
	public async Task Invoke_CapsRetryDelay_WhenMaxDelayIsConfigured()
	{
		var middleware = new RetryMiddleware(NullLogger<RetryMiddleware>.Instance,
			new RetryOptions
			{
				MaxRetries = 1,
				InitialDelay = TimeSpan.FromSeconds(60),
				MaxDelay = TimeSpan.FromMilliseconds(10),
			});

		var sw = System.Diagnostics.Stopwatch.StartNew();
		await Assert.ThrowsAsync<Exception>(() =>
			middleware.InvokeAsync(CancellationToken.None, _ => throw new Exception("fail")));
		sw.Stop();

		Assert.True(sw.ElapsedMilliseconds < 500, $"Delay was not capped: {sw.ElapsedMilliseconds}ms");
	}
}
