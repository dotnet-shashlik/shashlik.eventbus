using System;

namespace Shashlik.EventBus.Extensions.EfCore
{
    public class EventBusEfCoreOptions
    {
        public  Type DbContextType { get; set; }
    }
}