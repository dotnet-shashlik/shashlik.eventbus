using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using CommonTestLogical.EfCore;
using CommonTestLogical.TestEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Shashlik.EventBus;
using Shashlik.EventBus.DefaultImpl;
using Shashlik.Kernel.Dependency;
using Shashlik.Utils.Extensions;
using Shashlik.Utils.Helpers;
using Shouldly;

namespace CommonTestLogical
{
    [Transient]
    public class IntegrationTests
    {
        public IntegrationTests(IOptions<EventBusOptions> eventBusOptions,
            IServiceProvider serviceProvider, IEventPublisher eventPublisher, IMessageStorage messageStorage)
        {
            EventPublisher = eventPublisher;
            MessageStorage = messageStorage;
            Options = eventBusOptions.Value;
            DbContext = serviceProvider.GetService<DemoDbContext>();
        }

        private EventBusOptions Options { get; }
        private DemoDbContext DbContext { get; }
        private IEventPublisher EventPublisher { get; }
        private IMessageStorage MessageStorage { get; }

        public async Task DoTests()
        {
            await XaTransactionCommitTest();
            await XaTransactionRollBackTest();

            var beginTime = DateTimeOffset.Now;
            var testEvent = new TestEvent { Name = Guid.NewGuid().ToString("n") };
            var testDelayEvent = new TestDelayEvent { Name = Guid.NewGuid().ToString("n") };
            var testCustomNameEvent = new TestCustomNameEvent { Name = Guid.NewGuid().ToString("n") };
            var testExceptionEvent = new TestExceptionEvent { Name = Guid.NewGuid().ToString("n") };

            string testEventRandomCode = RandomHelper.GetRandomCode(6);
            string testDelayEventRandomCode = RandomHelper.GetRandomCode(6);
            string testCustomNameEventRandomCode = RandomHelper.GetRandomCode(6);
            string testExceptionEventRandomCode = RandomHelper.GetRandomCode(6);

            await DbContext.PublishEventAsync(testEvent, new Dictionary<string, string>
            {
                { "code", testEventRandomCode }
            });

            var delaySeconds = 7;
            var delayAt = DateTimeOffset.Now.AddSeconds(delaySeconds);
            await EventPublisher.PublishAsync(testDelayEvent, delayAt, null, new Dictionary<string, string>
            {
                { "code", testDelayEventRandomCode }
            });
            await EventPublisher.PublishAsync(testCustomNameEvent, null, new Dictionary<string, string>
            {
                { "code", testCustomNameEventRandomCode }
            });
            await EventPublisher.PublishAsync(testExceptionEvent, null, new Dictionary<string, string>
            {
                { "code", testExceptionEventRandomCode }
            });

            // // 延迟事件在到点之前不能被执行
            // while (DateTimeOffset.Now < delayAt.AddMilliseconds(-500))
            // {
            //     TestDelayEventHandler.Instance.ShouldBeNull();
            //     TestDelayEventGroup2Handler.Instance.ShouldBeNull();
            //     TestDelayEventGroup3Handler.Instance.ShouldBeNull();
            // }

            await Task.Delay(Options.StartRetryAfter * 1000 - 1000);

            // TestEvent
            {
                TestEventHandler.Instance.ShouldNotBeNull();
                TestEventHandler.Instance.Name.ShouldBe(testEvent.Name);
                TestEventHandler.Items["code"].ShouldBe(testEventRandomCode);
                TestEventHandler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestEventHandler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestEventHandler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestEvent)}.{Options.Environment}");

