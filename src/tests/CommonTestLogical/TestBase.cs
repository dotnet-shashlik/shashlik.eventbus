using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shashlik.EventBus;
using Xunit;
using Xunit.Abstractions;

namespace CommonTestLogical;

public abstract class TestBase<TStartup> : IClassFixture<TestWebApplicationFactory<TStartup>>, IDisposable
    where TStartup : class
{
    protected TestWebApplicationFactory<TStartup> Factory { get; }
    protected HttpClient HttpClient { get; }
    protected IServiceScope ServiceScope { get; }
    protected EventBusOptions Options { get; }

    public TestBase(TestWebApplicationFactory<TStartup> factory, ITestOutputHelper testOutputHelper)
    {
        Factory = factory;
        factory.Output = testOutputHelper;
        HttpClient = factory.CreateClient();
        ServiceScope = factory.Services.CreateScope();
        Options = GetService<IOptions<EventBusOptions>>().Value;
    }

    protected T GetService<T>()
    {
        return ServiceScope.ServiceProvider.GetService<T>();
    }

    protected IEnumerable<T> GetServices<T>()
    {
        return ServiceScope.ServiceProvider.GetServices<T>();
    }

    public virtual void Dispose()
    {
        ServiceScope.Dispose();
    }
}