using System.Collections.Generic;
using System.Threading.Tasks;
using Shashlik.EventBus;

namespace CommonTestLogical.TestEvents
{
    public class TestEvent : IEvent
    {
        public string Name { get; set; }
    }

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