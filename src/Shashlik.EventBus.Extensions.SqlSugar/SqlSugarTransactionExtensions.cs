using System;
using System.Data;
using System.Linq;
using System.Reflection;
using Shashlik.EventBus.RelationDbStorage;
using SqlSugar;

namespace Shashlik.EventBus.Extensions.SqlSugar;

public static class SqlSugarTransactionExtensions
{
    /// <summary>
    /// 从SqlSugar IAdo中获取当前事务上下文
    /// </summary>
    /// <param name="ado"></param>
    /// <returns></returns>
    public static ITransactionContext? GetTransactionContext(this IAdo ado)
    {
        var tran = ado.Transaction;
        return tran is null ? null : new RelationDbStorageTransactionContext(tran);
    }

    /// <summary>
    /// 从ISqlSugarClient中获取当前事务上下文
    /// </summary>
    /// <param name="sqlSugarClient"></param>
    /// <returns></returns>
    public static ITransactionContext? GetTransactionContext(this ISqlSugarClient sqlSugarClient)
    {
        IDbTransaction? tran;
        if (sqlSugarClient is SqlSugarScope scope)
            tran = scope.ScopedContext?.Ado?.Transaction;
        else
            tran = sqlSugarClient.Ado.Transaction;

        return tran is null ? null : new RelationDbStorageTransactionContext(tran);
    }

    /// <summary>
    /// 从SqlSugarTransaction中获取当前事务上下文, 对应`SqlSugarTransaction tran = sugar.UseTran()`的用法
    /// </summary>
    /// <param name="sqlSugarTransaction"></param>
    /// <returns></returns>
    public static ITransactionContext? GetTransactionContext(this SqlSugarTransaction sqlSugarTransaction)
    {
        var fieldInfo = typeof(SqlSugarTransaction).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .FirstOrDefault(r => r.FieldType == typeof(SqlSugarClient));
        if (fieldInfo is null)
            throw new InvalidCastException();
        var sqlSugarClient = fieldInfo.GetValue(sqlSugarTransaction) as SqlSugarClient;
        if (sqlSugarClient is null) throw new InvalidCastException();
        var tran = sqlSugarClient.Ado.Transaction;
        return tran is null ? null : new RelationDbStorageTransactionContext(tran);
    }


    /// <summary>
    /// 从ISugarUnitOfWork获取当前事务上下文
    /// </summary>
    /// <param name="sugarUnitOfWork"></param>
    /// <returns></returns>
    public static ITransactionContext? GetTransactionContext(this ISugarUnitOfWork sugarUnitOfWork)
    {
        var tran = sugarUnitOfWork.Db?.Ado?.Transaction;
        return tran is null ? null : new RelationDbStorageTransactionContext(tran);
    }
}