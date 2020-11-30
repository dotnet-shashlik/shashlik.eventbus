using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shashlik.EventBus.Tests
{
    [EventBusName(nameof(TestCustomNameEvent) + "_Test")]
    public class TestCustomNameEvent : IEvent
    {
        public string Name { get; set; }
    }

    [EventBusName(nameof(TestCustomNameEventHandler) + "_Test")]
    public class TestCustomNameEventHandler : IEventHandler<TestCustomNameEvent>
    {
        public static TestCustomNameEvent Instance { get; private set; }

        public static IDictionary<string, string> Items { get; private set; }

        public Task Execute(TestCustomNameEvent @event, IDictionary<string, string> items)
        {
            Instance = @event;
            Items = items;

            return Task.CompletedTask;
        }
    }
}