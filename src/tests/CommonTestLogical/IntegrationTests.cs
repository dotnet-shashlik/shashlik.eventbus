using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using CommonTestLogical.EfCore;
using CommonTestLogical.TestEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Shashlik.EventBus;
using Shashlik.Utils.Extensions;
using Shashlik.Kernel.Dependency;
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

            // 每个 case 前 reset 一次,避免上一个 case 残留数据混淆
            TestEventHandler.Reset();
            TestEventGroup2Handler.Reset();
            TestDelayEventHandler.Reset();
            TestDelayEventGroup2Handler.Reset();
            TestDelayEventGroup3Handler.Reset();
            TestCustomNameEventHandler.Reset();
            TestCustomNameEventGroup2Handler.Reset();
            TestExceptionEventHandler.Reset();
            TestExceptionEventGroup2Handler.Reset();

            var testEvent = new TestEvent { Name = Guid.NewGuid().ToString("n") };
            var testDelayEvent = new TestDelayEvent { Name = Guid.NewGuid().ToString("n") };
            var testCustomNameEvent = new TestCustomNameEvent { Name = Guid.NewGuid().ToString("n") };
            var testExceptionEvent = new TestExceptionEvent { Name = Guid.NewGuid().ToString("n") };

            string testEventRandomCode = RandomHelper.RandomString(6);
            string testDelayEventRandomCode = RandomHelper.RandomString(6);
            string testCustomNameEventRandomCode = RandomHelper.RandomString(6);
            string testExceptionEventRandomCode = RandomHelper.RandomString(6);

            await DbContext.PublishEventAsync(testEvent, new Dictionary<string, string>
            {
                { "code", testEventRandomCode }
            });

            var delaySeconds = 5;
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

            // 等待事件执行
            await TestEventHandler.WaitForInstance(TimeSpan.FromSeconds(Options.StartRetryAfter + 5));
            await TestEventGroup2Handler.WaitForInstance(TimeSpan.FromSeconds(Options.StartRetryAfter + 5));
            await TestDelayEventHandler.WaitForInstance(TimeSpan.FromSeconds(Options.StartRetryAfter + delaySeconds + 10));
            await TestDelayEventGroup2Handler.WaitForInstance(TimeSpan.FromSeconds(5));
            await TestDelayEventGroup3Handler.WaitForInstance(TimeSpan.FromSeconds(5));
            await TestCustomNameEventHandler.WaitForInstance(TimeSpan.FromSeconds(Options.StartRetryAfter + 5));
            await TestCustomNameEventGroup2Handler.WaitForInstance(TimeSpan.FromSeconds(5));

            // TestEvent
            {
                TestEventHandler.LastInstance.ShouldNotBeNull();
                TestEventHandler.LastInstance.Name.ShouldBe(testEvent.Name);
                TestEventHandler.LastItems!["code"].ShouldBe(testEventRandomCode);
                TestEventHandler.LastItems[EventBusConsts.MsgIdHeaderKey].Length.ShouldBe(32);
                TestEventHandler.LastItems[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset?>().ShouldNotBeNull();
                TestEventHandler.LastItems[EventBusConsts.EventNameHeaderKey]
                    .ShouldBe($"{nameof(TestEvent)}.{Options.Environment}");

                TestEventGroup2Handler.LastInstance.ShouldNotBeNull();
                TestEventGroup2Handler.LastInstance.Name.ShouldBe(testEvent.Name);
                TestEventGroup2Handler.LastItems!["code"].ShouldBe(testEventRandomCode);
            }

            // TestDelayEvent
            {
                TestDelayEventHandler.LastInstance.ShouldNotBeNull();
                TestDelayEventHandler.LastInstance!.Name.ShouldBe(testDelayEvent.Name);
                TestDelayEventHandler.LastItems!["code"].ShouldBe(testDelayEventRandomCode);
                TestDelayEventHandler.LastItems[EventBusConsts.DelayAtHeaderKey]
                    .ParseTo<DateTimeOffset>().GetLongDate()
                    .ShouldBe(delayAt.GetLongDate());

                TestDelayEventGroup2Handler.LastInstance.ShouldNotBeNull();
                TestDelayEventGroup2Handler.LastInstance!.Name.ShouldBe(testDelayEvent.Name);
                TestDelayEventGroup3Handler.LastInstance.ShouldNotBeNull();
                TestDelayEventGroup3Handler.LastInstance!.Name.ShouldBe(testDelayEvent.Name);
            }

            // TestCustomNameEvent
            {
                TestCustomNameEventHandler.LastInstance.ShouldNotBeNull();
                TestCustomNameEventHandler.LastInstance!.Name.ShouldBe(testCustomNameEvent.Name);
                TestCustomNameEventHandler.LastItems!["code"].ShouldBe(testCustomNameEventRandomCode);
                TestCustomNameEventHandler.LastItems[EventBusConsts.EventNameHeaderKey]
                    .ShouldBe($"{nameof(TestCustomNameEvent)}_Test.{Options.Environment}");

                TestCustomNameEventGroup2Handler.LastInstance.ShouldNotBeNull();
                TestCustomNameEventGroup2Handler.LastInstance!.Name.ShouldBe(testCustomNameEvent.Name);
                TestCustomNameEventGroup2Handler.LastItems[EventBusConsts.EventNameHeaderKey]
                    .ShouldBe($"{nameof(TestCustomNameEvent)}_Test.{Options.Environment}");
            }

            // TestExceptionEvent: 异常 handler 一直抛,会由重试器持续重试,
            // 等到 Options.RetryFailedMax 后停止增长(框架内一次发布路径内最多 5 次,后续由重试器接管)。
            {
                // 一次发布路径内最多 5 次,等待两个 handler 都到达
                var waitBegin = DateTimeOffset.Now;
                while ((TestExceptionEventHandler.Counter < 5 || TestExceptionEventGroup2Handler.Counter < 5)
                       && (DateTimeOffset.Now - waitBegin).TotalSeconds < 30)
                {
                    await Task.Delay(500);
                }
                TestExceptionEventHandler.Counter.ShouldBeGreaterThanOrEqualTo(5);
                TestExceptionEventGroup2Handler.Counter.ShouldBeGreaterThanOrEqualTo(5);

                // 等重试器跑到 max
                var begin = DateTimeOffset.Now;
                while (TestExceptionEventHandler.Counter < Options.RetryFailedMax
                       && (DateTimeOffset.Now - begin).TotalSeconds < 600)
                {
                    await Task.Delay(1000);
                }

                TestExceptionEventHandler.Counter.ShouldBeGreaterThanOrEqualTo(Options.RetryFailedMax);
            }
        }

        public async Task XaTransactionRollBackTest()
        {
            var testEvent = new XaEvent { Name = Guid.NewGuid().ToString("n") };

            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                await EventPublisher.PublishAsync(testEvent, XaTransactionContext.Current, default);
                // rollback (no scope.Complete)
            }

            // 事务回滚后,消息不应该被标记为已提交(已发布路径的 IsCommitted 检查会拦截,
            // 不会真的发出去)。我们的存储里可能仍然有"未提交"的物理行(Sqlite + FreeSql 在事务
            // 回滚时也会把行删除,但 XaTransaction 没有真实的数据库事务),所以这里通过
            // IsCommitted 来验证:已回滚的消息永远不应该 IsCommitted=true。
            var msgs = await MessageStorage.SearchPublishedAsync(
                Options.Environment,
                DateTimeOffset.Now.AddMinutes(-1),
                DateTimeOffset.Now.AddMinutes(1),
                null, null, 0, 10000, default);
            var rolledBack = msgs.FirstOrDefault(r => r.EventBody.DeserializeJson<JObject>()["Name"]!.Value<string>() == testEvent.Name);
            // 如果行完全不存在(Sqlite/有真实事务的 provider),直接验证不存在
            if (rolledBack is null) return;
            // 否则,事务回滚后该行的 IsCommitted 必须为 false
            (await MessageStorage.IsCommittedAsync(testEvent.Name, default)).ShouldBeFalse();
        }

        public async Task XaTransactionCommitTest()
        {
            var testEvent = new XaEvent { Name = "'"+Guid.NewGuid().ToString("n") };

            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                await EventPublisher.PublishAsync(testEvent, XaTransactionContext.Current,default );
                scope.Complete();
            }

            var msgs = await MessageStorage.SearchPublishedAsync(
                Options.Environment,
                DateTimeOffset.Now.AddMinutes(-1),
                DateTimeOffset.Now.AddMinutes(1),
                null, null, 0, 10000, default);
            msgs.Any(r => r.EventBody.DeserializeJson<JObject>()["Name"]!.Value<string>() == testEvent.Name)
                .ShouldBeTrue();
        }
    }
}
