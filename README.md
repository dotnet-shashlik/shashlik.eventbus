# Shashlik.EventBus

[![build and test](https://github.com/dotnet-shashlik/shashlik.eventbus/workflows/build%20and%20test/badge.svg)](https://github.com/dotnet-shashlik/shashlik.eventbus)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/dotnet-shashlik/shashlik.eventbus/blob/main/LICENSE)

基于 .NET 8+ 的开源事件总线解决方案，采用异步确保（本地消息表）思路，提供分布式事务最终一致性、延迟事件支持。

## 亮点

- **先持久化，后发送** — 消息与业务数据同事务提交，进程崩溃 / 网络中断由重试器兜底，零消息丢失
- **三维度正交组合** — 消息传输、消息存储、事务集成独立选用，按需搭配
- **多中间件支持** — RabbitMQ / Kafka / Pulsar / Redis Stream / 内存队列
- **多存储支持** — 关系型数据库（MySQL / PostgreSQL / SqlServer / Sqlite / Oracle，基于 FreeSql）/ MongoDB / 内存存储
- **多 ORM 事务集成** — EF Core / SqlSugar / FreeSql / XA (TransactionScope) 开箱即用
- **延迟事件** — 基于本地执行的延迟机制，不依赖中间件延迟功能，最大程度保证消息可靠
- **高性能** — 启动时编译处理器委托，运行时零反射；生产者 / 消费者池化复用
- **易扩展** — 18 个可替换接口，覆盖从 ID 生成到消息收发的全流程

## 设计原理

![eventbus](https://raw.githubusercontent.com/dotnet-shashlik/shashlik.eventbus/main/pictures/eventbus.jpg)

消息数据与业务数据在同一事务中提交或回滚，EventBus 确认消息已提交后才真正发送。事务隔离级别最低要求 **读已提交 (RC)**。

## 架构

系统分为三个正交维度，可独立组合选用：

| 维度 | 可选实现 |
|------|----------|
| **消息传输** | RabbitMQ / Kafka / Pulsar / Redis Stream / MemoryQueue |
| **消息存储** | RelationDbStorage (MySQL/PG/SqlServer/Sqlite/Oracle) / MongoDb / MemoryStorage |
| **事务集成** | EF Core / SqlSugar / FreeSql / XA (TransactionScope) |

> 详细设计文档请参阅 [Wiki](https://github.com/dotnet-shashlik/shashlik.eventbus/wiki)

## NuGet 包

| Package | Description |
|---|---|
| [Shashlik.EventBus.Abstract](https://www.nuget.org/packages/Shashlik.EventBus.Abstract) | 接口抽象 |
| [Shashlik.EventBus](https://www.nuget.org/packages/Shashlik.EventBus) | 核心包，消息收发、存储抽象及默认实现 |
| [Shashlik.EventBus.RelationDbStorage](https://www.nuget.org/packages/Shashlik.EventBus.RelationDbStorage) | 关系型数据库存储（基于 FreeSql，需引入对应 Provider） |
| [Shashlik.EventBus.MongoDb](https://www.nuget.org/packages/Shashlik.EventBus.MongoDb) | MongoDB 消息存储 |
| [Shashlik.EventBus.RabbitMQ](https://www.nuget.org/packages/Shashlik.EventBus.RabbitMQ) | RabbitMQ 消息传输 |
| [Shashlik.EventBus.Kafka](https://www.nuget.org/packages/Shashlik.EventBus.Kafka) | Kafka 消息传输 |
| [Shashlik.EventBus.Pulsar](https://www.nuget.org/packages/Shashlik.EventBus.Pulsar) | Pulsar 消息传输 |
| [Shashlik.EventBus.Redis](https://www.nuget.org/packages/Shashlik.EventBus.Redis) | Redis Stream 消息传输 |
| [Shashlik.EventBus.Extensions.EfCore](https://www.nuget.org/packages/Shashlik.EventBus.Extensions.EfCore) | EF Core 扩展 |
| [Shashlik.EventBus.Extensions.SqlSugar](https://www.nuget.org/packages/Shashlik.EventBus.Extensions.SqlSugar) | SqlSugar 扩展 |
| [Shashlik.EventBus.Dashboard](https://www.nuget.org/packages/Shashlik.EventBus.Dashboard) | Web 管理面板 |
| [Shashlik.EventBus.MemoryQueue](https://www.nuget.org/packages/Shashlik.EventBus.MemoryQueue) | 内存消息队列（仅测试） |
| [Shashlik.EventBus.MemoryStorage](https://www.nuget.org/packages/Shashlik.EventBus.MemoryStorage) | 内存消息存储（仅测试） |

## 快速上手

```csharp
// 1. 服务配置
services.AddEventBus()
    .AddRelationDb(options => options.UseConnection(DataType.MySql, "Server=...;Database=...;"))
    .AddRabbitMQ(r => { r.Host = "localhost"; r.UserName = "guest"; r.Password = "guest"; });

// 2. 定义事件
public class NewUserEvent : IEvent { public string Id { get; set; } public string Name { get; set; } }

// 3. 发布事件（EF Core 扩展，自动共享事务）
await DbContext.PublishEventAsync(new NewUserEvent { Id = "1", Name = "张三" });

// 4. 定义处理器
public class NewUserSmsHandler : IEventHandler<NewUserEvent>
{
    public async Task Execute(NewUserEvent @event, IDictionary<string, string> items) { /* 发送短信 */ }
}
```

> 完整使用文档请参阅 [Wiki](https://github.com/dotnet-shashlik/shashlik.eventbus/wiki)

## 关于消息幂等

EventBus 及消息中间件 QOS 为 `at least once`，一个处理器可能多次收到同一事件，业务方需自行处理幂等性。例如在处理器中检查订单状态，已处理则直接返回。

## 延迟事件

延迟事件基于本地执行，不依赖中间件延迟功能以保证消息可靠性。定义和处理器声明与普通事件完全相同，仅在发布时指定延迟时间：

```csharp
await DbContext.PublishEventAsync(new NewUserPromotionEvent { ... }, DateTimeOffset.Now.AddMinutes(30));
```

若延迟事件处理异常由重试器介入，实际执行时间可能偏离预期，是否容忍此差异由业务决定。

## Dashboard

![dashboard](https://raw.githubusercontent.com/dotnet-shashlik/shashlik.eventbus/main/pictures/dashboard.png)

```csharp
services.AddEventBus()
    .AddRelationDb(options => options.UseConnection(DataType.MySql, "..."))
    .AddRabbitMQ(r => { /* ... */ })
    // secret 必须为 32 字符,仅支持英文/数字/常见密码特殊符号 (!@#$%^&*()_+-=[]{};':"\|,.<>/?`~)
    .AddDashboard(options => options.UseSecretAuthenticate("ShashlikEventBus.DashboardKey#32"));

// app.UseEventBusDashboard();
```

## 许可

[MIT](https://github.com/dotnet-shashlik/shashlik.eventbus/blob/main/LICENSE)
