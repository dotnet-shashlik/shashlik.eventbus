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
    }
}