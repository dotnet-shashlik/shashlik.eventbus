using Shashlik.EventBus;

namespace Shashlik.Dashboard.Demo;

public class TestEvent : IEvent
{
    public string? Title { get; set; }
}