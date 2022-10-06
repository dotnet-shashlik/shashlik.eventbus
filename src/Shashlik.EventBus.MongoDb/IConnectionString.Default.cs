using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Utils;

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
            if (Options.DbContextType is null)
            {
                if (Options.ConnectionString!.IsNullOrWhiteSpace())
                    throw new OptionsValidationException(
                        nameof(Options.ConnectionString),
                        typeof(EventBusMySqlOptions),
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