using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shashlik.EventBus.Tests.SendMsgWithoutLosing
{
    public class SendMsgWithoutLosingTestEvent : IEvent
    {
        public string Name { get; set; }
    }

    public class SendMsgWithoutLosingTestEventHandler : IEventHandler<SendMsgWithoutLosingTestEvent>
    {
        public Task Execute(SendMsgWithoutLosingTestEvent @event, IDictionary<string, string> items)
        {
            throw  new Exception();
        }
    }
}