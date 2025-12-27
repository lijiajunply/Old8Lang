using Microsoft.Data.Sqlite;

namespace Old8Lang.DatabaseLib;

/// <summary>
/// SQLite 事务包装器
/// </summary>
public class SqliteTransactionWrapper : IDisposable
{
    private readonly SqliteTransaction Transaction;
    private bool Disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="transaction">SQLite 事务</param>
    public SqliteTransactionWrapper(SqliteTransaction transaction)
    {
        Transaction = transaction;
    }

    /// <summary>
    /// 提交事务
    /// </summary>
    public void Commit()
    {
        Transaction.Commit();
    }

    /// <summary>
    /// 回滚事务
    /// </summary>
    public void Rollback()
    {
        Transaction.Rollback();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!Disposed)
        {
            Transaction?.Dispose();
            Disposed = true;
        }
    }
}