namespace KatzuoOgust.Looop;

/// <summary>
/// Runs an async action in a loop, driven by an <see cref="ITrigger"/>.
/// </summary>
public static class Loop
{
	/// <summary>
	/// Run <paramref name="job"/> using its own trigger and error policy.
	/// </summary>
	/// <param name="job">The job to run.</param>
	/// <param name="cancellationToken">Cancels the loop gracefully.</param>
	public static Task RunAsync(IJob job, CancellationToken cancellationToken = default) =>
		RunAsync(job.HandleAsync, job, job, cancellationToken);

	/// <summary>
	/// Run <paramref name="action"/> repeatedly according to <paramref name="trigger"/>.
	/// </summary>
	/// <param name="action">The async work to execute on each tick.</param>
	/// <param name="trigger">Controls when (and how many times) the action fires.</param>
	/// <param name="errorPolicy">Error-handling policy. Defaults to <see cref="ErrorPolicy.Stop"/>.</param>
	/// <param name="cancellationToken">Cancels the loop gracefully.</param>
	public static async Task RunAsync(
		Func<CancellationToken, Task> action,
		ITrigger trigger,
		IErrorPolicy? errorPolicy = null,
		CancellationToken cancellationToken = default)
	{
		errorPolicy ??= ErrorPolicy.Stop;
		DateTimeOffset? lastRunAt = null;

		while (!cancellationToken.IsCancellationRequested)
		{
			DateTimeOffset? fireAt;
			try
			{
				fireAt = await trigger.NextAsync(lastRunAt, cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
			{
				return;
			}

			if (fireAt is null)
				return;

			lastRunAt = fireAt.Value;

			var delay = fireAt.Value - DateTimeOffset.UtcNow;
			if (delay > TimeSpan.Zero)
			{
				try
				{
					await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
				}
				catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
				{
					return;
				}
			}

			if (cancellationToken.IsCancellationRequested)
				return;

			try
			{
				await action(cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
			{
				return;
			}
			catch (Exception ex)
			{
				await errorPolicy.HandleErrorAsync(ex, cancellationToken).ConfigureAwait(false);
			}
		}
	}
}
