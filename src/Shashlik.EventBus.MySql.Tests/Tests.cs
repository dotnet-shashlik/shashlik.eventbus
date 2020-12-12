using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.MySql.Tests.Efcore;
using Shashlik.EventBus.RelationDbStorage;
using Shashlik.Utils.Extensions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.MySql.Tests
{
    public class Tests : TestBase
    {
        public Tests(TestWebApplicationFactory<TestStartup> factory, ITestOutputHelper testOutputHelper) : base(factory, testOutputHelper)
        {
        }

        private EventBusMySqlOptions MySqlOptions => GetService<IOptions<EventBusMySqlOptions>>().Value;
        private EventBusOptions EventBusOptions => GetService<IOptions<EventBusOptions>>().Value;
        private DemoDbContext DbContext => GetService<DemoDbContext>();
        private IMessageStorage MessageStorage => GetService<IMessageStorage>();

        [Fact]
        public async Task SavePublishedNoTransactionTest()
        {
            // 正常的已发布消息写入，查询测试, 不带事务
            var @event = new TestEvent {Name = "张三"};
            var msg = new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Env,
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
            var id = await MessageStorage.SavePublishedAsync(msg, null, default);
            msg.Id.ShouldBe(id);

            var dbMsg = await MessageStorage.FindPublishedByMsgIdAsync(msg.MsgId, default);
            dbMsg!.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);
            dbMsg.EventHandlerName.ShouldBeNullOrWhiteSpace();
            dbMsg.Status.ShouldBe(msg.Status);
        }

        [Fact]
        public async Task SavePublishedWithTransactionCommitTest()
        {
            await using var tran = await DbContext.Database.BeginTransactionAsync();
            DbContext.Add(new Users {Name = "张三"});
            await DbContext.SaveChangesAsync();

            var @event = new TestEvent {Name = "张三"};
            var msg = new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Env,
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

            var transactionContext = DbContext.GetTransactionContext();
            var id = await MessageStorage.SavePublishedAsync(msg, transactionContext, default);
            await DbContext.SaveChangesAsync();

            var begin = DateTimeOffset.Now;
            while ((DateTimeOffset.Now - begin).TotalSeconds < 20)
            {
                // 20秒内不提交事务， 消息就应该是未提交
                (await MessageStorage.TransactionIsCommittedAsync(msg.MsgId, transactionContext, default)).ShouldBeFalse();
                await Task.Delay(300);
            }

            await tran.CommitAsync();
            (await MessageStorage.TransactionIsCommittedAsync(msg.MsgId, transactionContext, default)).ShouldBeTrue();

            msg.Id.ShouldBe(id);
            var dbMsg = await MessageStorage.FindPublishedByMsgIdAsync(msg.MsgId, default);
            dbMsg!.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);
            dbMsg.EventHandlerName.ShouldBeNullOrWhiteSpace();
            dbMsg.Status.ShouldBe(msg.Status);
        }

        [Fact]
        public async Task SavePublishedWithTransactionRollBackTest()
        {
            await using var tran = await DbContext.Database.BeginTransactionAsync();
            DbContext.Add(new Users {Name = "张三"});
            await DbContext.SaveChangesAsync();
            var @event = new TestEvent {Name = "张三"};
            var msg = new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Env,
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
            var transactionContext = new RelationDbStorageTransactionContext(tran.GetDbTransaction());
            var id = await MessageStorage.SavePublishedAsync(msg, transactionContext, default);

            var begin = DateTimeOffset.Now;
            while ((DateTimeOffset.Now - begin).TotalSeconds < 20)
            {
                // 20秒内不提交事务， 消息就应该是未提交
                (await MessageStorage.TransactionIsCommittedAsync(msg.MsgId, transactionContext, default)).ShouldBeFalse();
                await Task.Delay(300);
            }

            await tran.RollbackAsync();
            (await MessageStorage.TransactionIsCommittedAsync(msg.MsgId, transactionContext, default)).ShouldBeFalse();

            var dbMsg = await MessageStorage.FindPublishedByMsgIdAsync(msg.MsgId, default);
            dbMsg.ShouldBeNull();
        }

        [Fact]
        public async Task SavePublishedWithTransactionDisposeTest()
        {
            var tran = await DbContext.Database.BeginTransactionAsync();
            DbContext.Add(new Users {Name = "张三"});
            await DbContext.SaveChangesAsync();
            var @event = new TestEvent {Name = "张三"};
            var msg = new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Env,
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
            var transactionContext = new RelationDbStorageTransactionContext(tran.GetDbTransaction());
            var id = await MessageStorage.SavePublishedAsync(msg, transactionContext, default);

            var begin = DateTimeOffset.Now;
            while ((DateTimeOffset.Now - begin).TotalSeconds < 20)
            {
                // 20秒内不提交事务， 消息就应该是未提交
                (await MessageStorage.TransactionIsCommittedAsync(msg.MsgId, transactionContext, default)).ShouldBeFalse();
                await Task.Delay(300);
            }

            await tran.DisposeAsync();
            (await MessageStorage.TransactionIsCommittedAsync(msg.MsgId, transactionContext, default)).ShouldBeFalse();

            var dbMsg = await MessageStorage.FindPublishedByMsgIdAsync(msg.MsgId, default);
            dbMsg.ShouldBeNull();
        }

        [Fact]
        public async Task SaveReceivedTest()
        {
            var @event = new TestEvent {Name = "张三"};
            var msg = new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Env,
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
            var id = await MessageStorage.SaveReceivedAsync(msg, default);
            msg.Id.ShouldBe(id);

            (await MessageStorage.FindReceivedByMsgIdAsync(msg.MsgId, new EventHandlerDescriptor
            {
                EventHandlerName = "TestEventHandlerName1"
            }, default))!.Id.ShouldBe(id);
        }

        [Fact]
        public async Task TryLockReceivedTests()
        {
            var @event = new TestEvent {Name = "张三"};
            var msg = new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Env,
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
            var id = await MessageStorage.SaveReceivedAsync(msg, default);
            msg.Id.ShouldBe(id);

            // 锁5秒
            (await MessageStorage.TryLockReceivedAsync(id, DateTimeOffset.Now.AddSeconds(5), default)).ShouldBeTrue();
            // 再锁失败
            (await MessageStorage.TryLockReceivedAsync(id, DateTimeOffset.Now.AddSeconds(5), default)).ShouldBeFalse();

            // 6秒后再锁成功
            await Task.Delay(6000);
            (await MessageStorage.TryLockReceivedAsync(id, DateTimeOffset.Now.AddSeconds(6), default)).ShouldBeTrue();
        }

        [Fact]
        public async Task UpdatePublishedTests()
        {
            var @event = new TestEvent {Name = "张三"};
            var msg = new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Env,
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
            var id = await MessageStorage.SavePublishedAsync(msg, null, default);
            msg.Id.ShouldBe(id);

            var expireAt = DateTimeOffset.Now.AddHours(1);
            await MessageStorage.UpdatePublishedAsync(id, MessageStatus.Succeeded, 1, DateTimeOffset.Now.AddHours(1), default);

            var dbMsg = await MessageStorage.FindPublishedByMsgIdAsync(msg.MsgId, default);
            dbMsg!.RetryCount.ShouldBe(1);
            dbMsg!.Status.ShouldBe(MessageStatus.Succeeded);
            dbMsg.ExpireTime!.Value.GetLongDate().ShouldBe(expireAt.GetLongDate());
        }

        [Fact]
        public async Task UpdateReceivedTests()
        {
            var @event = new TestEvent {Name = "张三"};
            var msg = new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Env,
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
            var id = await MessageStorage.SaveReceivedAsync(msg, default);
            msg.Id.ShouldBe(id);

            var expireAt = DateTimeOffset.Now.AddHours(1);
            await MessageStorage.UpdateReceivedAsync(id, MessageStatus.Succeeded, 1, DateTimeOffset.Now.AddHours(1), default);

            var dbMsg = await MessageStorage.FindReceivedByMsgIdAsync(msg.MsgId, new EventHandlerDescriptor
            {
                EventHandlerName = msg.EventHandlerName
            }, default);
            dbMsg!.RetryCount.ShouldBe(1);
            dbMsg!.Status.ShouldBe(MessageStatus.Succeeded);
            dbMsg.ExpireTime!.Value.GetLongDate().ShouldBe(expireAt.GetLongDate());
        }

        [Fact]
        public async Task DeleteExpiresTests()
        {
            var @event = new TestEvent {Name = "张三"};
            var msg1 = new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Env,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = DateTimeOffset.Now.AddHours(-1),
                EventHandlerName = "TestEventHandlerName1",
                EventName = "TestEventName1",
                EventBody = @event.ToJson(),
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Succeeded,
                IsLocking = false,
                LockEnd = null
            };
            var msg2 = new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Env,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = DateTimeOffset.Now.AddHours(-1),
                EventHandlerName = "TestEventHandlerName1",
                EventName = "TestEventName1",
                EventBody = @event.ToJson(),
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Failed,
                IsLocking = false,
                LockEnd = null
            };
            var id1 = await MessageStorage.SavePublishedAsync(msg1, null, default);
            var id2 = await MessageStorage.SaveReceivedAsync(msg2, default);

            await MessageStorage.DeleteExpiresAsync(default);
            var dbMsg1 = await MessageStorage.FindPublishedByMsgIdAsync(msg1.MsgId, default);
            dbMsg1.ShouldBeNull();
            var dbMsg2 = await MessageStorage.FindReceivedByMsgIdAsync(msg2.MsgId, new EventHandlerDescriptor
            {
                EventHandlerName = msg1.EventHandlerName
            }, default);
            dbMsg2.ShouldBeNull();
        }

        [Fact]
        public async Task GetPublishedMessagesOfNeedRetryAndLockTests()
        {
            var @event = new TestEvent {Name = "张三"};
            var msg = new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Env,
                CreateTime = DateTimeOffset.Now.AddSeconds(-this.EventBusOptions.StartRetryAfterSeconds).AddSeconds(-10),
                DelayAt = null,
                ExpireTime = null,
                EventHandlerName = "TestEventHandlerName1",
                EventName = "TestEventName1",
                EventBody = @event.ToJson(),
                EventItems = "{}",
                RetryCount = 5,
                Status = MessageStatus.Scheduled,
                IsLocking = false,
                LockEnd = null
            };

            var id1 = await MessageStorage.SavePublishedAsync(msg, null, default);
            var list1 = await MessageStorage.GetPublishedMessagesOfNeedRetryAndLockAsync(100, this.EventBusOptions.StartRetryAfterSeconds, this
                .EventBusOptions
                .RetryFailedMax, this.EventBusOptions.Environment, 5, default);
            list1.Any(r => r.Id == id1).ShouldBeTrue();

            // 马上 再获取并锁定，必须不包含
            var list2 = await MessageStorage.GetPublishedMessagesOfNeedRetryAndLockAsync(100, this.EventBusOptions.StartRetryAfterSeconds, this
                .EventBusOptions
                .RetryFailedMax, this.EventBusOptions.Environment, 5, default);
            list2.Any(r => r.Id == id1).ShouldBeFalse();

            // 延迟6秒后，应该可以拿到数据了
            await Task.Delay(6000);

            // 马上 再获取并锁定，必须不包含
            var list3 = await MessageStorage.GetPublishedMessagesOfNeedRetryAndLockAsync(100, this.EventBusOptions.StartRetryAfterSeconds, this
                .EventBusOptions
                .RetryFailedMax, this.EventBusOptions.Environment, 5, default);
            list3.Any(r => r.Id == id1).ShouldBeTrue();
        }

        [Fact]
        public async Task GetReceivedMessagesOfNeedRetryAndLockTests()
        {
            var @event = new TestEvent {Name = "张三"};
            var msg = new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Env,
                CreateTime = DateTimeOffset.Now.AddSeconds(-this.EventBusOptions.StartRetryAfterSeconds).AddSeconds(-10),
                DelayAt = null,
                ExpireTime = null,
                EventHandlerName = "TestEventHandlerName1",
                EventName = "TestEventName1",
                EventBody = @event.ToJson(),
                EventItems = "{}",
                RetryCount = 5,
                Status = MessageStatus.Scheduled,
                IsLocking = false,
                LockEnd = null
            };

            var id1 = await MessageStorage.SaveReceivedAsync(msg, default);
            var list1 = await MessageStorage.GetReceivedMessagesOfNeedRetryAndLockAsync(100, this.EventBusOptions.StartRetryAfterSeconds, this
                .EventBusOptions
                .RetryFailedMax, this.EventBusOptions.Environment, 5, default);
            list1.Any(r => r.Id == id1).ShouldBeTrue();

            // 马上 再获取并锁定，必须不包含
            var list2 = await MessageStorage.GetReceivedMessagesOfNeedRetryAndLockAsync(100, this.EventBusOptions.StartRetryAfterSeconds, this
                .EventBusOptions
                .RetryFailedMax, this.EventBusOptions.Environment, 5, default);
            list2.Any(r => r.Id == id1).ShouldBeFalse();

            // 延迟6秒后，应该可以拿到数据了
            await Task.Delay(6000);

            // 马上 再获取并锁定，必须不包含
            var list3 = await MessageStorage.GetReceivedMessagesOfNeedRetryAndLockAsync(100, this.EventBusOptions.StartRetryAfterSeconds, this
                .EventBusOptions
                .RetryFailedMax, this.EventBusOptions.Environment, 5, default);
            list3.Any(r => r.Id == id1).ShouldBeTrue();
        }

        [Fact]
        public async Task QueryPublishedTests()
        {
            var @event = new TestEvent {Name = "张三"};
            var msg = new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Env,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = DateTimeOffset.Now.AddHours(-1),
                EventHandlerName = "TestEventHandlerName1",
                EventName = "TestEventName1",
                EventBody = @event.ToJson(),
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Succeeded,
                IsLocking = false,
                LockEnd = null
            };

            var id = await MessageStorage.SavePublishedAsync(msg, null, default);
            var dbMsg = await MessageStorage.FindPublishedByIdAsync(id, default);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);

            var list = await MessageStorage.SearchPublishedAsync(msg.EventName, msg.Status, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);

            list = await MessageStorage.SearchPublishedAsync(string.Empty, msg.Status, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);

            list = await MessageStorage.SearchPublishedAsync(string.Empty, string.Empty, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);

            list = await MessageStorage.SearchPublishedAsync(msg.EventName, string.Empty, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);
        }

        [Fact]
        public async Task QueryReceivedTests()
        {
            var @event = new TestEvent {Name = "张三"};
            var msg = new MessageStorageModel
            {
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = Env,
                CreateTime = DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = DateTimeOffset.Now.AddHours(-1),
                EventHandlerName = "TestEventHandlerName1",
                EventName = "TestEventName1",
                EventBody = @event.ToJson(),
                EventItems = "{}",
                RetryCount = 0,
                Status = MessageStatus.Succeeded,
                IsLocking = false,
                LockEnd = null
            };

            var id = await MessageStorage.SaveReceivedAsync(msg, default);
            var dbMsg = await MessageStorage.FindReceivedByIdAsync(id, default);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);

            var list = await MessageStorage.SearchReceived(msg.EventName, msg.EventHandlerName, msg.Status, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);

            list = await MessageStorage.SearchReceived(msg.EventName, msg.EventHandlerName, string.Empty, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);

            list = await MessageStorage.SearchReceived(msg.EventName, string.Empty, string.Empty, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);

            list = await MessageStorage.SearchReceived(msg.EventName, string.Empty, msg.Status, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);


            list = await MessageStorage.SearchReceived(string.Empty, msg.EventHandlerName, msg.Status, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);

            list = await MessageStorage.SearchReceived(string.Empty, msg.EventHandlerName, string.Empty, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);

            list = await MessageStorage.SearchReceived(msg.EventName, msg.EventHandlerName, string.Empty, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);

            list = await MessageStorage.SearchReceived(string.Empty, msg.EventHandlerName, msg.Status, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);

            list = await MessageStorage.SearchReceived(string.Empty, string.Empty, msg.Status, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);

            list = await MessageStorage.SearchReceived(msg.EventName, string.Empty, msg.Status, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);
        }
    }
}