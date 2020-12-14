using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shashlik.EventBus;
using Shashlik.Kernel.Dependency;
using Shouldly;

namespace CommonTestLogical.MsgWithoutLosing
{
    [Transient]
    public class MsgWithoutLosingTests
    {
        public MsgWithoutLosingTests(IEventPublisher eventPublisher, IOptions<EventBusOptions> options)
        {
            EventPublisher = eventPublisher;
            Options = options;
        }

        private IEventPublisher EventPublisher { get; }
        private IOptions<EventBusOptions> Options { get; }

        public async Task ReceiveMsgError_Should_ReceiveAgainTest()
        {
            await EventPublisher.PublishAsync(new MsgWithoutLosingTestEvent {Name = "zhangsan"}, null);

            await Task.Delay((Options.Value.StartRetryAfterSeconds + Options.Value.ConfirmTransactionSeconds) * 1000);

            MsgWithoutLosingListener.MsgIds.Count.ShouldBeGreaterThan(1);
            MsgWithoutLosingListener.MsgIds.Distinct().Count().ShouldBe(1);
        }
    }
}