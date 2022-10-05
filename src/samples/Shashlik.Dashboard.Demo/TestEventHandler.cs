using Shashlik.EventBus;

namespace Shashlik.Dashboard.Demo;

public class TestEventHandler : IEventHandler<TestEvent>
{
    public Task Execute(TestEvent @event, IDictionary<string, string> additionalItems)
    {
        Console.WriteLine("Executing: " + @event.Id);
        var rand = new Random();
        if (rand.Next(5) == 0)
        {
            throw new Exception();
        }

        return Task.CompletedTask;
    }
}