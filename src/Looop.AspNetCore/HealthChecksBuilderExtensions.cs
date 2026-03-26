using KatzuoOgust.Looop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KatzuoOgust.Looop.AspNetCore;

/// <summary>Extension methods for registering background job health checks.</summary>
public static class HealthChecksBuilderExtensions
{
	/// <summary>
	/// Registers a health check for the <see cref="BackgroundJob{T}"/> running <typeparamref name="T"/>,
	/// using <c>typeof(T).Name</c> as the check name and <see cref="HealthStatus.Degraded"/> as the
	/// failure status.
	/// </summary>
	public static IHealthChecksBuilder AddBackgroundJobCheck<T>(this IHealthChecksBuilder builder)
		where T : class, IJob =>
		builder.AddCheck<BackgroundJob<T>>(typeof(T).Name, HealthStatus.Degraded);

	/// <summary>
	/// Registers a health check for the <see cref="BackgroundJob{T}"/> running <typeparamref name="T"/>
	/// with a custom <paramref name="name"/>, using <see cref="HealthStatus.Degraded"/> as the
	/// failure status.
	/// </summary>
	public static IHealthChecksBuilder AddBackgroundJobCheck<T>(
		this IHealthChecksBuilder builder,
		string name)
		where T : class, IJob =>
		builder.AddCheck<BackgroundJob<T>>(name, HealthStatus.Degraded);
}
