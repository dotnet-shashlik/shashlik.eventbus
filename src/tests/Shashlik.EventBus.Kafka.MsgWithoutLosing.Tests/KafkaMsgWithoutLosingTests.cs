using System.Threading.Tasks;
using CommonTestLogical;
using CommonTestLogical.MsgWithoutLosing;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.Kafka.MsgWithoutLosing.Tests
{
    public class KafkaMsgWithoutLosingTests : TestBase<MsgWithoutLosingStartup>
    {
        public KafkaMsgWithoutLosingTests(TestWebApplicationFactory<MsgWithoutLosingStartup> factory, ITestOutputHelper testOutputHelper) : base(
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