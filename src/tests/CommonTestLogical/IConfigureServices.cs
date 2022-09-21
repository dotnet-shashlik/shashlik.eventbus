using Microsoft.Extensions.DependencyInjection;

namespace CommonTestLogical
{
    public interface IConfigureServices
    {
        void ConfigureServices(IServiceCollection services);
    }
}