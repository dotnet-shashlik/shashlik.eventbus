using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.EventBus;
using Xunit;
using Xunit.Abstractions;

namespace CommonTestLogical
{
    public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        public ITestOutputHelper Output { get; set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSolutionRelativeContentRoot("tests");
        }

        protected override IHostBuilder CreateHostBuilder()
        {
            var builder = Host.CreateDefaultBuilder();

            return builder
                .ConfigureLogging(r =>
                {
                    r.ClearProviders();
                    r.AddXUnit(Output).SetMinimumLevel(LogLevel.Error);
                })
                .ConfigureAppConfiguration((host, config) =>
                {
                    config.AddYamlFile(Path.Combine(Directory.GetCurrentDirectory(), "config.test.yaml"));
                    config.AddEnvironmentVariables();
                })
                .ConfigureWebHostDefaults(x =>
                {
                    Output.WriteLine($"ConfigureWebHostDefaults, {AppDomain.CurrentDomain.FriendlyName}");
                    x.UseStartup<TStartup>();
                });
        }
    }

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
}