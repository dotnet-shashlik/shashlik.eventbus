using System.Threading.Tasks;
using CommonTestLogical.MsgWithoutLosing;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.RabbitMQ.Tests.MsgWithoutLosing
{
    public class RabbitMsgWithoutLosingTests : MsgWithoutLosingTestBase
    {
        public RabbitMsgWithoutLosingTests(MsgWithoutLosingWebApplicationFactory<MsgWithoutLosingStartup> factory,
            ITestOutputHelper testOutputHelper) :
            base(factory, testOutputHelper)
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