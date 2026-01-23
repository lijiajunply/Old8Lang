using Microsoft.Data.Sqlite;
using Dapper;
using System.Data;

namespace Old8Lang.DatabaseLib;

/// <summary>
/// 内存数据库连接包装器
/// 使用内存存储数据，适合测试和临时操作
/// </summary>
public class InMemoryConnectionWrapper : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly string _connectionString;
    private readonly HashSet<string> _createdTables;
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="databaseName">数据库名称，用于区分不同的内存数据库实例</param>
    public InMemoryConnectionWrapper(string databaseName = "default")
    {
        _connectionString = $"Data Source=file:{databaseName}?mode=memory&cache=shared";
        _connection = new SqliteConnection(_connectionString);
        _createdTables = [];
    }

    /// <summary>
    /// 打开连接
    /// </summary>
    public void Open()
    {
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
            // 启用外键约束
            _connection.Execute("PRAGMA foreign_keys = ON");
            // 设置WAL模式以提高并发性能
            _connection.Execute("PRAGMA journal_mode = WAL");
        }
    }

    /// <summary>
    /// 关闭连接
    /// </summary>
    public void Close()
    {
        _connection.Close();
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
        return _connection.Query(sql, parameters);
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
        return _connection.Query<T>(sql, parameters);
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
        
        // 记录创建的表
        if (sql.TrimStart().StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
        {
            var tableName = ExtractTableName(sql);
            if (!string.IsNullOrEmpty(tableName))
            {
                _createdTables.Add(tableName);
            }
        }
        
        return _connection.Execute(sql, parameters);
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
        return _connection.ExecuteScalar(sql, parameters)!;
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
        return _connection.ExecuteScalar<T>(sql, parameters);
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
        return _connection.QueryFirst(sql, parameters);
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
        return _connection.QueryFirst<T>(sql, parameters);
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
        return _connection.QueryFirstOrDefault(sql, parameters);
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
        return _connection.QueryFirstOrDefault<T>(sql, parameters);
    }

    /// <summary>
    /// 开始事务
    /// </summary>
    /// <returns>事务对象</returns>
    public InMemoryTransactionWrapper BeginTransaction()
    {
        EnsureConnectionOpen();
        var transaction = _connection.BeginTransaction();
        return new InMemoryTransactionWrapper(transaction);
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
        return _connection.Execute($"INSERT INTO {tableName} VALUES (@Value)", entities);
    }

    /// <summary>
    /// 清空所有表数据（保留表结构）
    /// </summary>
    public void ClearAllTables()
    {
        EnsureConnectionOpen();
        
        foreach (var tableName in _createdTables)
        {
            _connection.Execute($"DELETE FROM {tableName}");
        }
    }

    /// <summary>
    /// 获取所有已创建的表名
    /// </summary>
    /// <returns>表名列表</returns>
    public IEnumerable<string> GetCreatedTables()
    {
        return _createdTables.ToList();
    }

    /// <summary>
    /// 重置数据库（删除所有表）
    /// </summary>
    public void ResetDatabase()
    {
        EnsureConnectionOpen();
        
        foreach (var tableName in _createdTables)
        {
            _connection.Execute($"DROP TABLE IF EXISTS {tableName}");
        }
        
        _createdTables.Clear();
    }

    /// <summary>
    /// 获取数据库统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public DatabaseStatistics GetStatistics()
    {
        EnsureConnectionOpen();
        
        var stats = new DatabaseStatistics
        {
            TotalTables = _createdTables.Count,
            ConnectionString = _connectionString,
            IsOpen = _connection.State == ConnectionState.Open
        };

        // 获取每个表的记录数
        foreach (var tableName in _createdTables)
        {
            try
            {
                var count = _connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {tableName}");
                stats.TableRecords[tableName] = count;
            }
            catch
            {
                stats.TableRecords[tableName] = 0;
            }
        }

        return stats;
    }

    /// <summary>
    /// 确保连接已打开
    /// </summary>
    private void EnsureConnectionOpen()
    {
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
            // 重新设置PRAGMA设置
            _connection.Execute("PRAGMA foreign_keys = ON");
            _connection.Execute("PRAGMA journal_mode = WAL");
        }
    }

    /// <summary>
    /// 从CREATE TABLE语句中提取表名
    /// </summary>
    private static string ExtractTableName(string sql)
    {
        var simplifiedSql = sql.Trim();
        var createIndex = simplifiedSql.IndexOf("CREATE TABLE", StringComparison.OrdinalIgnoreCase);
        if (createIndex >= 0)
        {
            var afterCreate = simplifiedSql.Substring(createIndex + 12).Trim();
            var ifNotExists = afterCreate.IndexOf("IF NOT EXISTS", StringComparison.OrdinalIgnoreCase);
            if (ifNotExists >= 0)
            {
                afterCreate = afterCreate.Substring(ifNotExists + 12).Trim();
            }
            
            var spaceIndex = afterCreate.IndexOf(' ');
            var parenIndex = afterCreate.IndexOf('(');
            
            var endIndex = Math.Min(
                spaceIndex > 0 ? spaceIndex : int.MaxValue,
                parenIndex > 0 ? parenIndex : int.MaxValue
            );
            
            if (endIndex > 0)
            {
                return afterCreate.Substring(0, endIndex).Trim(' ', '"', '[', ']', '`');
            }
        }
        
        return string.Empty;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// 内存数据库事务包装器
/// </summary>
public class InMemoryTransactionWrapper : IDisposable
{
    private readonly SqliteTransaction _transaction;
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="transaction">SQLite 事务</param>
    public InMemoryTransactionWrapper(SqliteTransaction transaction)
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

/// <summary>
/// 数据库统计信息
/// </summary>
public class DatabaseStatistics
{
    /// <summary>
    /// 总表数
    /// </summary>
    public int TotalTables { get; set; }

    /// <summary>
    /// 连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 是否打开
    /// </summary>
    public bool IsOpen { get; set; }

    /// <summary>
    /// 各表的记录数
    /// </summary>
    public Dictionary<string, int> TableRecords { get; set; } = new();

    /// <summary>
    /// 获取总记录数
    /// </summary>
    public int TotalRecords => TableRecords.Values.Sum();
}