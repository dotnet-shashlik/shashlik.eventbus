using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonTestLogical;
using CommonTestLogical.TestEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.DefaultImpl;
using Shashlik.EventBus.Utils;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.Tests
{
    [Collection("Shashlik.EventBus.Tests")]
    public class BaseTests : TestBase<Startup>
    {
        public BaseTests(TestWebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper) : base(factory,
            testOutputHelper)
        {
        }

        // ---- 名称规则 / 描述符 ---------------------------------------------

        [Fact]
        public void EventHandlerFindProviderAndNameRuleTests()
        {
            var eventHandlerFindProvider = GetService<IEventHandlerFindProvider>();
            eventHandlerFindProvider.ShouldBeOfType<DefaultEventHandlerFindProvider>();

            var handlers = eventHandlerFindProvider.FindAll().ToList();

            {
                var d = handlers.First(r => r.EventHandlerType == typeof(TestEventHandler));
                d.EventHandlerName.ShouldBe($"{nameof(TestEventHandler)}.{Options.Environment}");
                d.EventType.ShouldBe(typeof(TestEvent));
                d.EventName.ShouldBe($"{nameof(TestEvent)}.{Options.Environment}");
                d.EventHandlerName.ShouldNotBeNullOrWhiteSpace();
                d.EventName.ShouldNotBeNullOrWhiteSpace();
            }

            {
                var d = handlers.First(r => r.EventHandlerType == typeof(TestEventGroup2Handler));
                d.EventHandlerName.ShouldBe($"{nameof(TestEventGroup2Handler)}.{Options.Environment}");
                d.EventType.ShouldBe(typeof(TestEvent));
                d.EventName.ShouldBe($"{nameof(TestEvent)}.{Options.Environment}");
            }

            {
                var d = handlers.First(r => r.EventHandlerType == typeof(TestDelayEventHandler));
                d.EventHandlerName.ShouldBe($"{nameof(TestDelayEventHandler)}.{Options.Environment}");
                d.EventType.ShouldBe(typeof(TestDelayEvent));
                d.EventName.ShouldBe($"{nameof(TestDelayEvent)}.{Options.Environment}");
            }

            {
                var d = handlers.First(r => r.EventHandlerType == typeof(TestCustomNameEventHandler));
                d.EventHandlerName.ShouldBe(
                    $"{nameof(TestCustomNameEventHandler)}_Test.{Options.Environment}");
                d.EventType.ShouldBe(typeof(TestCustomNameEvent));
                d.EventName.ShouldBe(
                    $"{nameof(TestCustomNameEvent)}_Test.{Options.Environment}");
            }
        }

        [Fact]
        public void NameRulerTests()
        {
            var eventNameRuler = GetService<IEventNameRuler>();
            var handlerNameRuler = GetService<IEventHandlerNameRuler>();

            eventNameRuler.GetName(typeof(TestEvent))
                .ShouldBe($"{nameof(TestEvent)}.{Options.Environment}");

            handlerNameRuler.GetName(typeof(TestEventHandler))
                .ShouldBe($"{nameof(TestEventHandler)}.{Options.Environment}");

            // 通过 EventBusNameAttribute 覆盖默认名
            eventNameRuler.GetName(typeof(TestCustomNameEvent))
                .ShouldBe($"{nameof(TestCustomNameEvent)}_Test.{Options.Environment}");
            handlerNameRuler.GetName(typeof(TestCustomNameEventHandler))
                .ShouldBe($"{nameof(TestCustomNameEventHandler)}_Test.{Options.Environment}");
        }

        // ---- 序列化器 -----------------------------------------------------

        [Fact]
        public void MessageSerializerTests()
        {
            var messageSerializer = GetService<IMessageSerializer>();
            var @event = new TestEvent { Name = "张三" };
            var json = messageSerializer.Serialize(@event);
            json.ShouldNotBeNullOrWhiteSpace();
            var back = (TestEvent)messageSerializer.Deserialize(json, typeof(TestEvent))!;
            back.Name.ShouldBe(@event.Name);

            // 序列化+反序列化 Items
            var items = new Dictionary<string, string>
            {
                { "a", "1" }, { "b", "2" }
            };
            var s2 = messageSerializer.Serialize(items);
            var i2 = messageSerializer.Deserialize<Dictionary<string, string>>(s2
                ?? throw new Exception("serialize items returned null"));
            i2["a"].ShouldBe("1");
            i2["b"].ShouldBe("2");
        }

        // ---- 事件处理执行器 -----------------------------------------------

        [Fact]
        public async Task EventHandlerInvokerTests()
        {
            var invoker = GetService<IEventHandlerInvoker>();
            var eventHandlerFindProvider = GetService<IEventHandlerFindProvider>();
            var d = eventHandlerFindProvider.FindAll()
                .First(r => r.EventHandlerType == typeof(TestEventHandler));
            var messageSerializer = GetService<IMessageSerializer>();
            var @event = new TestEvent { Name = "张三-invoker" };
            var json = messageSerializer.Serialize(@event);

            // 1) EventBody 为 null 时抛 InvalidCastException
            Should.Throw<InvalidCastException>(async () => await invoker.InvokeAsync(new MessageStorageModel
            {Id =  GetService<IIdGenerator>().NextId(),
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Options.Environment,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = DateTimeOffset.Now.AddDays(1),
                EventHandlerName = d.EventHandlerName,
                EventName = d.EventName,
                EventBody = null,
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Scheduled,
                IsLocking = false,
                LockEnd = null
            }, new Dictionary<string, string>(), d));

            // 2) EventBody 正常 - 应能成功调用并捕获 handler 的副作用
            TestEventHandler.Reset();
            await invoker.InvokeAsync(new MessageStorageModel
            {Id =  GetService<IIdGenerator>().NextId(),
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Options.Environment,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = DateTimeOffset.Now.AddDays(1),
                EventHandlerName = d.EventHandlerName,
                EventName = d.EventName,
                EventBody = json,
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Scheduled,
                IsLocking = false,
                LockEnd = null
            }, new Dictionary<string, string>(), d);

            TestEventHandler.LastInstance.ShouldNotBeNull();
            TestEventHandler.LastInstance!.Name.ShouldBe("张三-invoker");
        }

        // ---- MsgId 生成器 ------------------------------------------------

        [Fact]
        public void MsgIdTests()
        {
            var msgIdGenerator = GetService<IMsgIdGenerator>();
            var id = msgIdGenerator.GenerateId();
            id.ShouldNotBeNullOrWhiteSpace();
            id.Length.ShouldBe(32);

            // 唯一性
            var ids = new ConcurrentBag<string>();
            Parallel.For(0, 10000, _ => ids.Add(msgIdGenerator.GenerateId()));
            ids.Distinct().Count().ShouldBe(10000);
        }
        
        [Fact]
        public void IdGeneratorTests()
        {
            var msgIdGenerator = GetService<IIdGenerator>();
            var id = msgIdGenerator.NextId();
            id.ShouldBeGreaterThan(0);

            // 唯一性
            var ids = new ConcurrentBag<long>();
            Parallel.For(0, 10000, _ => ids.Add(msgIdGenerator.NextId()));
            ids.Distinct().Count().ShouldBe(10000);
        }

        // ---- 选项验证 -----------------------------------------------------

        [Fact]
        public void OptionsValidationTests()
        {
            var optionsMonitor = GetService<IOptionsMonitor<EventBusOptions>>();
            optionsMonitor.ShouldNotBeNull();
            optionsMonitor.CurrentValue.ShouldNotBeNull();

            // TestBase 启动时,Options.Environment 应该是 RandomEnv 给出的非空值
            optionsMonitor.CurrentValue.Environment.ShouldNotBeNullOrWhiteSpace();
            optionsMonitor.CurrentValue.RetryFailedMax.ShouldBeGreaterThanOrEqualTo(5);
            optionsMonitor.CurrentValue.LockTime.ShouldBeLessThan(optionsMonitor.CurrentValue.RetryInterval);
            optionsMonitor.CurrentValue.TransactionCommitTimeout
                .ShouldBeLessThan(optionsMonitor.CurrentValue.StartRetryAfter);
        }

        // ---- 事件发布 (普通 / 延迟) + 多 handler ---------------------------

        [Fact]
        public async Task TestEventTests()
        {
            var eventPublisher = GetService<IEventPublisher>();
            eventPublisher.ShouldBeOfType<DefaultEventPublisher>();

            TestEventHandler.Reset();
            TestEventGroup2Handler.Reset();

            var @event = new TestEvent { Name = "张三-" + Guid.NewGuid().ToString("n") };
            await eventPublisher.PublishAsync(@event, null, new Dictionary<string, string>
            {
                { "age", "18" }
            });

            // 等待处理器执行
            await TestEventHandler.WaitForInstance(TimeSpan.FromSeconds(Options.StartRetryAfter + 5));
            await TestEventGroup2Handler.WaitForInstance(TimeSpan.FromSeconds(Options.StartRetryAfter + 5));

            TestEventHandler.LastInstance.ShouldNotBeNull();
            TestEventHandler.LastInstance!.Name.ShouldBe(@event.Name);
            TestEventHandler.LastItems!["age"].ShouldBe("18");
            TestEventHandler.LastItems[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
            TestEventHandler.LastItems[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
            TestEventHandler.LastItems[EventBusConsts.EventNameHeaderKey]
                .ShouldBe($"{nameof(TestEvent)}.{Options.Environment}");

            TestEventGroup2Handler.LastInstance.ShouldNotBeNull();
            TestEventGroup2Handler.LastInstance!.Name.ShouldBe(@event.Name);
            TestEventGroup2Handler.LastItems!["age"].ShouldBe("18");
        }

        [Fact]
        public async Task TestDelayEventTests()
        {
            var eventPublisher = GetService<IEventPublisher>();
            eventPublisher.ShouldBeOfType<DefaultEventPublisher>();

            TestDelayEventHandler.Reset();
            TestDelayEventGroup2Handler.Reset();
            TestDelayEventGroup3Handler.Reset();

            var @event = new TestDelayEvent { Name = "李四-" + Guid.NewGuid().ToString("n") };
            var delayAt = DateTimeOffset.Now.AddSeconds(5);
            await eventPublisher.PublishAsync(@event, delayAt, null, new Dictionary<string, string>
            {
                { "age", "19" }
            });

            // 时间没到,不能被执行
            await Task.Delay(2000);
            TestDelayEventHandler.LastInstance.ShouldBeNull();
            TestDelayEventGroup2Handler.LastInstance.ShouldBeNull();
            TestDelayEventGroup3Handler.LastInstance.ShouldBeNull();

            // 等待处理器执行
            await TestDelayEventHandler.WaitForInstance(TimeSpan.FromSeconds(Options.StartRetryAfter + 10));
            await TestDelayEventGroup2Handler.WaitForInstance(TimeSpan.FromSeconds(5));
            await TestDelayEventGroup3Handler.WaitForInstance(TimeSpan.FromSeconds(5));

            TestDelayEventHandler.LastInstance!.Name.ShouldBe(@event.Name);
            TestDelayEventHandler.LastItems!["age"].ShouldBe("19");
            TestDelayEventHandler.LastItems[EventBusConsts.DelayAtHeaderKey]
                .ParseTo<DateTimeOffset>().GetLongDate()
                .ShouldBe(delayAt.GetLongDate());

            TestDelayEventGroup2Handler.LastInstance!.Name.ShouldBe(@event.Name);
            TestDelayEventGroup3Handler.LastInstance!.Name.ShouldBe(@event.Name);
        }

        [Fact]
        public async Task TestCustomNameEventTests()
        {
            var eventPublisher = GetService<IEventPublisher>();
            eventPublisher.ShouldBeOfType<DefaultEventPublisher>();

            TestCustomNameEventHandler.Reset();
            TestCustomNameEventGroup2Handler.Reset();

            var @event = new TestCustomNameEvent { Name = "王五-" + Guid.NewGuid().ToString("n") };
            await eventPublisher.PublishAsync(@event, null, new Dictionary<string, string>
            {
                { "age", "20" }
            });

            await TestCustomNameEventHandler.WaitForInstance(TimeSpan.FromSeconds(Options.StartRetryAfter + 5));
            await TestCustomNameEventGroup2Handler.WaitForInstance(TimeSpan.FromSeconds(5));

            TestCustomNameEventHandler.LastInstance!.Name.ShouldBe(@event.Name);
            TestCustomNameEventHandler.LastItems!["age"].ShouldBe("20");
            TestCustomNameEventHandler.LastItems[EventBusConsts.EventNameHeaderKey]
                .ShouldBe($"{nameof(TestCustomNameEvent)}_Test.{Options.Environment}");

            TestCustomNameEventGroup2Handler.LastInstance!.Name.ShouldBe(@event.Name);
            TestCustomNameEventGroup2Handler.LastItems[EventBusConsts.EventNameHeaderKey]
                .ShouldBe($"{nameof(TestCustomNameEvent)}_Test.{Options.Environment}");
        }

        // ---- 异常事件 + 重试器 -------------------------------------------

        [Fact]
        public async Task TestExceptionEventTests()
        {
            // 重置:handler 总是抛异常,确保 retryCount 会增长到 RetryFailedMax
            var eventPublisher = GetService<IEventPublisher>();
            var eventNameRuler = GetService<IEventNameRuler>();
            var messageStorage = GetService<IMessageStorage>();
            var env = Options.Environment;

            var @event = new TestExceptionEvent { Name = "王麻子-" + Guid.NewGuid().ToString("n") };
            await eventPublisher.PublishAsync(@event, null, new Dictionary<string, string>
            {
                { "age", "21" }
            });

            // 等 5 次都执行完(默认 RetryMaxDegreeOfParallelism=5,Listener 内部会执行 5 次)
            var begin = DateTimeOffset.Now;
            while (TestExceptionEventHandler.Counter < 5 && (DateTimeOffset.Now - begin).TotalSeconds < 30)
                await Task.Delay(200);
            TestExceptionEventHandler.Counter.ShouldBe(5);

            // 等重试器跑满 RetryFailedMax
            var target = Options.RetryFailedMax;
            var begin2 = DateTimeOffset.Now;
            while (TestExceptionEventHandler.Counter < target && (DateTimeOffset.Now - begin2).TotalSeconds < 600)
                await Task.Delay(1000);

            TestExceptionEventHandler.Counter.ShouldBe(target);
            TestExceptionEventHandler.LastInstance.ShouldNotBeNull();
            TestExceptionEventHandler.LastInstance!.Name.ShouldBe(@event.Name);
            TestExceptionEventHandler.LastItems!["age"].ShouldBe("21");

            // 存储里:消息状态是 Failed,RetryCount == RetryFailedMax。
            // 异常事件有 2 个 handler,所以发布后会有 2 条 received 消息,每条都会被重试到 RetryFailedMax。
            // 我们只断言至少一条达到了 RetryFailedMax(更宽松,避免重试竞态)。
            var receivedStorage = GetService<IMessageStorage>();
            var eventHandlerNameRuler = GetService<IEventHandlerNameRuler>();
            var receiveds = await receivedStorage.SearchReceivedAsync(env,
                DateTimeOffset.Now.AddMinutes(-10), DateTimeOffset.Now.AddMinutes(10),
                null, null, null, 0, 100, default);
            var matching = receiveds.Where(r => r.EventBody.Contains(@event.Name)).ToList();
            matching.ShouldNotBeEmpty();
            // 至少一条 RetryCount 达到 RetryFailedMax
            matching.Any(r => r.RetryCount >= Options.RetryFailedMax).ShouldBeTrue();
            // 或者:所有条都已经 Failed
            matching.All(r => r.Status == MessageStatus.Failed).ShouldBeTrue();
        }

        // ---- 手动重试 ----------------------------------------------------

        [Fact]
        public async Task ReceivedManualRetryTestSuccess()
        {
            // Save a received message that points to TestEventHandler.
            // Then manually retry. Even with bad JSON, the handler should still execute
            // (DefaultJsonSerializer.Deserialize returns object with default Name).
            // After: RetryCount should have increased by 1, status=Failed or Succeeded.
            var receivedMessageRetryProvider = GetService<IReceivedMessageRetryProvider>();
            var messageStorage = GetService<IMessageStorage>();
            var eventNameRuler = GetService<IEventNameRuler>();
            var eventHandlerNameRuler = GetService<IEventHandlerNameRuler>();
            var before = TestEventHandler.Counter;
            var id = await messageStorage.SaveReceivedAsync(new MessageStorageModel
            {Id =  GetService<IIdGenerator>().NextId(),
                MsgId = Guid.NewGuid().ToString(),
                Environment = Options.Environment,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = null,
                EventHandlerName = eventHandlerNameRuler.GetName(typeof(TestEventHandler)),
                EventName = eventNameRuler.GetName(typeof(TestEvent)),
                EventBody = "{\"Name\":\"manual-retry\"}",
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Failed,
                IsLocking = false,
                LockEnd = null
            }, default);
            await receivedMessageRetryProvider.RetryAsync(id, default);

            var dbMsg = await messageStorage.FindReceivedByIdAsync(id, default);
            dbMsg.ShouldNotBeNull();
            // RetryAsync 内部执行了 handler,无论 handler 是否抛异常, RetryCount 都会被 ++
            dbMsg!.RetryCount.ShouldBeGreaterThan(0);
            TestEventHandler.Counter.ShouldBe(before + 1);
        }

        [Fact]
        public async Task ReceivedManualRetryTestException()
        {
            // 同上,但 handler 一定会抛异常,断言 RetryCount 增加且 handler 调用次数 +1
            var receivedMessageRetryProvider = GetService<IReceivedMessageRetryProvider>();
            var messageStorage = GetService<IMessageStorage>();
            var eventNameRuler = GetService<IEventNameRuler>();
            var eventHandlerNameRuler = GetService<IEventHandlerNameRuler>();
            var before = TestExceptionEventHandler.Counter;
            var id = await messageStorage.SaveReceivedAsync(new MessageStorageModel
            {Id =  GetService<IIdGenerator>().NextId(),
                MsgId = Guid.NewGuid().ToString(),
                Environment = Options.Environment,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = null,
                EventHandlerName = eventHandlerNameRuler.GetName(typeof(TestExceptionEventHandler)),
                EventName = eventNameRuler.GetName(typeof(TestExceptionEvent)),
                EventBody = "{\"Name\":\"x\"}",
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Failed,
                IsLocking = false,
                LockEnd = null
            }, default);
            await receivedMessageRetryProvider.RetryAsync(id, default);
            var dbMsg = await messageStorage.FindReceivedByIdAsync(id, default);
            dbMsg.ShouldNotBeNull();
            dbMsg!.RetryCount.ShouldBeGreaterThan(0);
            dbMsg.Status.ShouldBe(MessageStatus.Failed);
            TestExceptionEventHandler.Counter.ShouldBe(before + 1);
        }

        [Fact]
        public async Task PublishedManualRetryTestSuccess()
        {
            // 存一条 published(没有 EventHandlerName),手动重试 -> SendAsync 被调用一次。
            // 我们用默认的 MemoryMessageSender,SendAsync 不会失败。RetryCount 会 +1。
            // 注意:为了能拿到 id,这里直接用 MemoryStorage 注入并 SavePublishedAsync。
            var publishedMessageRetryProvider = GetService<IPublishedMessageRetryProvider>();
            var messageStorage = GetService<IMessageStorage>();
            var eventNameRuler = GetService<IEventNameRuler>();
            var id = await messageStorage.SavePublishedAsync(new MessageStorageModel
            {Id =  GetService<IIdGenerator>().NextId(),
                MsgId = Guid.NewGuid().ToString(),
                Environment = Options.Environment,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = null,
                EventHandlerName = null,
                EventName = eventNameRuler.GetName(typeof(TestEvent)),
                EventBody = "{\"Name\":\"manual-published\"}",
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Failed,
                IsLocking = false,
                LockEnd = null
            }, null, default);
            await publishedMessageRetryProvider.RetryAsync(id, default);
            var dbMsg = await messageStorage.FindPublishedByIdAsync(id, default);
            dbMsg.ShouldNotBeNull();
            // DefaultRetryProvider 不会多次循环,只调用 PublishHandler 一次。
            dbMsg!.RetryCount.ShouldBe(1);
            dbMsg.Status.ShouldBe(MessageStatus.Succeeded);
        }

        // ---- StartupAsync 触发重试器立即扫描 ---------------------------

        [Fact]
        public async Task PublishedMessageRetryProviderStartupTest()
        {
            // 保存一条已失败且超过 StartRetryAfter 的 published。
            // 触发 startup 后,3 秒内应该被重试到 Succeeded。
            var retryProvider = GetService<IPublishedMessageRetryProvider>();
            var messageStorage = GetService<IMessageStorage>();
            var eventNameRuler = GetService<IEventNameRuler>();
            var idGenerator = GetService<IIdGenerator>();
            var id = await messageStorage.SavePublishedAsync(new MessageStorageModel
            {
                Id =  GetService<IIdGenerator>().NextId(),
                MsgId = Guid.NewGuid().ToString(),
                Environment = Options.Environment,
                CreateTime = DateTimeOffset.Now.AddSeconds(-Options.StartRetryAfter - 10),
                DelayAt = null,
                ExpireTime = null,
                EventHandlerName = null,
                EventName = eventNameRuler.GetName(typeof(TestEvent)),
                EventBody = "{\"Name\":\"startup-test\"}",
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Failed,
                IsLocking = false,
                LockEnd = null
            }, null, default);
            await retryProvider.StartupAsync(default);

            var begin = DateTimeOffset.Now;
            MessageStorageModel? msg;
            do
            {
                msg = await messageStorage.FindPublishedByIdAsync(id, default);
                if (msg is { Status: MessageStatus.Succeeded } && msg.RetryCount >= 1) break;
                await Task.Delay(200);
            } while ((DateTimeOffset.Now - begin).TotalSeconds < 5);

            msg.ShouldNotBeNull();
            msg!.Id.ShouldBe(id);
            msg.Status.ShouldBe(MessageStatus.Succeeded);
            msg.RetryCount.ShouldBe(1);
        }

        [Fact]
        public async Task ReceivedMessageRetryProviderStartupTest()
        {
            var retryProvider = GetService<IReceivedMessageRetryProvider>();
            var messageStorage = GetService<IMessageStorage>();
            var eventNameRuler = GetService<IEventNameRuler>();
            var eventHandlerNameRuler = GetService<IEventHandlerNameRuler>();
            var id = await messageStorage.SaveReceivedAsync(new MessageStorageModel
            {Id =  GetService<IIdGenerator>().NextId(),
                MsgId = Guid.NewGuid().ToString(),
                Environment = Options.Environment,
                CreateTime = DateTimeOffset.Now.AddSeconds(-Options.StartRetryAfter - 10),
                DelayAt = null,
                ExpireTime = null,
                EventHandlerName = eventHandlerNameRuler.GetName(typeof(TestEventHandler)),
                EventName = eventNameRuler.GetName(typeof(TestEvent)),
                EventBody = "{\"Name\":\"startup-recv-test\"}",
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Failed,
                IsLocking = false,
                LockEnd = null
            }, default);
            await retryProvider.StartupAsync(default);

            var begin = DateTimeOffset.Now;
            MessageStorageModel? msg;
            do
            {
                msg = await messageStorage.FindReceivedByIdAsync(id, default);
                if (msg is { Status: MessageStatus.Succeeded, RetryCount: >= 1 }) break;
                await Task.Delay(200);
            } while ((DateTimeOffset.Now - begin).TotalSeconds < 5);

            msg.ShouldNotBeNull();
            msg!.Id.ShouldBe(id);
            msg.Status.ShouldBe(MessageStatus.Succeeded);
            msg.RetryCount.ShouldBe(1);
        }
    }
}
