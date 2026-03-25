using KatzuoOgust.Looop;
using KatzuoOgust.Looop.Triggers;

namespace KatzuoOgust.Looop;

public class TriggerTests
{

	[Fact]
	public async Task Once_FiresOnce()
	{
		var t = Trigger.Once();
		var first = await t.NextAsync();
		var second = await t.NextAsync();

		Assert.NotNull(first);
		Assert.Null(second);
	}

	[Fact]
	public async Task Once_FiresImmediately()
	{
		var t = Trigger.Once();
		var before = DateTimeOffset.UtcNow;
		var next = await t.NextAsync();
		Assert.True(next!.Value >= before);
		Assert.True(next.Value <= DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(100));
	}


	[Fact]
	public async Task After_FiresOnceAfterDelay()
	{
		var delay = TimeSpan.FromSeconds(5);
		var t = Trigger.After(delay);
		var before = DateTimeOffset.UtcNow;

		var first = await t.NextAsync();
		var second = await t.NextAsync();

		Assert.NotNull(first);
		Assert.True(first!.Value >= before + delay - TimeSpan.FromMilliseconds(100));
		Assert.Null(second);
	}


	[Fact]
	public async Task Every_FiresRepeatedly()
	{
		var interval = TimeSpan.FromSeconds(10);
		var t = Trigger.Every(interval);

		var first = await t.NextAsync();
		var second = await t.NextAsync();
		var third = await t.NextAsync();

		Assert.NotNull(first);
		Assert.NotNull(second);
		Assert.NotNull(third);
		Assert.True(second!.Value - first!.Value >= interval - TimeSpan.FromMilliseconds(100));
		Assert.True(third!.Value - second.Value >= interval - TimeSpan.FromMilliseconds(100));
	}


	[Fact]
	public async Task Before_FiresBeforeInner()
	{
		var interval = TimeSpan.FromMinutes(10);
		var lead = TimeSpan.FromMinutes(1);
		var inner = Trigger.Every(interval);
		var t = Trigger.Before(inner, lead);

		var innerNext = await Trigger.Every(interval).NextAsync();
		var beforeNext = await t.NextAsync();

		// beforeNext should be approximately (innerNext - lead)
		Assert.NotNull(beforeNext);
		var diff = innerNext!.Value - beforeNext!.Value;
		Assert.True(diff >= lead - TimeSpan.FromMilliseconds(200));
		Assert.True(diff <= lead + TimeSpan.FromMilliseconds(200));
	}

	[Fact]
	public async Task Before_ReturnsNullWhenInnerExhausted()
	{
		var inner = Trigger.Once();
		var t = Trigger.Before(inner, TimeSpan.FromSeconds(5));

		var first = await t.NextAsync();
		var second = await t.NextAsync();

		Assert.NotNull(first);
		Assert.Null(second);
	}

	[Fact]
	public async Task Before_ClampsToNowWhenLeadExceedsFireTime()
	{
		// Inner fires in 1s but lead is 10s — should clamp to now
		var inner = Trigger.After(TimeSpan.FromSeconds(1));
		var t = Trigger.Before(inner, TimeSpan.FromSeconds(10));
		var before = DateTimeOffset.UtcNow;

		var next = await t.NextAsync();

		Assert.NotNull(next);
		Assert.True(next!.Value >= before);
		Assert.True(next.Value <= DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(100));
	}


	[Fact]
	public async Task After_InnerOverload_FiresAfterInner()
	{
		var interval = TimeSpan.FromMinutes(10);
		var delay = TimeSpan.FromMinutes(1);
		var inner = Trigger.Every(interval);
		var t = Trigger.After(inner, delay);

		var innerNext = await Trigger.Every(interval).NextAsync();
		var afterNext = await t.NextAsync();

		// afterNext should be approximately (innerNext + delay)
		Assert.NotNull(afterNext);
		var diff = afterNext!.Value - innerNext!.Value;
		Assert.True(diff >= delay - TimeSpan.FromMilliseconds(200));
		Assert.True(diff <= delay + TimeSpan.FromMilliseconds(200));
	}

