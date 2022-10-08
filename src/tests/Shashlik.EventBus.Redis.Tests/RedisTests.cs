using System.Threading.Tasks;
using CommonTestLogical;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.Redis.Tests
{
    [Collection("shashlik.EventBus.Redis.Tests")]
    public class RedisTests : TestBase<Startup>
    {
        public RedisTests(TestWebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper) : base(factory, testOutputHelper)
        {
        }

        [Fact]
        public async Task DoTests()
        {
            await GetService<IntegrationTests>().DoTests();
        }
    }
}