﻿using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus.MemoryQueue
{
    public class MemoryEventSubscriber : IEventSubscriber
    {
        public Task Subscribe(IMessageListener listener, CancellationToken token)
        {
            InternalMemoryQueue.AddListener(listener);
            return Task.CompletedTask;
        }
    }
}