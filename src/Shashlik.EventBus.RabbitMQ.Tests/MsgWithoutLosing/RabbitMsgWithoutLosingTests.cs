using System.Threading.Tasks;
using CommonTestLogical;
using CommonTestLogical.MsgWithoutLosing;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.RabbitMQ.Tests.MsgWithoutLosing
{
    [Collection("Shashlik.EventBus.RabbitMQ.Tests")]
    public class RabbitMsgWithoutLosingTests : TestBase<MsgWithoutLosingStartup>
    {
        public RabbitMsgWithoutLosingTests(TestWebApplicationFactory<MsgWithoutLosingStartup> factory,
            ITestOutputHelper testOutputHelper) : base(factory, testOutputHelper)
        {
        }

        [Fact]
        public async Task DoTests()
        {
            var t = GetService<MsgWithoutLosingTests>();
            await t.ReceiveMsgError_Should_ReceiveAgainTest();
        }
    }
}