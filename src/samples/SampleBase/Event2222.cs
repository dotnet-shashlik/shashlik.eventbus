using Shashlik.EventBus;

namespace SampleBase
{
    [EventBusName("Event2")]
    public class Event2222 : IEvent
    {
        public string Name { get; set; }
    }
}