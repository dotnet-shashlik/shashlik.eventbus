using System.Threading.Tasks;
using CommonTestLogical;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.RabbitMQ.Tests
{
    public class Tests : TestBase
    {
        public Tests(TestWebApplicationFactory<TestStartup> factory, ITestOutputHelper testOutputHelper) : base(factory, testOutputHelper)
        {
        }

        [Fact]
        public async Task IntegrationTests()
        {
            await GetService<IntegrationTests>().DoTests();
        }
    }
}