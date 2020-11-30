using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shashlik.EventBus.Tests
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
}