	[Fact]
	public async Task After_InnerOverload_ReturnsNullWhenInnerExhausted()
	{
		var inner = Trigger.Once();
		var t = Trigger.After(inner, TimeSpan.FromSeconds(5));

		var first = await t.NextAsync();
		var second = await t.NextAsync();

		Assert.NotNull(first);
		Assert.Null(second);
	}

	[Theory]
	[InlineData("@hourly", "0 * * * *")]
	[InlineData("@daily", "0 0 * * *")]
	[InlineData("@midnight", "0 0 * * *")]
	[InlineData("@weekly", "0 0 * * 0")]
	[InlineData("@monthly", "0 0 1 * *")]
	[InlineData("@yearly", "0 0 1 1 *")]
	[InlineData("@annually", "0 0 1 1 *")]
	[InlineData("@minutely", "* * * * *")]
	public async Task Cron_NamedMacrosResolveCorrectly(string named, string equivalent)
	{
		var tNamed = Trigger.Cron(named);
		var tExpr = Trigger.Cron(equivalent);

		var nextNamed = await tNamed.NextAsync();
		var nextExpr = await tExpr.NextAsync();

		Assert.NotNull(nextNamed);
		Assert.NotNull(nextExpr);
		// Allow 1 second tolerance for test execution time
		Assert.Equal(nextExpr!.Value, nextNamed!.Value, TimeSpan.FromSeconds(1));
	}

	[Fact]
	public async Task Cron_FullExpressionReturnsNextOccurrence()
	{
		var t = Trigger.Cron("*/5 * * * *"); // every 5 minutes
		var now = DateTimeOffset.UtcNow;
		var next = await t.NextAsync();

		Assert.NotNull(next);
		Assert.True(next!.Value > now);
		Assert.True(next.Value <= now + TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));
	}


	[Fact]
	public async Task WhenAny_PicksEarliest()
	{
		var early = Trigger.After(TimeSpan.FromSeconds(1));
		var late = Trigger.After(TimeSpan.FromSeconds(10));
		var t = Trigger.WhenAny(early, late);

		var next = await t.NextAsync();

		Assert.NotNull(next);
		// Should be close to 1s from now, not 10s
		Assert.True(next!.Value <= DateTimeOffset.UtcNow + TimeSpan.FromSeconds(3));
	}

	[Fact]
	public async Task WhenAny_ContinuesAfterOneChildExhausted()
	{
		var once = Trigger.Once();
		var repeating = Trigger.Every(TimeSpan.FromSeconds(5));
		var t = Trigger.WhenAny(once, repeating);

		var first = await t.NextAsync();
		var second = await t.NextAsync(); // once is done; repeating still fires
		var third = await t.NextAsync();

		Assert.NotNull(first);
		Assert.NotNull(second);
		Assert.NotNull(third);
	}

	[Fact]
	public async Task WhenAny_StopsWhenAllChildrenExhausted()
	{
		// WhenAny polls all active triggers simultaneously to pick the earliest.
		// Two Once() triggers are both consumed on the first call, so the second
		// call finds _active empty and returns null.
		var t = Trigger.WhenAny(Trigger.Once(), Trigger.Once());

		var first = await t.NextAsync(); // both Once() polled; pick earliest; both consumed
		var second = await t.NextAsync(); // _active is now empty

		Assert.NotNull(first);
		Assert.Null(second);
	}


	[Fact]
	public async Task WhenAll_PicksLatest()
	{
		var early = Trigger.After(TimeSpan.FromSeconds(1));
		var late = Trigger.After(TimeSpan.FromSeconds(10));
		var t = Trigger.WhenAll(early, late);

		var next = await t.NextAsync();

		Assert.NotNull(next);
		Assert.True(next!.Value >= DateTimeOffset.UtcNow + TimeSpan.FromSeconds(9));
	}

	[Fact]
	public async Task WhenAll_StopsWhenAnyChildExhausted()
	{
		var once = Trigger.Once();
		var repeating = Trigger.Every(TimeSpan.FromSeconds(5));
		var t = Trigger.WhenAll(once, repeating);

		var first = await t.NextAsync(); // both fire
		var second = await t.NextAsync(); // once is exhausted → stop

		Assert.NotNull(first);
		Assert.Null(second);
	}
}
