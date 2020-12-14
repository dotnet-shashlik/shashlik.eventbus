using System.Threading.Tasks;
using CommonTestLogical;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.Kafka.Tests
{
    public class Tests : TestBase<Startup>
    {
        public Tests(TestWebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper) : base(factory, testOutputHelper)
        {
        }

        [Fact]
        public async Task DoTests()
        {
            await GetService<IntegrationTests>().DoTests();
        }
    }
}