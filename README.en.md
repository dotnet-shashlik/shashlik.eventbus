# Shashlik.EventBus

[![build and test](https://github.com/dotnet-shashlik/shashlik.eventbus/workflows/build%20and%20test/badge.svg)](https://github.com/dotnet-shashlik/shashlik.eventbus)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/dotnet-shashlik/shashlik.eventbus/blob/main/LICENSE)

An open-source .NET event bus solution using the async-ensure pattern (local message table), providing eventual consistency for distributed transactions and delayed event support.

## NuGet Packages

| PackageName | NuGet | Description |
|---|---|---|
| Shashlik.EventBus.Abstract | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.Abstract.svg)](https://www.nuget.org/packages/Shashlik.EventBus.Abstract) | Interface abstractions |
| Shashlik.EventBus | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.svg)](https://www.nuget.org/packages/Shashlik.EventBus) | Core package: message send/receive, storage abstractions, and default implementations |
| Shashlik.EventBus.RelationDbStorage | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.RelationDbStorage.svg)](https://www.nuget.org/packages/Shashlik.EventBus.RelationDbStorage) | Relational database storage (MySQL/PostgreSQL/SqlServer/Sqlite/Oracle), based on FreeSql |
| Shashlik.EventBus.MongoDb | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.MongoDb.svg)](https://www.nuget.org/packages/Shashlik.EventBus.MongoDb) | MongoDB message storage driver |
| Shashlik.EventBus.RabbitMQ | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.RabbitMQ.svg)](https://www.nuget.org/packages/Shashlik.EventBus.RabbitMQ) | RabbitMQ message transport driver |
| Shashlik.EventBus.Kafka | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.Kafka.svg)](https://www.nuget.org/packages/Shashlik.EventBus.Kafka) | Kafka message transport driver |
| Shashlik.EventBus.Pulsar | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.Pulsar.svg)](https://www.nuget.org/packages/Shashlik.EventBus.Pulsar) | Pulsar message transport driver |
| Shashlik.EventBus.Redis | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.Redis.svg)](https://www.nuget.org/packages/Shashlik.EventBus.Redis) | Redis Stream message transport driver |
| Shashlik.EventBus.Extensions.EfCore | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.Extensions.EfCore.svg)](https://www.nuget.org/packages/Shashlik.EventBus.Extensions.EfCore) | EF Core extension: publish events directly via DbContext with shared transaction |
| Shashlik.EventBus.Extensions.SqlSugar | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.Extensions.SqlSugar.svg)](https://www.nuget.org/packages/Shashlik.EventBus.Extensions.SqlSugar) | SqlSugar extension: obtain transaction context |
| Shashlik.EventBus.Dashboard | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.Dashboard.svg)](https://www.nuget.org/packages/Shashlik.EventBus.Dashboard) | Web management dashboard |
| Shashlik.EventBus.MemoryQueue | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.MemoryQueue.svg)](https://www.nuget.org/packages/Shashlik.EventBus.MemoryQueue) | In-memory message queue driver (testing only) |
| Shashlik.EventBus.MemoryStorage | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.MemoryStorage.svg)](https://www.nuget.org/packages/Shashlik.EventBus.MemoryStorage) | In-memory message storage (testing only) |

## Overview

Distributed transactions, the CAP theorem, and event buses are unavoidable concepts in modern microservice, distributed, and clustered architectures. `Shashlik.EventBus` is a .NET-based event bus solution that also provides eventual consistency for distributed transactions and delayed event support.

`Shashlik.EventBus` uses the **async-ensure** pattern (local message table): message data is committed or rolled back within the same transaction as business data, guaranteeing message reliability. The design goals are high performance, simplicity, ease of use, and extensibility. It targets .NET 6+ and uses the permissive MIT license.

**Core design principle: persist first, send later.** Messages must be written to local storage and committed along with the business transaction before they are actually sent to the message broker. In case of process crashes, network failure, or other exceptions, the retry providers serve as a safety net.

The principle is illustrated below:

![image](https://raw.githubusercontent.com/dotnet-shashlik/shashlik.eventbus/main/pictures/eventbus.jpg)

As shown, message data and business data are committed or rolled back within the same transaction. `Shashlik.EventBus` then checks whether the message data has been committed before performing the actual send. This requires a minimum transaction isolation level of **Read Committed (RC)**.

## Architecture

The system is divided into three orthogonal dimensions that can be independently combined:

- **Message Transport** (IMessageSender + IEventSubscriber): RabbitMQ / Kafka / Pulsar / Redis / MemoryQueue
- **Message Storage** (IMessageStorage): RelationDbStorage (MySQL/PG/SqlServer/Sqlite/Oracle via FreeSql) / MongoDb / MemoryStorage
- **Transaction Integration**: EfCore extension / SqlSugar extension / FreeSql extension / XaTransactionContext (TransactionScope)

### Message Publishing Flow

1. Generate a globally unique MsgId, resolve the event name, and inject metadata (msg-id, event-name, send-at, delay-at) into the additional items
2. Persist `MessageStorageModel` to storage (if a transaction context is provided, the message insert and business operation share the same transaction), status is `SCHEDULED`
3. Asynchronously wait for the transaction to commit (poll `ITransactionContext.IsDone()`, timeout is `TransactionCommitTimeout`)
4. Confirm the message has been committed via `IsCommittedAsync` (handles rollback scenarios)
5. Call `IMessageSender.SendAsync` to send the message to the broker, with up to 5 immediate retries
6. If all immediate retries fail, give up and let the retry provider handle it later

### Message Consumption Flow

1. `IEventSubscriber` (RabbitMQ/Kafka/etc.) receives a message from the broker, deserializes it into `MessageTransferModel`, and calls `IMessageListener.OnReceiveAsync`
2. `DefaultMessageListener`:
   - Resolves the EventHandler, saves the received message to storage (status `SCHEDULED`)
   - Non-delayed messages: immediately call `IReceivedHandler.HandleAsync` (up to 5 immediate retries)
   - Delayed messages: schedule execution at the specified time via `TimerHelper.SetTimeout`

### Handler Invocation

- Handler instances are created within a new `IServiceScope` (supports scoped dependency injection)
- Prefers compiled delegates from startup (`EventHandlerDescriptor.ExecuteDelegate`), falls back to reflection
- Automatically unwraps `TargetInvocationException` to expose the real exception stack trace

### Retry Mechanism

Two independent retry providers:

- `DefaultPublishedMessageRetryProvider`: retries failed publish operations
- `DefaultReceivedMessageRetryProvider`: retries failed consume operations

Both:
- Execute once at startup, then run at `RetryInterval` intervals
- Query storage for messages with status `SCHEDULED`/`FAILED`, `RetryCount < RetryFailedMax`, and creation time earlier than `StartRetryAfter`
- Use `SemaphoreSlim` + `Task.WhenAll` for concurrent execution (controlled by `RetryMaxDegreeOfParallelism`)
- Acquire an optimistic lock via `TryLockPublishedAsync`/`TryLockReceivedAsync` before execution, preventing duplicate processing across multiple instances

### Expired Message Cleanup

`DefaultExpiredMessageProvider` runs once per hour:
- Deletes messages with status `SUCCEEDED` that have expired
- Deletes messages with status `FAILED` and retry count >= `RetryFailedMax`
- Batch deletion (1000 per batch) to avoid long-running locks

## Message Idempotency

`Shashlik.EventBus` cannot guarantee message idempotency. To ensure reliable delivery, the EventBus and message brokers must use `at least once` QOS. In practice, this means an event handler may receive the same event multiple times, and idempotency must be handled by the business logic. For example, if a "payment completed" event updates an order status to "pending shipment", the handler should check the current order status first and skip if it is already updated.

## Delayed Events

`Shashlik.EventBus` supports local delayed event execution. Since not all message brokers support delayed delivery and to maximize message reliability, `System.Timers.Timer` is used for scheduling.

Delayed events also support distributed transaction eventual consistency. However, if a delayed event handler fails and the retry provider takes over, the actual execution time may differ significantly from the intended delay. Whether this time difference is acceptable depends on your business requirements. For example, a 30-minute order cancellation that executes at 35 or 40 minutes may still be acceptable, whereas a flash sale reminder with a 1-minute window may not.

Delayed events and regular events are identical in definition and handler declaration. The only difference is specifying a delay time when publishing.

## Quick Start

Requirement: After a new user registers, the system should: 1. Send a welcome SMS; 2. Issue new-user coupons; 3. Push a promotional activity notification after 30 minutes.

### 1. Service Configuration

Example using `RelationDbStorage` + `RabbitMQ`:

```csharp
services.AddEventBus(r =>
    {
        // These are default values; you can simply call services.AddEventBus()
        // Environment suffix appended to event/handler names in the message broker
        r.Environment = "Production";
        // Max failed retry count, default 60
        r.RetryFailedMax = 60;
        // Retry interval, default 2 minutes
        r.RetryInterval = 60 * 2;
        // Max messages per retry batch, default 100
        r.RetryLimitCount = 100;
        // Retry parallelism degree, default 5
        r.RetryMaxDegreeOfParallelism = 5;
        // Successful message expiration, default 3 days; failed messages never expire
        r.SucceedExpireHour = 24 * 3;
        // Time before retry provider kicks in, default 5 minutes
        r.StartRetryAfter = 60 * 5;
        // Transaction commit timeout in seconds, default 60
        r.TransactionCommitTimeout = 60;
        // Retry lock duration in seconds, default 110; must be less than RetryInterval
        r.LockTime = 110;
    })
    // Relational database storage (specify database type and connection string)
    .AddRelationDb(options =>
    {
        options.UseConnection(DataType.MySql, "Server=...;Database=...;Uid=...;Pwd=...;");
    })
    // Configure RabbitMQ
    .AddRabbitMQ(r =>
    {
        r.Host = "localhost";
        r.UserName = "rabbit";
        r.Password = "123123";
    });
```

Using configuration sections:

```csharp
services.AddEventBus(configuration.GetSection("EventBus"))
    .AddRelationDb(options =>
    {
        options.UseConnection(DataType.MySql, "Server=...;Database=...;Uid=...;Pwd=...;");
    })
    .AddRabbitMQ(configuration.GetSection("RabbitMQ"));
```

### 2. Define Events

```csharp
// New user registration event, implements IEvent
public class NewUserEvent : IEvent
{
    public string Id { get; set; }
    public string Name { get; set; }
}

// Delayed promotional push event
public class NewUserPromotionEvent : IEvent
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string PromotionId { get; set; }
}
```

### 3. Publish Events

**Option A: Via IEventPublisher**

```csharp
public class UserManager
{
    public UserManager(IEventPublisher eventPublisher, DemoDbContext dbContext)
    {
        EventPublisher = eventPublisher;
        DbContext = dbContext;
    }

    private IEventPublisher EventPublisher { get; }
    private DemoDbContext DbContext { get; }

    public async Task CreateUserAsync(UserInput input)
    {
        using var tran = await DbContext.Database.BeginTransactionAsync();
        try
        {
            // User creation logic...

            // Publish event with transaction context
            await EventPublisher.PublishAsync(new NewUserEvent
            {
                Id = user.Id,
                Name = input.Name
            }, DbContext.GetTransactionContext());

            // Publish delayed event
            await EventPublisher.PublishAsync(new NewUserPromotionEvent
            {
                Id = user.Id,
                Name = input.Name,
                PromotionId = "1"
            }, DateTimeOffset.Now.AddMinutes(30),
               DbContext.GetTransactionContext());

            await tran.CommitAsync();
        }
        catch (Exception ex)
        {
            // Rollback: message data will also be rolled back
            await tran.RollbackAsync();
        }
    }
}
```

**Option B: Via EF Core extension (recommended)**

The `Shashlik.EventBus.Extensions.EfCore` package provides `PublishEventAsync` extension methods that automatically use the DbContext's current transaction context:

```csharp
public class UserManager
{
    public UserManager(DemoDbContext dbContext)
    {
        DbContext = dbContext;
    }

    private DemoDbContext DbContext { get; }

    public async Task CreateUserAsync(UserInput input)
    {
        using var tran = await DbContext.Database.BeginTransactionAsync();
        try
        {
            // User creation logic...

            // Automatically uses DbContext's current transaction context
            await DbContext.PublishEventAsync(new NewUserEvent
            {
                Id = user.Id,
                Name = input.Name
            });

            // Publish delayed event
            await DbContext.PublishEventAsync(new NewUserPromotionEvent
            {
                Id = user.Id,
                Name = input.Name,
                PromotionId = "1"
            }, DateTimeOffset.Now.AddMinutes(30));

            await tran.CommitAsync();
        }
        catch (Exception ex)
        {
            await tran.RollbackAsync();
        }
    }
}
```

### 4. Define Event Handlers

```csharp
// One event can have multiple handlers, distributed across different microservices

// SMS handler
public class NewUserEventForSmsHandler : IEventHandler<NewUserEvent>
{
    public async Task Execute(NewUserEvent @event, IDictionary<string, string> items)
    {
        // Send SMS...
    }
}

// Coupon handler
public class NewUserEventForCouponsHandler : IEventHandler<NewUserEvent>
{
    public async Task Execute(NewUserEvent @event, IDictionary<string, string> items)
    {
        // Business logic...
    }
}

// Delayed promotion handler, executes at the specified time
public class NewUserPromotionEventHandler : IEventHandler<NewUserPromotionEvent>
{
    public async Task Execute(NewUserPromotionEvent @event, IDictionary<string, string> items)
    {
        // Business logic...
    }
}
```

### Event/Handler Naming Convention

By default, event names and handler names follow the pattern `{Type.Name}.{Options.Environment}`, which allows environment-based isolation in distributed deployments. You can also use the `[EventBusName]` attribute for explicit naming:

```csharp
[EventBusName("order.created")]
public class OrderCreatedEvent : IEvent
{
    // ...
}

[EventBusName("order.send-sms")]
public class OrderCreatedSmsHandler : IEventHandler<OrderCreatedEvent>
{
    // ...
}
```

## XA Transaction Support (TransactionScope)

While `TransactionScope` should generally be avoided, some scenarios still require it. `Shashlik.EventBus` supports it via `XaTransactionContext.Current`:

```csharp
public class UserManager
{
    public UserManager(IEventPublisher eventPublisher)
    {
        EventPublisher = eventPublisher;
    }

    private IEventPublisher EventPublisher { get; }

    public async Task CreateUserAsync(UserInput input)
    {
        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        try
        {
            // User creation logic...

            // Use XaTransactionContext.Current
            await EventPublisher.PublishAsync(new NewUserEvent
            {
                Id = user.Id,
                Name = input.Name
            }, XaTransactionContext.Current);

            scope.Complete();
        }
        catch (Exception ex)
        {
            // Rollback: message data will also be rolled back
        }
    }
}
```

## SqlSugar Transaction Integration

Use `Shashlik.EventBus.Extensions.SqlSugar` to obtain transaction context from SqlSugar:

```csharp
// From IAdo
var txContext = ado.GetTransactionContext();

// From ISqlSugarClient
var txContext = sqlSugarClient.GetTransactionContext();

// From ISugarUnitOfWork
var txContext = sugarUnitOfWork.GetTransactionContext();

// Publish event
await eventPublisher.PublishAsync(newEvent, txContext);
```
## FreeSql Transaction Integration

`Shashlik.EventBus.RelationDbStorage` includes built-in FreeSql transaction context extensions, no additional package required:

```csharp
// From FreeSql same-thread transaction (fsql.Transaction(() => ...) scenario)
var txContext = fsql.GetCurrentThreadTransactionContext();

// From IUnitOfWork
var txContext = unitOfWork.GetTransactionContextFromUnitOfWork();

// From IUnitOfWorkManager (typically registered in DI, resolve from services)
var txContext = unitOfWorkManager.GetTransactionContextFromUnitOfWorkManager();

// Publish event
await eventPublisher.PublishAsync(newEvent, txContext);
```

## Dashboard

![dashboard](https://raw.githubusercontent.com/dotnet-shashlik/shashlik.eventbus/main/pictures/dashboard.png)

Configure Dashboard:

```csharp
services.AddEventBus()
    .AddRelationDb(options =>
    {
        options.UseConnection(DataType.MySql, "...");
    })
    .AddRabbitMQ(r => { /* ... */ })
    .AddDashboard(options =>
    {
        // Use Secret authentication
        options.UseSecretAuthenticate("your-secret-key");
        // Or use custom authentication
        // options.UseAuthenticate<MyAuthenticate>();
    });
```

## EventBusOptions Reference

| Parameter | Default | Description |
|---|---|---|
| Environment | `"Production"` | Environment identifier appended to event/handler names for multi-environment isolation |
| TransactionCommitTimeout | 60 | Transaction commit wait timeout (seconds); must be less than StartRetryAfter |
| StartRetryAfter | 300 | Time before retry provider starts processing failed messages (seconds) |
| RetryLimitCount | 100 | Max messages fetched per retry cycle |
| RetryMaxDegreeOfParallelism | 5 | Retry parallelism degree |
| RetryFailedMax | 60 | Max retry attempts for failed messages (minimum 5) |
| RetryInterval | 120 | Retry execution interval (seconds) |
| LockTime | 110 | Message lock duration during retry execution (seconds); must be less than RetryInterval |
| SucceedExpireHour | 72 | Expiration time for successful messages before deletion (hours) |
| HandlerServiceLifetime | Transient | DI service lifetime for event handlers |

Parameters are validated at startup via `EventBusOptionsValidation`, which checks value ranges and cross-constraint relationships.

## Extensibility

If the default implementations don't meet your needs, you can implement any of the extensible interfaces and register them to override the defaults.

| Interface | Description |
|---|---|
| `IMsgIdGenerator` | Message transport ID generator (globally unique), default: Guid |
| `IEventPublisher` | Event publisher |
| `IMessageSerializer` | Message serialization/deserialization, default: System.Text.Json |
| `IReceivedMessageRetryProvider` | Received message retry provider |
| `IPublishedMessageRetryProvider` | Published message retry provider |
| `IEventHandlerInvoker` | Event handler invoker |
| `IEventNameRuler` | Event name rule (maps to message queue topic/route) |
| `IEventHandlerNameRuler` | Event handler name rule (maps to message queue queue/group) |
| `IEventHandlerFindProvider` | Event handler finder |
| `IExpiredMessageProvider` | Expired message cleanup handler |
| `IMessageListener` | Message listener |
| `IPublishHandler` | Publish handler |
| `IReceivedHandler` | Receive handler |
| `IMessageStorageInitializer` | Storage initializer |
| `IMessageStorage` | Message storage operations |
| `IMessageSender` | Message sender to broker |
| `IEventSubscriber` | Event subscriber (receives messages from broker) |

Example:

```csharp
// Replace default IMsgIdGenerator
service.AddSingleton<IMsgIdGenerator, CustomMsgIdGenerator>();

service.AddEventBus()
    .AddRabbitMQ(r => { /* ... */ })
    .AddRelationDb(options =>
    {
        options.UseConnection(DataType.MySql, "...");
    })
    .AddDashboard(options =>
    {
        options.UseSecretAuthenticate("your-secret-key");
    });
```