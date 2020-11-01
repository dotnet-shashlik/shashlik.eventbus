using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus
{
    public interface IEventBusBuilder
    {
        IServiceCollection ServiceCollection { get; }

        IServiceCollection Build();
    }
}