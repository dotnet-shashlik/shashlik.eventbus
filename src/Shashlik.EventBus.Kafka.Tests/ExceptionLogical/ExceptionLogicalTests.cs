using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.Kafka.Tests.ExceptionLogical
{
    public class ExceptionLogicalTests : TestBase2
    {
        public ExceptionLogicalTests(TestWebApplicationFactory2<TestStartup2> factory, ITestOutputHelper testOutputHelper) : base(factory,
            testOutputHelper)
        {
        }

        [Fact]
        public async Task ReceiveMsgError_Should_ReceiveAgain()
        {
            var eventPublisher = GetService<IEventPublisher>();
            await eventPublisher.PublishAsync(new ExceptionLogicalTestEvent {Name = "zhangsan"}, null);

            await Task.Delay(1000 * 60);
            
            ExceptionMessageListener.MsgIds.Count.ShouldBeGreaterThan(1);
            ExceptionMessageListener.MsgIds.Distinct().Count().ShouldBe(1);
        }
    }
}