using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shashlik.EventBus;

namespace CommonTestLogical.MsgWithoutLosing
{
    public class MsgWithoutLosingTestEvent : IEvent
    {
        public static string _Id = Guid.NewGuid().ToString();

        public string Id { get; set; } = _Id;

        public string Name { get; set; }
    }

    public class MsgWithoutLosingEventHandler : IEventHandler<MsgWithoutLosingTestEvent>
    {
        public Task Execute(MsgWithoutLosingTestEvent @event, IDictionary<string, string> items)
        {
            return Task.CompletedTask;
        }
    }

    public class ExceptionLogicalTestEventHandler2 : IEventHandler<MsgWithoutLosingTestEvent>
    {
        public Task Execute(MsgWithoutLosingTestEvent @event, IDictionary<string, string> items)
        {
            return Task.CompletedTask;
        }
    }

    public class ExceptionLogicalTestEventHandler3 : IEventHandler<MsgWithoutLosingTestEvent>
    {
        public Task Execute(MsgWithoutLosingTestEvent @event, IDictionary<string, string> items)
        {
            return Task.CompletedTask;
        }
    }

    public class ExceptionLogicalTestEventHandler4 : IEventHandler<MsgWithoutLosingTestEvent>
    {
        public Task Execute(MsgWithoutLosingTestEvent @event, IDictionary<string, string> items)
        {
            return Task.CompletedTask;
        }
    }
}