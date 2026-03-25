using KatzuoOgust.Looop.Pipeline.Middlewares;
using Microsoft.Extensions.Logging;

namespace KatzuoOgust.Looop.AspNetCore.Middlewares;

/// <summary>
/// Job middleware that emits structured log events around each execution.
/// </summary>
public sealed partial class LoggingMiddleware(ILogger<LoggingMiddleware> logger) : IJobMiddleware
{
	/// <inheritdoc/>
	public async Task InvokeAsync(CancellationToken cancellationToken, JobDelegate next)
	{
		Log.Starting(logger);
		var sw = System.Diagnostics.Stopwatch.StartNew();
		try
		{
			await next(cancellationToken).ConfigureAwait(false);
			Log.Completed(logger, sw.Elapsed);
		}
		catch (OperationCanceledException)
		{
			Log.Cancelled(logger, sw.Elapsed);
			throw;
		}
		catch (Exception ex)
		{
			Log.Failed(logger, sw.Elapsed, ex);
			throw;
		}
	}

	private static partial class Log
	{
		[LoggerMessage(Level = LogLevel.Debug, Message = "Job starting")]
		public static partial void Starting(ILogger logger);

		[LoggerMessage(Level = LogLevel.Debug, Message = "Job completed in {Elapsed}")]
		public static partial void Completed(ILogger logger, TimeSpan elapsed);

		[LoggerMessage(Level = LogLevel.Warning, Message = "Job cancelled after {Elapsed}")]
		public static partial void Cancelled(ILogger logger, TimeSpan elapsed);

		[LoggerMessage(Level = LogLevel.Error, Message = "Job failed after {Elapsed}")]
		public static partial void Failed(ILogger logger, TimeSpan elapsed, Exception exception);
	}
}
