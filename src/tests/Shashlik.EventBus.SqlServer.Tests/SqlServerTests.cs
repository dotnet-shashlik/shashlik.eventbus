using System.Threading.Tasks;
using CommonTestLogical;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.SqlServer.Tests
{
    [Collection("Shashlik.EventBus.SqlServer.Tests")]
    public class SqlServerTests : TestBase<Startup>
    {
        public SqlServerTests(TestWebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper) : base(
            factory, testOutputHelper)
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
        public async Task TryLockPublishedTests()
        {
            await StorageTests.TryLockPublishedTests();
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
        public async Task GetReceivedMessagesOfNeedRetryTests()
        {
            await StorageTests.GetReceivedMessagesOfNeedRetryTests();
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

        [Fact]
        public void RelationDbStorageTransactionContextCommitTest()
        {
            StorageTests.RelationDbStorageTransactionContextCommitTest();
        }

        [Fact]
        public void RelationDbStorageTransactionContextRollbackTest()
        {
            StorageTests.RelationDbStorageTransactionContextRollbackTest();
        }

        [Fact]
        public void RelationDbStorageTransactionContextDisposeTest()
        {
            StorageTests.RelationDbStorageTransactionContextDisposeTest();
        }

        [Fact]
        public void XaTransactionContextCommitTest()
        {
            StorageTests.XaTransactionContextCommitTest();
        }

        [Fact]
        public void XaTransactionContextRollbackTest()
        {
            StorageTests.XaTransactionContextRollbackTest();
        }

        [Fact]
        public void XaTransactionContextDisposeTest()
        {
            StorageTests.XaTransactionContextDisposeTest();
        }

        [Fact]
        public async Task GetPublishedMessageStatusCountsTest()
        {
            await StorageTests.GetPublishedMessageStatusCountsTest();
        }

        [Fact]
        public async Task GetReceivedMessageStatusCountsTest()
        {
            await StorageTests.GetReceivedMessageStatusCountsTest();
        }
    }
}