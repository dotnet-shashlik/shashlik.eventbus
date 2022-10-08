using System.Threading.Tasks;
using CommonTestLogical;
using CommonTestLogical.MsgWithoutLosing;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.Redis.MsgWithoutLosing.Tests
{
    [Collection("Shashlik.EventBus.Redis.MsgWithoutLosing.Tests")]
    public class RedisMsgWithoutLosingTests : TestBase<MsgWithoutLosingStartup>
    {
        public RedisMsgWithoutLosingTests(TestWebApplicationFactory<MsgWithoutLosingStartup> factory,
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