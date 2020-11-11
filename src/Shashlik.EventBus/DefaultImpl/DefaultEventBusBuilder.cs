using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus.DefaultImpl
{
    internal class DefaultEventBusBuilder : IEventBusBuilder
    {
        public DefaultEventBusBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}