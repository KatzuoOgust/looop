using KatzuoOgust.Looop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KatzuoOgust.Looop.AspNetCore;

/// <summary>Extension methods for registering Looop background jobs and middleware.</summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers <typeparamref name="T"/> as a singleton and starts it as a hosted background job.
	/// </summary>
	public static IServiceCollection AddBackgroundJob<T>(this IServiceCollection services)
		where T : class, IJob
	{
		services.TryAddSingleton<T>();
		services.AddHostedService<BackgroundJob<T>>();
		return services;
	}

	/// <summary>
	/// Registers <typeparamref name="T"/> using <paramref name="factory"/> and starts it
	/// as a hosted background job.
	/// </summary>
	public static IServiceCollection AddBackgroundJob<T>(
		this IServiceCollection services,
		Func<IServiceProvider, T> factory)
		where T : class, IJob
	{
		services.TryAddSingleton<T>(factory);
		services.AddHostedService<BackgroundJob<T>>();
		return services;
	}

	/// <summary>
	/// Adds <typeparamref name="TMiddleware"/> to the job middleware pipeline.
	/// Middleware is executed in registration order, outermost first.
	/// </summary>
	public static IServiceCollection AddJobMiddleware<TMiddleware>(this IServiceCollection services)
		where TMiddleware : class, IJobMiddleware
	{
		services.AddSingleton<IJobMiddleware, TMiddleware>();
		return services;
	}

	/// <summary>
	/// Adds a middleware instance to the job middleware pipeline using <paramref name="factory"/>.
	/// </summary>
	public static IServiceCollection AddJobMiddleware<TMiddleware>(
		this IServiceCollection services,
		Func<IServiceProvider, TMiddleware> factory)
		where TMiddleware : class, IJobMiddleware
	{
		services.AddSingleton<IJobMiddleware>(factory);
		return services;
	}
}
