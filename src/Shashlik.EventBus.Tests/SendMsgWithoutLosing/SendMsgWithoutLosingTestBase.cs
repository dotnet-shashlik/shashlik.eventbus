using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.Tests.SendMsgWithoutLosing
{
    public class SendMsgWithoutLosingTestBase : IClassFixture<SendMsgWithoutLosingFactory<SendMsgWithoutLosingTestStartup>>, IDisposable
    {
        protected SendMsgWithoutLosingFactory<SendMsgWithoutLosingTestStartup> Factory { get; }
        protected HttpClient HttpClient { get; }
        protected IServiceScope ServiceScope { get; }
        public static string Env { get; } = "EventBusTest2";

        public SendMsgWithoutLosingTestBase(SendMsgWithoutLosingFactory<SendMsgWithoutLosingTestStartup> factory, ITestOutputHelper testOutputHelper)
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