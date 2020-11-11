using Shashlik.EventBus;

namespace NodeCommon
{
    [EventBusName("Event2")]
    public class Event2222 : IEvent
    {
        public string Name { get; set; }
    }
}