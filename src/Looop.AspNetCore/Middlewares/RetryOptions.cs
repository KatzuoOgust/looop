namespace KatzuoOgust.Looop.AspNetCore.Middlewares;

/// <summary>Controls exponential back-off behaviour in <see cref="RetryMiddleware"/>.</summary>
public sealed class RetryOptions
{
	/// <summary>Maximum number of retry attempts after the first failure. Default: 3.</summary>
	public int MaxRetries { get; init; } = 3;

	/// <summary>Delay before the first retry. Default: 1 second.</summary>
	public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(1);

	/// <summary>Upper bound for computed back-off delay. Default: 1 minute.</summary>
	public TimeSpan MaxDelay { get; init; } = TimeSpan.FromMinutes(1);

	/// <summary>Multiplier applied to the delay on each successive attempt. Default: 2.</summary>
	public double BackoffMultiplier { get; init; } = 2.0;
}
