using KatzuoOgust.Looop.AspNetCore.Middlewares;
using KatzuoOgust.Looop.Pipeline.Middlewares;
using Microsoft.Extensions.Logging.Abstractions;

namespace KatzuoOgust.Looop;

public class LoggingMiddlewareTests
{
	[Fact]
	public async Task Invoke_CallsNext()
	{
		bool called = false;
		var middleware = new LoggingMiddleware(NullLogger<LoggingMiddleware>.Instance);

		await middleware.InvokeAsync(CancellationToken.None, _ =>
		{
			called = true;
			return Task.CompletedTask;
		});

		Assert.True(called);
	}

	[Fact]
	public async Task Invoke_RethrowsException()
	{
		var middleware = new LoggingMiddleware(NullLogger<LoggingMiddleware>.Instance);

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			middleware.InvokeAsync(CancellationToken.None,
				_ => throw new InvalidOperationException("boom")));
	}

	[Fact]
	public async Task Invoke_RethrowsCancellation()
	{
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		var middleware = new LoggingMiddleware(NullLogger<LoggingMiddleware>.Instance);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			middleware.InvokeAsync(cts.Token,
				ct => Task.FromCanceled(ct)));
	}
}
