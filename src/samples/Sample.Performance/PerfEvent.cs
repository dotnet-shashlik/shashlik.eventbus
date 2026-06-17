using Shashlik.EventBus;

namespace Sample.Performance
{
    public class PerfEvent : IEvent
    {
        public long Index { get; set; }

        public string Payload { get; set; } = string.Empty;
    }
}
