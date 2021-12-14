using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonTestLogical;
using CommonTestLogical.TestEvents;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.DefaultImpl;
using Shashlik.Utils.Extensions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.Tests
{
    public class BaseTests : TestBase<Startup>
    {
        public BaseTests(TestWebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper) : base(factory, testOutputHelper)
        {
        }

        [Fact]
        public void EventHandlerFindProviderAndNameRuleTests()
        {
            var eventHandlerFindProvider = GetService<IEventHandlerFindProvider>();
            eventHandlerFindProvider.ShouldBeOfType<DefaultEventHandlerFindProvider>();

            var handlers = eventHandlerFindProvider.FindAll().ToList();

            {
                var testEventHandlerDescriptor = handlers.First(r => r.EventHandlerType == typeof(TestEventHandler));
                testEventHandlerDescriptor.EventHandlerName.ShouldBe($"{nameof(TestEventHandler)}.{Options.Environment}");
                testEventHandlerDescriptor.EventType.ShouldBe(typeof(TestEvent));
                testEventHandlerDescriptor.EventName.ShouldBe($"{nameof(TestEvent)}.{Options.Environment}");
                // testEventHandlerDescriptor.IsDelay.ShouldBeFalse();
            }

            {
                var testEventHandlerDescriptor = handlers.First(r => r.EventHandlerType == typeof(TestEventGroup2Handler));
                testEventHandlerDescriptor.EventHandlerName.ShouldBe($"{nameof(TestEventGroup2Handler)}.{Options.Environment}");
                testEventHandlerDescriptor.EventType.ShouldBe(typeof(TestEvent));
                testEventHandlerDescriptor.EventName.ShouldBe($"{nameof(TestEvent)}.{Options.Environment}");
                // testEventHandlerDescriptor.IsDelay.ShouldBeFalse();
            }

            {
                var testEventHandlerDescriptor = handlers.First(r => r.EventHandlerType == typeof(TestDelayEventHandler));
                testEventHandlerDescriptor.EventHandlerName.ShouldBe($"{nameof(TestDelayEventHandler)}.{Options.Environment}");
                testEventHandlerDescriptor.EventType.ShouldBe(typeof(TestDelayEvent));
                testEventHandlerDescriptor.EventName.ShouldBe($"{nameof(TestDelayEvent)}.{Options.Environment}");
                // testEventHandlerDescriptor.IsDelay.ShouldBeTrue();
            }

            {
                var testEventHandlerDescriptor = handlers.First(r => r.EventHandlerType == typeof(TestCustomNameEventHandler));
                testEventHandlerDescriptor.EventHandlerName.ShouldBe($"{nameof(TestCustomNameEventHandler)}_Test.{Options.Environment}");
                testEventHandlerDescriptor.EventType.ShouldBe(typeof(TestCustomNameEvent));
                testEventHandlerDescriptor.EventName.ShouldBe($"{nameof(TestCustomNameEvent)}_Test.{Options.Environment}");
                // testEventHandlerDescriptor.IsDelay.ShouldBeFalse();
            }
        }

        [Fact]
        public void MessageSerializerTests()
        {
            var messageSerializer = GetService<IMessageSerializer>();
            var @event = new TestEvent { Name = "张三" };
            var json = messageSerializer.Serialize(@event);
            messageSerializer.Deserialize<TestEvent>(json).Name.ShouldBe(@event.Name);
        }

        [Fact]
        public void EventHandlerInvokerTests()
        {
            var invoker = GetService<IEventHandlerInvoker>();
            var eventHandlerFindProvider = GetService<IEventHandlerFindProvider>();
            var testEventHandlerDescriptor = eventHandlerFindProvider.FindAll().First(r => r.EventHandlerType == typeof(TestEventHandler));
            var messageSerializer = GetService<IMessageSerializer>();
            var @event = new TestEvent { Name = "张三" };
            var json = messageSerializer.Serialize(@event);

            {
                Should.Throw<InvalidCastException>(() => invoker.InvokeAsync(new MessageStorageModel
                {
                    MsgId = Guid.NewGuid().ToString("n"),
                    Environment = Options.Environment,
                    CreateTime = DateTimeOffset.Now,
                    DelayAt = null,
                    ExpireTime = DateTimeOffset.Now.AddDays(1),
                    EventHandlerName = testEventHandlerDescriptor.EventHandlerName,
                    EventName = testEventHandlerDescriptor.EventName,
                    EventBody = null,
                    EventItems = "{}",
                    RetryCount = 0,
                    Status = MessageStatus.Scheduled,
                    IsLocking = false,
                    LockEnd = null
                }, new Dictionary<string, string>(), testEventHandlerDescriptor));
            }

            invoker.InvokeAsync(new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Options.Environment,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = DateTimeOffset.Now.AddDays(1),
                EventHandlerName = testEventHandlerDescriptor.EventHandlerName,
                EventName = testEventHandlerDescriptor.EventName,
                EventBody = json,
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Scheduled,
                IsLocking = false,
                LockEnd = null
            }, new Dictionary<string, string>(), testEventHandlerDescriptor);

            TestEventHandler.Instance.Name.ShouldBe(@event.Name);
        }

        [Fact]
        public void MsgIdTests()
        {
            var msgIdGenerator = GetService<IMsgIdGenerator>();
            msgIdGenerator.GenerateId().Length.ShouldBe(32);
            ConcurrentBag<string> list = new ConcurrentBag<string>();
            Parallel.For(0, 100000, item => { list.Add(msgIdGenerator.GenerateId()); });
            list.Distinct().Count().ShouldBe(100000);
        }

        [Fact]
        public async Task TestEventTests()
        {
            var beginTime = DateTimeOffset.Now;

            var eventPublisher = GetService<IEventPublisher>();
            eventPublisher.ShouldBeOfType<DefaultEventPublisher>();
            var @event = new TestEvent { Name = "张三" };
            await eventPublisher.PublishAsync(@event, null, new Dictionary<string, string>
            {
                { "age", "18" }
            });

            await Task.Delay(60 * 1000);

            TestEventHandler.Instance.ShouldNotBeNull();
            TestEventGroup2Handler.Instance.ShouldNotBeNull();

            TestEventHandler.Instance.Name.ShouldBe(@event.Name);
            TestEventHandler.Items["age"].ShouldBe("18");
            TestEventHandler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
            TestEventHandler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
            TestEventHandler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestEvent)}.{Options.Environment}");

            TestEventGroup2Handler.Instance.Name.ShouldBe(@event.Name);
            TestEventGroup2Handler.Items["age"].ShouldBe("18");
            TestEventGroup2Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
            TestEventGroup2Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
            TestEventGroup2Handler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestEvent)}.{Options.Environment}");
        }

        [Fact]
        public async Task TestDelayEventTests()
        {
            var beginTime = DateTimeOffset.Now;

            var eventPublisher = GetService<IEventPublisher>();
            eventPublisher.ShouldBeOfType<DefaultEventPublisher>();
            var @event = new TestDelayEvent { Name = "李四" };
            var delayAt = DateTimeOffset.Now.AddSeconds(10);
            await eventPublisher.PublishAsync(@event, delayAt, null, new Dictionary<string, string>
            {
                { "age", "19" }
            });

            // 时间没到，不能被执行
            while (DateTimeOffset.Now < delayAt.AddSeconds(-1))
            {
                TestDelayEventHandler.Instance.ShouldBeNull();
                TestDelayEventGroup2Handler.Instance.ShouldBeNull();
                TestDelayEventGroup3Handler.Instance.ShouldBeNull();
            }

            await Task.Delay(Options.StartRetryAfter * 1000);


            TestDelayEventHandler.Instance.ShouldNotBeNull();
            TestDelayEventGroup2Handler.Instance.ShouldNotBeNull();
            TestDelayEventGroup3Handler.Instance.ShouldNotBeNull();


            TestDelayEventHandler.Instance.Name.ShouldBe(@event.Name);
            TestDelayEventHandler.Items["age"].ShouldBe("19");
            TestDelayEventHandler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
            TestDelayEventHandler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
            TestDelayEventHandler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestDelayEvent)}.{Options.Environment}");
            TestDelayEventHandler.Items[EventBusConsts.DelayAtHeaderKey].ParseTo<DateTimeOffset>().GetLongDate()
                .ShouldBe(delayAt.GetLongDate());

            TestDelayEventGroup2Handler.Instance.Name.ShouldBe(@event.Name);
            TestDelayEventGroup2Handler.Items["age"].ShouldBe("19");
            TestDelayEventGroup2Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
            TestDelayEventGroup2Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
            TestDelayEventGroup2Handler.Items[EventBusConsts.EventNameHeaderKey]
                .ShouldBe($"{nameof(TestDelayEvent)}.{Options.Environment}");
            TestDelayEventGroup2Handler.Items[EventBusConsts.DelayAtHeaderKey].ParseTo<DateTimeOffset>().GetLongDate()
                .ShouldBe(delayAt.GetLongDate());

            TestDelayEventGroup3Handler.Instance.Name.ShouldBe(@event.Name);
            TestDelayEventGroup3Handler.Items["age"].ShouldBe("19");
            TestDelayEventGroup3Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
            TestDelayEventGroup3Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
            TestDelayEventGroup3Handler.Items[EventBusConsts.EventNameHeaderKey]
                .ShouldBe($"{nameof(TestDelayEvent)}.{Options.Environment}");
            TestDelayEventGroup3Handler.Items[EventBusConsts.DelayAtHeaderKey].ParseTo<DateTimeOffset>().GetLongDate()
                .ShouldBe(delayAt.GetLongDate());
        }

        [Fact]
        public async Task TestCustomNameEventTests()
        {
            var beginTime = DateTimeOffset.Now;

            var eventPublisher = GetService<IEventPublisher>();
            eventPublisher.ShouldBeOfType<DefaultEventPublisher>();
            var @event = new TestCustomNameEvent { Name = "王五" };
            await eventPublisher.PublishAsync(@event, null, new Dictionary<string, string>
            {
                { "age", "20" }
            });

            // 1分钟之内必须被执行
            while ((DateTimeOffset.Now - beginTime).TotalSeconds <= Options.StartRetryAfter)
            {
                if (TestCustomNameEventHandler.Instance is null || TestCustomNameEventGroup2Handler.Instance is null
                                                                || TestCustomNameEventHandler.Items is null ||
                                                                TestCustomNameEventGroup2Handler.Items is null)
                    continue;
                TestCustomNameEventHandler.Instance.Name.ShouldBe(@event.Name);
                TestCustomNameEventHandler.Items["age"].ShouldBe("20");
                TestCustomNameEventHandler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestCustomNameEventHandler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestCustomNameEventHandler.Items[EventBusConsts.EventNameHeaderKey]
                    .ShouldBe($"{nameof(TestCustomNameEvent)}_Test.{Options.Environment}");

                TestCustomNameEventGroup2Handler.Instance.Name.ShouldBe(@event.Name);
                TestCustomNameEventGroup2Handler.Items["age"].ShouldBe("20");
                TestCustomNameEventGroup2Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestCustomNameEventGroup2Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestCustomNameEventGroup2Handler.Items[EventBusConsts.EventNameHeaderKey]
                    .ShouldBe($"{nameof(TestCustomNameEvent)}_Test.{Options.Environment}");

                break;
            }

            TestCustomNameEventHandler.Instance.ShouldNotBeNull();
            TestCustomNameEventGroup2Handler.Instance.ShouldNotBeNull();
        }

        [Fact]
        public async Task TestExceptionEventTests()
        {
            var beginTime = DateTimeOffset.Now;
            var eventPublisher = GetService<IEventPublisher>();
            eventPublisher.ShouldBeOfType<DefaultEventPublisher>();
            var @event = new TestExceptionEvent { Name = "王麻子" };
            await eventPublisher.PublishAsync(@event, null, new Dictionary<string, string>
            {
                { "age", "21" }
            });

            TestExceptionEventHandler.Instance.ShouldBeNull();
            TestExceptionEventGroup2Handler.Instance.ShouldBeNull();
            await Task.Delay(Options.StartRetryAfter * 1000 - 1000);
            TestExceptionEventHandler.Instance.ShouldNotBeNull();
            TestExceptionEventGroup2Handler.Instance.ShouldNotBeNull();

            // 应该被执行了5次
            TestExceptionEventHandler.Counter.ShouldBe(5);
            TestExceptionEventGroup2Handler.Counter.ShouldBe(5);

            // 重试器开始工作
            await Task.Delay(2000);
            // 重试器轮询6次以后
            await Task.Delay(Options.RetryInterval * 6 * 1000);

            // 错误次数达到最大
            TestExceptionEventHandler.Counter.ShouldBe(Options.RetryFailedMax);
            TestExceptionEventHandler.Instance.ShouldNotBeNull();
            TestExceptionEventHandler.Items.ShouldNotBeNull();

            // 保持在5次
            TestExceptionEventGroup2Handler.Counter.ShouldBe(5);
            TestExceptionEventGroup2Handler.Instance!.Name.ShouldBe(@event.Name);
            TestExceptionEventGroup2Handler.Items["age"].ShouldBe("21");
            TestExceptionEventGroup2Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
            TestExceptionEventGroup2Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
            TestExceptionEventGroup2Handler.Items[EventBusConsts.EventNameHeaderKey]
                .ShouldBe($"{nameof(TestExceptionEvent)}.{Options.Environment}");
        }

        /// <summary>
        /// 手动retry
        /// </summary>
        [Fact]
        public async Task ManualRetryTestSuccess()
        {
            var receivedMessageRetryProvider = GetService<IReceivedMessageRetryProvider>();
            var messageStorage = GetService<IMessageStorage>();
            var eventNameRuler = GetService<IEventNameRuler>();
            var eventHandlerNameRuler = GetService<IEventHandlerNameRuler>();
            var id = await messageStorage.SaveReceivedAsync(new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString(),
                Environment = "dev",
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = null,
                EventHandlerName = eventHandlerNameRuler.GetName(typeof(TestEventHandler)),
                EventName = eventNameRuler.GetName(typeof(TestEvent)),
                EventBody = "{Name:\"张三\"}",
                EventItems = "{}",
                RetryCount = Options.RetryFailedMax + 1,
                Status = MessageStatus.Failed,
                IsLocking = false,
                LockEnd = null
            }, default);
            await receivedMessageRetryProvider.RetryAsync(id, default);
            var messageStorageModel = await messageStorage.FindReceivedByIdAsync(id, default);
            messageStorageModel.ShouldNotBeNull();
            messageStorageModel.RetryCount.ShouldBe(Options.RetryFailedMax + 1);
        }

        /// <summary>
        /// 手动retry
        /// </summary>
        [Fact]
        public async Task ManualRetryTestException()
        {
            var receivedMessageRetryProvider = GetService<IReceivedMessageRetryProvider>();
            var messageStorage = GetService<IMessageStorage>();
            var eventNameRuler = GetService<IEventNameRuler>();
            var eventHandlerNameRuler = GetService<IEventHandlerNameRuler>();
            var eventBusOptions = GetService<IOptions<EventBusOptions>>().Value;
            var id = await messageStorage.SaveReceivedAsync(new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString(),
                Environment = "dev",
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = null,
                EventHandlerName = eventHandlerNameRuler.GetName(typeof(TestExceptionEventHandler)),
                EventName = eventNameRuler.GetName(typeof(TestExceptionEvent)),
                EventBody = "{Name:\"张三\"}",
                EventItems = "{}",
                RetryCount = eventBusOptions.RetryFailedMax + 1,
                Status = MessageStatus.Failed,
                IsLocking = false,
                LockEnd = null
            }, default);
            var count = TestExceptionEventHandler.Counter;
            await receivedMessageRetryProvider.RetryAsync(id, default);
            var messageStorageModel = await messageStorage.FindReceivedByIdAsync(id, default);
            messageStorageModel.ShouldNotBeNull();
            messageStorageModel.RetryCount.ShouldBe(eventBusOptions.RetryFailedMax + 2);
            TestExceptionEventHandler.Counter.ShouldBe(count + 1);
        }

        [Fact]
        public async Task RetryProviderMaxFailedTest()
        {
            var retryProvider = GetService<IRetryProvider>();
            var counter = 0;
            retryProvider.Retry(1, () => Task.FromResult(new HandleResult(false, new MessageStorageModel
            {
                RetryCount = ++counter
            })));
            await Task.Delay(Options.RetryInterval * Options.RetryFailedMax * 1000 + 3000);
            counter.ShouldBe(Options.RetryFailedMax);
        }

        [Fact]
        public async Task RetryProviderMaxSuccessTest()
        {
            var retryProvider = GetService<IRetryProvider>();
            var counter = 0;
            retryProvider.Retry(1, () => Task.FromResult(new HandleResult(true, new MessageStorageModel { RetryCount = ++counter })));
            await Task.Delay(Options.RetryInterval * 1000 + 1000);
            counter.ShouldBe(1);
        }
    }
}