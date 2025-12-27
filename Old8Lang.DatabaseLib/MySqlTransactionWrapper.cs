using MySql.Data.MySqlClient;

namespace Old8Lang.DatabaseLib;

/// <summary>
/// MySQL 事务包装器
/// </summary>
public class MySqlTransactionWrapper : IDisposable
{
    private readonly MySqlTransaction Transaction;
    private bool Disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="transaction">MySQL 事务</param>
    public MySqlTransactionWrapper(MySqlTransaction transaction)
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