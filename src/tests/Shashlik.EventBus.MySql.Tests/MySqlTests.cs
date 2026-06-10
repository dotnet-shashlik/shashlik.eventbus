using System.Threading.Tasks;
using CommonTestLogical;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.MySql.Tests
{
    [Collection("Shashlik.EventBus.MySql.Tests")]
    public class MySqlIntegrationTests : TestBase<Startup>
    {
        public MySqlIntegrationTests(TestWebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper)
            : base(factory, testOutputHelper)
        {
        }

        [Fact]
        public async Task EndToEnd_With_RelationDb_Storage()
        {
            await GetService<IntegrationTests>().DoTests();
        }
    }

    [Collection("Shashlik.EventBus.MySql.Tests")]
    public class MySqlStorageTests : TestBase<Startup>
    {
        public MySqlStorageTests(TestWebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper)
            : base(factory, testOutputHelper)
        {
        }

        private StorageTests Storage => GetService<StorageTests>();

        [Fact]
        public async Task SavePublishedNoTransactionTest() =>
            await Storage.SavePublishedNoTransactionTest();

        [Fact]
        public async Task SavePublishedWithTransactionCommitTest() =>
            await Storage.SavePublishedWithTransactionCommitTest();

        [Fact]
        public async Task SavePublishedWithTransactionRollBackTest() =>
            await Storage.SavePublishedWithTransactionRollBackTest();

        [Fact]
        public async Task SavePublishedWithTransactionDisposeTest() =>
            await Storage.SavePublishedWithTransactionDisposeTest();

        [Fact]
        public async Task SaveReceivedTest() =>
            await Storage.SaveReceivedTest();

        [Fact]
        public async Task TryLockPublishedTests() =>
            await Storage.TryLockPublishedTests();

        [Fact]
        public async Task TryLockReceivedTests() =>
            await Storage.TryLockReceivedTests();

        [Fact]
        public async Task UpdatePublishedTests() =>
            await Storage.UpdatePublishedTests();

        [Fact]
        public async Task UpdateReceivedTests() =>
            await Storage.UpdateReceivedTests();

        [Fact]
        public async Task DeleteExpiresTests() =>
            await Storage.DeleteExpiresTests();

        [Fact]
        public async Task GetPublishedMessagesOfNeedRetryAndLockTests() =>
            await Storage.GetPublishedMessagesOfNeedRetryAndLockTests();

        [Fact]
        public async Task GetReceivedMessagesOfNeedRetryTests() =>
            await Storage.GetReceivedMessagesOfNeedRetryTests();

        [Fact]
        public async Task QueryPublishedTests() =>
            await Storage.QueryPublishedTests();

        [Fact]
        public async Task QueryReceivedTests() =>
            await Storage.QueryReceivedTests();

        [Fact]
        public async Task GetPublishedMessageStatusCountsTest() =>
            await Storage.GetPublishedMessageStatusCountsTest();

        [Fact]
        public async Task GetReceivedMessageStatusCountsTest() =>
            await Storage.GetReceivedMessageStatusCountsTest();

        [Fact]
        public void RelationDbStorageTransactionContextCommitTest() =>
            Storage.RelationDbStorageTransactionContextCommitTest();

        [Fact]
        public void RelationDbStorageTransactionContextRollbackTest() =>
            Storage.RelationDbStorageTransactionContextRollbackTest();

        [Fact]
        public void RelationDbStorageTransactionContextDisposeTest() =>
            Storage.RelationDbStorageTransactionContextDisposeTest();

        [Fact]
        public void XaTransactionContextCommitTest() =>
            Storage.XaTransactionContextCommitTest();

        [Fact]
        public void XaTransactionContextRollbackTest() =>
            Storage.XaTransactionContextRollbackTest();

        [Fact]
        public void XaTransactionContextDisposeTest() =>
            Storage.XaTransactionContextDisposeTest();
    }
}
