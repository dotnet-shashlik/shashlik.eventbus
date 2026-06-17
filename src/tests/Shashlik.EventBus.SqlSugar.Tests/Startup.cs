using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus.MemoryQueue;
using Shashlik.EventBus.RelationDbStorage;
using Shashlik.Kernel;
using Shashlik.Utils.Extensions;
using SqlSugar;
using DataType = FreeSql.DataType;
using DbType = SqlSugar.DbType;

namespace Shashlik.EventBus.SqlSugar.Tests
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }
        private readonly string _env = CommonTestLogical.Utils.RandomEnv();

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddControllers()
                .AddControllersAsServices();

            services.AddAuthentication();
            services.AddAuthorization();

            var mySqlConn = Configuration.GetConnectionString("MySql");

            var sugar = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = mySqlConn,
                DbType = DbType.MySqlConnector,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });

            sugar.DbMaintenance.CreateDatabase();
            sugar.CodeFirst.InitTables<SsTestUser>();

            services.AddSingleton<ISqlSugarClient>(sugar);

            services.AddEventBus(r =>
                {
                    var options = Configuration.GetSection("EventBus")
                        .Get<EventBusOptions>();
                    options.CopyTo(r);
                    r.Environment = _env;
                })
                .AddMemoryQueue()
                .AddRelationDb(opt => opt.UseConnection(DataType.MySql, mySqlConn));

            services.AddShashlik(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.ApplicationServices.UseShashlik()
                .AssembleServiceProvider()
                ;
        }
    }

    [SugarTable("sqlsugar_test_users")]
    public class SsTestUser
    {
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
        public int Id { get; set; }

        public string Name { get; set; }
    }
}