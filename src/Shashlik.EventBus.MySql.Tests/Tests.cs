using System.Threading.Tasks;
using CommonTestLogical;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.MySql.Tests
{
    public class Tests : TestBase
    {
        public Tests(TestWebApplicationFactory<TestStartup> factory, ITestOutputHelper testOutputHelper) : base(factory, testOutputHelper)
        {
        }

        private StorageTests StorageTests => GetService<StorageTests>();

        [Fact]
        public async Task SavePublishedNoTransactionTest()
        {
            await StorageTests.SavePublishedNoTransactionTest();
        }

        [Fact]
        public async Task SavePublishedWithTransactionCommitTest()
        {
            await StorageTests.SavePublishedWithTransactionCommitTest();
        }

        [Fact]
        public async Task SavePublishedWithTransactionRollBackTest()
        {
            await StorageTests.SavePublishedWithTransactionRollBackTest();
        }

        [Fact]
        public async Task SavePublishedWithTransactionDisposeTest()
        {
            await StorageTests.SavePublishedWithTransactionDisposeTest();
        }

        [Fact]
        public async Task SaveReceivedTest()
        {
            await StorageTests.SaveReceivedTest();
        }

        [Fact]
        public async Task TryLockReceivedTests()
        {
            await StorageTests.TryLockReceivedTests();
        }

        [Fact]
        public async Task UpdatePublishedTests()
        {
            await StorageTests.UpdatePublishedTests();
        }

        [Fact]
        public async Task UpdateReceivedTests()
        {
            await StorageTests.UpdateReceivedTests();
        }

        [Fact]
        public async Task DeleteExpiresTests()
        {
            await StorageTests.DeleteExpiresTests();
        }

        [Fact]
        public async Task GetPublishedMessagesOfNeedRetryAndLockTests()
        {
            await StorageTests.GetPublishedMessagesOfNeedRetryAndLockTests();
        }

        [Fact]
        public async Task GetReceivedMessagesOfNeedRetryAndLockTests()
        {
            await StorageTests.GetReceivedMessagesOfNeedRetryAndLockTests();
        }


        [Fact]
        public async Task QueryPublishedTests()
        {
            await StorageTests.QueryPublishedTests();
        }

        [Fact]
        public async Task QueryReceivedTests()
        {
            await StorageTests.QueryReceivedTests();
        }
    }
}