using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shashlik.Utils.Extensions;

// ReSharper disable ConvertIfStatementToSwitchExpression
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable RedundantIfElseBlock

namespace Shashlik.EventBus.MemoryStorage
{
    public class MemoryMessageStorage : IMessageStorage
    {
        private readonly ConcurrentDictionary<long, MessageStorageModel> _published = new ConcurrentDictionary<long, MessageStorageModel>();
        private readonly ConcurrentDictionary<long, MessageStorageModel> _received = new ConcurrentDictionary<long, MessageStorageModel>();
        private static int _lastId;
        private static readonly object IdLck = new object();
        private static readonly object PublishedRetryLck = new object();
        private static readonly object ReceivedRetryLck = new object();

        private static long AutoIncrementId()
        {
            lock (IdLck)
            {
                _lastId++;
            }

            return _lastId;
        }

        public ValueTask<bool> TransactionIsCommitted(string msgId, ITransactionContext? transactionContext, CancellationToken cancellationToken)
        {
            return new ValueTask<bool>(_published.Any(r => r.Value.MsgId == msgId));
        }

        public async Task<MessageStorageModel?> FindPublishedByMsgId(string msgId, CancellationToken cancellationToken)
        {
            var res = _published.Values.FirstOrDefault(r => r.MsgId == msgId);
            if (res != null)
                res.EventHandlerName = null;
            return await Task.FromResult(res);
        }

        public Task<MessageStorageModel?> FindPublishedById(long id, CancellationToken cancellationToken)
        {
            return Task.FromResult<MessageStorageModel?>(_published.GetOrDefault(id));
        }

        public async Task<MessageStorageModel?> FindReceivedByMsgId(string msgId, EventHandlerDescriptor eventHandlerDescriptor,
            CancellationToken cancellationToken)
        {
            return await Task.FromResult(_received.Values.FirstOrDefault(r =>
                r.MsgId == msgId && r.EventHandlerName == eventHandlerDescriptor.EventHandlerName));
        }

        public Task<MessageStorageModel?> FindReceivedById(long id, CancellationToken cancellationToken)
        {
            return Task.FromResult<MessageStorageModel?>(_received.GetOrDefault(id));
        }

        public Task<List<MessageStorageModel>> SearchPublished(string eventName, string status, int skip, int take,
            CancellationToken cancellationToken)
        {
            var list = _published.Values
                .Where(r => r.EventName == eventName && r.Status == status)
                .Skip(skip)
                .Take(take)
                .ToList();
            return Task.FromResult(list);
        }

        public Task<List<MessageStorageModel>> SearchReceived(string eventName, string eventHandlerName, string status, int skip, int take,
            CancellationToken cancellationToken)
        {
            var list = _received.Values
                .Where(r => r.EventName == eventName && r.Status == status)
                .Skip(skip)
                .Take(take)
                .ToList();
            return Task.FromResult(list);
        }

        public Task<long> SavePublished(MessageStorageModel message, ITransactionContext? transactionContext, CancellationToken cancellationToken)
        {
            message.Id = AutoIncrementId();
            if (_published.TryAdd(message.Id, message))
                return Task.FromResult(message.Id);
            throw new Exception($"save published message error, msgId: {message.MsgId}.");
        }

        public Task<long> SaveReceived(MessageStorageModel message, CancellationToken cancellationToken)
        {
            message.Id = AutoIncrementId();
            if (_received.TryAdd(message.Id, message))
                return Task.FromResult(message.Id);
            throw new Exception($"save received message error, msgId: {message.MsgId}.");
        }

        public Task UpdatePublished(long id, string status, int retryCount, DateTimeOffset? expireTime, CancellationToken cancellationToken)
        {
            if (_published.TryGetValue(id, out var model))
            {
                model.Status = status;
                model.RetryCount = retryCount;
                model.ExpireTime = expireTime;
            }
            else
                throw new InvalidOperationException();

            return Task.CompletedTask;
        }

