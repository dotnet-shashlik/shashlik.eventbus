using Shashlik.EventBus;

namespace SampleBase
{
    public class Event1 : IEvent
    {
        public string Name { get; set; }
    }
    
    public class DelayEvent : IDelayEvent
    {
        public string Name { get; set; }
    }
}