using KatzuoOgust.Looop.Pipeline.Middlewares;
using Microsoft.Extensions.Logging;

namespace KatzuoOgust.Looop.AspNetCore.Middlewares;

/// <summary>
/// Job middleware that retries the next delegate on failure using exponential back-off.
/// </summary>
public sealed partial class RetryMiddleware(
	ILogger<RetryMiddleware> logger,
	RetryOptions? options = null) : IJobMiddleware
{
	private readonly RetryOptions _options = options ?? new();

	/// <inheritdoc/>
	public async Task InvokeAsync(CancellationToken cancellationToken, JobDelegate next)
	{
		for (var attempt = 0; ; attempt++)
		{
			try
			{
				await next(cancellationToken).ConfigureAwait(false);
				return;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex) when (attempt < _options.MaxRetries)
			{
				var delay = ComputeDelay(attempt);
				Log.Retrying(logger, attempt + 1, _options.MaxRetries, delay, ex);
				await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.RetriesExhausted(logger, _options.MaxRetries, ex);
				throw;
			}
		}
	}

	private TimeSpan ComputeDelay(int attempt)
	{
		var ms = _options.InitialDelay.TotalMilliseconds * Math.Pow(_options.BackoffMultiplier, attempt);
		return TimeSpan.FromMilliseconds(Math.Min(ms, _options.MaxDelay.TotalMilliseconds));
	}

	private static partial class Log
	{
		[LoggerMessage(Level = LogLevel.Warning,
			Message = "Retry attempt {Attempt}/{MaxRetries}, waiting {Delay} before next try")]
		public static partial void Retrying(
			ILogger logger, int attempt, int maxRetries, TimeSpan delay, Exception exception);

		[LoggerMessage(Level = LogLevel.Error,
			Message = "All {MaxRetries} retry attempt(s) exhausted")]
		public static partial void RetriesExhausted(
			ILogger logger, int maxRetries, Exception exception);
	}
}
