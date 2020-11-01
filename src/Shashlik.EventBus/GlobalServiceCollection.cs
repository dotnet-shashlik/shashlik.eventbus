using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus
{
    public static class GlobalServiceCollection
    {
        public static IServiceCollection ServiceCollection { get; internal set; }
    }
}