using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.Sqlite
{
    public class DefaultConnectionString : IConnectionString
    {
        public DefaultConnectionString(IServiceScopeFactory serviceScopeFactory, IOptions<EventBusSqliteOptions> options)
        {
            ServiceScopeFactory = serviceScopeFactory;
            Options = options.Value;
            _connectionString = new Lazy<string>(GetConnectionString);
        }

        private readonly Lazy<string> _connectionString;
        private IServiceScopeFactory ServiceScopeFactory { get; }
        private EventBusSqliteOptions Options { get; }

        private string GetConnectionString()
        {
            if (Options.DbContextType is null)
            {
                if (Options.ConnectionString!.IsNullOrWhiteSpace())
                    throw new OptionsValidationException(
                        nameof(Options.ConnectionString),
                        typeof(EventBusSqliteOptions),
                        new[] {"ConnectionString and DbContextType can't all be empty"});
                return Options.ConnectionString!;
            }

            using var scope = ServiceScopeFactory.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService(Options.DbContextType) as DbContext;
            return dbContext!.Database.GetDbConnection().ConnectionString;
        }

        public string ConnectionString => _connectionString.Value;
    }
}