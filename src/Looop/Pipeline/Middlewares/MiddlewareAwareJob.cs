namespace KatzuoOgust.Looop.Pipeline.Middlewares;

/// <summary>
/// An <see cref="IJob"/> decorator that runs <see cref="IJob.HandleAsync"/> through
/// a chain of <see cref="IJobMiddleware"/> instances. Trigger and error policy are
/// delegated to the inner job unchanged.
/// </summary>
public sealed class MiddlewareAwareJob : IJob
{
	private readonly IJob _inner;
	private readonly JobDelegate _pipeline;

	public MiddlewareAwareJob(IJob inner, IEnumerable<IJobMiddleware> middlewares)
	{
		_inner = inner;
		_pipeline = BuildPipeline(inner.HandleAsync, middlewares);
	}

	/// <inheritdoc/>
	public ValueTask<DateTimeOffset?> NextAsync(CancellationToken cancellationToken = default) =>
		_inner.NextAsync(cancellationToken);

	/// <inheritdoc/>
	public ValueTask HandleErrorAsync(Exception exception, CancellationToken cancellationToken) =>
		_inner.HandleErrorAsync(exception, cancellationToken);

	/// <inheritdoc/>
	public Task HandleAsync(CancellationToken cancellationToken) =>
		_pipeline(cancellationToken);

	private static JobDelegate BuildPipeline(
		Func<CancellationToken, Task> handler,
		IEnumerable<IJobMiddleware> middlewares)
	{
		JobDelegate pipeline = ct => handler(ct);
		foreach (var middleware in middlewares.Reverse())
		{
			var next = pipeline;
			var m = middleware;
			pipeline = ct => m.InvokeAsync(ct, next);
		}
		return pipeline;
	}
}
