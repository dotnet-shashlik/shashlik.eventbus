using System;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using FreeSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Npgsql;
using Shashlik.EventBus;
using Shashlik.EventBus.Kafka;
using Shashlik.EventBus.MemoryQueue;
using Shashlik.EventBus.MemoryStorage;

namespace Sample.Performance
{
    public class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var benchmarkOptions = new BenchmarkOptions();

            var configFile = Path.Combine(AppContext.BaseDirectory, "config.yaml");

            var host = new HostBuilder()
                .ConfigureHostConfiguration(config =>
                {
                    if (File.Exists(configFile))
                        config.AddYamlFile(configFile, optional: false, reloadOnChange: false);
                    config.AddEnvironmentVariables();
                    config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;

                    services.AddOptions<BenchmarkOptions>()
                        .Bind(configuration.GetSection("Benchmark"));
                    configuration.GetSection("Benchmark").Bind(benchmarkOptions);

                    services.AddLogging(logging =>
                    {
                        logging.AddConfiguration(configuration.GetSection("Logging"));
                        logging.AddSimpleConsole(o =>
                        {
                            o.SingleLine = true;
                            o.TimestampFormat = "HH:mm:ss ";
                        });
                    });

                    services.AddSingleton<BenchmarkState>();

                    ConfigureStorageAndMq(services, benchmarkOptions, configuration);

                    services.AddHostedService<BenchmarkRunner>();
                })
                .UseConsoleLifetime()
                .Build();

            try
            {
                await EnsureDatabaseAsync(benchmarkOptions, host.Services).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[FATAL] ensure database failed: {ex.Message}");
                return 1;
            }

            await host.RunAsync().ConfigureAwait(false);
            return 0;
        }

        private static void ConfigureStorageAndMq(
            IServiceCollection services,
            BenchmarkOptions options,
            IConfiguration configuration)
        {
            var storage = (options.Storage ?? "memory").Trim().ToLowerInvariant();
            var mq = (options.MQ ?? "memory").Trim().ToLowerInvariant();

            if (storage == "mysql" || storage == "postgresql")
            {
                var connectionString = configuration.GetConnectionString("Default")
                    ?? throw new InvalidOperationException($"ConnectionStrings:Default is required when Storage={storage}");

                if (storage == "mysql")
                {
                    services.AddDbContextPool<PerfDbContext>(opt =>
                    {
                        opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    }, poolSize: 256);
                }
                else
                {
                    services.AddDbContextPool<PerfDbContext>(opt =>
                    {
                        opt.UseNpgsql(connectionString);
                    }, poolSize: 256);
                }
            }

            var builder = services.AddEventBus(r =>
            {
                r.Environment = options.ResolvedEnvironment;
            });

            switch (storage)
            {
                case "memory":
                    builder.AddMemoryStorage();
                    break;
                case "mysql":
                    builder.AddRelationDb<PerfDbContext>(DataType.MySql);
                    break;
                case "postgresql":
                    builder.AddRelationDb<PerfDbContext>(DataType.PostgreSQL);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported Benchmark:Storage='{options.Storage}'. Supported: memory, mysql, postgresql");
            }

            switch (mq)
            {
                case "memory":
                    builder.AddMemoryQueue();
                    break;
                case "kafka":
                    builder.AddKafka(configuration.GetSection("EventBus:Kafka"));
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported Benchmark:MQ='{options.MQ}'. Supported: memory, kafka");
            }
        }

        private static async Task EnsureDatabaseAsync(
            BenchmarkOptions options,
            IServiceProvider serviceProvider)
        {
            var storage = options.Storage?.Trim().ToLowerInvariant();
            if (storage != "mysql" && storage != "postgresql")
                return;

            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(connectionString)) return;

            if (storage == "mysql")
            {
                var builder = new MySqlConnectionStringBuilder(connectionString);
                var dbName = builder.Database;
                if (string.IsNullOrWhiteSpace(dbName)) return;

                builder.Database = string.Empty;
                await using var conn = new MySqlConnection(builder.ConnectionString);
                await conn.OpenAsync().ConfigureAwait(false);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{dbName}` DEFAULT CHARACTER SET utf8mb4";
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                Console.WriteLine($"[INFO] MySQL database '{dbName}' ensured.");
            }
            else if (storage == "postgresql")
            {
                var builder = new NpgsqlConnectionStringBuilder(connectionString);
                var dbName = builder.Database;
                if (string.IsNullOrWhiteSpace(dbName)) return;

                builder.Database = "postgres";
                await using var conn = new NpgsqlConnection(builder.ConnectionString);
                await conn.OpenAsync().ConfigureAwait(false);
                await using var cmd = conn.CreateCommand();
                // PG 没有 CREATE DATABASE IF NOT EXISTS, 先查再建
                cmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname='{dbName.Replace("'", "''")}'";
                var exists = await cmd.ExecuteScalarAsync().ConfigureAwait(false) != null;
                if (!exists)
                {
                    conn.Close();
                    // CREATE DATABASE 必须在 autocommit 模式下且不能有活跃事务
                    await using var conn2 = new NpgsqlConnection(builder.ConnectionString);
                    await conn2.OpenAsync().ConfigureAwait(false);
                    await using var cmd2 = conn2.CreateCommand();
                    cmd2.CommandText = $"CREATE DATABASE \"{dbName.Replace("\"", "\"\"")}\"";
                    await cmd2.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                Console.WriteLine($"[INFO] PostgreSQL database '{dbName}' ensured.");
            }
        }
    }
}
