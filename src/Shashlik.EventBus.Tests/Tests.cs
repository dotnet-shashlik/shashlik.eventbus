﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.DefaultImpl;
using Shashlik.Utils.Extensions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.Tests
{
    public class Tests : TestBase
    {
        public Tests(TestWebApplicationFactory<TestStartup> factory, ITestOutputHelper testOutputHelper) : base(factory, testOutputHelper)
        {
        }

        [Fact]
        public void EventHandlerFindProviderAndNameRuleTests()
        {
            var eventHandlerFindProvider = GetService<IEventHandlerFindProvider>();
            eventHandlerFindProvider.ShouldBeOfType<DefaultEventHandlerFindProvider>();

            var handlers = eventHandlerFindProvider.LoadAll().ToList();

            {
                var testEventHandlerDescriptor = handlers.First(r => r.EventHandlerType == typeof(TestEventHandler));
                testEventHandlerDescriptor.EventHandlerName.ShouldBe($"{nameof(TestEventHandler)}.{Env}");
                testEventHandlerDescriptor.EventType.ShouldBe(typeof(TestEvent));
                testEventHandlerDescriptor.EventName.ShouldBe($"{nameof(TestEvent)}.{Env}");
                testEventHandlerDescriptor.IsDelay.ShouldBeFalse();
            }

            {
                var testEventHandlerDescriptor = handlers.First(r => r.EventHandlerType == typeof(TestEventGroup2Handler));
                testEventHandlerDescriptor.EventHandlerName.ShouldBe($"{nameof(TestEventGroup2Handler)}.{Env}");
                testEventHandlerDescriptor.EventType.ShouldBe(typeof(TestEvent));
                testEventHandlerDescriptor.EventName.ShouldBe($"{nameof(TestEvent)}.{Env}");
                testEventHandlerDescriptor.IsDelay.ShouldBeFalse();
            }

            {
                var testEventHandlerDescriptor = handlers.First(r => r.EventHandlerType == typeof(TestDelayEventHandler));
                testEventHandlerDescriptor.EventHandlerName.ShouldBe($"{nameof(TestDelayEventHandler)}.{Env}");
                testEventHandlerDescriptor.EventType.ShouldBe(typeof(TestDelayEvent));
                testEventHandlerDescriptor.EventName.ShouldBe($"{nameof(TestDelayEvent)}.{Env}");
                testEventHandlerDescriptor.IsDelay.ShouldBeTrue();
            }

            {
                var testEventHandlerDescriptor = handlers.First(r => r.EventHandlerType == typeof(TestCustomNameEventHandler));
                testEventHandlerDescriptor.EventHandlerName.ShouldBe($"{nameof(TestCustomNameEventHandler)}_Test.{Env}");
                testEventHandlerDescriptor.EventType.ShouldBe(typeof(TestCustomNameEvent));
                testEventHandlerDescriptor.EventName.ShouldBe($"{nameof(TestCustomNameEvent)}_Test.{Env}");
                testEventHandlerDescriptor.IsDelay.ShouldBeFalse();
            }
        }

        [Fact]
        public void MessageSerializerTests()
        {
            var messageSerializer = GetService<IMessageSerializer>();
            var @event = new TestEvent {Name = "张三"};
            var json = messageSerializer.Serialize(@event);
            messageSerializer.Deserialize<TestEvent>(json).Name.ShouldBe(@event.Name);
        }

        [Fact]
        public void EventHandlerInvokerTests()
        {
            var invoker = GetService<IEventHandlerInvoker>();
            var eventHandlerFindProvider = GetService<IEventHandlerFindProvider>();
            var testEventHandlerDescriptor = eventHandlerFindProvider.LoadAll().First(r => r.EventHandlerType == typeof(TestEventHandler));
            var messageSerializer = GetService<IMessageSerializer>();
            var @event = new TestEvent {Name = "张三"};
            var json = messageSerializer.Serialize(@event);

            {
                Should.Throw<InvalidCastException>(() => invoker.Invoke(new MessageStorageModel
                {
                    MsgId = Guid.NewGuid().ToString("n"),
                    Environment = Env,
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

            invoker.Invoke(new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Env,
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
            var @event = new TestEvent {Name = "张三"};
            await eventPublisher.PublishAsync(@event, null, new Dictionary<string, string>
            {
                {"age", "18"}
            });

            // 1分钟之内必须被执行
            while ((DateTimeOffset.Now - beginTime).TotalMinutes <= 1)
            {
                if (TestEventHandler.Instance is null || TestEventGroup2Handler.Instance is null)
                    continue;
                TestEventHandler.Instance.Name.ShouldBe(@event.Name);
                TestEventHandler.Items["age"].ShouldBe("18");
                TestEventHandler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestEventHandler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestEventHandler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestEvent)}.{Env}");

                TestEventGroup2Handler.Instance.Name.ShouldBe(@event.Name);
                TestEventGroup2Handler.Items["age"].ShouldBe("18");
                TestEventGroup2Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestEventGroup2Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestEventGroup2Handler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestEvent)}.{Env}");

                break;
            }

            TestEventHandler.Instance.ShouldNotBeNull();
            TestEventGroup2Handler.Instance.ShouldNotBeNull();
        }

        [Fact]
        public async Task TestDelayEventTests()
        {
            var beginTime = DateTimeOffset.Now;

            var eventPublisher = GetService<IEventPublisher>();
            eventPublisher.ShouldBeOfType<DefaultEventPublisher>();
            var @event = new TestDelayEvent {Name = "李四"};
            var delayAt = DateTimeOffset.Now.AddSeconds(10);
            await eventPublisher.PublishAsync(@event, delayAt, null, new Dictionary<string, string>
            {
                {"age", "19"}
            });

            // 时间没到，不能被执行
            while (DateTimeOffset.Now < delayAt)
            {
                TestDelayEventHandler.Instance.ShouldBeNull();
                TestDelayEventGroup2Handler.Instance.ShouldBeNull();
                TestDelayEventGroup3Handler.Instance.ShouldBeNull();
            }

            // 1分钟之内必须被执行
            while ((DateTimeOffset.Now - beginTime).TotalMinutes <= 1)
            {
                if (TestDelayEventHandler.Instance is null
                    || TestDelayEventGroup2Handler.Instance is null
                    || TestDelayEventGroup3Handler.Instance is null)
                    continue;

                TestDelayEventHandler.Instance.Name.ShouldBe(@event.Name);
                TestDelayEventHandler.Items["age"].ShouldBe("19");
                TestDelayEventHandler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestDelayEventHandler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestDelayEventHandler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestDelayEvent)}.{Env}");
                TestDelayEventHandler.Items[EventBusConsts.DelayAtHeaderKey].ParseTo<DateTimeOffset>().GetLongDate()
                    .ShouldBe(delayAt.GetLongDate());

                TestDelayEventGroup2Handler.Instance.Name.ShouldBe(@event.Name);
                TestDelayEventGroup2Handler.Items["age"].ShouldBe("19");
                TestDelayEventGroup2Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestDelayEventGroup2Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestDelayEventGroup2Handler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestDelayEvent)}.{Env}");
                TestDelayEventGroup2Handler.Items[EventBusConsts.DelayAtHeaderKey].ParseTo<DateTimeOffset>().GetLongDate()
                    .ShouldBe(delayAt.GetLongDate());

                TestDelayEventGroup3Handler.Instance.Name.ShouldBe(@event.Name);
                TestDelayEventGroup3Handler.Items["age"].ShouldBe("19");
                TestDelayEventGroup3Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestDelayEventGroup3Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestDelayEventGroup3Handler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestDelayEvent)}.{Env}");
                TestDelayEventGroup3Handler.Items[EventBusConsts.DelayAtHeaderKey].ParseTo<DateTimeOffset>().GetLongDate()
                    .ShouldBe(delayAt.GetLongDate());
                break;
            }

            TestDelayEventHandler.Instance.ShouldNotBeNull();
            TestDelayEventGroup2Handler.Instance.ShouldNotBeNull();
            TestDelayEventGroup3Handler.Instance.ShouldNotBeNull();
        }

        [Fact]
        public async Task TestCustomNameEventTests()
        {
            var beginTime = DateTimeOffset.Now;

            var eventPublisher = GetService<IEventPublisher>();
            eventPublisher.ShouldBeOfType<DefaultEventPublisher>();
            var @event = new TestCustomNameEvent {Name = "王五"};
            await eventPublisher.PublishAsync(@event, null, new Dictionary<string, string>
            {
                {"age", "20"}
            });

            // 1分钟之内必须被执行
            while ((DateTimeOffset.Now - beginTime).TotalMinutes <= 1)
            {
                if (TestCustomNameEventHandler.Instance is null || TestCustomNameEventGroup2Handler.Instance is null)
                    continue;
                TestCustomNameEventHandler.Instance.Name.ShouldBe(@event.Name);
                TestCustomNameEventHandler.Items["age"].ShouldBe("20");
                TestCustomNameEventHandler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestCustomNameEventHandler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestCustomNameEventHandler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestCustomNameEvent)}_Test.{Env}");

                TestCustomNameEventGroup2Handler.Instance.Name.ShouldBe(@event.Name);
                TestCustomNameEventGroup2Handler.Items["age"].ShouldBe("20");
                TestCustomNameEventGroup2Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestCustomNameEventGroup2Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestCustomNameEventGroup2Handler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestCustomNameEvent)}_Test.{Env}");

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
            var options = GetService<IOptions<EventBusOptions>>().Value;
            var @event = new TestExceptionEvent {Name = "王麻子"};
            await eventPublisher.PublishAsync(@event, null, new Dictionary<string, string>
            {
                {"age", "21"}
            });

            // 1分钟之内会全部异常,未执行
            while ((DateTimeOffset.Now - beginTime).TotalSeconds < options.ConfirmTransactionSeconds)
            {
                TestExceptionEventHandler.Instance.ShouldBeNull();
                TestExceptionEventGroup2Handler.Instance.ShouldBeNull();
            }

            // 应该被执行了5次
            TestExceptionEventHandler.Counter.ShouldBe(5);
            TestExceptionEventGroup2Handler.Counter.ShouldBe(5);

            // 再过1分钟
            await Task.Delay((options.StartRetryAfterSeconds - options.ConfirmTransactionSeconds) * 1000);
            // 再等20秒
            await Task.Delay(20 * 1000);

            // 错误次数达到最大
            TestExceptionEventHandler.Counter.ShouldBe(options.RetryFailedMax);
            TestExceptionEventHandler.Instance.ShouldBeNull();
            TestExceptionEventHandler.Items.ShouldBeNull();

            // 保持在5次
            TestExceptionEventGroup2Handler.Counter.ShouldBe(5);
            TestExceptionEventGroup2Handler.Instance!.Name.ShouldBe(@event.Name);
            TestExceptionEventGroup2Handler.Items["age"].ShouldBe("21");
            TestExceptionEventGroup2Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
            TestExceptionEventGroup2Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
            TestExceptionEventGroup2Handler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestExceptionEvent)}.{Env}");
        }
    }
}