using System;
using System.Data.Common;
using FreeSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Shashlik.EventBus.RelationDbStorage;
using DbContext = Microsoft.EntityFrameworkCore.DbContext;

namespace Shashlik.EventBus.Extensions.EfCore;

public class EfCoreConnectionFactory : IConnectionFactory
{
    public EfCoreConnectionFactory(IServiceScopeFactory serviceScopeFactory, IOptions<EventBusEfCoreOptions> options)
    {
        var type = options.Value.DbContextType;
        using var scope = serviceScopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService(type) as DbContext;
        if (dbContext is null)
            throw new OptionsValidationException("DbContextType", typeof(EventBusEfCoreOptions),
                ["Invalid DbContextType"]);

        var conn = dbContext.Database.GetDbConnection();
        if (string.IsNullOrWhiteSpace(conn.ConnectionString))
            throw new InvalidCastException();
        ConnectionString = conn.ConnectionString;
        DataType = options.Value.DataType;
    }

    public DataType DataType { get; }

    public string ConnectionString { get; }
}