# 【Shashlik.EventBus】


## 简介

分布式事务、CAP定理、事件总线，在当前微服务、分布式、集群大行其道的架构前提下，是不可逃避的几个关键字，在此不会过多阐述相关的理论知识。`Shashlik.EventBus`就是一个基于.NET6的开源事件总线解决方案，同时也是分布式事务最终一致性、延迟事件解决方案。`Shashlik.EventBus`采用的是异步确保的思路（本地消息表），将消息数据与业务数据在同一事务中进行提交或回滚，以此来保证消息数据的可靠性。其设计目标是高性能、简单、易用、易扩展，为抛弃历史包袱，仅支持NET6，采用最宽松的 MIT 开源协议。

原理如下图：

![image](https://raw.githubusercontent.com/dotnet-shashlik/shashlik.eventbus/main/pictures/eventbus.jpg)

如图所示，消息数据需要和业务数据在同一的事务中进行提交或者回滚，最后`Shashlik.EventBus`会检查消息数据是否已提交，如果已提交才会执行真正的消息发送。所以要求事务的隔离级别最低为**读已提交(RC)**。

## 关于消息幂等

`Shashlik.EventBus`不能保证业务消息的幂等性，为了保证消息的可靠传输，EventBus以及消息中间件对消息QOS处理等级必须为`at least once` (至少到达一次)，一般消息中间件都需要开启消息持久化避免消息丢失。简而言之就是一个事件处理类可能处理多次同一个事件，事件消息的幂等性应该由业务方进行处理。比如用户订单付款完成为一个事件，付款完成后需要修改订单状态为待发货，也就是在付款完成事件处理类中可能收到多次这个订单的付款完成事件，那么业务的幂等性处理就可以使用锁，判断订单状态，如果订单状态已经为待发货，则直接返回并忽略本次事件响应。

## 延迟事件

`Shashlik.EventBus`支持基于本地的延迟事件机制，考虑到不是所有的消息中间件都支持延迟功能，且为了最大程度保证消息的可靠性，最后采用了`System.Timers.Timer`来执行延迟功能。

延迟事件同样适用于分布式事务最终一致性，但如果延迟事件处理类处理异常由重试器介入处理后，那么最终的延迟执行时间和期望的延迟时间就会产生较大的差异，是否忽略这里的时间差需要由具体的业务来决定。比如订单30分钟未付款需要关闭订单，30分钟后关闭订单出现了异常，最后由重试器到了40分钟后才关闭，也不影响订单，那么认为这个时间差可以容忍。又比如双11啦，发布一个延迟事件，晚上12点叫醒我起来买买买，只有1分钟时间，过了就买不到了，那么这种情况可以在事件处理类中，自行根据当前时间、事件发送时间、延迟执行时间等要素，自行决定业务如何处理。

延迟事件和普通事件在事件定义和事件处理类声明和处理时没有任何区别，仅仅是在发布事件时需要指定延迟时间。

## 上代码

需求：一个新用户注册以后有以下需求：1. 发送欢迎注册短信；2. 发放新用户优惠券；3. 30分钟后推送新用户优惠活动信息。

1. 服务配置，这里以`MySql` + `RabbitMQ`为例：

```c#
    services.AddEventBus(r =>
        {
            // 这些都是缺省配置，可以直接services.AddEventBus()
            // 运行环境，注册到MQ的事件名称和事件处理名称会带上此后缀
            r.Environment = "Production";
            // 最大失败重试次数，默认60次
            r.RetryFailedMax = 60;
            // 消息重试间隔，默认2分钟
            r.RetryInterval = 60 * 2;
            // 单次重试消息数量限制，默认100
            r.RetryLimitCount = 100;
            // 成功的消息过期时间，默认3天，失败的消息永不过期，必须处理
            r.SucceedExpireHour = 24 * 3;            
            // 消息处理失败后，重试器介入时间，默认5分钟后
            r.StartRetryAfter = 60 * 5;            
            // 事务提交超时时间,单位秒,默认60秒
            r.TransactionCommitTimeout = 60;
            // 重试器执行时消息锁定时长
            r.LockTime = 110;
        })
        // 使用ef DbContext mysql
        .AddMySql<DemoDbContext>()
        // 配置RabbitMQ
        .AddRabbitMQ(r =>
        {
            r.Host = "localhost";
            r.UserName = "rabbit";
            r.Password = "123123";
        });
```

2. 定义事件

```c#

    // 新用户注册完成事件，实现接口IEvent
    public class NewUserEvent : IEvent
    {
        public string Id { get;set; }
        public string Name { get; set; }
    }
    
    // 定义新用户注册延迟活动推送事件
    public class NewUserPromotionEvent : IEvent
    {
        public string Id { get;set; }
        public string Name { get; set; }
        public string PromotionId { get; set; }
    }

```

3. 发布事件

```c#

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
        using var tran = await DbContext.DataBase.BeginTransactionAsync();
        try
        {
            // 创建用户逻辑处理...

            // 发布新用户事件
            // 通过注入IEventPublisher发布事件，需要传入事务上下文数据
            await EventPublisher.PublishAsync(new NewUserEvent{
                Id = user.Id,
                Name = input.Name
            }, DbContext.GetTransactionContext());

            // 发布延迟事件
            // 通过ef扩展，直接使用DbContext发布事件，自动使用当前上下文事务
            await DbContext.PublishEventAsync(new NewUserPromotionEvent{
                Id = user.Id,
                Name = input.Name,
                PromotionId = "1"
            }, DatetimeOffset.Now.AddMinutes(30));

            // 提交本地事务
            await tran.CommitAsync();
        }catch(Exception ex)
        {
            // 回滚事务，消息数据也将回滚不会发布
            await tran.RollbackAsync();
        }
    }
}

```

4. 定义事件处理类

```c#
    
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

    // 用于新用户延迟活动的事件处理类,将在指定时间自行
    public class NewUserPromotionEventHandler : IEventHandler<NewUserPromotionEvent>
    {
        public async Task Execute(NewUserPromotionEvent @event, IDictionary<string, string> items)
        {
            // 业务处理...
        }
    }    
```


默认的，发布、声明到消息中间件的事件、事件处理器名称生产规则为`{Type.Name}.{Options.Environment}`，在分布式架构下需要，您需要了解这个默认规则，这点不同于`CAP`框架必须显示声明，当然`Shashlik.EventBus`也可以使用`EventBusNameAttribute`特性来显示声明，详细说明请上github查看[wiki文档](https://github.com/dotnet-shashlik/shashlik.eventbus/wiki/Event.Publish#eventbusnameattribute)。


## XA事务支持（TransactionScope）

虽然尽可能的不要使用`TransactionScope`，但在某些场景仍然是需要的，`Shashlik.EventBus`对其提供了事务支持，可以通过`XaTransactionContext.Current`获取当前环境的事务上下文，发布事件如下：

```c#

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
        // 开启事务
        using var scope = new TransactionScope();
        try
        {
            // 创建用户逻辑处理...

            // 发布新用户事件
            // 通过注入IEventPublisher发布事件，需要传入事务上下文数据
            await EventPublisher.PublishAsync(new NewUserEvent{
                Id = user.Id,
                Name = input.Name
            // 使用 XaTransactionContext.Current
            }, XaTransactionContext.Current);

            // 提交事务
            await scope.Complete();
        }catch(Exception ex)
        {
            // 回滚事务，消息数据也将回滚不会发布
            await tran.RollbackAsync();
        }
    }
}

```

## 扩展

如果默认实现不能满足你的需求，可以自行实现可扩展接口，并注册即可。

- `IMsgIdGenerator`：消息Id生成器，是指传输的全局唯一id，不是指存储的id。默认guid
- `IEventPublisher`：事件发布处理器。
- `IMessageSerializer`：消息序列化、反序列化处理类。默认`Newtonsoft.Json`。
- `IReceivedMessageRetryProvider`：已接收消息重试器。
- `IPublishedMessageRetryProvider`：已发布消息重试器。
- `IEventHandlerInvoker`: 事件处理执行器
- `IEventNameRuler`：事件名称规则生成(对应消息队列topic/route)。
- `IEventHandlerNameRuler`：事件处理名称规则生成(对应消息队列queue/group)。
- `IEventHandlerFindProvider`：事件处理类查找器
- `IExpiredMessageProvider`：已过期消息删除处理器。
- `IMessageListener`：消息监听处理器。
- `IRetryProvider`：重试执行器。
- `IPublishHandler`：消息发布处理器。
- `IReceivedHandler`：消息接收处理器。
- `IMessageStorageInitializer`：存储介质初始化。
- `IMessageStorage`：消息存储、读取等操作。

例：

```c#

    // 替换默认的IMsgIdGenerator
    service.AddSingleton<IMsgIdGenerator, CustomMsgIdGenerator>();
    service.AddEventBus()
        .AddMemoryQueue()
        .AddMemoryStorage();

```
## 后续计划
- 功能
  - [ ] Dashboard
- 消息中间件支持
    - [x] RabbitMQ
    - [x] Kafka
    - [ ] RocketMQ
    - [ ] ActiveMQ
    - [ ] Pulsar
    - [ ] Redis
- 存储支持
    - [x] MySql
    - [x] PostgreSql
    - [x] SqlServer
    - [ ] Oracle
    - [ ] MongoDb
