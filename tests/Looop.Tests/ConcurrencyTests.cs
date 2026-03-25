using System.Collections.Concurrent;
using KatzuoOgust.Looop;
using KatzuoOgust.Looop.Triggers;

namespace KatzuoOgust.Looop;

public class ConcurrencyTests
{
	[Fact]
	public async Task EveryTrigger_MayProduceDuplicates_WhenCalledConcurrently()
	{
		// This test demonstrates that EveryTrigger is not thread-safe
		var trigger = Trigger.Every(TimeSpan.FromSeconds(1));
		var results = new ConcurrentBag<DateTimeOffset>();

		var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
		{
			var result = await trigger.NextAsync();
			if (result.HasValue)
				results.Add(result.Value);
		}));

		await Task.WhenAll(tasks);

		// If not thread-safe, we may get duplicates
		var uniqueCount = results.Distinct().Count();
		var totalCount = results.Count;

		// This assertion may fail if there's a race condition
		Assert.Equal(totalCount, uniqueCount);
	}

	[Fact]
	public async Task OnceTrigger_MayFireMultipleTimes_WhenCalledConcurrently()
	{
		// This test demonstrates that OnceTrigger is not thread-safe
		var trigger = Trigger.Once();
		var fireCount = 0;

		var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
		{
			var result = await trigger.NextAsync();
			if (result.HasValue)
				Interlocked.Increment(ref fireCount);
		}));

		await Task.WhenAll(tasks);

		// Should only fire once, but may fire multiple times due to race condition
		Assert.Equal(1, fireCount);
	}
}
