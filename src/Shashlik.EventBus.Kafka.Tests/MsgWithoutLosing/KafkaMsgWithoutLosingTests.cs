using System.Threading.Tasks;
using CommonTestLogical.MsgWithoutLosing;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.Kafka.Tests.MsgWithoutLosing
{
    public class KafkaMsgWithoutLosingTests : MsgWithoutLosingTestBase
    {
        public KafkaMsgWithoutLosingTests(MsgWithoutLosingWebApplicationFactory<MsgWithoutLosingStartup> factory,
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