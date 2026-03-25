using KatzuoOgust.Looop;
using KatzuoOgust.Looop.AspNetCore;
using KatzuoOgust.Looop.AspNetCore.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Register middleware pipeline — first registered = outermost
builder.Services.AddJobMiddleware<LoggingMiddleware>();
builder.Services.AddJobMiddleware<RetryMiddleware>(_ =>
	new RetryMiddleware(
		builder.Services
			.BuildServiceProvider()
			.GetRequiredService<ILogger<RetryMiddleware>>(),
		new RetryOptions
		{
			MaxRetries = 3,
			InitialDelay = TimeSpan.FromSeconds(1),
			BackoffMultiplier = 2.0,
			MaxDelay = TimeSpan.FromMinutes(1),
		}));

// Register and start the background job
builder.Services.AddBackgroundJob<HealthCheckJob>();

var app = builder.Build();

app.MapGet("/", () => "Looop AspNetCore example is running.");

app.Run();

// ── Job definition ────────────────────────────────────────────────────────────

sealed class HealthCheckJob(IHttpClientFactory http, ILogger<HealthCheckJob> logger) : IJob
{
	private readonly ITrigger _trigger = Trigger.Every(TimeSpan.FromSeconds(30));

	public ValueTask<DateTimeOffset?> NextAsync(CancellationToken ct = default) =>
		_trigger.NextAsync(ct);

	public async Task HandleAsync(CancellationToken ct)
	{
		using var client = http.CreateClient();
		var response = await client.GetAsync("https://example.com", ct);
		logger.LogInformation("Health check: {StatusCode}", response.StatusCode);
	}

	public ValueTask HandleErrorAsync(Exception ex, CancellationToken ct)
	{
		logger.LogWarning(ex, "Health check failed — middleware pipeline will decide whether to retry");
		return ValueTask.CompletedTask;
	}
}
