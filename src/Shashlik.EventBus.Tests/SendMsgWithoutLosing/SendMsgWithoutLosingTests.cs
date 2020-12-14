using System;
using System.Threading.Tasks;
using CommonTestLogical;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.Tests.SendMsgWithoutLosing
{
    [Collection("Shashlik.EventBus.Tests")]
    public class SendMsgWithoutLosingTests : TestBase<SendMsgWithoutLosingTestStartup>
    {
        public SendMsgWithoutLosingTests(TestWebApplicationFactory<SendMsgWithoutLosingTestStartup> factory, ITestOutputHelper testOutputHelper) :
            base(factory, testOutputHelper)
        {
        }

        [Fact]
        public async Task DoTests()
        {
            var options = GetService<IOptions<EventBusOptions>>().Value;
            var publishedMessageRetryProvider = GetService<IPublishedMessageRetryProvider>();
            var eventPublisher = GetService<IEventPublisher>();
            var messageStorage = GetService<IMessageStorage>();

            var name = Guid.NewGuid().ToString();
            await eventPublisher.PublishAsync(new SendMsgWithoutLosingTestEvent {Name = name}, null);
            await Task.Delay(5000);

            var list = await messageStorage.GetPublishedMessagesOfNeedRetryAndLockAsync(100, options.RetryIntervalSeconds,
                options.RetryFailedMax, options.Environment, 100, default);
            list.Count.ShouldBe(1);
            var item = list[0];
            var id = item.Id;

            await publishedMessageRetryProvider.RetryAsync(id, default);
            await publishedMessageRetryProvider.RetryAsync(id, default);

            // 再过1分钟
            await Task.Delay((options.StartRetryAfterSeconds - options.ConfirmTransactionSeconds) * 1000);
            // 再等30秒
            await Task.Delay(options.RetryWorkingIntervalSeconds * 6 * 1000);
            item.RetryCount.ShouldBe(options.RetryFailedMax);

            await publishedMessageRetryProvider.RetryAsync(id, default);
            await publishedMessageRetryProvider.RetryAsync(id, default);

            item.RetryCount.ShouldBe(options.RetryFailedMax + 2);
        }
    }
}