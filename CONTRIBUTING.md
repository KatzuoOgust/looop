# Contributing

## Getting started

```
git clone <repo>
cd looop
dotnet build
dotnet test
```

## Code style

- Tabs for indentation (enforced by `.editorconfig` and `dotnet format`)
- Run `dotnet format` before submitting a PR

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
