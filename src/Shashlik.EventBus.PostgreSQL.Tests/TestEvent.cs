using System.Collections.Generic;
using System.Threading.Tasks;
using Shashlik.Kernel.Dependency;

namespace Shashlik.EventBus.Tests
{
    public class TestEvent : IEvent
    {
        public string Name { get; set; }
    }

    [Transient(typeof(IEventHandler<>))]
    public class TestEventHandler : IEventHandler<TestEvent>
    {
        public static TestEvent Instance { get; private set; }

        public static IDictionary<string, string> Items { get; private set; }

        public Task Execute(TestEvent @event, IDictionary<string, string> items)
        {
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
            Instance = @event;
            Items = items;

            return Task.CompletedTask;
        }
    }
}