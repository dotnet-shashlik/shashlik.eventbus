using System.Threading.Tasks;
using CommonTestLogical;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.Kafka.Tests
{
    [Collection("shashlik.EventBus.Kafka.Tests")]
    public class KafkaTests : TestBase<Startup>
    {
        public KafkaTests(TestWebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper) : base(factory, testOutputHelper)
        {
        }

        [Fact]
        public async Task DoTests()
        {
            await GetService<IntegrationTests>().DoTests();
        }
    }
}