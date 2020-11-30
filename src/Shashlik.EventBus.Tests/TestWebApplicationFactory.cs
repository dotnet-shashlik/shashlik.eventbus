using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shashlik.EventBus.Tests
{
    public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override IHostBuilder CreateHostBuilder()
        {
            var builder = Host.CreateDefaultBuilder();

            return builder.UseEnvironment("EventBusUnitTest")
                .ConfigureLogging(r =>
                {
                    r.AddConsole().SetMinimumLevel(LogLevel.Debug);
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