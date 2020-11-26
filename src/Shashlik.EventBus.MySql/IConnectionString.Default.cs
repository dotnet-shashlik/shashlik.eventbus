using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.MySql
{
    public class DefaultConnectionString : IConnectionString
    {
        public DefaultConnectionString(IServiceScopeFactory serviceScopeFactory, IOptions<EventBusMySqlOptions> options)
        {
            ServiceScopeFactory = serviceScopeFactory;
            Options = options.Value;
            _connectionString = new Lazy<string>(GetConnectionString);
        }

        private readonly Lazy<string> _connectionString;
        private IServiceScopeFactory ServiceScopeFactory { get; }
        private EventBusMySqlOptions Options { get; }

        private string GetConnectionString()
        {
            if (Options.DbContextType is null && string.IsNullOrWhiteSpace(Options.ConnectionString))
                throw new InvalidOperationException($"DbContextType or ConnectionString can't be always empty");
            if (Options.DbContextType == null) return Options.ConnectionString!;
            using var scope = ServiceScopeFactory.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService(Options.DbContextType) as DbContext;
            return dbContext!.Database.GetDbConnection().ConnectionString;
        }

        public string ConnectionString => _connectionString.Value;
    }
}