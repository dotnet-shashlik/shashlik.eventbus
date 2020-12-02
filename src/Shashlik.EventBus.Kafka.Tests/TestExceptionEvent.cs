using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shashlik.Kernel.Dependency;

namespace Shashlik.EventBus.Kafka.Tests
{
    /// <summary>
    /// 异常事件测试
    /// </summary>
    public class TestExceptionEvent : IEvent
    {
        public string TestId { get; set; } = TestIdClass.TestIdNo;
        public string Name { get; set; }
    }

    [Transient(typeof(IEventHandler<>))]
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
            if (@event.TestId != TestIdClass.TestIdNo)
                return Task.CompletedTask;
            Counter++;
            throw new Exception("...111");
        }
    }

    [Transient(typeof(IEventHandler<>))]
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
            if (@event.TestId != TestIdClass.TestIdNo)
                return Task.CompletedTask;

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