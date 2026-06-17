using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using CommonTestLogical.EfCore;
using CommonTestLogical.TestEvents;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shashlik.EventBus;
using Shashlik.EventBus.RelationDbStorage;
using Shashlik.Kernel.Dependency;
using Shashlik.Utils.Extensions;
using Shashlik.Utils.Helpers;
using Shouldly;

namespace CommonTestLogical
{
    /// <summary>
    /// 存储相关的测试逻辑
    /// </summary>
    [Transient]
    public class StorageTests
    {
        public StorageTests(IMessageStorage messageStorage, IOptions<EventBusOptions> eventBusOptions,
            IIdGenerator idGenerator, IServiceProvider serviceProvider)
        {
            MessageStorage = messageStorage;
            EventBusOptions = eventBusOptions.Value;
            IdGenerator = idGenerator;
            DbContext = serviceProvider.GetService<DemoDbContext>();
        }

        private EventBusOptions EventBusOptions { get; }
        private IIdGenerator IdGenerator { get; }
        private DemoDbContext DbContext { get; }
        private IMessageStorage MessageStorage { get; }

        // ---- helpers -------------------------------------------------------

        private static readonly DateTimeOffset TimeBegin =
            DateTimeOffset.Now.AddYears(-1);

        private static readonly DateTimeOffset TimeEnd =
            DateTimeOffset.Now.AddYears(1);

        private MessageStorageModel NewMsg(
            string eventName = "TestEventName1",
            string handlerName = "TestEventHandlerName1",
            DateTimeOffset? createTime = null,
            DateTimeOffset? expireTime = null,
            string? status = null,
            int retryCount = 0)
        {
            return new MessageStorageModel
            {
                Id = IdGenerator.NextId(),
                MsgId = Guid.NewGuid().ToString("n"),
                Environment = EventBusOptions.Environment,
                CreateTime = createTime ?? DateTimeOffset.Now,
                DelayAt = null,
                ExpireTime = expireTime,
                EventHandlerName = handlerName,
                EventName = eventName,
                EventBody = new TestEvent { Name = "张三" }.ToJson(),
                EventItems = "{}",
                RetryCount = retryCount,
                Status = status ?? MessageStatus.Scheduled,
                IsLocking = false,
                LockEnd = null
            };
        }

        // ---- 已发布/已接收消息 基础读写测试 ---------------------------------

        public async Task SavePublishedNoTransactionTest()
        {
            var msg = NewMsg();
            var id = await MessageStorage.SavePublishedAsync(msg, null, default);
            msg.Id.ShouldBe(id);

            var dbMsg = await MessageStorage.FindPublishedByMsgIdAsync(msg.MsgId, default);
            dbMsg!.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);
            dbMsg.Status.ShouldBe(msg.Status);
        }

