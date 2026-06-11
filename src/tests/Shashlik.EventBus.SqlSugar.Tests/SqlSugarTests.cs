using System;
using System.Threading.Tasks;
using CommonTestLogical;
using CommonTestLogical.TestEvents;
using Shashlik.EventBus.Extensions.SqlSugar;
using Shouldly;
using SqlSugar;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.SqlSugar.Tests
{
    [Collection("Shashlik.EventBus.SqlSugar.Tests")]
    public class SqlSugarTransactionExtensionsTests : TestBase<Startup>
    {
        public SqlSugarTransactionExtensionsTests(TestWebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper)
            : base(factory, testOutputHelper)
        {
        }

        // ---- ISqlSugarClient.GetTransactionContext (单库事务) -----------------

        [Fact]
        public async Task SqlSugarClient_GetTransactionContext_Commit()
        {
            var sugar = GetService<ISqlSugarClient>();
            var publisher = GetService<IEventPublisher>();
            TestEventHandler.Reset();

            var testEvent = new TestEvent { Name = Guid.NewGuid().ToString("n") };
            var userName = Guid.NewGuid().ToString("n");

            sugar.Ado.BeginTran();
            sugar.Insertable(new SsTestUser { Name = userName }).ExecuteCommand();
            var ctx = sugar.GetTransactionContext();
            ctx.ShouldNotBeNull();
            await publisher.PublishAsync(testEvent, ctx);
            sugar.Ado.CommitTran();

            await TestEventHandler.WaitForInstance(TimeSpan.FromSeconds(30));
            TestEventHandler.LastInstance.ShouldNotBeNull();
            TestEventHandler.LastInstance!.Name.ShouldBe(testEvent.Name);

            sugar.Queryable<SsTestUser>().Where(u => u.Name == userName).Any().ShouldBeTrue();
        }

        [Fact]
        public async Task SqlSugarClient_GetTransactionContext_Rollback()
        {
            var sugar = GetService<ISqlSugarClient>();
            var publisher = GetService<IEventPublisher>();
            TestEventHandler.Reset();

            var testEvent = new TestEvent { Name = Guid.NewGuid().ToString("n") };
            var userName = Guid.NewGuid().ToString("n");

            sugar.Ado.BeginTran();
            sugar.Insertable(new SsTestUser { Name = userName }).ExecuteCommand();
            var ctx = sugar.GetTransactionContext();
            ctx.ShouldNotBeNull();
            await publisher.PublishAsync(testEvent, ctx);
            sugar.Ado.RollbackTran();

            await Task.Delay(3000);
            TestEventHandler.LastInstance.ShouldBeNull();

            sugar.Queryable<SsTestUser>().Where(u => u.Name == userName).Any().ShouldBeFalse();
        }

        // ---- IAdo.GetTransactionContext ------------------------------------

        [Fact]
        public async Task IAdo_GetTransactionContext_Commit()
        {
            var sugar = GetService<ISqlSugarClient>();
            var publisher = GetService<IEventPublisher>();
            TestEventHandler.Reset();

            var testEvent = new TestEvent { Name = Guid.NewGuid().ToString("n") };
            var userName = Guid.NewGuid().ToString("n");

            sugar.Ado.BeginTran();
            sugar.Insertable(new SsTestUser { Name = userName }).ExecuteCommand();
            var ctx = sugar.Ado.GetTransactionContext();
            ctx.ShouldNotBeNull();
            await publisher.PublishAsync(testEvent, ctx);
            sugar.Ado.CommitTran();

            await TestEventHandler.WaitForInstance(TimeSpan.FromSeconds(30));
            TestEventHandler.LastInstance.ShouldNotBeNull();
            TestEventHandler.LastInstance!.Name.ShouldBe(testEvent.Name);

            sugar.Queryable<SsTestUser>().Where(u => u.Name == userName).Any().ShouldBeTrue();
        }

        [Fact]
        public async Task IAdo_GetTransactionContext_Rollback()
        {
            var sugar = GetService<ISqlSugarClient>();
            var publisher = GetService<IEventPublisher>();
            TestEventHandler.Reset();

            var testEvent = new TestEvent { Name = Guid.NewGuid().ToString("n") };
            var userName = Guid.NewGuid().ToString("n");

            sugar.Ado.BeginTran();
            sugar.Insertable(new SsTestUser { Name = userName }).ExecuteCommand();
            var ctx = sugar.Ado.GetTransactionContext();
            ctx.ShouldNotBeNull();
            await publisher.PublishAsync(testEvent, ctx);
            sugar.Ado.RollbackTran();

            await Task.Delay(3000);
            TestEventHandler.LastInstance.ShouldBeNull();

            sugar.Queryable<SsTestUser>().Where(u => u.Name == userName).Any().ShouldBeFalse();
        }

        // ---- SqlSugarTransaction.GetTransactionContext (UseTran语法糖) ----

        [Fact]
        public async Task SqlSugarTransaction_GetTransactionContext_Commit()
        {
            var sugar = (SqlSugarClient)GetService<ISqlSugarClient>();
            var publisher = GetService<IEventPublisher>();
            TestEventHandler.Reset();

            var testEvent = new TestEvent { Name = Guid.NewGuid().ToString("n") };
            var userName = Guid.NewGuid().ToString("n");

            using (var tran = sugar.UseTran())
            {
                sugar.Insertable(new SsTestUser { Name = userName }).ExecuteCommand();
                var ctx = tran.GetTransactionContext();
                ctx.ShouldNotBeNull();
                publisher.PublishAsync(testEvent, ctx).GetAwaiter().GetResult();
                tran.CommitTran();
            }

            await TestEventHandler.WaitForInstance(TimeSpan.FromSeconds(30));
            TestEventHandler.LastInstance.ShouldNotBeNull();
            TestEventHandler.LastInstance!.Name.ShouldBe(testEvent.Name);

            sugar.Queryable<SsTestUser>().Where(u => u.Name == userName).Any().ShouldBeTrue();
        }

        [Fact]
        public async Task SqlSugarTransaction_GetTransactionContext_Rollback()
        {
            var sugar = (SqlSugarClient)GetService<ISqlSugarClient>();
            var publisher = GetService<IEventPublisher>();
            TestEventHandler.Reset();

            var testEvent = new TestEvent { Name = Guid.NewGuid().ToString("n") };
            var userName = Guid.NewGuid().ToString("n");

            using (var tran = sugar.UseTran())
            {
                sugar.Insertable(new SsTestUser { Name = userName }).ExecuteCommand();
                var ctx = tran.GetTransactionContext();
                ctx.ShouldNotBeNull();
                publisher.PublishAsync(testEvent, ctx).GetAwaiter().GetResult();
                // no CommitTran → rollback on dispose
            }

            await Task.Delay(3000);
            TestEventHandler.LastInstance.ShouldBeNull();

            sugar.Queryable<SsTestUser>().Where(u => u.Name == userName).Any().ShouldBeFalse();
        }

        // ---- ISugarUnitOfWork.GetTransactionContext (工作单元) --------------

        [Fact]
        public async Task SugarUnitOfWork_GetTransactionContext_Commit()
        {
            var sugar = (SqlSugarClient)GetService<ISqlSugarClient>();
            var publisher = GetService<IEventPublisher>();
            TestEventHandler.Reset();

            var testEvent = new TestEvent { Name = Guid.NewGuid().ToString("n") };
            var userName = Guid.NewGuid().ToString("n");

            using (var uow = sugar.CreateContext(sugar.Ado.IsNoTran()))
            {
                sugar.Insertable(new SsTestUser { Name = userName }).ExecuteCommand();
                var ctx = uow.GetTransactionContext();
                ctx.ShouldNotBeNull();
                publisher.PublishAsync(testEvent, ctx).GetAwaiter().GetResult();
                uow.Commit();
            }

            await TestEventHandler.WaitForInstance(TimeSpan.FromSeconds(30));
            TestEventHandler.LastInstance.ShouldNotBeNull();
            TestEventHandler.LastInstance!.Name.ShouldBe(testEvent.Name);

            sugar.Queryable<SsTestUser>().Where(u => u.Name == userName).Any().ShouldBeTrue();
        }

        [Fact]
        public async Task SugarUnitOfWork_GetTransactionContext_Rollback()
        {
            var sugar = (SqlSugarClient)GetService<ISqlSugarClient>();
            var publisher = GetService<IEventPublisher>();
            TestEventHandler.Reset();

            var testEvent = new TestEvent { Name = Guid.NewGuid().ToString("n") };
            var userName = Guid.NewGuid().ToString("n");

            using (var uow = sugar.CreateContext(sugar.Ado.IsNoTran()))
            {
                sugar.Insertable(new SsTestUser { Name = userName }).ExecuteCommand();
                var ctx = uow.GetTransactionContext();
                ctx.ShouldNotBeNull();
                publisher.PublishAsync(testEvent, ctx).GetAwaiter().GetResult();
                // no Commit → rollback on dispose
            }

            await Task.Delay(3000);
            TestEventHandler.LastInstance.ShouldBeNull();

            sugar.Queryable<SsTestUser>().Where(u => u.Name == userName).Any().ShouldBeFalse();
        }
    }
}
