using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus
{
    public interface IEventBusBuilder
    {
        IServiceCollection Services { get; }
    }
}