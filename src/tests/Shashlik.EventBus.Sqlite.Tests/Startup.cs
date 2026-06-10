using System;
using System.IO;
using CommonTestLogical.EfCore;
using FreeSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus.MemoryQueue;
using Shashlik.EventBus.RelationDbStorage;
using Shashlik.Kernel;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.Sqlite.Tests
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }
        private readonly string _env = CommonTestLogical.Utils.RandomEnv();

        // 一个进程内,所有 test collection 共享一个 Sqlite 文件,避免多 host 抢占文件锁
        private static readonly string SqliteFile = Path.Combine(
            AppContext.BaseDirectory, "eventbus-test.db");

        // FreeSql.Provider.Sqlite 依赖 System.Data.SQLite(老 ADO.NET provider),与
        // Microsoft.Data.Sqlite 不互通。EventBus 框架会自己 CodeFirst 建表,
        // 这里给一个最简连接串(只声明 Data Source,其他走默认)。
        private static readonly string FreeSqlConnString =
            $"Data Source={SqliteFile};Version=3;";

        // EF Core 用 Microsoft.Data.Sqlite + 共享 cache,允许同进程多连接
        private static readonly string EfConnString =
            new SqliteConnectionStringBuilder
            {
                DataSource = SqliteFile,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddControllers()
                .AddControllersAsServices();

            services.AddAuthentication();
            services.AddAuthorization();

            // 用 EF Core Sqlite provider 跑业务侧数据(Sqlite EF 走 Microsoft.Data.Sqlite)
            services.AddDbContextPool<DemoDbContext>(r =>
            {
                r.UseSqlite(EfConnString,
                    db => { db.MigrationsAssembly(GetType().Assembly.GetName().FullName); });
            }, 5);

            // 首次启动时确保 EF 侧 Users 表存在
            EnsureEfSchema();

            // 用 FreeSql Sqlite provider 作为 EventBus 存储(走 System.Data.SQLite,两者独立)
            services.AddEventBus(r =>
                {
                    var options = Configuration.GetSection("EventBus")
                        .Get<EventBusOptions>();
                    options.CopyTo(r);
                    r.Environment = _env;
                })
                .AddMemoryQueue()
                .AddRelationDb(opt => opt.UseConnection(DataType.Sqlite, FreeSqlConnString));

            services.AddShashlik(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.ApplicationServices.UseShashlik()
                .AssembleServiceProvider()
                ;
        }

        private static void EnsureEfSchema()
        {
            using var conn = new SqliteConnection(EfConnString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }
    }
}