        public async Task SavePublishedWithTransactionCommitTest()
        {
            await using var tran = await DbContext.Database.BeginTransactionAsync();
            DbContext.Add(new Users { Name = "张三" });
            await DbContext.SaveChangesAsync();

            var msg = NewMsg();
            var transactionContext = DbContext.GetTransactionContext();
            var id = await MessageStorage.SavePublishedAsync(msg, transactionContext, default);
            await DbContext.SaveChangesAsync();

            var begin = DateTimeOffset.Now;
            while ((DateTimeOffset.Now - begin).TotalSeconds < 5)
            {
                // 未提交前 IsCommitted 必须为 false
                (await MessageStorage.IsCommittedAsync(msg.MsgId, default)).ShouldBeFalse();
                await Task.Delay(100);
            }

            await tran.CommitAsync();
            (await MessageStorage.IsCommittedAsync(msg.MsgId, default)).ShouldBeTrue();

            msg.Id.ShouldBe(id);
            var dbMsg = await MessageStorage.FindPublishedByMsgIdAsync(msg.MsgId, default);
            dbMsg!.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);
            dbMsg.Status.ShouldBe(msg.Status);
        }

        public async Task SavePublishedWithTransactionRollBackTest()
        {
            await using var tran = await DbContext.Database.BeginTransactionAsync();
            DbContext.Add(new Users { Name = "张三" });
            await DbContext.SaveChangesAsync();
            var msg = NewMsg();
            var transactionContext = new RelationDbStorageTransactionContext(tran.GetDbTransaction());
            var id = await MessageStorage.SavePublishedAsync(msg, transactionContext, default);

            var begin = DateTimeOffset.Now;
            while ((DateTimeOffset.Now - begin).TotalSeconds < 5)
            {
                (await MessageStorage.IsCommittedAsync(msg.MsgId, default)).ShouldBeFalse();
                await Task.Delay(100);
            }

            await tran.RollbackAsync();
            (await MessageStorage.IsCommittedAsync(msg.MsgId, default)).ShouldBeFalse();

            var dbMsg = await MessageStorage.FindPublishedByMsgIdAsync(msg.MsgId, default);
            dbMsg.ShouldBeNull();
        }

        public async Task SavePublishedWithTransactionDisposeTest()
        {
            var tran = await DbContext.Database.BeginTransactionAsync();
            DbContext.Add(new Users { Name = "张三" });
            await DbContext.SaveChangesAsync();
            var msg = NewMsg();
            var transactionContext = new RelationDbStorageTransactionContext(tran.GetDbTransaction());
            var id = await MessageStorage.SavePublishedAsync(msg, transactionContext, default);

            var begin = DateTimeOffset.Now;
            while ((DateTimeOffset.Now - begin).TotalSeconds < 5)
            {
                (await MessageStorage.IsCommittedAsync(msg.MsgId, default)).ShouldBeFalse();
                await Task.Delay(100);
            }

            await tran.DisposeAsync();
            (await MessageStorage.IsCommittedAsync(msg.MsgId, default)).ShouldBeFalse();

            var dbMsg = await MessageStorage.FindPublishedByMsgIdAsync(msg.MsgId, default);
            dbMsg.ShouldBeNull();
        }

        public async Task SaveReceivedTest()
        {
            var msg = NewMsg();
            var id = await MessageStorage.SaveReceivedAsync(msg, default);
            msg.Id.ShouldBe(id);

            (await MessageStorage.FindReceivedByMsgIdAsync(msg.MsgId, new EventHandlerDescriptor
            {
                EventHandlerName = msg.EventHandlerName
            }, default))!.Id.ShouldBe(id);
        }

        // ---- 锁测试 --------------------------------------------------------

        public async Task TryLockPublishedTests()
        {
            var msg = NewMsg();
            var id = await MessageStorage.SavePublishedAsync(msg, null, default);
            msg.Id.ShouldBe(id);

            // 锁5秒
            (await MessageStorage.TryLockPublishedAsync(id, DateTimeOffset.Now.AddSeconds(5), default)).ShouldBeTrue();
            // 再锁失败
            (await MessageStorage.TryLockPublishedAsync(id, DateTimeOffset.Now.AddSeconds(5), default)).ShouldBeFalse();

            // 6秒后再锁成功
            await Task.Delay(6000);
            (await MessageStorage.TryLockPublishedAsync(id, DateTimeOffset.Now.AddSeconds(6), default)).ShouldBeTrue();
        }

        public async Task TryLockReceivedTests()
        {
            var msg = NewMsg();
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

        public async Task UpdatePublishedTests()
        {
            var msg = NewMsg();
            var id = await MessageStorage.SavePublishedAsync(msg, null, default);
            msg.Id.ShouldBe(id);

            var expireAt = DateTimeOffset.Now.AddHours(1);
            await MessageStorage.UpdatePublishedAsync(id, MessageStatus.Succeeded, 1, expireAt, default);

            var dbMsg = await MessageStorage.FindPublishedByMsgIdAsync(msg.MsgId, default);
            dbMsg!.RetryCount.ShouldBe(1);
            dbMsg!.Status.ShouldBe(MessageStatus.Succeeded);
            dbMsg.ExpireTime!.Value.GetLongDate().ShouldBe(expireAt.GetLongDate());
        }

        public async Task UpdateReceivedTests()
        {
            var msg = NewMsg();
            var id = await MessageStorage.SaveReceivedAsync(msg, default);
            msg.Id.ShouldBe(id);

            var expireAt = DateTimeOffset.Now.AddHours(1);
            await MessageStorage.UpdateReceivedAsync(id, MessageStatus.Succeeded, 1, expireAt, default);

            var dbMsg = await MessageStorage.FindReceivedByMsgIdAsync(msg.MsgId, new EventHandlerDescriptor
            {
                EventHandlerName = msg.EventHandlerName
            }, default);
            dbMsg!.RetryCount.ShouldBe(1);
            dbMsg!.Status.ShouldBe(MessageStatus.Succeeded);
            dbMsg.ExpireTime!.Value.GetLongDate().ShouldBe(expireAt.GetLongDate());
        }

        public async Task DeleteExpiresTests()
        {
            // 删除逻辑只关心 (ExpireTime < now) && (Status=Succeeded OR (Status=Failed AND RetryCount >= maxRetry))
            // - 已发布表 msg1(过期&失败但未到 maxRetry) -> 保留
            // - msg2(过期&待发送) -> 保留
            // - msg3(过期&成功) -> 删除
            // - msg4(未过期&成功) -> 保留
            // - msg5(过期&失败达到 maxRetry) -> 删除
            // 已接收表 同理
            var now = DateTimeOffset.Now;
            var past = now.AddHours(-EventBusOptions.MessageExpireHour - 1);
            var future = now.AddHours(1);

            long addPublished(DateTimeOffset expire, string status, int retry)
            {
                var m = NewMsg(expireTime: expire, status: status, retryCount: retry);
                MessageStorage.SavePublishedAsync(m, null, default).GetAwaiter().GetResult();
                return m.Id;
            }

            long addReceived(DateTimeOffset expire, string status, int retry)
            {
                var m = NewMsg(expireTime: expire, status: status, retryCount: retry);
                MessageStorage.SaveReceivedAsync(m, default).GetAwaiter().GetResult();
                return m.Id;
            }

            var p1 = addPublished(past, MessageStatus.Failed, 0);
            var p2 = addPublished(past, MessageStatus.Scheduled, 0);
            var p3 = addPublished(past, MessageStatus.Succeeded, 0);
            var p4 = addPublished(future, MessageStatus.Succeeded, 0);
            var p5 = addPublished(past, MessageStatus.Failed, EventBusOptions.RetryFailedMax + 1);

            var r1 = addReceived(past, MessageStatus.Failed, 0);
            var r2 = addReceived(past, MessageStatus.Scheduled, 0);
            var r3 = addReceived(past, MessageStatus.Succeeded, 0);
            var r4 = addReceived(future, MessageStatus.Succeeded, 0);
            var r5 = addReceived(past, MessageStatus.Failed, EventBusOptions.RetryFailedMax + 1);

            await MessageStorage.DeleteExpiresAsync(EventBusOptions.RetryFailedMax, default);

            (await MessageStorage.FindPublishedByIdAsync(p1, default)).ShouldNotBeNull();
            (await MessageStorage.FindPublishedByIdAsync(p2, default)).ShouldNotBeNull();
            (await MessageStorage.FindPublishedByIdAsync(p3, default)).ShouldBeNull();
            (await MessageStorage.FindPublishedByIdAsync(p4, default)).ShouldNotBeNull();
            (await MessageStorage.FindPublishedByIdAsync(p5, default)).ShouldBeNull();

            (await MessageStorage.FindReceivedByIdAsync(r1, default)).ShouldNotBeNull();
            (await MessageStorage.FindReceivedByIdAsync(r2, default)).ShouldNotBeNull();
            (await MessageStorage.FindReceivedByIdAsync(r3, default)).ShouldBeNull();
            (await MessageStorage.FindReceivedByIdAsync(r4, default)).ShouldNotBeNull();
            (await MessageStorage.FindReceivedByIdAsync(r5, default)).ShouldBeNull();
        }

        // ---- 重试数据查询 ---------------------------------------------------

        public async Task GetPublishedMessagesOfNeedRetryAndLockTests()
        {
            // 构造:一条超过 StartRetryAfter 时间的 Scheduled、一条 Failed、一条 Succeeded、一条未到时间的
            // 期望:前两条需要重试,后两条不需要
            var limit = DateTimeOffset.Now.AddSeconds(-this.EventBusOptions.StartRetryAfter).AddSeconds(-10);

            var m1 = NewMsg(createTime: limit, status: MessageStatus.Scheduled);
            var m2 = NewMsg(createTime: limit, status: MessageStatus.Failed);
            var m3 = NewMsg(createTime: limit, status: MessageStatus.Succeeded);
            var m4 = NewMsg(createTime: DateTimeOffset.Now, status: MessageStatus.Failed);
            await MessageStorage.SavePublishedAsync(m1, null, default);
            await MessageStorage.SavePublishedAsync(m2, null, default);
            await MessageStorage.SavePublishedAsync(m3, null, default);
            await MessageStorage.SavePublishedAsync(m4, null, default);

            var list = await MessageStorage.GetPublishedMessagesOfNeedRetryAsync(100,
                this.EventBusOptions.StartRetryAfter, this.EventBusOptions.RetryFailedMax,
                this.EventBusOptions.Environment, default);
            list.Any(r => r.Id == m1.Id).ShouldBeTrue();
            list.Any(r => r.Id == m2.Id).ShouldBeTrue();
            list.Any(r => r.Id == m3.Id).ShouldBeFalse();
            list.Any(r => r.Id == m4.Id).ShouldBeFalse();
        }

        public async Task GetReceivedMessagesOfNeedRetryTests()
        {
            var limit = DateTimeOffset.Now.AddSeconds(-this.EventBusOptions.StartRetryAfter).AddSeconds(-10);

            var m1 = NewMsg(createTime: limit, status: MessageStatus.Scheduled);
            var m2 = NewMsg(createTime: limit, status: MessageStatus.Failed);
            var m3 = NewMsg(createTime: limit, status: MessageStatus.Succeeded);
            var m4 = NewMsg(createTime: DateTimeOffset.Now, status: MessageStatus.Failed);
            await MessageStorage.SaveReceivedAsync(m1, default);
            await MessageStorage.SaveReceivedAsync(m2, default);
            await MessageStorage.SaveReceivedAsync(m3, default);
            await MessageStorage.SaveReceivedAsync(m4, default);

            var list = await MessageStorage.GetReceivedMessagesOfNeedRetryAsync(
                EventBusOptions.RetryLimitCount,
                EventBusOptions.StartRetryAfter,
                EventBusOptions.RetryFailedMax,
                EventBusOptions.Environment,
                default);
            list.Any(r => r.Id == m1.Id).ShouldBeTrue();
            list.Any(r => r.Id == m2.Id).ShouldBeTrue();
            list.Any(r => r.Id == m3.Id).ShouldBeFalse();
            list.Any(r => r.Id == m4.Id).ShouldBeFalse();
        }

        // ---- 通用查询(分页/过滤) ------------------------------------------

        public async Task QueryPublishedTests()
        {
            // 单独插一条,避免和 DeleteExpiresTests 残留的脏数据混淆:
            // 我们这里按 eventName+status 强过滤,只要能查到我们这条就行。
            var msg = NewMsg(eventName: $"unique_{Guid.NewGuid():N}",
                status: MessageStatus.Succeeded, expireTime: DateTimeOffset.Now.AddHours(-1));
            var id = await MessageStorage.SavePublishedAsync(msg, null, default);
            var dbMsg = await MessageStorage.FindPublishedByIdAsync(id, default);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);

            var list = await MessageStorage.SearchPublishedAsync(
                EventBusOptions.Environment, TimeBegin, TimeEnd, msg.EventName, msg.Status, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);

            list = await MessageStorage.SearchPublishedAsync(
                EventBusOptions.Environment, TimeBegin, TimeEnd, msg.EventName, null, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);

            list = await MessageStorage.SearchPublishedAsync(
                EventBusOptions.Environment, TimeBegin, TimeEnd, null, null, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);

            list = await MessageStorage.SearchPublishedAsync(
                EventBusOptions.Environment, TimeBegin, TimeEnd, msg.EventName, null, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
        }

        public async Task QueryReceivedTests()
        {
            var msg = NewMsg(eventName: $"unique_{Guid.NewGuid():N}",
                handlerName: $"unique_{Guid.NewGuid():N}",
                status: MessageStatus.Succeeded, expireTime: DateTimeOffset.Now.AddHours(-1));
            var id = await MessageStorage.SaveReceivedAsync(msg, default);
            var dbMsg = await MessageStorage.FindReceivedByIdAsync(id, default);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
            dbMsg.EventName.ShouldBe(msg.EventName);

            // eventName + handlerName + status
            var list = await MessageStorage.SearchReceivedAsync(
                EventBusOptions.Environment, TimeBegin, TimeEnd,
                msg.EventName, msg.EventHandlerName, msg.Status, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);

            // eventName + handlerName, status null
            list = await MessageStorage.SearchReceivedAsync(
                EventBusOptions.Environment, TimeBegin, TimeEnd,
                msg.EventName, msg.EventHandlerName, null, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);

            // eventName only
            list = await MessageStorage.SearchReceivedAsync(
                EventBusOptions.Environment, TimeBegin, TimeEnd,
                msg.EventName, null, null, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);

            // eventName + status
            list = await MessageStorage.SearchReceivedAsync(
                EventBusOptions.Environment, TimeBegin, TimeEnd,
                msg.EventName, null, msg.Status, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);

            // handlerName + status
            list = await MessageStorage.SearchReceivedAsync(
                EventBusOptions.Environment, TimeBegin, TimeEnd,
                null, msg.EventHandlerName, msg.Status, 0, 100, default);
            dbMsg = list.FirstOrDefault(r => r.Id == id);
            dbMsg.ShouldNotBeNull();
            dbMsg.Id.ShouldBe(id);
        }

        // ---- 事务上下文 IsDone 测试 ----------------------------------------

        public void RelationDbStorageTransactionContextCommitTest()
        {
            using var tran = DbContext.Database.BeginTransaction();
            var transactionContext = DbContext.GetTransactionContext();
            transactionContext!.IsDone().ShouldBeFalse();
            tran.Commit();
            transactionContext!.IsDone().ShouldBeTrue();
        }

        public void RelationDbStorageTransactionContextRollbackTest()
        {
            using var tran = DbContext.Database.BeginTransaction();
            var transactionContext = DbContext.GetTransactionContext();
            transactionContext!.IsDone().ShouldBeFalse();
            tran.Rollback();
            transactionContext!.IsDone().ShouldBeTrue();
        }

        public void RelationDbStorageTransactionContextDisposeTest()
        {
            var tran = DbContext.Database.BeginTransaction();
            var transactionContext = DbContext.GetTransactionContext();
            transactionContext!.IsDone().ShouldBeFalse();
            tran.Dispose();
            transactionContext!.IsDone().ShouldBeTrue();
        }

        public void XaTransactionContextCommitTest()
        {
            var tran = new TransactionScope();
            var transactionContext = new XaTransactionContext(Transaction.Current!);
            transactionContext!.IsDone().ShouldBeFalse();
            tran.Complete();
            tran.Dispose();
            transactionContext!.IsDone().ShouldBeTrue();
        }

        public void XaTransactionContextRollbackTest()
        {
            var tran = new TransactionScope();
            var transactionContext = new XaTransactionContext(Transaction.Current!);
            transactionContext!.IsDone().ShouldBeFalse();
            Transaction.Current.Rollback();
            tran.Dispose();
            transactionContext!.IsDone().ShouldBeTrue();
        }

        public void XaTransactionContextDisposeTest()
        {
            var tran = new TransactionScope();
            var transactionContext = new XaTransactionContext(Transaction.Current!);
            transactionContext!.IsDone().ShouldBeFalse();
            tran.Dispose();
            transactionContext!.IsDone().ShouldBeTrue();
        }

        // ---- 状态计数 -------------------------------------------------------
    }
}