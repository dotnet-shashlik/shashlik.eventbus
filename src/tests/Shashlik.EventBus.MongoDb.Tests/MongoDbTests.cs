using System;
using System.Linq;
using System.Threading.Tasks;
using CommonTestLogical;
using CommonTestLogical.EfCore;
using CommonTestLogical.TestEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shashlik.Utils.Extensions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.MongoDb.Tests
{
    [Collection("Shashlik.EventBus.MongoDb.Tests")]
    public class MongoDbTests : TestBase<Startup>
    {
        public MongoDbTests(TestWebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper) : base(
            factory, testOutputHelper)
        {
        }

        private StorageTests StorageTests => GetService<StorageTests>();
        private IMongoClient MongoClient => GetService<IMongoClient>();
        private EventBusMongoDbOptions MongoOptions => GetService<IOptions<EventBusMongoDbOptions>>().Value;
        private EventBusOptions EventBusOptions => GetService<IOptions<EventBusOptions>>().Value;
        private IMessageStorage MessageStorage => GetService<IMessageStorage>();

        [Fact]
        public async Task SavePublishedNoTransactionTest()
        {
            await StorageTests.SavePublishedNoTransactionTest();
        }

        [Fact]
        public async Task SavePublishedWithTransactionCommitTest()
        {
            var mongoDatabase = MongoClient.GetDatabase(MongoOptions.DataBase);
            if ((await mongoDatabase.ListCollectionNamesAsync())
                .ToList()
                .All(r => r != nameof(UsersStringId)))
                await mongoDatabase.CreateCollectionAsync(nameof(UsersStringId));
            var mongoCollection = mongoDatabase.GetCollection<UsersStringId>(nameof(UsersStringId));
            var clientSessionHandle = await MongoClient.StartSessionAsync();
            clientSessionHandle.StartTransaction();
            var user = new UsersStringId() { Name = "张三" };
            await mongoCollection.InsertOneAsync(clientSessionHandle, user);

            var @event = new TestEvent { Name = "张三" };
            var msg = new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = EventBusOptions.Environment,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = null,
                EventHandlerName = "TestEventHandlerName1",
                EventName = "TestEventName1",
                EventBody = @event.ToJson(),
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Scheduled,
                IsLocking = false,
                LockEnd = null
            };

            var id = await MessageStorage.SavePublishedAsync(msg, clientSessionHandle.GetTransactionContext(), default);

            var begin = DateTimeOffset.Now;
            while ((DateTimeOffset.Now - begin).TotalSeconds < 20)
            {
                // 20秒内不提交事务， 消息就应该是未提交
                (await MessageStorage.IsCommittedAsync(msg.MsgId, default)).ShouldBeFalse();
                await Task.Delay(300);
            }

            await clientSessionHandle.CommitTransactionAsync();
            (await MessageStorage.IsCommittedAsync(msg.MsgId, default)).ShouldBeTrue();

            msg.Id.ShouldBe(id);
            var dbMsg = await MessageStorage.FindPublishedByMsgIdAsync(msg.MsgId, default);
            dbMsg!.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);
            dbMsg.Status.ShouldBe(msg.Status);
        }

        [Fact]
        public async Task SavePublishedWithTransactionRollBackTest()
        {
            var mongoDatabase = MongoClient.GetDatabase(MongoOptions.DataBase);
            if ((await mongoDatabase.ListCollectionNamesAsync())
                .ToList()
                .All(r => r != nameof(UsersStringId)))
                await mongoDatabase.CreateCollectionAsync(nameof(UsersStringId));
            var mongoCollection = mongoDatabase.GetCollection<UsersStringId>(nameof(UsersStringId));
            var clientSessionHandle = await MongoClient.StartSessionAsync();
            clientSessionHandle.StartTransaction();
            var user = new UsersStringId { Name = "张三" };
            await mongoCollection.InsertOneAsync(clientSessionHandle, user);

            var @event = new TestEvent { Name = "张三" };
            var msg = new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = EventBusOptions.Environment,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = null,
                EventHandlerName = "TestEventHandlerName1",
                EventName = "TestEventName1",
                EventBody = @event.ToJson(),
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Scheduled,
                IsLocking = false,
                LockEnd = null
            };

            var id = await MessageStorage.SavePublishedAsync(msg, clientSessionHandle.GetTransactionContext(), default);

            var begin = DateTimeOffset.Now;
            while ((DateTimeOffset.Now - begin).TotalSeconds < 20)
            {
                // 20秒内不提交事务， 消息就应该是未提交
                (await MessageStorage.IsCommittedAsync(msg.MsgId, default)).ShouldBeFalse();
                await Task.Delay(300);
            }

            await clientSessionHandle.AbortTransactionAsync();
            (await MessageStorage.IsCommittedAsync(msg.MsgId, default)).ShouldBeFalse();

            var dbMsg = await MessageStorage.FindPublishedByMsgIdAsync(msg.MsgId, default);
            dbMsg.ShouldBeNull();
        }

        [Fact]
        public async Task SavePublishedWithTransactionDisposeTest()
        {
            var mongoDatabase = MongoClient.GetDatabase(MongoOptions.DataBase);
            if ((await mongoDatabase.ListCollectionNamesAsync())
                .ToList()
                .All(r => r != nameof(UsersStringId)))
                await mongoDatabase.CreateCollectionAsync(nameof(UsersStringId));
            var mongoCollection = mongoDatabase.GetCollection<UsersStringId>(nameof(UsersStringId));
            var clientSessionHandle = await MongoClient.StartSessionAsync();
            clientSessionHandle.StartTransaction();
            var user = new UsersStringId { Name = "张三" };
            await mongoCollection.InsertOneAsync(clientSessionHandle, user);

            var @event = new TestEvent { Name = "张三" };
            var msg = new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = EventBusOptions.Environment,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = null,
                EventHandlerName = "TestEventHandlerName1",
                EventName = "TestEventName1",
                EventBody = @event.ToJson(),
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Scheduled,
                IsLocking = false,
                LockEnd = null
            };

            var id = await MessageStorage.SavePublishedAsync(msg, clientSessionHandle.GetTransactionContext(), default);

            var begin = DateTimeOffset.Now;
            while ((DateTimeOffset.Now - begin).TotalSeconds < 20)
            {
                // 20秒内不提交事务， 消息就应该是未提交
                (await MessageStorage.IsCommittedAsync(msg.MsgId, default)).ShouldBeFalse();
                await Task.Delay(300);
            }

            clientSessionHandle.Dispose();
            (await MessageStorage.IsCommittedAsync(msg.MsgId, default)).ShouldBeFalse();

            var dbMsg = await MessageStorage.FindPublishedByMsgIdAsync(msg.MsgId, default);
            dbMsg.ShouldBeNull();
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
            await StorageTests.QueryPublishedTests();
        }

        [Fact]
        public void DbStorageTransactionContextCommitTest()
        {
            using var clientSessionHandle = MongoClient.StartSession();
            var mongoDbTransactionContext = new MongoDbTransactionContext(clientSessionHandle);
            clientSessionHandle.StartTransaction();
            mongoDbTransactionContext.IsDone().ShouldBeFalse();
            clientSessionHandle.CommitTransaction();
            mongoDbTransactionContext.IsDone().ShouldBeTrue();
        }

        [Fact]
        public void DbStorageTransactionContextRollbackTest()
        {
            using var clientSessionHandle = MongoClient.StartSession();
            var mongoDbTransactionContext = new MongoDbTransactionContext(clientSessionHandle);
            clientSessionHandle.StartTransaction();
            mongoDbTransactionContext.IsDone().ShouldBeFalse();
            clientSessionHandle.AbortTransaction();
            mongoDbTransactionContext.IsDone().ShouldBeTrue();
        }

        [Fact]
        public void DbStorageTransactionContextDisposeTest()
        {
            var clientSessionHandle = MongoClient.StartSession();
            var mongoDbTransactionContext = new MongoDbTransactionContext(clientSessionHandle);
            clientSessionHandle.StartTransaction();
            mongoDbTransactionContext.IsDone().ShouldBeFalse();
            clientSessionHandle.Dispose();
            mongoDbTransactionContext.IsDone().ShouldBeTrue();
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