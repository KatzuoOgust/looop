using KatzuoOgust.Looop.Pipeline.Middlewares;

namespace KatzuoOgust.Looop;

public class MiddlewarePipelineTests
{
	private sealed class StubJob : IJob
	{
		private readonly ITrigger _trigger = Trigger.Once();
		public ValueTask<DateTimeOffset?> NextAsync(CancellationToken ct = default) => _trigger.NextAsync(ct);
		public ValueTask HandleErrorAsync(Exception ex, CancellationToken ct) => ValueTask.CompletedTask;
		public Task HandleAsync(CancellationToken ct) => Task.CompletedTask;
	}

	private sealed class OrderMiddleware(string name, List<string> log) : IJobMiddleware
	{
		public async Task InvokeAsync(CancellationToken ct, JobDelegate next)
		{
			log.Add(name);
			await next(ct);
		}
	}

	[Fact]
	public async Task Pipeline_MiddlewaresExecuteInRegistrationOrder()
	{
		var order = new List<string>();
		var middlewares = new IJobMiddleware[]
		{
			new OrderMiddleware("A", order),
			new OrderMiddleware("B", order),
			new OrderMiddleware("C", order),
		};

		JobDelegate pipeline = ct => { order.Add("job"); return Task.CompletedTask; };
		foreach (var m in middlewares.Reverse())
		{
			var next = pipeline;
			var current = m;
			pipeline = ct => current.InvokeAsync(ct, next);
		}

		await pipeline(CancellationToken.None);

		Assert.Equal(["A", "B", "C", "job"], order);
	}

	[Fact]
	public async Task MiddlewareAwareJob_ExecutesPipelineThenJob()
	{
		var order = new List<string>();
		var inner = new StubJob();
		var middlewares = new IJobMiddleware[]
		{
			new OrderMiddleware("A", order),
			new OrderMiddleware("B", order),
		};

		var wrapped = new MiddlewareAwareJob(inner, middlewares);

		await wrapped.HandleAsync(CancellationToken.None);

		Assert.Equal(["A", "B"], order);
	}

	[Fact]
	public async Task MiddlewareAwareJob_DelegatesNextAsyncToInner()
	{
		var inner = new StubJob();
		var wrapped = new MiddlewareAwareJob(inner, []);

		var first = await wrapped.NextAsync();
		var second = await wrapped.NextAsync();

		Assert.NotNull(first);
		Assert.Null(second);
	}
}
