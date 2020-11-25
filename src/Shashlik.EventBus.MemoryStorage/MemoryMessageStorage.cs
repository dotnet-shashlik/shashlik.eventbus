using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shashlik.Utils.Extensions;

// ReSharper disable ConvertIfStatementToSwitchExpression

// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable RedundantIfElseBlock

namespace Shashlik.EventBus.MemoryStorage
{
    public class MemoryMessageStorage : IMessageStorage
    {
        private readonly ConcurrentDictionary<string, MessageStorageModel> _published = new ConcurrentDictionary<string, MessageStorageModel>();
        private readonly ConcurrentDictionary<string, MessageStorageModel> _received = new ConcurrentDictionary<string, MessageStorageModel>();

        public ValueTask<bool> ExistsPublishMessage(string msgId, CancellationToken cancellationToken)
        {
            return new ValueTask<bool>(_published.ContainsKey(msgId));
        }

        public ValueTask<bool> ExistsReceiveMessage(string msgId, CancellationToken cancellationToken)
        {
            return new ValueTask<bool>(_received.ContainsKey(msgId));
        }

        public Task<MessageStorageModel> FindPublishedById(string id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_published.GetOrDefault(id));
        }

        public Task<MessageStorageModel> FindReceivedById(string id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_received.GetOrDefault(id));
        }

        public Task SavePublished(MessageStorageModel message, TransactionContext transactionContext, CancellationToken cancellationToken)
        {
            _published.TryAdd(message.MsgId, message);
            return Task.CompletedTask;
        }

        public Task SaveReceived(MessageStorageModel message, CancellationToken cancellationToken)
        {
            _received.TryAdd(message.MsgId, message);
            return Task.CompletedTask;
        }

        public Task UpdatePublished(string msgId, string status, int retryCount, DateTimeOffset? expireTime, CancellationToken cancellationToken)
        {
            if (_published.TryGetValue(msgId, out var model))
            {
                model.Status = status;
                model.RetryCount = retryCount;
                model.ExpireTime = expireTime;
            }
            else
                throw new InvalidOperationException();

            return Task.CompletedTask;
        }

        public Task UpdateReceived(string msgId, string status, int retryCount, DateTimeOffset? expireTime, CancellationToken cancellationToken)
        {
            if (_received.TryGetValue(msgId, out var model))
            {
                model.Status = status;
                model.RetryCount = retryCount;
                model.ExpireTime = expireTime;
            }
            else
                throw new InvalidOperationException();

            return Task.CompletedTask;
        }

        public Task<bool> TryLockReceived(string msgId, DateTimeOffset lockEndAt, CancellationToken cancellationToken)
        {
            if (lockEndAt <= DateTimeOffset.Now)
                throw new ArgumentOutOfRangeException(nameof(lockEndAt));
            if (_received.TryGetValue(msgId, out var model))
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
            var items1 = _published.Where(r => r.Value.ExpireTime.HasValue && r.Value.ExpireTime < DateTimeOffset.Now).ToList();
            foreach (var item in items1)
                _published.Remove(item.Key, out _);

            var items2 = _received.Where(r => r.Value.ExpireTime.HasValue && r.Value.ExpireTime < DateTimeOffset.Now).ToList();
            foreach (var item in items2)
                _received.Remove(item.Key, out _);

            return Task.CompletedTask;
        }

        public Task<List<MessageStorageModel>> GetPublishedMessagesOfNeedRetryAndLock(int count, int delayRetrySecond, int maxFailedRetryCount,
            string environment, int lockSecond,
            CancellationToken cancellationToken)
        {
            var createTimeLimit = DateTime.Now.AddSeconds(-delayRetrySecond);
            var now = DateTime.Now;

            var list = _published.Values.Where(r =>
                    r.Environment == environment
                    && r.CreateTime < createTimeLimit
                    && r.RetryCount < maxFailedRetryCount
                    && !r.IsLocking || r.LockEnd < now
                    && (r.Status == MessageStatus.Scheduled || r.Status == MessageStatus.Failed)
                )
                .ToList();

            return Task.FromResult(list);
        }

        public Task<List<MessageStorageModel>> GetReceivedMessagesOfNeedRetryAndLock(int count, int delayRetrySecond, int maxFailedRetryCount,
            string environment, int lockSecond,
            CancellationToken cancellationToken)
        {
            var createTimeLimit = DateTime.Now.AddSeconds(-delayRetrySecond);
            var now = DateTime.Now;

            var list = _received.Values.Where(r =>
                    r.Environment == environment
                    && r.CreateTime < createTimeLimit
                    && r.RetryCount < maxFailedRetryCount
                    && !r.IsLocking || r.LockEnd < now
                    && (r.Status == MessageStatus.Scheduled || r.Status == MessageStatus.Failed)
                )
                .ToList();

            return Task.FromResult(list);
        }
    }
}