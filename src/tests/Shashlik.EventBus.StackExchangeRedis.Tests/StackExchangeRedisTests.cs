using System.Threading.Tasks;
using CommonTestLogical;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.StackExchangeRedis.Tests
{
    [Collection("shashlik.EventBus.StackExchangeRedis.Tests")]
    public class StackExchangeRedisTests : TestBase<Startup>
    {
        public StackExchangeRedisTests(TestWebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper) : base(factory, testOutputHelper)
        {
        }

        [Fact]
        public async Task DoTests()
        {
            await GetService<IntegrationTests>().DoTests();
        }
    }
}
