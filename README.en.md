# Shashlik.EventBus

[![build and test](https://github.com/dotnet-shashlik/shashlik.eventbus/workflows/build%20and%20test/badge.svg)](https://github.com/dotnet-shashlik/shashlik.eventbus)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/dotnet-shashlik/shashlik.eventbus/blob/main/LICENSE)

An open-source .NET 8+ event bus solution using the async-ensure pattern (local message table), providing eventual consistency for distributed transactions and delayed event support.

## Highlights

- **Persist first, send later** — Messages are committed with business data in the same transaction; process crashes / network failures are handled by retry providers — zero message loss
- **Three orthogonal dimensions** — Message transport, message storage, and transaction integration can be independently selected and combined
- **Multiple broker support** — RabbitMQ / Kafka / Pulsar / Redis Stream / In-memory queue
- **Multiple storage support** — Relational databases (MySQL / PostgreSQL / SqlServer / Sqlite / Oracle, via FreeSql) / MongoDB / In-memory storage
- **Multiple ORM transaction integration** — EF Core / SqlSugar / FreeSql / XA (TransactionScope) out of the box
- **Delayed events** — Local Timer-based scheduling, independent of broker delay features for maximum reliability
- **High performance** — Handler delegates compiled at startup (zero runtime reflection); pooled producers / consumers
- **Easily extensible** — 18 replaceable interfaces covering the entire pipeline from ID generation to message send/receive

## Design Principle

![eventbus](https://raw.githubusercontent.com/dotnet-shashlik/shashlik.eventbus/main/pictures/eventbus.en.png)

Message data and business data are committed or rolled back within the same transaction. EventBus only sends the message after confirming the transaction has been committed. Minimum transaction isolation level: **Read Committed (RC)**.

## Architecture

The system is divided into three orthogonal dimensions that can be independently combined:

| Dimension | Options |
|-----------|---------|
| **Message Transport** | RabbitMQ / Kafka / Pulsar / Redis Stream / MemoryQueue |
| **Message Storage** | RelationDbStorage (MySQL/PG/SqlServer/Sqlite/Oracle) / MongoDb / MemoryStorage |
| **Transaction Integration** | EF Core / SqlSugar / FreeSql / XA (TransactionScope) |

> For detailed documentation, see the [Wiki](https://github.com/dotnet-shashlik/shashlik.eventbus/wiki)

## NuGet Packages

| Package | Description |
|---|---|
| [Shashlik.EventBus.Abstract](https://www.nuget.org/packages/Shashlik.EventBus.Abstract) | Interface abstractions |
| [Shashlik.EventBus](https://www.nuget.org/packages/Shashlik.EventBus) | Core package: message send/receive, storage abstractions, and default implementations |
| [Shashlik.EventBus.RelationDbStorage](https://www.nuget.org/packages/Shashlik.EventBus.RelationDbStorage) | Relational database storage (via FreeSql, requires corresponding Provider) |
| [Shashlik.EventBus.MongoDb](https://www.nuget.org/packages/Shashlik.EventBus.MongoDb) | MongoDB message storage |
| [Shashlik.EventBus.RabbitMQ](https://www.nuget.org/packages/Shashlik.EventBus.RabbitMQ) | RabbitMQ message transport |
| [Shashlik.EventBus.Kafka](https://www.nuget.org/packages/Shashlik.EventBus.Kafka) | Kafka message transport |
| [Shashlik.EventBus.Pulsar](https://www.nuget.org/packages/Shashlik.EventBus.Pulsar) | Pulsar message transport |
| [Shashlik.EventBus.Redis](https://www.nuget.org/packages/Shashlik.EventBus.Redis) | Redis Stream message transport |
| [Shashlik.EventBus.Extensions.EfCore](https://www.nuget.org/packages/Shashlik.EventBus.Extensions.EfCore) | EF Core extension |
| [Shashlik.EventBus.Extensions.SqlSugar](https://www.nuget.org/packages/Shashlik.EventBus.Extensions.SqlSugar) | SqlSugar extension |
| [Shashlik.EventBus.Dashboard](https://www.nuget.org/packages/Shashlik.EventBus.Dashboard) | Web management dashboard |
| [Shashlik.EventBus.MemoryQueue](https://www.nuget.org/packages/Shashlik.EventBus.MemoryQueue) | In-memory message queue (testing only) |
| [Shashlik.EventBus.MemoryStorage](https://www.nuget.org/packages/Shashlik.EventBus.MemoryStorage) | In-memory message storage (testing only) |

## Quick Start

```csharp
// 1. Service configuration
services.AddEventBus()
    .AddRelationDb(options => options.UseConnection(DataType.MySql, "Server=...;Database=...;"))
    .AddRabbitMQ(r => { r.Host = "localhost"; r.UserName = "guest"; r.Password = "guest"; });

// 2. Define event
public class NewUserEvent : IEvent { public string Id { get; set; } public string Name { get; set; } }

// 3. Publish event (EF Core extension, auto shared transaction)
await DbContext.PublishEventAsync(new NewUserEvent { Id = "1", Name = "Alice" });

// 4. Define handler
public class NewUserSmsHandler : IEventHandler<NewUserEvent>
{
    public async Task Execute(NewUserEvent @event, IDictionary<string, string> items) { /* send SMS */ }
}
```

> For complete usage documentation, see the [Wiki](https://github.com/dotnet-shashlik/shashlik.eventbus/wiki)

## Message Idempotency

EventBus and message brokers use `at least once` QOS. A handler may receive the same event multiple times — idempotency must be handled by business logic. For example, check order status in the handler and return early if already processed.

## Delayed Events

Delayed events use local `System.Timers.Timer` scheduling, independent of broker delay features for maximum reliability. Definition and handler declaration are identical to regular events — only the publish call specifies a delay time:

```csharp
await DbContext.PublishEventAsync(new NewUserPromotionEvent { ... }, DateTimeOffset.Now.AddMinutes(30));
```

If a delayed event handler fails and the retry provider takes over, the actual execution time may differ from the intended delay. Whether this is acceptable depends on your business requirements.

## Dashboard

![dashboard](https://raw.githubusercontent.com/dotnet-shashlik/shashlik.eventbus/main/pictures/dashboard.png)

```csharp
services.AddEventBus()
    .AddRelationDb(options => options.UseConnection(DataType.MySql, "..."))
    .AddRabbitMQ(r => { /* ... */ })
    // The secret must be exactly 32 characters and only contain English letters, digits,
    // or these common password special characters: !@#$%^&*()_+-=[]{};':"\|,.<>/?`~
    .AddDashboard(options => options.UseSecretAuthenticate("ShashlikEventBus.DashboardKey#32"));

// app.UseEventBusDashboard();
```

## License

[MIT](https://github.com/dotnet-shashlik/shashlik.eventbus/blob/main/LICENSE)
