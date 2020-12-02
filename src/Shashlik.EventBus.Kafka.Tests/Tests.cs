using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.DefaultImpl;
using Shashlik.Utils.Extensions;
using Shashlik.Utils.Helpers;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.Kafka.Tests
{
    public class Tests : TestBase
    {
        public Tests(TestWebApplicationFactory<TestStartup> factory, ITestOutputHelper testOutputHelper) : base(factory, testOutputHelper)
        {
        }

        [Fact]
        public async Task IntegrationTests()
        {
            TestOutputHelper.WriteLine($"TestId: {TestIdClass.TestIdNo}");
            var beginTime = DateTimeOffset.Now;
            var options = GetService<IOptions<EventBusOptions>>().Value;
            var eventPublisher = GetService<IEventPublisher>();
            eventPublisher.ShouldBeOfType<DefaultEventPublisher>();
            var testEvent = new TestEvent {Name = Guid.NewGuid().ToString("n")};
            var testDelayEvent = new TestDelayEvent {Name = Guid.NewGuid().ToString("n")};
            var testCustomNameEvent = new TestCustomNameEvent {Name = Guid.NewGuid().ToString("n")};
            var testExceptionEvent = new TestExceptionEvent {Name = Guid.NewGuid().ToString("n")};

            string testEventRandomCode = RandomHelper.GetRandomCode(6);
            string testDelayEventRandomCode = RandomHelper.GetRandomCode(6);
            string testCustomNameEventRandomCode = RandomHelper.GetRandomCode(6);
            string testExceptionEventRandomCode = RandomHelper.GetRandomCode(6);

            var delayAt = DateTimeOffset.Now.AddSeconds(10);

            await eventPublisher.PublishAsync(testEvent, null, new Dictionary<string, string>
            {
                {"code", testEventRandomCode}
            });
            await eventPublisher.PublishAsync(testDelayEvent, delayAt, null, new Dictionary<string, string>
            {
                {"code", testDelayEventRandomCode}
            });
            await eventPublisher.PublishAsync(testCustomNameEvent, null, new Dictionary<string, string>
            {
                {"code", testCustomNameEventRandomCode}
            });
            await eventPublisher.PublishAsync(testExceptionEvent, null, new Dictionary<string, string>
            {
                {"code", testExceptionEventRandomCode}
            });

            // 延迟事件在到点之前不能被执行
            while (DateTimeOffset.Now < delayAt.AddMilliseconds(-500))
            {
                TestDelayEventHandler.Instance.ShouldBeNull();
                TestDelayEventGroup2Handler.Instance.ShouldBeNull();
                TestDelayEventGroup3Handler.Instance.ShouldBeNull();
            }

            // 1分钟之内会全部异常,未执行
            while ((DateTimeOffset.Now - beginTime).TotalSeconds < options.ConfirmTransactionSeconds)
            {
                TestExceptionEventHandler.Instance.ShouldBeNull();
                TestExceptionEventGroup2Handler.Instance.ShouldBeNull();
            }

            // 1分钟以后，所有的正常事件处理类必须被处理
            await Task.Delay(60 * 1000);

            // TestEvent
            {
                TestEventHandler.Instance.Name.ShouldBe(testEvent.Name);
                TestEventHandler.Items["code"].ShouldBe(testEventRandomCode);
                TestEventHandler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestEventHandler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestEventHandler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestEvent)}.{Env}");

                TestEventGroup2Handler.Instance.Name.ShouldBe(testEvent.Name);
                TestEventGroup2Handler.Items["code"].ShouldBe(testEventRandomCode);
                TestEventGroup2Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestEventGroup2Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestEventGroup2Handler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestEvent)}.{Env}");
            }

            // TestDelayEvent
            {
                TestDelayEventHandler.Instance!.Name.ShouldBe(testDelayEvent.Name);
                TestDelayEventHandler.Items["code"].ShouldBe(testDelayEventRandomCode);
                TestDelayEventHandler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestDelayEventHandler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestDelayEventHandler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestDelayEvent)}.{Env}");
                TestDelayEventHandler.Items[EventBusConsts.DelayAtHeaderKey].ParseTo<DateTimeOffset>().GetLongDate()
                    .ShouldBe(delayAt.GetLongDate());

                TestDelayEventGroup2Handler.Instance!.Name.ShouldBe(testDelayEvent.Name);
                TestDelayEventGroup2Handler.Items["code"].ShouldBe(testDelayEventRandomCode);
                TestDelayEventGroup2Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestDelayEventGroup2Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestDelayEventGroup2Handler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestDelayEvent)}.{Env}");
                TestDelayEventGroup2Handler.Items[EventBusConsts.DelayAtHeaderKey].ParseTo<DateTimeOffset>().GetLongDate()
                    .ShouldBe(delayAt.GetLongDate());

                TestDelayEventGroup3Handler.Instance!.Name.ShouldBe(testDelayEvent.Name);
                TestDelayEventGroup3Handler.Items["code"].ShouldBe(testDelayEventRandomCode);
                TestDelayEventGroup3Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestDelayEventGroup3Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestDelayEventGroup3Handler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestDelayEvent)}.{Env}");
                TestDelayEventGroup3Handler.Items[EventBusConsts.DelayAtHeaderKey].ParseTo<DateTimeOffset>().GetLongDate()
                    .ShouldBe(delayAt.GetLongDate());
            }

            // TestCustomNameEvent
            {
                TestCustomNameEventHandler.Instance.Name.ShouldBe(testCustomNameEvent.Name);
                TestCustomNameEventHandler.Items["code"].ShouldBe(testCustomNameEventRandomCode);
                TestCustomNameEventHandler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestCustomNameEventHandler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestCustomNameEventHandler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestCustomNameEvent)}_Test.{Env}");

                TestCustomNameEventGroup2Handler.Instance.Name.ShouldBe(testCustomNameEvent.Name);
                TestCustomNameEventGroup2Handler.Items["code"].ShouldBe(testCustomNameEventRandomCode);
                TestCustomNameEventGroup2Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestCustomNameEventGroup2Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestCustomNameEventGroup2Handler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestCustomNameEvent)}_Test.{Env}");
            }

            // TestExceptionEvent
            {
                // 应该被执行了5次
                TestExceptionEventHandler.Counter.ShouldBe(5);
                TestExceptionEventGroup2Handler.Counter.ShouldBe(5);

                // 再过1分钟
                await Task.Delay((options.StartRetryAfterSeconds - options.ConfirmTransactionSeconds) * 1000);
                // 再等30秒
                await Task.Delay(30 * 1000);

                // 错误次数达到最大
                TestExceptionEventHandler.Counter.ShouldBe(options.RetryFailedMax);
                TestExceptionEventHandler.Instance.ShouldBeNull();
                TestExceptionEventHandler.Items.ShouldBeNull();

                // 保持在5次
                TestExceptionEventGroup2Handler.Counter.ShouldBe(5);
                TestExceptionEventGroup2Handler.Instance!.Name.ShouldBe(testExceptionEvent.Name);
                TestExceptionEventGroup2Handler.Items["code"].ShouldBe(testExceptionEventRandomCode);
                TestExceptionEventGroup2Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestExceptionEventGroup2Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestExceptionEventGroup2Handler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestExceptionEvent)}.{Env}");
            }
        }
    }
}