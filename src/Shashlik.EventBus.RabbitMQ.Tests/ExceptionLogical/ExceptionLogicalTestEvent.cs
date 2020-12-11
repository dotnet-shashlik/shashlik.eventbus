using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shashlik.EventBus.RabbitMQ.Tests.ExceptionLogical
{
    public class ExceptionLogicalTestEvent : IEvent
    {
        public static string _Id = Guid.NewGuid().ToString();

        public string Id { get; set; } = _Id;

        public string Name { get; set; }
    }

    public class ExceptionLogicalTestEventHandler : IEventHandler<ExceptionLogicalTestEvent>
    {
        public Task Execute(ExceptionLogicalTestEvent @event, IDictionary<string, string> items)
        {
            return Task.CompletedTask;
        }
    }

    public class ExceptionLogicalTestEventHandler2 : IEventHandler<ExceptionLogicalTestEvent>
    {
        public Task Execute(ExceptionLogicalTestEvent @event, IDictionary<string, string> items)
        {
            return Task.CompletedTask;
        }
    }

    public class ExceptionLogicalTestEventHandler3 : IEventHandler<ExceptionLogicalTestEvent>
    {
        public Task Execute(ExceptionLogicalTestEvent @event, IDictionary<string, string> items)
        {
            return Task.CompletedTask;
        }
    }

    public class ExceptionLogicalTestEventHandler4 : IEventHandler<ExceptionLogicalTestEvent>
    {
        public Task Execute(ExceptionLogicalTestEvent @event, IDictionary<string, string> items)
        {
            return Task.CompletedTask;
        }
    }
}