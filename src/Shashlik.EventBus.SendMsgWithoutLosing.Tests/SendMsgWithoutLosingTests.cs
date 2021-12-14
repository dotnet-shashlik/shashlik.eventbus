using System;
using System.Threading.Tasks;
using CommonTestLogical;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.SendMsgWithoutLosing.Tests
{
    public class SendMsgWithoutLosingTests : TestBase<SendMsgWithoutLosingTestStartup>
    {
        public SendMsgWithoutLosingTests(TestWebApplicationFactory<SendMsgWithoutLosingTestStartup> factory,
            ITestOutputHelper testOutputHelper) :
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
            var eventNameRuler = GetService<IEventNameRuler>();

            var name = Guid.NewGuid().ToString();
            await eventPublisher.PublishAsync(new SendMsgWithoutLosingTestEvent { Name = name }, null);

            await Task.Delay(options.StartRetryAfter * 1000 + 500);
            var list = await messageStorage.SearchPublishedAsync(eventNameRuler.GetName(typeof(SendMsgWithoutLosingTestEvent)), null, 0, 20,
                default);
            list.Count.ShouldBe(1);
            var item = list[0];
            var id = item.Id;

            // 再等重试器循环6次
            await Task.Delay(options.RetryInterval * 6 * 1000);
            item = await messageStorage.FindPublishedByIdAsync(id, default);
            item.RetryCount.ShouldBe(options.RetryFailedMax);

            await publishedMessageRetryProvider.RetryAsync(id, default);
            await publishedMessageRetryProvider.RetryAsync(id, default);

            item = await messageStorage.FindPublishedByIdAsync(id, default);
            item.RetryCount.ShouldBe(options.RetryFailedMax + 2);
        }
    }
}