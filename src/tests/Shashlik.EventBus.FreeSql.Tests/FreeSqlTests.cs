using System;
using System.Threading.Tasks;
using CommonTestLogical;
using CommonTestLogical.TestEvents;
using FreeSql;
using Shashlik.EventBus.RelationDbStorage;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shashlik.EventBus.FreeSql.Tests
{
    [Collection("Shashlik.EventBus.FreeSql.Tests")]
    public class FreeSqlTransactionExtensionsTests : TestBase<Startup>
    {
        public FreeSqlTransactionExtensionsTests(TestWebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper)
            : base(factory, testOutputHelper)
        {
        }

        [Fact]
        public async Task GetCurrentThreadTransactionContext_Commit()
        {
            var fsql = GetService<IFreeSql>();
            var publisher = GetService<IEventPublisher>();
            TestEventHandler.Reset();

            var testEvent = new TestEvent { Name = Guid.NewGuid().ToString("n") };
            var userName = Guid.NewGuid().ToString("n");

            fsql.Transaction(() =>
            {
                fsql.Insert(new FsTestUser { Name = userName }).ExecuteAffrows();
                var ctx = fsql.GetCurrentThreadTransactionContext();
                ctx.ShouldNotBeNull();
                publisher.PublishAsync(testEvent, ctx).GetAwaiter().GetResult();
            });

            await TestEventHandler.WaitForInstance(TimeSpan.FromSeconds(30));
            TestEventHandler.LastInstance.ShouldNotBeNull();
            TestEventHandler.LastInstance!.Name.ShouldBe(testEvent.Name);

            fsql.Select<FsTestUser>().Where(u => u.Name == userName).Any().ShouldBeTrue();
        }

        [Fact]
        public async Task GetCurrentThreadTransactionContext_Rollback()
        {
            var fsql = GetService<IFreeSql>();
            var publisher = GetService<IEventPublisher>();
            TestEventHandler.Reset();

            var testEvent = new TestEvent { Name = Guid.NewGuid().ToString("n") };
            var userName = Guid.NewGuid().ToString("n");

            try
            {
                fsql.Transaction(() =>
                {
                    fsql.Insert(new FsTestUser { Name = userName }).ExecuteAffrows();
                    var ctx = fsql.GetCurrentThreadTransactionContext();
                    ctx.ShouldNotBeNull();
                    publisher.PublishAsync(testEvent, ctx).GetAwaiter().GetResult();
                    throw new Exception("rollback");
                });
            }
            catch
            {
                // expected
            }

            await Task.Delay(3000);
            TestEventHandler.LastInstance.ShouldBeNull();

            fsql.Select<FsTestUser>().Where(u => u.Name == userName).Any().ShouldBeFalse();
        }

        [Fact]
        public async Task GetTransactionContextFromUnitOfWork_Commit()
        {
            var fsql = GetService<IFreeSql>();
            var publisher = GetService<IEventPublisher>();
            TestEventHandler.Reset();

            var testEvent = new TestEvent { Name = Guid.NewGuid().ToString("n") };
            var userName = Guid.NewGuid().ToString("n");

            using (var uow = fsql.CreateUnitOfWork())
            {
                uow.Orm.Insert(new FsTestUser { Name = userName }).ExecuteAffrows();
                var ctx = uow.GetTransactionContextFromUnitOfWork();
                ctx.ShouldNotBeNull();
                publisher.PublishAsync(testEvent, ctx).GetAwaiter().GetResult();
                uow.Commit();
            }

            await TestEventHandler.WaitForInstance(TimeSpan.FromSeconds(30));
            TestEventHandler.LastInstance.ShouldNotBeNull();
            TestEventHandler.LastInstance!.Name.ShouldBe(testEvent.Name);

            fsql.Select<FsTestUser>().Where(u => u.Name == userName).Any().ShouldBeTrue();
        }

        [Fact]
        public async Task GetTransactionContextFromUnitOfWork_Rollback()
        {
            var fsql = GetService<IFreeSql>();
            var publisher = GetService<IEventPublisher>();
            TestEventHandler.Reset();

            var testEvent = new TestEvent { Name = Guid.NewGuid().ToString("n") };
            var userName = Guid.NewGuid().ToString("n");

            using (var uow = fsql.CreateUnitOfWork())
            {
                uow.Orm.Insert(new FsTestUser { Name = userName }).ExecuteAffrows();
                var ctx = uow.GetTransactionContextFromUnitOfWork();
                ctx.ShouldNotBeNull();
                publisher.PublishAsync(testEvent, ctx).GetAwaiter().GetResult();
                // no Commit → rollback on dispose
            }

            await Task.Delay(3000);
            TestEventHandler.LastInstance.ShouldBeNull();

            fsql.Select<FsTestUser>().Where(u => u.Name == userName).Any().ShouldBeFalse();
        }

        [Fact]
        public async Task GetTransactionContextFromUnitOfWorkManager_Commit()
        {
            var fsql = GetService<IFreeSql>();
            var publisher = GetService<IEventPublisher>();
            TestEventHandler.Reset();

            var testEvent = new TestEvent { Name = Guid.NewGuid().ToString("n") };
            var userName = Guid.NewGuid().ToString("n");

            using (var uowManager = new UnitOfWorkManager(fsql))
            {
                using (var uow = uowManager.Begin())
                {
                    uow.Orm.Insert(new FsTestUser { Name = userName }).ExecuteAffrows();
                    var ctx = uowManager.GetTransactionContextFromUnitOfWorkManager();
                    ctx.ShouldNotBeNull();
                    publisher.PublishAsync(testEvent, ctx).GetAwaiter().GetResult();
                    uow.Commit();
                }
            }

            await TestEventHandler.WaitForInstance(TimeSpan.FromSeconds(30));
            TestEventHandler.LastInstance.ShouldNotBeNull();
            TestEventHandler.LastInstance!.Name.ShouldBe(testEvent.Name);

            fsql.Select<FsTestUser>().Where(u => u.Name == userName).Any().ShouldBeTrue();
        }

        [Fact]
        public async Task GetTransactionContextFromUnitOfWorkManager_Rollback()
        {
            var fsql = GetService<IFreeSql>();
            var publisher = GetService<IEventPublisher>();
            TestEventHandler.Reset();

            var testEvent = new TestEvent { Name = Guid.NewGuid().ToString("n") };
            var userName = Guid.NewGuid().ToString("n");

            using (var uowManager = new UnitOfWorkManager(fsql))
            {
                using (var uow = uowManager.Begin())
                {
                    uow.Orm.Insert(new FsTestUser { Name = userName }).ExecuteAffrows();
                    var ctx = uowManager.GetTransactionContextFromUnitOfWorkManager();
                    ctx.ShouldNotBeNull();
                    publisher.PublishAsync(testEvent, ctx).GetAwaiter().GetResult();
                    // no Commit → rollback on dispose
                }
            }

            await Task.Delay(3000);
            TestEventHandler.LastInstance.ShouldBeNull();

            fsql.Select<FsTestUser>().Where(u => u.Name == userName).Any().ShouldBeFalse();
        }
    }
}
