using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shashlik.Kernel.Dependency;

namespace Shashlik.EventBus.Kafka.Tests
{
    public class TestEvent : IEvent
    {
        public string TestId { get; set; } = TestIdClass.TestIdNo;
        public string Name { get; set; }
    }

    [Transient(typeof(IEventHandler<>))]
    public class TestEventHandler : IEventHandler<TestEvent>
    {
        public static TestEvent Instance { get; private set; }

        public static IDictionary<string, string> Items { get; private set; }

        public Task Execute(TestEvent @event, IDictionary<string, string> items)
        {
            if (@event.TestId != TestIdClass.TestIdNo)
                return Task.CompletedTask;
            Instance = @event;
            Items = items;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 组2测试
    /// </summary>
    [Transient(typeof(IEventHandler<>))]
    public class TestEventGroup2Handler : IEventHandler<TestEvent>
    {
        public static TestEvent Instance { get; private set; }

        public static IDictionary<string, string> Items { get; private set; }

        public Task Execute(TestEvent @event, IDictionary<string, string> items)
        {
            if (@event.TestId != TestIdClass.TestIdNo)
                return Task.CompletedTask;
            Instance = @event;
            Items = items;

            return Task.CompletedTask;
        }
    }
}