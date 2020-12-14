using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace CommonTestLogical
{
    public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        public ITestOutputHelper Output { get; set; }

        protected override IHostBuilder CreateHostBuilder()
        {
            var builder = Host.CreateDefaultBuilder();

            return builder
                .ConfigureLogging(r =>
                {
                    r.ClearProviders();
                    r.AddXUnit(Output).SetMinimumLevel(LogLevel.Debug);
                })
                .ConfigureAppConfiguration((host, config) =>
                {
                    config.AddYamlFile("config.yaml");
                    config.AddEnvironmentVariables();
                })
                .ConfigureWebHostDefaults(x => { x.UseStartup<TStartup>(); });
        }
    }

    public abstract class TestBase<TStartup> : IClassFixture<TestWebApplicationFactory<TStartup>>, IDisposable
        where TStartup : class
    {
        protected TestWebApplicationFactory<TStartup> Factory { get; }
        protected HttpClient HttpClient { get; }
        protected IServiceScope ServiceScope { get; }

        public TestBase(TestWebApplicationFactory<TStartup> factory, ITestOutputHelper testOutputHelper)
        {
            Factory = factory;
            factory.Output = testOutputHelper;
            HttpClient = factory.CreateClient();
            ServiceScope = factory.Services.CreateScope();
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
}