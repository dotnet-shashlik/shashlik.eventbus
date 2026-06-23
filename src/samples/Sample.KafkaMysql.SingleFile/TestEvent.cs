using Shashlik.EventBus;

namespace Sample.KafkaMysql.SingleFile;

public class TestEvent : IEvent
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
}
