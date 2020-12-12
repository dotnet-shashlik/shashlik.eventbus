﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shashlik.EventBus.Tests.ExceptionLogical
{
    public class ExceptionLogicalTestEvent : IEvent
    {
        public string Name { get; set; }
    }

    public class ExceptionLogicalTestEventHandler : IEventHandler<ExceptionLogicalTestEvent>
    {
        public Task Execute(ExceptionLogicalTestEvent @event, IDictionary<string, string> items)
        {
            throw  new Exception();
        }
    }
}