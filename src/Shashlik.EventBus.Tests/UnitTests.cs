using System;
using System.Collections.Generic;
using System.Linq;
using Shashlik.EventBus.DefaultImpl;
using Shouldly;
using Xunit;

namespace Shashlik.EventBus.Tests
{
    public class UnitTests : TestBase
    {
        public UnitTests(TestWebApplicationFactory<TestStartup> factory) : base(factory)
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
    }
}