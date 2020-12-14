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

        public Task Execute(TestExceptionEvent @event, IDictionary<string, string> items)
        {
            Counter++;
            throw new Exception("...111");
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
                Counter++;
                throw new Exception("...2222");
            }

            return Task.CompletedTask;
        }
    }
}