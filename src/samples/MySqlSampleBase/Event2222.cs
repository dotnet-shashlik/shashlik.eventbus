using Shashlik.EventBus;

namespace MySqlSampleBase
{
    [EventBusName("Event2")]
    public class Event2222 : IEvent
    {
        public string Name { get; set; }
    }
}