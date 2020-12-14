using System.Threading.Tasks;
using CommonTestLogical;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.RabbitMQ.Tests
{
    [Collection("Shashlik.EventBus.RabbitMQ.Tests")]
    public class RabbitMQTests : TestBase<Startup>
    {
        public RabbitMQTests(TestWebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper) : base(factory, testOutputHelper)
        {
        }

        [Fact]
        public async Task IntegrationTests()
        {
            await GetService<IntegrationTests>().DoTests();
        }
    }
}