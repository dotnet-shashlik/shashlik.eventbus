using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shashlik.EventBus;

namespace CommonTestLogical.TestEvents
{
    /// <summary>
    /// 异常事件测试
    /// </summary>
    public class TestExceptionEvent : IEvent
    {
        public string Name { get; set; }
    }

    public class TestExceptionEventHandler : IEventHandler<TestExceptionEvent>
    {
        public TestExceptionEventHandler(ILogger<TestExceptionEventHandler> logger)
        {
            Logger = logger;
        }

        public static TestExceptionEvent Instance { get; private set; }

        public static IDictionary<string, string> Items { get; private set; }
        public static int Counter { get; private set; }
        private ILogger<TestExceptionEventHandler> Logger { get; }
        private static readonly object Lck = new object();

        public Task Execute(TestExceptionEvent @event, IDictionary<string, string> items)
        {
            CounterAutoIncrement();
            throw new Exception("执行异常啦...");
        }

        private void CounterAutoIncrement()
        {
            lock (Lck)
            {
                Counter++;
            }
        }
    }

    public class TestExceptionEventGroup2Handler : IEventHandler<TestExceptionEvent>
    {
        public TestExceptionEventGroup2Handler(ILogger<TestExceptionEventHandler> logger)
        {
            Logger = logger;
        }

        public static TestExceptionEvent Instance { get; private set; }

        public static IDictionary<string, string> Items { get; private set; }
        public static int Counter { get; private set; }
        private ILogger<TestExceptionEventHandler> Logger { get; }
        private static readonly object Lck = new object();

        public Task Execute(TestExceptionEvent @event, IDictionary<string, string> items)
        {
            // 模拟执行5次后，恢复正常
            if (Counter >= 5)
            {
                Instance = @event;
                Items = items;
            }
            else
            {
                CounterAutoIncrement();
                throw new Exception("...");
            }

            return Task.CompletedTask;
        }

        private void CounterAutoIncrement()
        {
            lock (Lck)
            {
                Counter++;
            }
        }
    }
}