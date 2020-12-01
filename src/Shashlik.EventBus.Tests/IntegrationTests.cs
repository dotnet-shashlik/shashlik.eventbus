using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shashlik.EventBus.DefaultImpl;
using Shashlik.Utils.Extensions;
using Shouldly;
using Xunit;

namespace Shashlik.EventBus.Tests
{
    public class IntegrationTests : TestBase
    {
        public IntegrationTests(TestWebApplicationFactory<TestStartup> factory) : base(factory)
        {
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
    }
}