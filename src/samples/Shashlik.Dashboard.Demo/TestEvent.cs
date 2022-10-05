using Shashlik.EventBus;

namespace Shashlik.Dashboard.Demo;

public class TestEvent : IEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
}