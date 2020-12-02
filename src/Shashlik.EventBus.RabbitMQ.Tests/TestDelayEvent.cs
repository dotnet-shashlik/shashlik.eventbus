using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shashlik.Kernel.Dependency;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.RabbitMQ.Tests
{
    public class TestDelayEvent : IDelayEvent
    {
        public string TestId { get; set; } = TestIdClass.TestIdNo;
        public string Name { get; set; }
    }

    [Transient(typeof(IEventHandler<>))]
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
            if (@event.TestId != TestIdClass.TestIdNo)
                return Task.CompletedTask;

            Logger.LogInformation($"NOW: {DateTimeOffset.Now}, TestId: {TestIdClass.TestIdNo}, items: {items.ToJson()}, event: {@event.ToJson()}");
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
            if (@event.TestId != TestIdClass.TestIdNo)
                return Task.CompletedTask;
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
            if (@event.TestId != TestIdClass.TestIdNo)
                return Task.CompletedTask;
            Instance = @event;
            Items = items;

            return Task.CompletedTask;
        }
    }
}