using System.Threading.Tasks;
using CommonTestLogical;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.Sqlite.Tests
{
    /// <summary>
    /// 完整端到端集成测试:在 Sqlite 关系库上验证 EventBus + EF 事务。
    /// </summary>
    [Collection("Shashlik.EventBus.Sqlite.Tests")]
    public class SqliteIntegrationTests : TestBase<Startup>
    {
        public SqliteIntegrationTests(TestWebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper)
            : base(factory, testOutputHelper)
        {
        }

        [Fact]
        public async Task EndToEnd_With_RelationDb_Storage()
        {
            await GetService<IntegrationTests>().DoTests();
        }
    }

    [Collection("Shashlik.EventBus.Sqlite.Tests")]
    public class SqliteStorageTests : TestBase<Startup>
    {
        public SqliteStorageTests(TestWebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper)
            : base(factory, testOutputHelper)
        {
        }

        private StorageTests Storage => GetService<StorageTests>();

        [Fact]
        public async Task SavePublishedNoTransactionTest() =>
            await Storage.SavePublishedNoTransactionTest();

        // 注:SavePublishedWithTransaction* 这几个测试依赖 EF Core + Microsoft.Data.Sqlite
        // 提供的 DbTransaction,与 FreeSql.Provider.Sqlite 内部使用的 System.Data.SQLite
        // 不互通(FreeSql 会把 DbConnection 强制转型)。所以 Sqlite 测试项目里跳过这些
        // 用例,只覆盖无事务场景。EF + FreeSql 跨 provider 的事务场景留给 MySQL/PG 等
        // 使用 Microsoft.Data.Sqlite 之外 provider 的测试项目(那里 EF Core 走
        // Pomelo/Npgsql,跟 FreeSql 的 MySqlConnector/Npgsql provider 通用)。

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

        // 事务上下文 IsDone(跟存储实现无关)
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
