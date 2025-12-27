# Old8Lang DatabaseLib

Old8Lang 数据库库提供统一的数据库访问接口，支持多种数据库类型和轻量级 ORM 功能。

## 支持的数据库

- **SQLite** - 轻量级文件数据库
- **MySQL** - 流行的关系型数据库
- **PostgreSQL** - 高级关系型数据库
- **内存数据库** - 基于SQLite的内存数据库，适合测试和临时操作

## 主要功能

### 1. 统一的数据库连接接口
- 支持多种数据库类型
- 统一的 API 设计
- 自动连接管理

### 2. 轻量级 ORM
- 自动映射实体到数据库表
- 支持基本的 CRUD 操作
- 灵活的查询功能

### 3. 事务支持
- 完整的事务管理
- 支持提交和回滚

## 使用方法

### 基本连接操作

#### SQLite
```csharp
// 创建连接
var connection = DatabaseLibBinding.CreateSqliteConnection("Data Source=test.db");

// 打开连接
DatabaseLibBinding.OpenConnection(connection);

// 执行查询
var results = DatabaseLibBinding.ExecuteQuery(connection, "SELECT * FROM users");

// 执行非查询
var rowsAffected = DatabaseLibBinding.ExecuteNonQuery(connection, "INSERT INTO users (name) VALUES (@name)", new { name = "John" });

// 关闭连接
DatabaseLibBinding.CloseConnection(connection);
```

#### MySQL
```csharp
// 创建连接
var connection = DatabaseLibBinding.CreateMySqlConnection("Server=localhost;Database=test;User Id=root;Password=password;");

// 操作方式与 SQLite 相同
```

#### PostgreSQL
```csharp
// 创建连接
var connection = DatabaseLibBinding.CreatePostgresConnection("Host=localhost;Database=test;Username=postgres;Password=password;");

// 操作方式与 SQLite 相同
```

#### 内存数据库
```csharp
// 创建内存数据库
var connection = DatabaseLibBinding.CreateInMemoryConnection("test_db");

// 内存数据库特有操作
DatabaseLibBinding.ClearMemoryDatabase(connection);  // 清空所有表数据
DatabaseLibBinding.ResetMemoryDatabase(connection);   // 重置数据库（删除所有表）
var stats = DatabaseLibBinding.GetMemoryDatabaseStatistics(connection); // 获取统计信息
```

### 事务操作

```csharp
// 开始事务
var transaction = DatabaseLibBinding.BeginTransaction(connection);

try
{
    // 执行多个操作
    DatabaseLibBinding.ExecuteNonQuery(connection, "INSERT INTO users (name) VALUES (@name)", new { name = "Alice" });
    DatabaseLibBinding.ExecuteNonQuery(connection, "INSERT INTO orders (user_id, amount) VALUES (@userId, @amount)", new { userId = 1, amount = 100 });
    
    // 提交事务
    DatabaseLibBinding.CommitTransaction(transaction);
}
catch
{
    // 发生错误时回滚事务
    DatabaseLibBinding.RollbackTransaction(transaction);
}
```

### ORM 操作

#### 定义实体类
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("users")]
public class User
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
```

#### 基本 ORM 操作
```csharp
// 创建 ORM 实例
var orm = DatabaseLibBinding.CreateOrm(connection);

// 插入
var user = new User { Name = "John", Email = "john@example.com" };
var insertResult = DatabaseLibBinding.Insert(orm, user);

// 更新
user.Email = "john.doe@example.com";
var updateResult = DatabaseLibBinding.Update(orm, user);

// 根据 ID 查询
var foundUser = DatabaseLibBinding.QueryById(orm, typeof(User), 1) as User;

// 查询所有
var allUsers = DatabaseLibBinding.QueryAll(orm, typeof(User)) as IEnumerable<User>;

// 删除
var deleteResult = DatabaseLibBinding.Delete(orm, user);
```

## 连接字符串示例

### SQLite
```
Data Source=path/to/database.db;Version=3;
```

### MySQL
```
Server=localhost;Database=mydatabase;User Id=myuser;Password=mypassword;Port=3306;
```

### PostgreSQL
```
Host=localhost;Database=mydatabase;Username=myuser;Password=mypassword;Port=5432;
```

## 最佳实践

1. **使用连接池** - 数据库连接应该被复用而不是频繁创建
2. **参数化查询** - 始终使用参数化查询防止 SQL 注入
3. **事务管理** - 相关的数据库操作应该在事务中执行
4. **错误处理** - 始终包含适当的错误处理逻辑
5. **资源释放** - 及时关闭连接和释放资源

## 依赖包

- `Microsoft.Data.Sqlite` - SQLite 支持
- `MySql.Data` - MySQL 支持  
- `Npgsql` - PostgreSQL 支持
- `Dapper` - 轻量级 ORM 框架
- `System.ComponentModel.DataAnnotations` - 数据注解

## 示例项目

完整的示例项目可以在 `TestProjects/DatabaseTest` 目录中找到，包括：

- 基本的 CRUD 操作
- 事务使用示例
- 多种数据库的使用方式
- ORM 映射示例

## 性能考虑

- SQLite 适合小型应用和开发环境
- MySQL 适合中小型应用
- PostgreSQL 适合大型企业应用
- ORM 操作比原生 SQL 稍慢，但提供更好的开发体验
- 批量操作比单条操作性能更好

## 错误处理

数据库库会抛出标准的异常类型：

- `ArgumentException` - 参数错误
- `InvalidOperationException` - 操作无效
- `Exception` - 数据库相关错误

建议在代码中适当捕获和处理这些异常。