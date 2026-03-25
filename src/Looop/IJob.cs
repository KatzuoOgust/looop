namespace KatzuoOgust.Looop;

/// <summary>
/// A self-contained unit of work: it knows when to run (<see cref="ITrigger"/>),
/// what to do when it runs, and how to handle errors (<see cref="IErrorPolicy"/>).
/// </summary>
public interface IJob : ITrigger, IErrorPolicy
{
	/// <summary>The work to perform on each tick.</summary>
	Task HandleAsync(CancellationToken cancellationToken);
}
