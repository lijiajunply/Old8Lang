namespace Old8Lang.DatabaseLib;

/// <summary>
/// Old8Lang 数据库库绑定类
/// 提供给 Old8Lang 语言使用的数据库功能
/// </summary>
public static class DatabaseLibBinding
{
    /// <summary>
    /// 创建 SQLite 数据库连接
    /// </summary>
    public static object CreateSqliteConnection(string connectionString)
    {
        return new SqliteConnectionWrapper(connectionString);
    }

    /// <summary>
    /// 创建 MySQL 数据库连接
    /// </summary>
    public static object CreateMySqlConnection(string connectionString)
    {
        return new MySqlConnectionWrapper(connectionString);
    }

    /// <summary>
    /// 创建 PostgreSQL 数据库连接
    /// </summary>
    public static object CreatePostgresConnection(string connectionString)
    {
        return new PostgresConnectionWrapper(connectionString);
    }

    /// <summary>
    /// 创建 ORM 实例
    /// </summary>
    public static object CreateOrm(object connection)
    {
        return new OrmWrapper(connection);
    }

    /// <summary>
    /// 执行 SQL 查询并返回结果
    /// </summary>
    public static object ExecuteQuery(object connection, string sql, object? parameters = null)
    {
        return connection switch
        {
            SqliteConnectionWrapper sqlite => sqlite.Query(sql, parameters),
            MySqlConnectionWrapper mysql => mysql.Query(sql, parameters),
            PostgresConnectionWrapper postgres => postgres.Query(sql, parameters),
            _ => throw new InvalidOperationException("不支持的数据库连接类型")
        };
    }

    /// <summary>
    /// 执行非查询 SQL 语句
    /// </summary>
    public static int ExecuteNonQuery(object connection, string sql, object? parameters = null)
    {
        return connection switch
        {
            SqliteConnectionWrapper sqlite => sqlite.Execute(sql, parameters),
            MySqlConnectionWrapper mysql => mysql.Execute(sql, parameters),
            PostgresConnectionWrapper postgres => postgres.Execute(sql, parameters),
            _ => throw new InvalidOperationException("不支持的数据库连接类型")
        };
    }

    /// <summary>
    /// 执行 SQL 查询并返回单个结果
    /// </summary>
    public static object ExecuteScalar(object connection, string sql, object? parameters = null)
    {
        return connection switch
        {
            SqliteConnectionWrapper sqlite => sqlite.ExecuteScalar(sql, parameters),
            MySqlConnectionWrapper mysql => mysql.ExecuteScalar(sql, parameters),
            PostgresConnectionWrapper postgres => postgres.ExecuteScalar(sql, parameters),
            _ => throw new InvalidOperationException("不支持的数据库连接类型")
        };
    }

    /// <summary>
    /// 开始事务
    /// </summary>
    public static object BeginTransaction(object connection)
    {
        return connection switch
        {
            SqliteConnectionWrapper sqlite => sqlite.BeginTransaction(),
            MySqlConnectionWrapper mysql => mysql.BeginTransaction(),
            PostgresConnectionWrapper postgres => postgres.BeginTransaction(),
            _ => throw new InvalidOperationException("不支持的数据库连接类型")
        };
    }

    /// <summary>
    /// 提交事务
    /// </summary>
    public static void CommitTransaction(object transaction)
    {
        switch (transaction)
        {
            case SqliteTransactionWrapper sqlite:
                sqlite.Commit();
                break;
            case MySqlTransactionWrapper mysql:
                mysql.Commit();
                break;
            case PostgresTransactionWrapper postgres:
                postgres.Commit();
                break;
            default:
                throw new InvalidOperationException("不支持的事务类型");
        }
    }

    /// <summary>
    /// 回滚事务
    /// </summary>
    public static void RollbackTransaction(object transaction)
    {
        switch (transaction)
        {
            case SqliteTransactionWrapper sqlite:
                sqlite.Rollback();
                break;
            case MySqlTransactionWrapper mysql:
                mysql.Rollback();
                break;
            case PostgresTransactionWrapper postgres:
                postgres.Rollback();
                break;
            default:
                throw new InvalidOperationException("不支持的事务类型");
        }
    }

    /// <summary>
    /// ORM 插入操作
    /// </summary>
    public static int Insert(object orm, object entity)
    {
        if (orm is OrmWrapper ormWrapper)
        {
            return ormWrapper.Insert(entity);
        }
        throw new InvalidOperationException("ORM 实例无效");
    }

    /// <summary>
    /// ORM 更新操作
    /// </summary>
    public static int Update(object orm, object entity)
    {
        if (orm is OrmWrapper ormWrapper)
        {
            return ormWrapper.Update(entity);
        }
        throw new InvalidOperationException("ORM 实例无效");
    }

    /// <summary>
    /// ORM 删除操作
    /// </summary>
    public static int Delete(object orm, object entity)
    {
        if (orm is OrmWrapper ormWrapper)
        {
            return ormWrapper.Delete(entity);
        }
        throw new InvalidOperationException("ORM 实例无效");
    }

    /// <summary>
    /// ORM 查询操作
    /// </summary>
    public static object QueryById(object orm, Type entityType, object id)
    {
        if (orm is OrmWrapper ormWrapper)
        {
            var method = typeof(OrmWrapper)
                .GetMethod(nameof(OrmWrapper.QueryById))!
                .MakeGenericMethod(entityType);
            return method.Invoke(ormWrapper, [id])!;
        }
        throw new InvalidOperationException("ORM 实例无效");
    }

    /// <summary>
    /// ORM 查询所有记录
    /// </summary>
    public static object QueryAll(object orm, Type entityType)
    {
        if (orm is OrmWrapper ormWrapper)
        {
            var method = typeof(OrmWrapper)
                .GetMethod(nameof(OrmWrapper.QueryAll))!
                .MakeGenericMethod(entityType);
            return method.Invoke(ormWrapper, [])!;
        }
        throw new InvalidOperationException("ORM 实例无效");
    }

    /// <summary>
    /// 打开数据库连接
    /// </summary>
    public static void OpenConnection(object connection)
    {
        switch (connection)
        {
            case SqliteConnectionWrapper sqlite:
                sqlite.Open();
                break;
            case MySqlConnectionWrapper mysql:
                mysql.Open();
                break;
            case PostgresConnectionWrapper postgres:
                postgres.Open();
                break;
            default:
                throw new InvalidOperationException("不支持的数据库连接类型");
        }
    }

    /// <summary>
    /// 关闭数据库连接
    /// </summary>
    public static void CloseConnection(object connection)
    {
        switch (connection)
        {
            case SqliteConnectionWrapper sqlite:
                sqlite.Close();
                break;
            case MySqlConnectionWrapper mysql:
                mysql.Close();
                break;
            case PostgresConnectionWrapper postgres:
                postgres.Close();
                break;
            default:
                throw new InvalidOperationException("不支持的数据库连接类型");
        }
    }
}