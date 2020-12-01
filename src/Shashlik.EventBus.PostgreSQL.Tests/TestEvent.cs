﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Shashlik.Kernel.Dependency;

namespace Shashlik.EventBus.PostgreSQL.Tests
{
    public class TestEvent : IEvent
    {
        public string Name { get; set; }
    }
}