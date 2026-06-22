using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
                    var envName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "";
                    var configFile = envName.Equals("GitHub", StringComparison.OrdinalIgnoreCase)
                        ? "config.test-github.yaml"
                        : "config.test.yaml";
                    var file = Path.Combine(Directory.GetCurrentDirectory(), configFile);
                    Console.WriteLine($"config file--->: {file}");
                    config.AddYamlFile(file);
                    config.AddEnvironmentVariables();
                })
                .ConfigureWebHostDefaults(x =>
                {
                    Output.WriteLine($"ConfigureWebHostDefaults, {AppDomain.CurrentDomain.FriendlyName}");
                    x.UseStartup<TStartup>();
                });
        }
    }
}