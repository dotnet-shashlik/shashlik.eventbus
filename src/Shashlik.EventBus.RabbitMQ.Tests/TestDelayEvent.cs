using System.Collections.Generic;
using System.Threading.Tasks;
using Shashlik.Kernel.Dependency;

namespace Shashlik.EventBus.Tests
{
    public class TestDelayEvent : IDelayEvent
    {
        public string Name { get; set; }
    }

    [Transient(typeof(IEventHandler<>))]
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

    [Transient(typeof(IEventHandler<>))]
    public class TestDelayEventGroup2Handler : IEventHandler<TestDelayEvent>
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

    [Transient(typeof(IEventHandler<>))]
    public class TestDelayEventGroup3Handler : IEventHandler<TestDelayEvent>
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