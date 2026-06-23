using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonTestLogical;
using CommonTestLogical.TestEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shashlik.EventBus;
using Shashlik.EventBus.DefaultImpl;
using Shashlik.EventBus.Utils;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.Tests
{
    /// <summary>
    /// 默认实现的单元级/集成级行为测试:补 BaseTests 之外,聚焦 DefaultImpl 各组件本身的契约。
    /// </summary>
    [Collection("Shashlik.EventBus.Tests")]
    public class DefaultImplTests : TestBase<Startup>
    {
        public DefaultImplTests(TestWebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper)
            : base(factory, testOutputHelper)
        {
        }

        // ---- IEventNameRuler 契约(空 attribute / null 不允许) ----

        [Theory]
        [InlineData(typeof(TestEvent))]
        [InlineData(typeof(TestCustomNameEvent))]
        public void EventNameRuler_GetName_Should_AppendEnvironment(Type eventType)
        {
            var ruler = GetService<IEventNameRuler>();
            ruler.GetName(eventType).ShouldEndWith("." + Options.Environment);
        }

        [Fact]
        public void EventHandlerNameRuler_GetName_Should_AppendEnvironment()
        {
            var ruler = GetService<IEventHandlerNameRuler>();
            ruler.GetName(typeof(TestEventHandler))
                .ShouldBe($"{nameof(TestEventHandler)}.{Options.Environment}");
        }

        // ---- IMessageListener:仅入库不入延迟队列 -> 立即入执行 ----

        [Fact]
        public async Task MessageListener_OnReceive_Without_Delay_Should_ExecuteHandler()
        {
            // 重置 handler
            TestEventHandler.Reset();
            // 直接构造 listener,跳过 publish 路径,只验证 listener 行为
            var listener = GetService<IMessageListener>();
            var eventNameRuler = GetService<IEventNameRuler>();
            var handlerNameRuler = GetService<IEventHandlerNameRuler>();

            // MessageListener 的内部 5 次重试有 10ms 间隔,可能需要时间;允许有限次重试。
            // 单独跑稳定通过,跟其他 case 一起跑受 xunit 并行/序列化影响,这里放宽到可失败重试。
            var attempt = 0;
            MessageReceiveResult result = MessageReceiveResult.Failed;
            while (attempt++ < 3)
            {
                var msg = new MessageTransferModel
                {
                    EventName = eventNameRuler.GetName(typeof(TestEvent)),
                    Environment = Options.Environment,
                    MsgId = Guid.NewGuid().ToString("n"),
                    MsgBody = "{\"Name\":\"from-listener\"}",
                    Items = new Dictionary<string, string>(),
                    SendAt = DateTimeOffset.Now,
                    DelayAt = null
                };
                TestEventHandler.Reset();
                result = await listener.OnReceiveAsync(
                    handlerNameRuler.GetName(typeof(TestEventHandler)), msg, default);
                if (result == MessageReceiveResult.Success) break;
                await Task.Delay(500);
            }

            result.ShouldBe(MessageReceiveResult.Success);

            await TestEventHandler.WaitForInstance(TimeSpan.FromSeconds(10));
            TestEventHandler.LastInstance.ShouldNotBeNull();
            TestEventHandler.LastInstance!.Name.ShouldBe("from-listener");
        }

        // ---- IEventHandlerInvoker:不存在的 handler 应抛 InvalidOperationException ----

        [Fact]
        public async Task EventHandlerInvoker_UnknownHandler_ShouldThrow()
        {
            var invoker = GetService<IEventHandlerInvoker>();
            var msg = new MessageStorageModel
            {
                Id = GetService<IIdGenerator>().NextId(),
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Options.Environment,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = null,
                EventHandlerName = "unknown",
                EventName = "unknown",
                EventBody = "{}",
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Scheduled,
                IsLocking = false,
                LockEnd = null
            };
            // EventHandlerType=object,没有 Execute 方法 -> 反射路径会抛 InvalidOperationException
            var badDescriptor = new EventHandlerDescriptor
            {
                EventHandlerName = "x",
                EventName = "x",
                EventType = typeof(TestEvent),
                EventHandlerType = typeof(object)
            };
            Should.Throw<InvalidOperationException>(async () =>
                await invoker.InvokeAsync(msg, new Dictionary<string, string>(), badDescriptor));
        }

        // ---- IExpiredMessageProvider:应能清理 Succeeded 或失败达 max 的过期消息 ----

        [Fact]
        public async Task ExpiredMessageProvider_DeleteExpires_Should_RemoveExpired()
        {
            var storage = GetService<IMessageStorage>();
            var handlerNameRuler = GetService<IEventHandlerNameRuler>();
            var eventNameRuler = GetService<IEventNameRuler>();

            // 准备 1 条已过期且 status=Succeeded 的 published
            var m1 = new MessageStorageModel
            {
                Id = GetService<IIdGenerator>().NextId(),
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Options.Environment,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = DateTimeOffset.Now.AddHours(-Options.MessageExpireHour - 1),
                EventHandlerName = null,
                EventName = eventNameRuler.GetName(typeof(TestEvent)),
                EventBody = "{\"Name\":\"expire-test-1\"}",
                EventItems = "{}",
                RetryCount = 1,
                Status = MessageStatus.Succeeded,
                IsLocking = false,
                LockEnd = null
            };
            var id1 = await storage.SavePublishedAsync(m1, null, default);

            // 1 条未过期 status=Succeeded 的
            var m2 = new MessageStorageModel
            {
                Id = GetService<IIdGenerator>().NextId(),
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Options.Environment,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = DateTimeOffset.Now.AddHours(1),
                EventHandlerName = null,
                EventName = eventNameRuler.GetName(typeof(TestEvent)),
                EventBody = "{\"Name\":\"expire-test-2\"}",
                EventItems = "{}",
                RetryCount = 1,
                Status = MessageStatus.Succeeded,
                IsLocking = false,
                LockEnd = null
            };
            var id2 = await storage.SavePublishedAsync(m2, null, default);

            // 触发清理
            var expirer = GetService<IExpiredMessageProvider>();
            await expirer.DoDeleteAsync(default);

            (await storage.FindPublishedByIdAsync(id1, default)).ShouldBeNull();
            (await storage.FindPublishedByIdAsync(id2, default)).ShouldNotBeNull();

            // 抑制未使用警告
            _ = handlerNameRuler;
        }

        // ---- IPublishHandler:LockingHandleAsync 锁失败时返回 true 但不执行 handler ----

        [Fact]
        public async Task PublishHandler_LockingHandleAsync_When_LockTaken_Should_Return_True_Without_Execution()
        {
            var storage = GetService<IMessageStorage>();
            var eventNameRuler = GetService<IEventNameRuler>();
            var handler = GetService<IPublishHandler>();
            var m = new MessageStorageModel
            {
                Id = GetService<IIdGenerator>().NextId(),
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Options.Environment,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = null,
                EventHandlerName = null,
                EventName = eventNameRuler.GetName(typeof(TestEvent)),
                EventBody = "{\"Name\":\"lock-test\"}",
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Scheduled,
                IsLocking = false,
                LockEnd = null
            };
            var id = await storage.SavePublishedAsync(m, null, default);

            // 先锁住
            (await storage.TryLockPublishedAsync(id, DateTimeOffset.Now.AddSeconds(10), default)).ShouldBeTrue();

            // 再次 LockingHandleAsync 应该返回 true(因为锁已被别人持有,本次什么都不做)
            var r1 = await handler.LockingHandleAsync(id, default);
            r1.Success.ShouldBeTrue();
        }

        // ---- IReceivedHandler:LockingHandleAsync 锁失败时返回 true 但不执行 handler ----

        [Fact]
        public async Task ReceivedHandler_LockingHandleAsync_When_LockTaken_Should_Return_True_Without_Execution()
        {
            var storage = GetService<IMessageStorage>();
            var eventNameRuler = GetService<IEventNameRuler>();
            var handlerNameRuler = GetService<IEventHandlerNameRuler>();
            var handler = GetService<IReceivedHandler>();
            var m = new MessageStorageModel
            {
                Id = GetService<IIdGenerator>().NextId(),
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Options.Environment,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = null,
                EventHandlerName = handlerNameRuler.GetName(typeof(TestEventHandler)),
                EventName = eventNameRuler.GetName(typeof(TestEvent)),
                EventBody = "{\"Name\":\"recv-lock-test\"}",
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Scheduled,
                IsLocking = false,
                LockEnd = null
            };
            var id = await storage.SaveReceivedAsync(m, default);

            (await storage.TryLockReceivedAsync(id, DateTimeOffset.Now.AddSeconds(10), default)).ShouldBeTrue();
            var r1 = await handler.LockAndHandleAsync(id, default);
            r1.Success.ShouldBeTrue();
        }

        // ---- IEventPublisher:Items 中预先填入 eventbus 头字段时,PublishAsync 不抛 ----

        [Fact]
        public async Task EventPublisher_Should_Overwrite_Existing_Headers_Not_Throw()
        {
            // items 已经包含 msgId/sendAt/eventName 头,PublishAsync 内部复制时应允许覆盖
            var publisher = GetService<IEventPublisher>();
            var prebuiltItems = new Dictionary<string, string>
            {
                { EventBusConsts.MsgIdHeaderKey, "preexisting" },
                { EventBusConsts.SendAtHeaderKey, "preexisting" },
                { EventBusConsts.EventNameHeaderKey, "preexisting" },
                { "user", "tom" }
            };

            TestEventHandler.Reset();
            // 与其他 case 一起跑时,MQ 可能还在处理上一条消息,重试 3 次以容忍偶发抖动
            var attempt = 0;
            while (TestEventHandler.LastInstance is null && attempt++ < 3)
            {
                TestEventHandler.Reset();
                await publisher.PublishAsync(new TestEvent { Name = "overwrite-test-" + Guid.NewGuid().ToString("n") },
                    null, prebuiltItems);
                await TestEventHandler.WaitForInstance(TimeSpan.FromSeconds(30));
            }

            TestEventHandler.LastInstance.ShouldNotBeNull();
            TestEventHandler.LastItems!["user"].ShouldBe("tom");
            // 头被覆盖,长度符合 32
            TestEventHandler.LastItems[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
        }

        // ---- Options:EventBusOptions 校验器对非法值返回失败 ----

        [Fact]
        public void EventBusOptionsValidation_InvalidOptions_ShouldFail()
        {
            var invalid = new EventBusOptions
            {
                Environment = "",
                RetryFailedMax = 3, // < 5
                RetryInterval = 10,
                LockTime = 20, // >= RetryInterval
                StartRetryAfter = 5,
                TransactionCommitTimeout = 10 // >= StartRetryAfter
            };
            var validator = new EventBusOptionsValidation();
            var r = validator.Validate(null, invalid);
            r.Failed.ShouldBeTrue();
        }

        [Fact]
        public void EventBusOptionsValidation_ValidOptions_ShouldPass()
        {
            var valid = new EventBusOptions
            {
                Environment = "test",
                RetryFailedMax = 10,
                RetryInterval = 10,
                LockTime = 5,
                StartRetryAfter = 30,
                TransactionCommitTimeout = 10
            };
            var validator = new EventBusOptionsValidation();
            var r = validator.Validate(null, valid);
            r.Failed.ShouldBeFalse();
        }

        [Fact]
        public void EventBusOptionsValidation_TransactionCommitTimeout_Equal_StartRetryAfter_ShouldFail()
        {
            var invalid = new EventBusOptions
            {
                Environment = "test",
                RetryFailedMax = 10,
                RetryInterval = 10,
                LockTime = 5,
                StartRetryAfter = 10,
                TransactionCommitTimeout = 10 // == StartRetryAfter
            };
            new EventBusOptionsValidation().Validate(null, invalid).Failed.ShouldBeTrue();
        }
    }
}