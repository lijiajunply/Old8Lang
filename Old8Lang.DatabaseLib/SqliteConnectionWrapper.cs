using Microsoft.Data.Sqlite;
using Dapper;
using System.Data;

namespace Old8Lang.DatabaseLib;

/// <summary>
/// SQLite 数据库连接包装器
/// </summary>
public class SqliteConnectionWrapper : IDisposable
{
    private readonly SqliteConnection Connection;
    private bool Disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connectionString">连接字符串</param>
    public SqliteConnectionWrapper(string connectionString)
    {
        Connection = new SqliteConnection(connectionString);
    }

    /// <summary>
    /// 打开连接
    /// </summary>
    public void Open()
    {
        if (Connection.State != ConnectionState.Open)
        {
            Connection.Open();
        }
    }

    /// <summary>
    /// 关闭连接
    /// </summary>
    public void Close()
    {
        Connection.Close();
    }

    /// <summary>
    /// 执行查询并返回结果
    /// </summary>
    /// <param name="sql">SQL 语句</param>
    /// <param name="parameters">参数</param>
    /// <returns>查询结果</returns>
    public IEnumerable<dynamic> Query(string sql, object? parameters = null)
    {
        EnsureConnectionOpen();
        return Connection.Query(sql, parameters);
    }

    /// <summary>
    /// 执行查询并返回强类型结果
    /// </summary>
    /// <typeparam name="T">结果类型</typeparam>
    /// <param name="sql">SQL 语句</param>
    /// <param name="parameters">参数</param>
    /// <returns>查询结果</returns>
    public IEnumerable<T> Query<T>(string sql, object? parameters = null)
    {
        EnsureConnectionOpen();
        return Connection.Query<T>(sql, parameters);
    }

    /// <summary>
    /// 执行非查询 SQL 语句
    /// </summary>
    /// <param name="sql">SQL 语句</param>
    /// <param name="parameters">参数</param>
    /// <returns>影响的行数</returns>
    public int Execute(string sql, object? parameters = null)
    {
        EnsureConnectionOpen();
        return Connection.Execute(sql, parameters);
    }

    /// <summary>
    /// 执行查询并返回单个结果
    /// </summary>
    /// <param name="sql">SQL 语句</param>
    /// <param name="parameters">参数</param>
    /// <returns>单个结果</returns>
    public object ExecuteScalar(string sql, object? parameters = null)
    {
        EnsureConnectionOpen();
        return Connection.ExecuteScalar(sql, parameters)!;
    }

    /// <summary>
    /// 执行查询并返回强类型单个结果
    /// </summary>
    /// <typeparam name="T">结果类型</typeparam>
    /// <param name="sql">SQL 语句</param>
    /// <param name="parameters">参数</param>
    /// <returns>单个结果</returns>
    public T ExecuteScalar<T>(string sql, object? parameters = null)
    {
        EnsureConnectionOpen();
        return Connection.ExecuteScalar<T>(sql, parameters);
    }

    /// <summary>
    /// 查询单个记录
    /// </summary>
    /// <param name="sql">SQL 语句</param>
    /// <param name="parameters">参数</param>
    /// <returns>单个记录</returns>
    public dynamic? QueryFirst(string sql, object? parameters = null)
    {
        EnsureConnectionOpen();
        return Connection.QueryFirst(sql, parameters);
    }

    /// <summary>
    /// 查询单个强类型记录
    /// </summary>
    /// <typeparam name="T">结果类型</typeparam>
    /// <param name="sql">SQL 语句</param>
    /// <param name="parameters">参数</param>
    /// <returns>单个记录</returns>
    public T? QueryFirst<T>(string sql, object? parameters = null)
    {
        EnsureConnectionOpen();
        return Connection.QueryFirst<T>(sql, parameters);
    }

    /// <summary>
    /// 查询单个记录或返回默认值
    /// </summary>
    /// <param name="sql">SQL 语句</param>
    /// <param name="parameters">参数</param>
    /// <returns>单个记录或默认值</returns>
    public dynamic? QueryFirstOrDefault(string sql, object? parameters = null)
    {
        EnsureConnectionOpen();
        return Connection.QueryFirstOrDefault(sql, parameters);
    }

    /// <summary>
    /// 查询单个强类型记录或返回默认值
    /// </summary>
    /// <typeparam name="T">结果类型</typeparam>
    /// <param name="sql">SQL 语句</param>
    /// <param name="parameters">参数</param>
    /// <returns>单个记录或默认值</returns>
    public T? QueryFirstOrDefault<T>(string sql, object? parameters = null)
    {
        EnsureConnectionOpen();
        return Connection.QueryFirstOrDefault<T>(sql, parameters);
    }

    /// <summary>
    /// 开始事务
    /// </summary>
    /// <returns>事务对象</returns>
    public SqliteTransactionWrapper BeginTransaction()
    {
        EnsureConnectionOpen();
        var transaction = Connection.BeginTransaction();
        return new SqliteTransactionWrapper(transaction);
    }

    /// <summary>
    /// 批量插入
    /// </summary>
    /// <param name="entities">实体列表</param>
    /// <param name="tableName">表名</param>
    /// <returns>插入的记录数</returns>
    public int BulkInsert<T>(IEnumerable<T> entities, string tableName)
    {
        EnsureConnectionOpen();
        return Connection.Execute($"INSERT INTO {tableName} VALUES (@Value)", entities);
    }

    /// <summary>
    /// 确保连接已打开
    /// </summary>
    private void EnsureConnectionOpen()
    {
        if (Connection.State != ConnectionState.Open)
        {
            Connection.Open();
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!Disposed)
        {
            Connection?.Dispose();
            Disposed = true;
        }
    }
}