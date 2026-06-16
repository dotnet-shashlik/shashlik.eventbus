# Shashlik.EventBus

[![build and test](https://github.com/dotnet-shashlik/shashlik.eventbus/workflows/build%20and%20test/badge.svg)](https://github.com/dotnet-shashlik/shashlik.eventbus)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/dotnet-shashlik/shashlik.eventbus/blob/main/LICENSE)

基于 .NET 的开源事件总线解决方案，采用异步确保（本地消息表）思路，提供分布式事务最终一致性、延迟事件支持。

## NuGet 包

| PackageName | NuGet | Description |
|---|---|---|
| Shashlik.EventBus.Abstract | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.Abstract.svg)](https://www.nuget.org/packages/Shashlik.EventBus.Abstract) | 接口抽象 |
| Shashlik.EventBus | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.svg)](https://www.nuget.org/packages/Shashlik.EventBus) | 核心包，消息收发、存储抽象及默认实现 |
| Shashlik.EventBus.RelationDbStorage | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.RelationDbStorage.svg)](https://www.nuget.org/packages/Shashlik.EventBus.RelationDbStorage) | 关系型数据库存储（MySQL/PostgreSQL/SqlServer/Sqlite/Oracle），基于 FreeSql |
| Shashlik.EventBus.MongoDb | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.MongoDb.svg)](https://www.nuget.org/packages/Shashlik.EventBus.MongoDb) | MongoDB 消息存储驱动 |
| Shashlik.EventBus.RabbitMQ | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.RabbitMQ.svg)](https://www.nuget.org/packages/Shashlik.EventBus.RabbitMQ) | RabbitMQ 消息收发驱动 |
| Shashlik.EventBus.Kafka | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.Kafka.svg)](https://www.nuget.org/packages/Shashlik.EventBus.Kafka) | Kafka 消息收发驱动 |
| Shashlik.EventBus.Pulsar | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.Pulsar.svg)](https://www.nuget.org/packages/Shashlik.EventBus.Pulsar) | Pulsar 消息收发驱动 |
| Shashlik.EventBus.Redis | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.Redis.svg)](https://www.nuget.org/packages/Shashlik.EventBus.Redis) | Redis Stream 消息收发驱动 |
| Shashlik.EventBus.Extensions.EfCore | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.Extensions.EfCore.svg)](https://www.nuget.org/packages/Shashlik.EventBus.Extensions.EfCore) | EF Core 扩展，通过 DbContext 直接发布事件并共享事务 |
| Shashlik.EventBus.Extensions.SqlSugar | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.Extensions.SqlSugar.svg)](https://www.nuget.org/packages/Shashlik.EventBus.Extensions.SqlSugar) | SqlSugar 扩展，获取事务上下文 |
| Shashlik.EventBus.Dashboard | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.Dashboard.svg)](https://www.nuget.org/packages/Shashlik.EventBus.Dashboard) | Web 管理面板 |
| Shashlik.EventBus.MemoryQueue | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.MemoryQueue.svg)](https://www.nuget.org/packages/Shashlik.EventBus.MemoryQueue) | 内存消息队列驱动，仅适用于测试 |
| Shashlik.EventBus.MemoryStorage | [![nuGet](https://img.shields.io/nuget/v/Shashlik.EventBus.MemoryStorage.svg)](https://www.nuget.org/packages/Shashlik.EventBus.MemoryStorage) | 内存消息存储，仅适用于测试 |

## 简介

分布式事务、CAP 定理、事件总线——在当前微服务、分布式、集群大行其道的架构前提下，是不可避免的几个关键字。`Shashlik.EventBus` 是一个基于 .NET 的事件总线解决方案，同时也是分布式事务最终一致性、延迟事件解决方案。

`Shashlik.EventBus` 采用**异步确保**的思路（本地消息表），将消息数据与业务数据在同一事务中进行提交或回滚，以此来保证消息数据的可靠性。其设计目标是高性能、简单、易用、易扩展，为抛弃历史包袱仅支持 .NET 6+，采用最宽松的 MIT 开源协议。

**核心设计原则：先持久化，后发送。** 消息必须先写入本地存储并随业务事务一起提交，才会被真正发送到消息中间件。进程崩溃、网络中断等异常情况下，由重试器负责兜底。

原理如下图：

![image](https://raw.githubusercontent.com/dotnet-shashlik/shashlik.eventbus/main/pictures/eventbus.jpg)

如图所示，消息数据需要和业务数据在同一的事务中进行提交或者回滚，最后 `Shashlik.EventBus` 会检查消息数据是否已提交，如果已提交才会执行真正的消息发送。所以要求事务的隔离级别最低为**读已提交(RC)**。

## 架构

系统分为三个正交维度，可独立组合选用：

- **消息传输**（IMessageSender + IEventSubscriber）：RabbitMQ / Kafka / Pulsar / Redis / MemoryQueue
- **消息存储**（IMessageStorage）：RelationDbStorage（MySQL/PG/SqlServer/Sqlite/Oracle via FreeSql）/ MongoDb / MemoryStorage
- **事务集成**：EfCore 扩展 / SqlSugar 扩展 / FreeSql 扩展 / XaTransactionContext（TransactionScope）

### 消息发布流程

1. 生成全局唯一 MsgId，解析事件名称，将元数据（msg-id、event-name、send-at、delay-at）注入附加数据
2. 将 `MessageStorageModel` 持久化到存储（若提供事务上下文，消息插入与业务操作在同一事务内），状态为 `SCHEDULED`
3. 后台异步等待事务提交（轮询 `ITransactionContext.IsDone()`，超时为 `TransactionCommitTimeout`）
4. 通过 `IsCommittedAsync` 确认消息已提交（处理回滚场景）
5. 调用 `IMessageSender.SendAsync` 将消息发送至中间件，最多即时重试 5 次
6. 超过即时重试次数后放弃，交给重试器兜底

### 消息消费流程

1. `IEventSubscriber`（RabbitMQ/Kafka/等）从中间件接收消息，反序列化为 `MessageTransferModel`，调用 `IMessageListener.OnReceiveAsync`
2. `DefaultMessageListener`：
   - 解析 EventHandler，将接收消息保存到存储（状态 `SCHEDULED`）
   - 非延迟消息：立即调用 `IReceivedHandler.HandleAsync`（最多即时重试 5 次）
   - 延迟消息：通过 `TimerHelper.SetTimeout` 在指定时间调度执行

### 处理器调用

- 在新的 `IServiceScope` 中创建处理器实例（支持 scoped 依赖注入）
- 优先使用启动时编译好的委托（`EventHandlerDescriptor.ExecuteDelegate`），回退到反射调用
- 自动解包 `TargetInvocationException`，暴露真实异常堆栈

### 重试机制

两个独立的重试提供者：

- `DefaultPublishedMessageRetryProvider`：重试发送失败的消息
- `DefaultReceivedMessageRetryProvider`：重试消费失败的消息

两者都：
- 启动时立即执行一次，之后按 `RetryInterval` 间隔定时执行
- 查询存储中状态为 `SCHEDULED`/`FAILED`、`RetryCount < RetryFailedMax` 且创建时间早于 `StartRetryAfter` 的消息
- 使用 `SemaphoreSlim` + `Task.WhenAll` 并发执行（`RetryMaxDegreeOfParallelism` 控制并行度）
- 执行前通过 `TryLockPublishedAsync`/`TryLockReceivedAsync` 获取乐观锁，防止多实例重复处理

### 过期清理

`DefaultExpiredMessageProvider` 每小时执行一次：
- 删除状态为 `SUCCEEDED` 且已过期的消息
- 删除状态为 `FAILED` 且重试次数达到 `RetryFailedMax` 的消息
- 批量删除（每批 1000 条），避免长事务锁表

## 关于消息幂等

`Shashlik.EventBus` 不能保证事务消息的幂等性。为了保证消息的可靠传输，EventBus 以及消息中间件对消息 QOS 处理等级必须为 `at least once`（至少到达一次），一般消息中间件都需要开启消息持久化避免消息丢失。简而言之就是一个事件处理类可能处理多次同一个事件，事件消息的幂等性应该由业务方进行处理。比如用户订单支付完成为一个事件，付款完成后需要修改订单状态为待发货，也就是在付款完成事件处理类中可能收到多次这个订单的付款完成事件，那么业务的幂等性处理就可以使用锁，判断订单状态，如果订单状态已经为待发货，则直接返回并忽略本次事件响应。

## 延迟事件

`Shashlik.EventBus` 支持基于本地的延迟事件机制，考虑到不是所有的消息中间件都支持延迟功能，且为了最大程度保证消息的可靠性，最后采用了 `System.Timers.Timer` 来执行延迟功能。

延迟事件同样适用于分布式事务最终一致性，但如果延迟事件处理类处理异常由重试器介入处理后，那么最终的延迟执行时间和期望的延迟时间就会产生较大的差异，是否忽略这里的时间差需要由具体的业务来决定。比如订单30分钟未付款需要关闭订单，35分钟后关闭订单出现了异常，最后由重试器到了40分钟后才关闭，也不影响订单，那么认为这个时间差可以容忍。又比如秒杀，发布一个延迟事件，晚上12点叫我起来买买买，只有1分钟时间，过了就买不到了，那么这种情况可以在事件处理类中，自行根据当前时间、事件发送时间、延迟执行时间等要素，自行决定业务如何处理。

延迟事件和普通事件在事件定义和事件处理类声明和处理时没有任何区别，仅仅是在发布事件时需要指定延迟时间。

## 快速开始

需求：一个新用户注册以后有以下需求：1. 发送欢迎注册短信；2. 发放新用户优惠券；3. 30分钟后推送新用户优惠活动信息。

### 1. 服务配置

以 `RelationDbStorage` + `RabbitMQ` 为例：

```csharp
services.AddEventBus(r =>
    {
        // 以下是默认配置，可以直接 services.AddEventBus()
        // 运行环境，注册到 MQ 的事件名称和事件处理名称会带上此后缀
        r.Environment = "Production";
        // 最大失败重试次数，默认 60 次
        r.RetryFailedMax = 60;
        // 消息重试间隔，默认 2 分钟
        r.RetryInterval = 60 * 2;
        // 单次重试消息数量限制，默认 100
        r.RetryLimitCount = 100;
        // 重试器并行执行数量，默认 5
        r.RetryMaxDegreeOfParallelism = 5;
        // 成功的消息过期时间，默认 3 天，失败的消息永不过期，必须处理
        r.SucceedExpireHour = 24 * 3;
        // 消息处理失败后，重试器介入时间，默认 5 分钟后
        r.StartRetryAfter = 60 * 5;
        // 事务提交超时时间，单位秒，默认 60 秒
        r.TransactionCommitTimeout = 60;
        // 重试器执行时消息锁定时长，默认 110 秒，需小于 RetryInterval
        r.LockTime = 110;
    })
    // 关系型数据库存储（需指定数据库类型和连接字符串）
    .AddRelationDb(options =>
    {
        options.UseConnection(DataType.MySql, "Server=...;Database=...;Uid=...;Pwd=...;");
    })
    // 配置 RabbitMQ
    .AddRabbitMQ(r =>
    {
        r.Host = "localhost";
        r.UserName = "rabbit";
        r.Password = "123123";
    });
```

使用配置文件方式：

```csharp
services.AddEventBus(configuration.GetSection("EventBus"))
    .AddRelationDb(options =>
    {
        options.UseConnection(DataType.MySql, "Server=...;Database=...;Uid=...;Pwd=...;");
    })
    .AddRabbitMQ(configuration.GetSection("RabbitMQ"));
```

### 2. 定义事件

```csharp
// 新用户注册完成事件，实现接口 IEvent
public class NewUserEvent : IEvent
{
    public string Id { get; set; }
    public string Name { get; set; }
}

// 定义新用户注册延迟活动推送事件
public class NewUserPromotionEvent : IEvent
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string PromotionId { get; set; }
}
```

### 3. 发布事件

**方式一：通过 IEventPublisher 发布**

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
        // 开启本地事务
        using var tran = await DbContext.Database.BeginTransactionAsync();
        try
        {
            // 创建用户逻辑处理...

            // 发布新用户事件，传入事务上下文
            await EventPublisher.PublishAsync(new NewUserEvent
            {
                Id = user.Id,
                Name = input.Name
            }, DbContext.GetTransactionContext());

            // 发布延迟事件
            await EventPublisher.PublishAsync(new NewUserPromotionEvent
            {
                Id = user.Id,
                Name = input.Name,
                PromotionId = "1"
            }, DateTimeOffset.Now.AddMinutes(30),
               DbContext.GetTransactionContext());

            // 提交本地事务
            await tran.CommitAsync();
        }
        catch (Exception ex)
        {
            // 回滚事务，消息数据也将回滚不会发布
            await tran.RollbackAsync();
        }
    }
}
```

**方式二：通过 EF Core 扩展发布（推荐）**

使用 `Shashlik.EventBus.Extensions.EfCore` 提供的 `PublishEventAsync` 扩展方法，可自动获取 DbContext 的事务上下文：

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
            // 创建用户逻辑处理...

            // 自动使用 DbContext 当前事务上下文
            await DbContext.PublishEventAsync(new NewUserEvent
            {
                Id = user.Id,
                Name = input.Name
            });

            // 发布延迟事件
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

### 4. 定义事件处理类

```csharp
// 一个事件可以有多个处理类，可以分布在不同的微服务中

// 用于发送短信的事件处理类
public class NewUserEventForSmsHandler : IEventHandler<NewUserEvent>
{
    public async Task Execute(NewUserEvent @event, IDictionary<string, string> items)
    {
        // 发送短信...
    }
}

// 用于发放消费券的事件处理类
public class NewUserEventForCouponsHandler : IEventHandler<NewUserEvent>
{
    public async Task Execute(NewUserEvent @event, IDictionary<string, string> items)
    {
        // 业务处理...
    }
}

// 用于新用户延迟活动的事件处理类，将在指定时间执行
public class NewUserPromotionEventHandler : IEventHandler<NewUserPromotionEvent>
{
    public async Task Execute(NewUserPromotionEvent @event, IDictionary<string, string> items)
    {
        // 业务处理...
    }
}
```

### 事件/处理器命名规则

默认的事件名称和事件处理器名称生成规则为 `{Type.Name}.{Options.Environment}`，在分布式架构下用于区分不同环境。你也可以使用 `[EventBusName]` 特性来显式声明名称：

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

## XA 事务支持（TransactionScope）

虽然尽可能的不要使用 `TransactionScope`，但在某些场景仍然是需要的。`Shashlik.EventBus` 对其提供了事务支持，可以通过 `XaTransactionContext.Current` 获取当前环境的事务上下文：

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
            // 创建用户逻辑处理...

            // 使用 XaTransactionContext.Current
            await EventPublisher.PublishAsync(new NewUserEvent
            {
                Id = user.Id,
                Name = input.Name
            }, XaTransactionContext.Current);

            scope.Complete();
        }
        catch (Exception ex)
        {
            // 回滚事务，消息数据也将回滚
        }
    }
}
```

## SqlSugar 事务集成

使用 `Shashlik.EventBus.Extensions.SqlSugar` 可以从 SqlSugar 中获取事务上下文：

```csharp
// 从 IAdo 获取
var txContext = ado.GetTransactionContext();

// 从 ISqlSugarClient 获取
var txContext = sqlSugarClient.GetTransactionContext();

// 从 ISugarUnitOfWork 获取
var txContext = sugarUnitOfWork.GetTransactionContext();

// 发布事件
await eventPublisher.PublishAsync(newEvent, txContext);
```
## FreeSql 事务集成

`Shashlik.EventBus.RelationDbStorage` 内置了 FreeSql 事务上下文扩展，无需额外安装包：

```csharp
// 从 FreeSql 同线程事务获取（fsql.Transaction(() => ...) 场景）
var txContext = fsql.GetCurrentThreadTransactionContext();

// 从 IUnitOfWork 获取
var txContext = unitOfWork.GetTransactionContextFromUnitOfWork();

// 从 IUnitOfWorkManager 获取（一般注册到 DI，从 services 中获取）
var txContext = unitOfWorkManager.GetTransactionContextFromUnitOfWorkManager();

// 发布事件
await eventPublisher.PublishAsync(newEvent, txContext);
```

## Dashboard

![dashboard](https://raw.githubusercontent.com/dotnet-shashlik/shashlik.eventbus/main/pictures/dashboard.png)

配置 Dashboard：

```csharp
services.AddEventBus()
    .AddRelationDb(options =>
    {
        options.UseConnection(DataType.MySql, "...");
    })
    .AddRabbitMQ(r => { /* ... */ })
    .AddDashboard(options =>
    {
        // 使用 Secret 认证
        options.UseSecretAuthenticate("your-secret-key");
        // 或使用自定义认证
        // options.UseAuthenticate<MyAuthenticate>();
    });
```

## EventBusOptions 参数说明

| 参数 | 默认值 | 说明 |
|---|---|---|
| Environment | `"Production"` | 环境标识，事件名称和处理器名称的后缀，用于多环境隔离 |
| TransactionCommitTimeout | 60 | 事务提交等待超时（秒），必须小于 StartRetryAfter |
| StartRetryAfter | 300 | 消息失败后多久重试器开始介入（秒） |
| RetryLimitCount | 100 | 重试器单次执行读取的消息数量 |
| RetryMaxDegreeOfParallelism | 5 | 重试器并行执行数量 |
| RetryFailedMax | 60 | 最大失败重试次数（最小值 5） |
| RetryInterval | 120 | 重试执行间隔（秒） |
| LockTime | 110 | 重试执行时消息锁定时长（秒），必须小于 RetryInterval |
| SucceedExpireHour | 72 | 成功消息过期删除时间（小时） |
| HandlerServiceLifetime | Transient | 事件处理器的 DI 生命周期 |

启动时会通过 `EventBusOptionsValidation` 校验参数范围和相互约束关系。

## 扩展

如果默认实现不能满足你的需求，可以自行实现可扩展接口，并注册即可覆盖默认实现。

| 接口 | 说明 |
|---|---|
| `IMsgIdGenerator` | 消息传输 ID 生成器（全局唯一），默认 Guid |
| `IEventPublisher` | 事件发布处理器 |
| `IMessageSerializer` | 消息序列化/反序列化，默认 System.Text.Json |
| `IReceivedMessageRetryProvider` | 已接收消息重试器 |
| `IPublishedMessageRetryProvider` | 已发布消息重试器 |
| `IEventHandlerInvoker` | 事件处理执行器 |
| `IEventNameRuler` | 事件名称规则（对应消息队列 topic/route） |
| `IEventHandlerNameRuler` | 事件处理名称规则（对应消息队列 queue/group） |
| `IEventHandlerFindProvider` | 事件处理类查找器 |
| `IExpiredMessageProvider` | 已过期消息删除处理器 |
| `IMessageListener` | 消息监听处理器 |
| `IPublishHandler` | 消息发布处理器 |
| `IReceivedHandler` | 消息接收处理器 |
| `IMessageStorageInitializer` | 存储介质初始化 |
| `IMessageStorage` | 消息存储、寻取等操作 |
| `IMessageSender` | 消息发送到中间件 |
| `IEventSubscriber` | 事件订阅器（从中间件接收消息） |

示例：

```csharp
// 替换默认的 IMsgIdGenerator
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