        public Task UpdateReceived(long id, string status, int retryCount,
            DateTimeOffset? expireTime, CancellationToken cancellationToken)
        {
            if (_received.TryGetValue(id, out var model))
            {
                model.Status = status;
                model.RetryCount = retryCount;
                model.ExpireTime = expireTime;
            }
            else
                throw new InvalidOperationException();

            return Task.CompletedTask;
        }

        public Task<bool> TryLockReceived(long id, DateTimeOffset lockEndAt,
            CancellationToken cancellationToken)
        {
            if (lockEndAt <= DateTimeOffset.Now)
                throw new ArgumentOutOfRangeException(nameof(lockEndAt));
            if (_received.TryGetValue(id, out var model))
            {
                if (!model.IsLocking || DateTimeOffset.Now > model.LockEnd)
                {
                    LockAndUpdate(model, r =>
                    {
                        model.IsLocking = true;
                        model.LockEnd = lockEndAt;
                    });
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
            else
                return Task.FromResult(false);
        }

        private void LockAndUpdate(MessageStorageModel model, Action<MessageStorageModel> update)
        {
            lock (this)
            {
                update(model);
            }
        }

        public Task DeleteExpires(CancellationToken cancellationToken)
        {
            var items1 = _published.Values.Where(r => r.ExpireTime.HasValue && r.ExpireTime < DateTimeOffset.Now).ToList();
            foreach (var item in items1)
                _published.Remove(item.Id, out _);

            var items2 = _received.Values.Where(r => r.ExpireTime.HasValue && r.ExpireTime < DateTimeOffset.Now).ToList();
            foreach (var item in items2)
                _received.Remove(item.Id, out _);

            return Task.CompletedTask;
        }

        public Task<List<MessageStorageModel>> GetPublishedMessagesOfNeedRetryAndLock(int count, int delayRetrySecond, int maxFailedRetryCount,
            string environment, int lockSecond,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(GetPublishedMessagesOfNeedRetryAndLock(count, delayRetrySecond, maxFailedRetryCount, environment, lockSecond));
        }

        public List<MessageStorageModel> GetPublishedMessagesOfNeedRetryAndLock(int count, int delayRetrySecond, int maxFailedRetryCount,
            string environment, int lockSecond)
        {
            lock (PublishedRetryLck)
            {
                var createTimeLimit = DateTime.Now.AddSeconds(-delayRetrySecond);
                var now = DateTime.Now;
                var res = new List<MessageStorageModel>();
                foreach (var r in _published.Values)
                {
                    if (r.Environment == environment
                        && r.CreateTime < createTimeLimit
                        && r.RetryCount < maxFailedRetryCount
                        && !r.IsLocking || r.LockEnd < now
                        && (r.Status == MessageStatus.Scheduled || r.Status == MessageStatus.Failed))
                    {
                        r.IsLocking = true;
                        r.LockEnd = DateTimeOffset.Now.AddSeconds(lockSecond);
                        res.Add(r);
                    }
                }

                return res;
            }
        }

        public Task<List<MessageStorageModel>> GetReceivedMessagesOfNeedRetryAndLock(int count, int delayRetrySecond, int maxFailedRetryCount,
            string environment, int lockSecond,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(GetReceivedMessagesOfNeedRetryAndLock(count, delayRetrySecond, maxFailedRetryCount, environment, lockSecond));
        }

        public List<MessageStorageModel> GetReceivedMessagesOfNeedRetryAndLock(int count, int delayRetrySecond, int maxFailedRetryCount,
            string environment, int lockSecond)
        {
            lock (ReceivedRetryLck)
            {
                var createTimeLimit = DateTime.Now.AddSeconds(-delayRetrySecond);
                var now = DateTime.Now;
                var res = new List<MessageStorageModel>();
                foreach (var r in _received.Values)
                {
                    if (r.Environment == environment
                        && r.CreateTime < createTimeLimit
                        && r.RetryCount < maxFailedRetryCount
                        && !r.IsLocking || r.LockEnd < now
                        && (r.Status == MessageStatus.Scheduled || r.Status == MessageStatus.Failed))
                    {
                        r.IsLocking = true;
                        r.LockEnd = DateTimeOffset.Now.AddSeconds(lockSecond);
                        res.Add(r);
                    }
                }

                return res;
            }
        }
    }
}