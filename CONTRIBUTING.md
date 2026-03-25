# Contributing

## Getting started

```sh
git clone https://github.com/KatzuoOgust/looop.git
cd looop
make build
make test
```

## Workflow

1. Fork the repository and create a branch from `main`.
2. Branch naming: `feature/<short-description>` for new features, `fix/<short-description>` for bug fixes.
3. Make your changes and ensure all tests pass (`make test`) and code is formatted (`make format`).
4. Open a pull request against `main`. Include a short summary of what changed and why.

All PRs must pass `make build` and `make test` before merge.

## Scope

In scope: bug fixes, new trigger types, new error policies, new middleware, documentation improvements.
Out of scope: changes to `Looop.slnx` unless you're adding/removing a project, auto-generated `obj/` and `bin/` files.

If you're unsure whether a change is welcome, open an issue first.

## Code style

All style rules are enforced by `.editorconfig` — run `dotnet format` before submitting a PR and it will fix everything automatically.

## Logging — the `Log` class pattern

All logging in this codebase is done through a **nested `private static partial class Log`** inside the class that owns the log events. Methods are declared with `[LoggerMessage]` for compile-time source generation, which avoids allocations on hot paths and keeps log message strings co-located with the code that emits them.

```csharp
public sealed partial class MyDecorator<T>(ILogger<MyDecorator<T>> logger) : IJob
    where T : IJob
{
    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        Log.Starting(logger);
        // ...
        Log.Completed(logger, elapsed);
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Debug, Message = "Starting")]
        public static partial void Starting(ILogger logger);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Completed in {Elapsed}")]
        public static partial void Completed(ILogger logger, TimeSpan elapsed);
    }
}
```

Rules:
- The class must be `private static partial` and named `Log`
- One method per event, named after what happened
- The `ILogger` is always the first parameter
- Exceptions go last (the source generator attaches them to the log scope automatically)
- No string interpolation or `string.Format` in log messages — use named placeholders
