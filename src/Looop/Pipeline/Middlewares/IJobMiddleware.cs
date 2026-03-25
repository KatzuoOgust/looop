namespace KatzuoOgust.Looop.Pipeline.Middlewares;

/// <summary>
/// Represents a step in the job execution pipeline.
/// Call <paramref name="next"/> to pass control to the next step or to the job itself.
/// </summary>
public delegate Task JobDelegate(CancellationToken cancellationToken);

/// <summary>Middleware that wraps job execution in a pipeline.</summary>
public interface IJobMiddleware
{
	Task InvokeAsync(CancellationToken cancellationToken, JobDelegate next);
}
