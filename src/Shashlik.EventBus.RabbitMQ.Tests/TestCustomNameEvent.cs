using System.Collections.Generic;
using System.Threading.Tasks;
using Shashlik.Kernel.Dependency;

namespace Shashlik.EventBus.RabbitMQ.Tests
{
    [EventBusName(nameof(TestCustomNameEvent) + "_Test")]
    public class TestCustomNameEvent : IEvent
    {
        public string TestId { get; set; } = TestIdClass.TestIdNo;
        public string Name { get; set; }
    }

    [Transient(typeof(IEventHandler<>))]
    [EventBusName(nameof(TestCustomNameEventHandler) + "_Test")]
    public class TestCustomNameEventHandler : IEventHandler<TestCustomNameEvent>
    {
        public static TestCustomNameEvent Instance { get; private set; }

        public static IDictionary<string, string> Items { get; private set; }

        public Task Execute(TestCustomNameEvent @event, IDictionary<string, string> items)
        {
            if (@event.TestId != TestIdClass.TestIdNo)
                return Task.CompletedTask;
            Instance = @event;
            Items = items;

            return Task.CompletedTask;
        }
    }

    [Transient(typeof(IEventHandler<>))]
    [EventBusName(nameof(TestCustomNameEventGroup2Handler) + "_Test")]
    public class TestCustomNameEventGroup2Handler : IEventHandler<TestCustomNameEvent>
    {
        public static TestCustomNameEvent Instance { get; private set; }

        public static IDictionary<string, string> Items { get; private set; }

        public Task Execute(TestCustomNameEvent @event, IDictionary<string, string> items)
        {
            if (@event.TestId != TestIdClass.TestIdNo)
                return Task.CompletedTask;
            Instance = @event;
            Items = items;

            return Task.CompletedTask;
        }
    }
}