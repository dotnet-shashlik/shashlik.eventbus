using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shashlik.EventBus.Tests
{
    public class TestDelayEvent : IDelayEvent
    {
        public string Name { get; set; }
    }

    public class TestDelayEventHandler : IEventHandler<TestDelayEvent>
    {
        public static TestDelayEvent Instance { get; private set; }

        public static IDictionary<string, string> Items { get; private set; }

        public Task Execute(TestDelayEvent @event, IDictionary<string, string> items)
        {
            Instance = @event;
            Items = items;

            return Task.CompletedTask;
        }
    }
}