using Shashlik.EventBus;

namespace CommonTestLogical.TestEvents
{
    public class XaEvent : IEvent
    {
        public string Name { get; set; }
    }
}