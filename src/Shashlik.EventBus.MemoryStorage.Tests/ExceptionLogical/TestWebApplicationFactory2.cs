using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Shashlik.EventBus.MemoryStorage.Tests.ExceptionLogical
{
    public class TestWebApplicationFactory2<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        public ITestOutputHelper Output { get; set; }

        protected override IHostBuilder CreateHostBuilder()
        {
            var builder = Host.CreateDefaultBuilder();

            return builder.UseEnvironment("EventBusUnitTest2")
                .ConfigureLogging(r =>
                {
                    r.ClearProviders();
                    r.AddXUnit(Output).SetMinimumLevel(LogLevel.Debug);
                })
                .ConfigureAppConfiguration((host, config) =>
                {
                    var file = new FileInfo("./config.yaml").FullName;
                    config.AddYamlFile(file);
                    config.AddEnvironmentVariables();
                })
                .ConfigureWebHostDefaults(x =>
                {
                    x.UseStartup<TStartup>()
                        .UseTestServer();
                });
        }
    }
}