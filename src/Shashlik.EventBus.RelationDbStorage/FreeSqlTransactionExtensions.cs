using FreeSql;

namespace Shashlik.EventBus.RelationDbStorage;

public static class FreeSqlTransactionExtensions
{
    /// <summary>
    /// 从free sql同步线程事务中获取当前事务上下文
    /// <para></para>
    /// freesql 同线程事务场景: fsql.Transaction(() =>xxxx
    /// </summary>
    /// <param name="fsql"></param>
    /// <returns></returns>
    public static ITransactionContext? GetCurrentThreadTransactionContext(this IFreeSql fsql)
    {
        var tran = fsql.Ado.TransactionCurrentThread;
        return tran is null ? null : new RelationDbStorageTransactionContext(tran);
    }

    /// <summary>
    /// 从free sql unit of work中获取当前事务上下文
    /// </summary>
    /// <param name="fsqlUnitOfWork">当前工作单元</param>
    /// <returns></returns>
    public static ITransactionContext? GetTransactionContextFromUnitOfWork(this IUnitOfWork fsqlUnitOfWork)
    {
        var tran = fsqlUnitOfWork.GetOrBeginTransaction(false);
        return tran is null ? null : new RelationDbStorageTransactionContext(tran);
    }

    /// <summary>
    /// 从free sql unit of work manager中获取当前事务上下文
    /// <para></para>
    /// <see cref="IUnitOfWorkManager"/>一般会注册到services, 从services中获取
    /// </summary>
    /// <param name="fsqlUnitOfWorkManager"></param>
    /// <returns></returns>
    public static ITransactionContext? GetTransactionContextFromUnitOfWorkManager(
        this IUnitOfWorkManager fsqlUnitOfWorkManager)
    {
        //TransactionalAttribute
        return fsqlUnitOfWorkManager.Current?.GetTransactionContextFromUnitOfWork();
    }
}