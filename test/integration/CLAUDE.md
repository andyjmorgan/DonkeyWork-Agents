# Integration Tests

## Test Infrastructure

Tests use **Testcontainers** (Docker) to spin up real PostgreSQL and NATS instances. The `InfrastructureFixture` (xUnit collection fixture) starts containers once, shared across all tests in the collection.

- PostgreSQL: `pgvector/pgvector:pg17` for database
- NATS: `nats:2.10-alpine --jetstream` for messaging

## Configuration Injection

The `WebApplicationFactory` in `Program.cs` reads `builder.Configuration` during host builder setup. This happens **before** `ConfigureAppConfiguration` and `ConfigureTestServices` take effect. Therefore:

- **Services registered via `IOptions<T>`** (e.g., `NatsOptions`, `PersistenceOptions`) can be overridden in `ConfigureTestServices` because options are resolved at runtime, not registration time.
- **Values read directly from `builder.Configuration` during host builder setup** (e.g., Wolverine's `UseNats(url)`, Orleans' `AddNatsStream`) are captured eagerly and cannot be overridden by `ConfigureAppConfiguration` or `ConfigureTestServices`.

For eagerly-read configuration, use **environment variables** set in `InfrastructureFixture.InitializeAsync()`. Environment variables are part of the default configuration sources and are visible to `builder.Configuration` at construction time, provided they are set before the `WebApplicationFactory` builds the host.

```csharp
// InfrastructureFixture.cs
public async Task InitializeAsync()
{
    await Task.WhenAll(Postgres.InitializeAsync(), Nats.InitializeAsync());
    Environment.SetEnvironmentVariable("Nats__Url", Nats.Url);
}
```

## Common Pitfalls

- **Never assume `ConfigureAppConfiguration` runs before host builder lambdas.** It doesn't. Use environment variables for values consumed during host builder setup.
- **Always test integration changes locally** (`dotnet test test/integration/...`) before pushing to CI. Testcontainer startup + host build takes ~30s locally vs 5min CI round trips.
- **Wolverine `AutoProvision` requires `DefineWorkQueueStream`** to actually create JetStream streams. Just calling `AutoProvision()` without defining streams does nothing.
- **Wolverine `UseJetStream` requires `Action<JetStreamDefaults>` parameter** — use `_ => { }` if no custom config needed.
