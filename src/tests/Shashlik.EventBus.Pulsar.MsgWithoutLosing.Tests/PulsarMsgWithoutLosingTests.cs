using System.Threading.Tasks;
using CommonTestLogical;
using CommonTestLogical.MsgWithoutLosing;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.Pulsar.MsgWithoutLosing.Tests
{
    [Collection("Shashlik.EventBus.Pulsar.MsgWithoutLosing.Tests")]
    public class PulsarMsgWithoutLosingTests : TestBase<MsgWithoutLosingStartup>
    {
        public PulsarMsgWithoutLosingTests(TestWebApplicationFactory<MsgWithoutLosingStartup> factory,
            ITestOutputHelper testOutputHelper) : base(
            factory, testOutputHelper)
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