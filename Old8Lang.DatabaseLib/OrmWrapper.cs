using Dapper;
using System.Data;
using System.Reflection;

namespace Old8Lang.DatabaseLib;

/// <summary>
/// ORM 包装器，提供轻量级对象关系映射功能
/// </summary>
public class OrmWrapper : IDisposable
{
    private readonly object _connection;
    private readonly Type _connectionType;
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connection">数据库连接对象</param>
    public OrmWrapper(object connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _connectionType = connection.GetType();
    }

    /// <summary>
    /// 插入实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实体对象</param>
    /// <returns>影响的行数</returns>
    public int Insert<T>(T entity)
    {
        var tableName = GetTableName<T>();
        var properties = GetProperties<T>();
        var columns = string.Join(", ", properties.Select(p => p.Name));
        var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));

        var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
        return ExecuteScalar<int>(sql, entity);
    }

    /// <summary>
    /// 更新实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实体对象</param>
    /// <returns>影响的行数</returns>
    public int Update<T>(T entity)
    {
        var tableName = GetTableName<T>();
        var properties = GetProperties<T>().Where(p => !IsPrimaryKey(p)).ToList();
        var primaryKey = GetPrimaryKey<T>();

        if (primaryKey == null)
            throw new InvalidOperationException($"实体 {typeof(T).Name} 没有定义主键");

        var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));
        var sql = $"UPDATE {tableName} SET {setClause} WHERE {primaryKey.Name} = @{primaryKey.Name}";

        return Execute(sql, entity);
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实体对象</param>
    /// <returns>影响的行数</returns>
    public int Delete<T>(T entity)
    {
        var tableName = GetTableName<T>();
        var primaryKey = GetPrimaryKey<T>();

        if (primaryKey == null)
            throw new InvalidOperationException($"实体 {typeof(T).Name} 没有定义主键");

        var sql = $"DELETE FROM {tableName} WHERE {primaryKey.Name} = @{primaryKey.Name}";
        return Execute(sql, entity);
    }

    /// <summary>
    /// 根据ID查询实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="id">主键值</param>
    /// <returns>实体对象</returns>
    public T? QueryById<T>(object id)
    {
        var tableName = GetTableName<T>();
        var primaryKey = GetPrimaryKey<T>();

        if (primaryKey == null)
            throw new InvalidOperationException($"实体 {typeof(T).Name} 没有定义主键");

        var sql = $"SELECT * FROM {tableName} WHERE {primaryKey.Name} = @Id";
        return QueryFirstOrDefault<T>(sql, new { Id = id });
    }

    /// <summary>
    /// 查询所有实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <returns>实体列表</returns>
    public IEnumerable<T> QueryAll<T>()
    {
        var tableName = GetTableName<T>();
        var sql = $"SELECT * FROM {tableName}";
        return Query<T>(sql);
    }

    /// <summary>
    /// 根据条件查询
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="whereSql">WHERE 子句</param>
    /// <param name="parameters">参数</param>
    /// <returns>实体列表</returns>
    public IEnumerable<T> QueryWhere<T>(string whereSql, object? parameters = null)
    {
        var tableName = GetTableName<T>();
        var sql = $"SELECT * FROM {tableName} WHERE {whereSql}";
        return Query<T>(sql, parameters);
    }

    /// <summary>
    /// 根据条件查询单个实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="whereSql">WHERE 子句</param>
    /// <param name="parameters">参数</param>
    /// <returns>实体对象</returns>
    public T? QueryFirstOrDefaultWhere<T>(string whereSql, object? parameters = null)
    {
        var tableName = GetTableName<T>();
        var sql = $"SELECT * FROM {tableName} WHERE {whereSql}";
        return QueryFirstOrDefault<T>(sql, parameters);
    }

    /// <summary>
    /// 批量插入
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entities">实体列表</param>
    /// <returns>影响的行数</returns>
    public int BulkInsert<T>(IEnumerable<T> entities)
    {
        var tableName = GetTableName<T>();
        var properties = GetProperties<T>();
        var columns = string.Join(", ", properties.Select(p => p.Name));
        var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));

        var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
        return Execute(sql, entities);
    }

    /// <summary>
    /// 执行查询并返回结果
    /// </summary>
    private IEnumerable<dynamic> Query(string sql, object? parameters = null)
    {
        return _connection switch
        {
            SqliteConnectionWrapper sqlite => sqlite.Query(sql, parameters),
            MySqlConnectionWrapper mysql => mysql.Query(sql, parameters),
            PostgresConnectionWrapper postgres => postgres.Query(sql, parameters),
            _ => throw new InvalidOperationException("不支持的数据库连接类型")
        };
    }

    /// <summary>
    /// 执行查询并返回强类型结果
    /// </summary>
    private IEnumerable<T> Query<T>(string sql, object? parameters = null)
    {
        return _connection switch
        {
            SqliteConnectionWrapper sqlite => sqlite.Query<T>(sql, parameters),
            MySqlConnectionWrapper mysql => mysql.Query<T>(sql, parameters),
            PostgresConnectionWrapper postgres => postgres.Query<T>(sql, parameters),
            _ => throw new InvalidOperationException("不支持的数据库连接类型")
        };
    }

    /// <summary>
    /// 执行查询并返回单个结果
    /// </summary>
    private T? QueryFirstOrDefault<T>(string sql, object? parameters = null)
    {
        return _connection switch
        {
            SqliteConnectionWrapper sqlite => sqlite.QueryFirstOrDefault<T>(sql, parameters),
            MySqlConnectionWrapper mysql => mysql.QueryFirstOrDefault<T>(sql, parameters),
            PostgresConnectionWrapper postgres => postgres.QueryFirstOrDefault<T>(sql, parameters),
            _ => throw new InvalidOperationException("不支持的数据库连接类型")
        };
    }

    /// <summary>
    /// 执行非查询 SQL 语句
    /// </summary>
    private int Execute(string sql, object? parameters = null)
    {
        return _connection switch
        {
            SqliteConnectionWrapper sqlite => sqlite.Execute(sql, parameters),
            MySqlConnectionWrapper mysql => mysql.Execute(sql, parameters),
            PostgresConnectionWrapper postgres => postgres.Execute(sql, parameters),
            _ => throw new InvalidOperationException("不支持的数据库连接类型")
        };
    }

    /// <summary>
    /// 执行查询并返回标量结果
    /// </summary>
    private T ExecuteScalar<T>(string sql, object? parameters = null)
    {
        return _connection switch
        {
            SqliteConnectionWrapper sqlite => sqlite.ExecuteScalar<T>(sql, parameters),
            MySqlConnectionWrapper mysql => mysql.ExecuteScalar<T>(sql, parameters),
            PostgresConnectionWrapper postgres => postgres.ExecuteScalar<T>(sql, parameters),
            _ => throw new InvalidOperationException("不支持的数据库连接类型")
        };
    }

    /// <summary>
    /// 获取表名
    /// </summary>
    private static string GetTableName<T>()
    {
        var type = typeof(T);
        var tableAttribute = type.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>();
        return tableAttribute?.Name ?? type.Name;
    }

    /// <summary>
    /// 获取实体属性
    /// </summary>
    private static IEnumerable<PropertyInfo> GetProperties<T>()
    {
        return typeof(T).GetProperties()
            .Where(p => !p.GetCustomAttributes().Any(a => a is System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute));
    }

    /// <summary>
    /// 获取主键属性
    /// </summary>
    private static PropertyInfo? GetPrimaryKey<T>()
    {
        return typeof(T).GetProperties()
            .FirstOrDefault(p => p.GetCustomAttributes()
                .Any(a => a is System.ComponentModel.DataAnnotations.KeyAttribute || 
                          a is System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute));
    }

    /// <summary>
    /// 检查属性是否为主键
    /// </summary>
    private static bool IsPrimaryKey(PropertyInfo property)
    {
        return property.GetCustomAttributes()
            .Any(a => a is System.ComponentModel.DataAnnotations.KeyAttribute || 
                      a is System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_connection is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _disposed = true;
        }
    }
}