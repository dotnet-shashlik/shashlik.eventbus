using System.Threading.Tasks;
using CommonTestLogical;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.Pulsar.Tests
{
    [Collection("Shashlik.EventBus.Pulsar.Tests")]
    public class PulsarTests : TestBase<Startup>
    {
        public PulsarTests(TestWebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper) : base(
            factory, testOutputHelper)
        {
        }

        [Fact]
        public async Task DoTests()
        {
            await GetService<IntegrationTests>().DoTests();
        }
    }
}