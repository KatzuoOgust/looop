using KatzuoOgust.Looop;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KatzuoOgust.Looop.AspNetCore;

/// <summary>
/// Hosted service that runs an <see cref="IJob"/> in a loop for the lifetime of the application,
/// passing each execution through the registered <see cref="IJobMiddleware"/> pipeline.
/// </summary>
public sealed partial class BackgroundJob<T>(
	T job,
	IEnumerable<IJobMiddleware> middlewares,
	ILogger<BackgroundJob<T>> logger) : BackgroundService
	where T : IJob
{
	private static readonly string JobName = typeof(T).Name;
	private readonly MiddlewareAwareJob _pipeline = new(job, middlewares);

	/// <inheritdoc/>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		Log.Starting(logger, JobName);
		try
		{
			await Loop.RunAsync(_pipeline, stoppingToken).ConfigureAwait(false);
			Log.Stopped(logger, JobName);
		}
		catch (Exception ex)
		{
			Log.Faulted(logger, JobName, ex);
			throw;
		}
	}

	private static partial class Log
	{
		[LoggerMessage(Level = LogLevel.Information, Message = "Background job {JobName} starting")]
		public static partial void Starting(ILogger logger, string jobName);

		[LoggerMessage(Level = LogLevel.Information, Message = "Background job {JobName} stopped")]
		public static partial void Stopped(ILogger logger, string jobName);

		[LoggerMessage(Level = LogLevel.Critical, Message = "Background job {JobName} faulted")]
		public static partial void Faulted(ILogger logger, string jobName, Exception exception);
	}
}

