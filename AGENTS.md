# AGENTS.md — Looop Codebase Guide

## Architecture Overview

Two NuGet packages, both targeting **net10.0**:

| Project | Namespace | Purpose |
|---|---|---|
| `src/Looop/` | `KatzuoOgust.Looop` | Core loop engine, triggers, error policies |
| `src/Looop.AspNetCore/` | `KatzuoOgust.Looop.AspNetCore` | `BackgroundService` hosting, DI extensions, built-in middleware |

The central entry point is `Loop.RunAsync`. It calls `trigger.NextAsync(lastRunAt)` → delays until that time → calls the action → feeds errors to `IErrorPolicy`. Returning `null` from a trigger stops the loop.

## Key Abstractions

- **`ITrigger`** — `ValueTask<DateTimeOffset?> NextAsync(DateTimeOffset? lastRunAt, ct)`. `lastRunAt` is the previously scheduled fire time; stateless triggers use it as the reference point to compute the next occurrence. Returning `null` terminates the loop. Trigger implementations are **internal**; always use the `Trigger.*` factory.
- **`IErrorPolicy`** — `ValueTask HandleErrorAsync(ex, ct)`. Return normally = continue loop; throw = stop loop.
- **`IJob`** — inherits `ITrigger` and `IErrorPolicy`, adding `Task HandleAsync(ct)`. Implement all three members in one type.
- **`IJobMiddleware`** — `Task InvokeAsync(ct, JobDelegate next)` — same pattern as ASP.NET Core request middleware.

## Stateful vs Stateless Triggers

Most triggers are **stateless** — they compute the next fire time purely from the `lastRunAt` argument passed by the loop. `EveryTrigger` returns `(lastRunAt ?? UtcNow) + interval`; `CronTrigger` calls `GetNextOccurrence(lastRunAt ?? UtcNow)`.

**`WhenAnyTrigger` is the only stateful trigger**: it mutates its `_active` list to remove exhausted children. For this reason, all trigger instances must be created once and reused, not recreated on each `NextAsync` call:

```csharp
// CORRECT — trigger instance is preserved across calls
private readonly ITrigger _trigger = Trigger.Every(TimeSpan.FromSeconds(30));
public ValueTask<DateTimeOffset?> NextAsync(DateTimeOffset? lastRunAt = null, CancellationToken ct = default) =>
    _trigger.NextAsync(lastRunAt, ct);

// WRONG — creates a new trigger on every call (harmless for Every/Cron, but
//         breaks WhenAny which would lose its exhausted-children state)
public ValueTask<DateTimeOffset?> NextAsync(DateTimeOffset? lastRunAt = null, CancellationToken ct = default) =>
    Trigger.Every(TimeSpan.FromSeconds(30)).NextAsync(lastRunAt, ct);
```

## Trigger Implementations

All classes under `src/Looop/Triggers/` are `internal sealed`. The `Trigger` static factory is the only public API for creating triggers.

- `OnceTrigger` fires at `UtcNow` on the first call, then returns `null` to stop the loop. Backs `Trigger.Once()` and (via `ShiftTrigger`) `Trigger.After(delay)`.
- `ShiftTrigger` is the shared primitive behind both `Trigger.Before(inner, lead)` (negative offset) and `Trigger.After(inner, delay)` (positive offset). It clamps the result to `lastRunAt ?? UtcNow` so it never returns a time in the past.
- `CronTrigger` uses the **Cronos** NuGet package; cron macros (`@daily`, `@hourly`, etc.) are expanded to 5-field expressions in the constructor. It is **stateless** — each call computes from `lastRunAt ?? UtcNow`.
- `EveryTrigger` is **stateless** — returns `(lastRunAt ?? UtcNow) + interval` with no internal fields.
- `WhenAnyTrigger` mutates its `_active` list (removes exhausted children); `WhenAllTrigger` stops on the first `null` result.
- Triggers are **not thread-safe** — do not call `NextAsync` concurrently (see `tests/Looop.Tests/ConcurrencyTests.cs`).

## Middleware Pipeline

`MiddlewareAwareJob` (`src/Looop/Pipeline/Middlewares/MiddlewareAwareJob.cs`) wraps an `IJob` in the middleware chain. The pipeline is built by iterating middlewares in **reverse** so that the first registered becomes the outermost wrapper. `BackgroundJob<T>` (the hosted service) applies this automatically via DI.

Registration order matters:
```csharp
// LoggingMiddleware runs first (outermost), RetryMiddleware is inner
builder.Services.AddJobMiddleware<LoggingMiddleware>();
builder.Services.AddJobMiddleware<RetryMiddleware>();
```

Built-in middleware lives in `src/Looop.AspNetCore/Middlewares/`. All logging uses `[LoggerMessage]` source-generated methods.

## Developer Workflows

```sh
make build   # dotnet build -v q
make test    # dotnet test -v minimal
make format  # dotnet format
make         # list all targets
```

## Conventions

- All trigger types are `internal sealed`; public API surfaces via static factory classes (`Trigger`, `ErrorPolicy`).
- Nullable reference types and implicit usings are enabled everywhere.
- Tests use **xunit** (no FluentAssertions) and share the `KatzuoOgust.Looop` namespace with src to access internals (e.g., `using KatzuoOgust.Looop.Triggers;` for internal trigger types).
- Log events use `partial` classes with `[LoggerMessage]` — follow the same pattern when adding log calls. See CONTRIBUTING.md for the exact pattern.
- `OperationCanceledException` caused by the loop's own `CancellationToken` is always caught and treated as a graceful stop (never rethrown to the error policy). This is handled in `src/Looop/Loop.cs`.
- Branch naming: `feature/<short-description>` for features, `fix/<short-description>` for bug fixes.
- Do not manually edit `Looop.slnx` unless adding or removing a project.

## Key Files

| File | What it shows |
|---|---|
| `src/Looop/Loop.cs` | Core run loop — the scheduler/dispatcher |
| `src/Looop/Pipeline/Middlewares/MiddlewareAwareJob.cs` | Pipeline assembly via reverse-iteration |
| `src/Looop/Triggers/ShiftTrigger.cs` | Shared base for `Before`/`After` |
| `src/Looop.AspNetCore/BackgroundJob.cs` | Hosted service wiring + structured logging pattern |
| `examples/AspNetCoreExample/Program.cs` | End-to-end DI registration example |
| `tests/Looop.Tests/ConcurrencyTests.cs` | Documents known thread-safety limitations |

