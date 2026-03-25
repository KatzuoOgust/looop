namespace KatzuoOgust.Looop;

/// <summary>
/// Represents a step in the job execution pipeline.
/// </summary>
public delegate Task JobDelegate(CancellationToken cancellationToken);

/// <summary>Middleware that wraps job execution in a pipeline.</summary>
public interface IJobMiddleware
{
	/// <summary>
	/// Executes this middleware step. Call <paramref name="next"/> to pass control
	/// to the following step (or to the job itself).
	/// </summary>
	Task InvokeAsync(CancellationToken cancellationToken, JobDelegate next);
}
