namespace KatzuoOgust.Looop;

/// <summary>Decides what happens when the action throws.</summary>
public interface IErrorPolicy
{
	/// <summary>
	/// Called with the thrown exception. Return normally to continue the loop;
	/// throw (or rethrow) to stop it.
	/// </summary>
	ValueTask HandleErrorAsync(Exception exception, CancellationToken cancellationToken);
}
