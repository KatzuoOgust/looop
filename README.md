# Looop

A tiny .NET library for running async actions in a loop with pluggable, composable triggers.

## Development

```
make          # show available targets
make build    # dotnet build
make test     # dotnet test
make format   # dotnet format
```

## Installation

```
dotnet add package KatzuoOgust.Looop
dotnet add package KatzuoOgust.Looop.AspNetCore   # for hosted background jobs
```

All public types live under the `KatzuoOgust.Looop` (and `KatzuoOgust.Looop.AspNetCore`) namespace.

## Quick start

```csharp
using KatzuoOgust.Looop;

// Run every 30 seconds until cancelled
await Loop.RunAsync(
    ct => DoWorkAsync(ct),
    Trigger.Every(TimeSpan.FromSeconds(30)),
    cancellationToken: cts.Token);
```

See [`examples/SimpleCli`](examples/SimpleCli/Program.cs) and [`examples/AspNetCoreExample`](examples/AspNetCoreExample/Program.cs) for runnable samples.

## Triggers

Every trigger implements `ITrigger`. Call `NextAsync()` to ask when the action should next fire; return `null` to stop the loop.

| Factory | Behaviour |
|---|---|
| `Trigger.Once()` | Fire immediately once, then stop |
| `Trigger.After(delay)` | Wait `delay`, fire once, then stop |
| `Trigger.Every(interval)` | Fire on a fixed interval |
| `Trigger.Before(inner, lead)` | Fire `lead` time before each tick of `inner` |
| `Trigger.After(inner, delay)` | Fire `delay` time after each tick of `inner` |
| `Trigger.Cron(expr)` | Full cron expression (`* * * * *`) or named macro |
| `Trigger.WhenAny(t1, t2, …)` | Fire at the earliest of several triggers |
| `Trigger.WhenAll(t1, t2, …)` | Fire when all triggers would have fired |
| `Trigger.Custom(next)` | Delegate-backed trigger |

`Before` and `After` are both built on a `ShiftTrigger` that applies a positive or negative `TimeSpan` offset to each inner tick. `Trigger.After(delay)` is shorthand for `Trigger.After(Trigger.Once(), delay)`.

### Cron macros

`@minutely`, `@hourly`, `@daily`, `@midnight`, `@weekly`, `@monthly`, `@yearly`, `@annually`

### Composition

```csharp
// Fire every minute OR 5 minutes before each daily job
var trigger = Trigger.WhenAny(
    Trigger.Every(TimeSpan.FromMinutes(1)),
    Trigger.Before(Trigger.Cron("@daily"), TimeSpan.FromMinutes(5)));
```

## Error policies

| Value | Behaviour |
|---|---|
| `ErrorPolicy.Stop` *(default)* | Re-throw; the loop exits |
| `ErrorPolicy.Continue` | Swallow; the loop keeps running |
| `ErrorPolicy.Custom(handler)` | Delegate decides — return to continue, throw to stop |

```csharp
await Loop.RunAsync(
    ct => DoWorkAsync(ct),
    Trigger.Every(TimeSpan.FromSeconds(10)),
    ErrorPolicy.Custom(async (ex, ct) =>
    {
        await alertService.NotifyAsync(ex, ct);
        // return normally → loop continues
    }),
    cts.Token);
```

## IJob — self-contained jobs

`IJob` combines `ITrigger`, `IErrorPolicy`, and the work itself into one type:

```csharp
public class HeartbeatJob : IJob
{
    // Trigger is a field — stateful triggers (e.g. Every, Cron) must be created
    // once and reused so they can track their internal state across calls.
    private readonly ITrigger _trigger = Trigger.Every(TimeSpan.FromSeconds(30));

    public ValueTask<DateTimeOffset?> NextAsync(CancellationToken ct = default) =>
        _trigger.NextAsync(ct);

    public async Task HandleAsync(CancellationToken ct) =>
        await httpClient.GetAsync("/health", ct);

    public ValueTask HandleErrorAsync(Exception ex, CancellationToken ct)
    {
        logger.LogWarning(ex, "Heartbeat failed");
        return ValueTask.CompletedTask; // continue
    }
}

// Run it
await Loop.RunAsync(new HeartbeatJob(), cancellationToken);
```

## Middleware pipeline (Looop.AspNetCore)

`IJobMiddleware` wraps each job execution in a composable pipeline — the same concept as ASP.NET Core request middleware.

```csharp
public interface IJobMiddleware
{
    Task InvokeAsync(CancellationToken cancellationToken, JobDelegate next);
}
```

`BackgroundJob<T>` resolves all registered `IJobMiddleware` instances from DI and chains them in registration order (first registered = outermost) around `job.HandleAsync`.

### Built-in middleware

#### LoggingMiddleware

Emits structured log events for job start, completion, cancellation, and failure.

#### RetryMiddleware

Retries on failure with configurable exponential back-off.

```csharp
new RetryOptions
{
    MaxRetries        = 5,
    InitialDelay      = TimeSpan.FromSeconds(2),
    MaxDelay          = TimeSpan.FromMinutes(2),
    BackoffMultiplier = 2.0,
}
```

### Custom middleware

```csharp
public class MetricsMiddleware(IMeterFactory meters) : IJobMiddleware
{
    public async Task InvokeAsync(CancellationToken ct, JobDelegate next)
    {
        using var _ = meter.StartTimer("job.duration");
        await next(ct);
    }
}
```

## ASP.NET Core integration

```csharp
// Program.cs
builder.Services.AddJobMiddleware<LoggingMiddleware>();
builder.Services.AddJobMiddleware<RetryMiddleware>();

builder.Services.AddBackgroundJob<HeartbeatJob>();

// With a factory
builder.Services.AddBackgroundJob<HeartbeatJob>(sp =>
    new HeartbeatJob(sp.GetRequiredService<IHttpClientFactory>()));

// RetryMiddleware with custom options
builder.Services.AddJobMiddleware<RetryMiddleware>(_ =>
    new RetryMiddleware(retryLogger, new RetryOptions { MaxRetries = 5 }));
```

`BackgroundJob<T>` is a `BackgroundService` that resolves all `IJobMiddleware` registrations, builds the pipeline around `job.HandleAsync`, and logs start, stop, and fault events.
