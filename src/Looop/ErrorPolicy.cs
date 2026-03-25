using System.Runtime.ExceptionServices;

namespace KatzuoOgust.Looop;

/// <summary>Built-in <see cref="IErrorPolicy"/> implementations.</summary>
public static class ErrorPolicy
{
	/// <summary>Re-throws the exception, stopping the loop.</summary>
	public static IErrorPolicy Stop { get; } = new StopPolicy();

	/// <summary>Swallows the exception and keeps looping.</summary>
	public static IErrorPolicy Continue { get; } = new ContinuePolicy();

	/// <summary>
	/// Delegates error handling to <paramref name="handler"/>.
	/// Return normally to continue the loop; throw to stop it.
	/// </summary>
	public static IErrorPolicy Custom(Func<Exception, CancellationToken, ValueTask> handler) =>
		new CustomPolicy(handler);


	private sealed class StopPolicy : IErrorPolicy
	{
		/// <inheritdoc/>
		public ValueTask HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
		{
			ExceptionDispatchInfo.Capture(exception).Throw();
			return ValueTask.CompletedTask; // unreachable
		}
	}

	private sealed class ContinuePolicy : IErrorPolicy
	{
		/// <inheritdoc/>
		public ValueTask HandleErrorAsync(Exception exception, CancellationToken cancellationToken) =>
			ValueTask.CompletedTask;
	}

	private sealed class CustomPolicy(Func<Exception, CancellationToken, ValueTask> handler) : IErrorPolicy
	{
		/// <inheritdoc/>
		public ValueTask HandleErrorAsync(Exception exception, CancellationToken cancellationToken) =>
			handler(exception, cancellationToken);
	}
}
