using MySql.Data.MySqlClient;

namespace Old8Lang.DatabaseLib;

/// <summary>
/// MySQL 事务包装器
/// </summary>
public class MySqlTransactionWrapper : IDisposable
{
    private readonly MySqlTransaction _transaction;
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="transaction">MySQL 事务</param>
    public MySqlTransactionWrapper(MySqlTransaction transaction)
    {
        _transaction = transaction;
    }

    /// <summary>
    /// 提交事务
    /// </summary>
    public void Commit()
    {
        _transaction.Commit();
    }

    /// <summary>
    /// 回滚事务
    /// </summary>
    public void Rollback()
    {
        _transaction.Rollback();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _transaction?.Dispose();
            _disposed = true;
        }
    }
}