using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shashlik.EventBus;
using Shashlik.Utils.Extensions;

namespace CommonTestLogical.TestEvents
{
    public class TestDelayEvent : IDelayEvent
    {
        public string Name { get; set; }
    }

    public class TestDelayEventHandler : IEventHandler<TestDelayEvent>
    {
        public TestDelayEventHandler(ILogger<TestDelayEventHandler> logger)
        {
            Logger = logger;
        }

        public static TestDelayEvent Instance { get; private set; }

        public static IDictionary<string, string> Items { get; private set; }
        private ILogger<TestDelayEventHandler> Logger { get; }

        public Task Execute(TestDelayEvent @event, IDictionary<string, string> items)
        {
            Instance = @event;
            Items = items;

            return Task.CompletedTask;
        }
    }

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