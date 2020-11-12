using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.PostgreSQL
{
    public class DefaultConnectionString : IConnectionString
    {
        public DefaultConnectionString(IServiceScopeFactory serviceScopeFactory,
            IOptions<EventBusPostgreSQLOptions> options)
        {
            ServiceScopeFactory = serviceScopeFactory;
            Options = options.Value;
            _connectionString = new Lazy<string>(GetConnectionString);
        }

        private readonly Lazy<string> _connectionString;
        private IServiceScopeFactory ServiceScopeFactory { get; }
        private EventBusPostgreSQLOptions Options { get; }

        private string GetConnectionString()
        {
            if (Options.DbContextType == null) return Options.ConnectionString;
            using var scope = ServiceScopeFactory.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService(Options.DbContextType) as DbContext;
            return dbContext!.Database.GetDbConnection().ConnectionString;
        }

        public string ConnectionString => _connectionString.Value;
    }
}