                TestEventGroup2Handler.Instance.Name.ShouldBe(testEvent.Name);
                TestEventGroup2Handler.Items["code"].ShouldBe(testEventRandomCode);
                TestEventGroup2Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestEventGroup2Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestEventGroup2Handler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestEvent)}.{Options.Environment}");
            }

            // TestDelayEvent
            {
                TestDelayEventHandler.Instance!.Name.ShouldBe(testDelayEvent.Name);
                TestDelayEventHandler.Items["code"].ShouldBe(testDelayEventRandomCode);
                TestDelayEventHandler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestDelayEventHandler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestDelayEventHandler.Items[EventBusConsts.EventNameHeaderKey].ShouldBe($"{nameof(TestDelayEvent)}.{Options.Environment}");
                TestDelayEventHandler.Items[EventBusConsts.DelayAtHeaderKey].ParseTo<DateTimeOffset>().GetLongDate()
                    .ShouldBe(delayAt.GetLongDate());

                TestDelayEventGroup2Handler.Instance!.Name.ShouldBe(testDelayEvent.Name);
                TestDelayEventGroup2Handler.Items["code"].ShouldBe(testDelayEventRandomCode);
                TestDelayEventGroup2Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestDelayEventGroup2Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestDelayEventGroup2Handler.Items[EventBusConsts.EventNameHeaderKey]
                    .ShouldBe($"{nameof(TestDelayEvent)}.{Options.Environment}");
                TestDelayEventGroup2Handler.Items[EventBusConsts.DelayAtHeaderKey].ParseTo<DateTimeOffset>().GetLongDate()
                    .ShouldBe(delayAt.GetLongDate());

                TestDelayEventGroup3Handler.Instance!.Name.ShouldBe(testDelayEvent.Name);
                TestDelayEventGroup3Handler.Items["code"].ShouldBe(testDelayEventRandomCode);
                TestDelayEventGroup3Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestDelayEventGroup3Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestDelayEventGroup3Handler.Items[EventBusConsts.EventNameHeaderKey]
                    .ShouldBe($"{nameof(TestDelayEvent)}.{Options.Environment}");
                TestDelayEventGroup3Handler.Items[EventBusConsts.DelayAtHeaderKey].ParseTo<DateTimeOffset>().GetLongDate()
                    .ShouldBe(delayAt.GetLongDate());
            }

            // TestCustomNameEvent
            {
                TestCustomNameEventHandler.Instance.Name.ShouldBe(testCustomNameEvent.Name);
                TestCustomNameEventHandler.Items["code"].ShouldBe(testCustomNameEventRandomCode);
                TestCustomNameEventHandler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestCustomNameEventHandler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestCustomNameEventHandler.Items[EventBusConsts.EventNameHeaderKey]
                    .ShouldBe($"{nameof(TestCustomNameEvent)}_Test.{Options.Environment}");

                TestCustomNameEventGroup2Handler.Instance.Name.ShouldBe(testCustomNameEvent.Name);
                TestCustomNameEventGroup2Handler.Items["code"].ShouldBe(testCustomNameEventRandomCode);
                TestCustomNameEventGroup2Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestCustomNameEventGroup2Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestCustomNameEventGroup2Handler.Items[EventBusConsts.EventNameHeaderKey]
                    .ShouldBe($"{nameof(TestCustomNameEvent)}_Test.{Options.Environment}");
            }

            // TestExceptionEvent
            {
                // 应该被执行了5次
                TestExceptionEventHandler.Counter.ShouldBe(5);
                TestExceptionEventGroup2Handler.Counter.ShouldBe(5);

                // 再等重试5次
                await Task.Delay(Options.RetryInterval * 5 * 1000 + 3000);

                // 错误次数达到最大
                TestExceptionEventHandler.Counter.ShouldBe(Options.RetryFailedMax);
                TestExceptionEventHandler.Instance.ShouldNotBeNull();
                TestExceptionEventHandler.Items.ShouldNotBeNull();

                // 保持在5次
                TestExceptionEventGroup2Handler.Counter.ShouldBe(5);
                TestExceptionEventGroup2Handler.Instance!.Name.ShouldBe(testExceptionEvent.Name);
                TestExceptionEventGroup2Handler.Items["code"].ShouldBe(testExceptionEventRandomCode);
                TestExceptionEventGroup2Handler.Items[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestExceptionEventGroup2Handler.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestExceptionEventGroup2Handler.Items[EventBusConsts.EventNameHeaderKey]
                    .ShouldBe($"{nameof(TestExceptionEvent)}.{Options.Environment}");
            }
        }

        public async Task XaTransactionRollBackTest()
        {
            var testEvent = new XaEvent { Name = Guid.NewGuid().ToString("n") };

            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                await EventPublisher.PublishByAutoAsync(testEvent, null, default);
                // rollback
            }


            var msgs = await MessageStorage.SearchPublishedAsync("", "", 0, 10000, default);
            msgs.Any(r => r.EventBody.DeserializeJson<JObject>()["Name"]!.Value<string>() == testEvent.Name).ShouldBeFalse();
        }

        public async Task XaTransactionCommitTest()
        {
            var testEvent = new XaEvent { Name = Guid.NewGuid().ToString("n") };

            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                await EventPublisher.PublishByAutoAsync(testEvent, null, default);
                scope.Complete();
            }

            var msgs = await MessageStorage.SearchPublishedAsync("", "", 0, 10000, default);
            msgs.Any(r => r.EventBody.DeserializeJson<JObject>()["Name"]!.Value<string>() == testEvent.Name).ShouldBeTrue();
        }
    }
}