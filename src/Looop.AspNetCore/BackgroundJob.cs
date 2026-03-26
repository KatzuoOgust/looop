using KatzuoOgust.Looop;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KatzuoOgust.Looop.AspNetCore;

/// <summary>
/// Hosted service that runs an <see cref="IJob"/> in a loop for the lifetime of the application,
/// passing each execution through the registered <see cref="IJobMiddleware"/> pipeline.
/// Reports its own health via <see cref="IHealthCheck"/>: <see cref="HealthStatus.Healthy"/> while
/// running, <see cref="HealthStatus.Degraded"/> after a graceful stop, and
/// <see cref="HealthStatus.Unhealthy"/> after a fault.
/// </summary>
public sealed partial class BackgroundJob<T>(
	T job,
	IEnumerable<IJobMiddleware> middlewares,
	ILogger<BackgroundJob<T>> logger) : BackgroundService, IHealthCheck
	where T : IJob
{
	private static readonly string JobName = typeof(T).Name;
	private readonly MiddlewareAwareJob _pipeline = new(job, middlewares);

	private volatile bool _stopped;
	private volatile bool _faulted;
	private Exception? _lastException;

	/// <inheritdoc/>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		Log.Starting(logger, JobName);
		try
		{
			await Loop.RunAsync(_pipeline, stoppingToken).ConfigureAwait(false);
			_stopped = true;
			Log.Stopped(logger, JobName);
		}
		catch (Exception ex)
		{
			_lastException = ex;
			_faulted = true;
			Log.Faulted(logger, JobName, ex);
			throw;
		}
	}

	/// <inheritdoc/>
	public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
	{
		if (_faulted)
			return Task.FromResult(HealthCheckResult.Unhealthy($"{JobName} faulted", _lastException));
		if (_stopped)
			return Task.FromResult(HealthCheckResult.Degraded($"{JobName} stopped"));
		return Task.FromResult(HealthCheckResult.Healthy($"{JobName} running"));
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

