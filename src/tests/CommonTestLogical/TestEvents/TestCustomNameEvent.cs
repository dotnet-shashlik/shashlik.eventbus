using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shashlik.EventBus;

namespace CommonTestLogical.TestEvents
{
    [EventBusName(nameof(TestCustomNameEvent) + "_Test")]
    public class TestCustomNameEvent : IEvent
    {
        public string Name { get; set; }
    }

    [EventBusName(nameof(TestCustomNameEventHandler) + "_Test")]
    public class TestCustomNameEventHandler : IEventHandler<TestCustomNameEvent>
    {
        private static TestCustomNameEvent? _lastInstance;
        private static IDictionary<string, string>? _lastItems;

        public static TestCustomNameEvent? LastInstance => _lastInstance;
        public static IDictionary<string, string>? LastItems => _lastItems;

        public static void Reset()
        {
            Interlocked.Exchange(ref _lastInstance, null);
            Interlocked.Exchange(ref _lastItems, null);
        }

        public static async Task WaitForInstance(TimeSpan timeout)
        {
            var begin = DateTimeOffset.Now;
            while (_lastInstance is null && (DateTimeOffset.Now - begin) < timeout)
                await Task.Delay(50);
        }

        public Task Execute(TestCustomNameEvent @event, IDictionary<string, string> items)
        {
            _lastInstance = @event;
            _lastItems = items;

            return Task.CompletedTask;
        }
    }

    [EventBusName(nameof(TestCustomNameEventGroup2Handler) + "_Test")]
    public class TestCustomNameEventGroup2Handler : IEventHandler<TestCustomNameEvent>
    {
        private static TestCustomNameEvent? _lastInstance;
        private static IDictionary<string, string>? _lastItems;

        public static TestCustomNameEvent? LastInstance => _lastInstance;
        public static IDictionary<string, string>? LastItems => _lastItems;

        public static void Reset()
        {
            Interlocked.Exchange(ref _lastInstance, null);
            Interlocked.Exchange(ref _lastItems, null);
        }

        public static async Task WaitForInstance(TimeSpan timeout)
        {
            var begin = DateTimeOffset.Now;
            while (_lastInstance is null && (DateTimeOffset.Now - begin) < timeout)
                await Task.Delay(50);
        }

        public Task Execute(TestCustomNameEvent @event, IDictionary<string, string> items)
        {
            _lastInstance = @event;
            _lastItems = items;

            return Task.CompletedTask;
        }
    }
}
