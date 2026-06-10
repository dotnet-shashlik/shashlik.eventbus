using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shashlik.EventBus;
using Shashlik.EventBus.Utils;

namespace CommonTestLogical.TestEvents
{
    public class TestDelayEvent : IEvent
    {
        public string Name { get; set; }
    }

    public class TestDelayEventHandler : IEventHandler<TestDelayEvent>
    {
        public TestDelayEventHandler(ILogger<TestDelayEventHandler> logger)
        {
            Logger = logger;
        }

        private static TestDelayEvent? _lastInstance;
        private static IDictionary<string, string>? _lastItems;
        private ILogger<TestDelayEventHandler> Logger { get; }

        public static TestDelayEvent? LastInstance => _lastInstance;
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

        public Task Execute(TestDelayEvent @event, IDictionary<string, string> items)
        {
            _lastInstance = @event;
            _lastItems = items;

            return Task.CompletedTask;
        }
    }

    public class TestDelayEventGroup2Handler : IEventHandler<TestDelayEvent>
    {
        private static TestDelayEvent? _lastInstance;
        private static IDictionary<string, string>? _lastItems;

        public static TestDelayEvent? LastInstance => _lastInstance;
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

        public Task Execute(TestDelayEvent @event, IDictionary<string, string> items)
        {
            _lastInstance = @event;
            _lastItems = items;

            return Task.CompletedTask;
        }
    }

    public class TestDelayEventGroup3Handler : IEventHandler<TestDelayEvent>
    {
        private static TestDelayEvent? _lastInstance;
        private static IDictionary<string, string>? _lastItems;

        public static TestDelayEvent? LastInstance => _lastInstance;
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

        public Task Execute(TestDelayEvent @event, IDictionary<string, string> items)
        {
            _lastInstance = @event;
            _lastItems = items;

            return Task.CompletedTask;
        }
    }
}
