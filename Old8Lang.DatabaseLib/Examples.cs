using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Old8Lang.DatabaseLib;

/// <summary>
/// 用户实体示例
/// </summary>
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

/// <summary>
/// 订单实体示例
/// </summary>
[Table("orders")]
public class Order
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }
    
    public DateTime OrderDate { get; set; } = DateTime.Now;
    
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";
}

/// <summary>
/// 产品实体示例
/// </summary>
[Table("products")]
public class Product
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }
    
    public int Stock { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 数据库库使用示例
/// </summary>
public class Examples
{
    /// <summary>
    /// SQLite 基本操作示例
    /// </summary>
    public static void SqliteBasicExample()
    {
        // 创建 SQLite 连接
        var connection = DatabaseLibBinding.CreateSqliteConnection("Data Source=example.db");
        DatabaseLibBinding.OpenConnection(connection);

        try
        {
            // 创建表
            DatabaseLibBinding.ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Email TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                )");

            // 插入数据
            DatabaseLibBinding.ExecuteNonQuery(connection, 
                "INSERT INTO users (Name, Email) VALUES (@name, @email)", 
                new { name = "John Doe", email = "john@example.com" });

            // 查询数据
            var users = DatabaseLibBinding.ExecuteQuery(connection, "SELECT * FROM users");
            foreach (var user in users)
            {
                Console.WriteLine($"User: {user.Name} - {user.Email}");
            }
        }
        finally
        {
            DatabaseLibBinding.CloseConnection(connection);
        }
    }

    /// <summary>
    /// 事务操作示例
    /// </summary>
    public static void TransactionExample()
    {
        var connection = DatabaseLibBinding.CreateSqliteConnection("Data Source=example.db");
        DatabaseLibBinding.OpenConnection(connection);

        var transaction = DatabaseLibBinding.BeginTransaction(connection);
        try
        {
            // 插入用户
            var userId = DatabaseLibBinding.ExecuteScalar(connection, 
                "INSERT INTO users (Name, Email) VALUES (@name, @email); SELECT last_insert_rowid();",
                new { name = "Alice", email = "alice@example.com" });

            // 插入订单
            DatabaseLibBinding.ExecuteNonQuery(connection,
                "INSERT INTO orders (UserId, Amount, Status) VALUES (@userId, @amount, @status)",
                new { userId, amount = 99.99m, status = "Pending" });

            // 提交事务
            DatabaseLibBinding.CommitTransaction(transaction);
            Console.WriteLine("事务提交成功");
        }
        catch
        {
            // 回滚事务
            DatabaseLibBinding.RollbackTransaction(transaction);
            Console.WriteLine("事务回滚");
        }
        finally
        {
            DatabaseLibBinding.CloseConnection(connection);
        }
    }

    /// <summary>
    /// ORM 操作示例
    /// </summary>
    public static void OrmExample()
    {
        var connection = DatabaseLibBinding.CreateSqliteConnection("Data Source=example.db");
        DatabaseLibBinding.OpenConnection(connection);

        try
        {
            // 创建 ORM 实例
            var orm = DatabaseLibBinding.CreateOrm(connection);

            // 插入用户
            var user = new User { Name = "Bob", Email = "bob@example.com" };
            DatabaseLibBinding.Insert(orm, user);
            Console.WriteLine($"插入用户 ID: {user.Id}");

            // 查询用户
            var foundUser = DatabaseLibBinding.QueryById(orm, typeof(User), user.Id) as User;
            if (foundUser != null)
            {
                Console.WriteLine($"查询到用户: {foundUser.Name}");
            }

            // 更新用户
            foundUser!.Email = "bob.updated@example.com";
            DatabaseLibBinding.Update(orm, foundUser);
            Console.WriteLine("用户更新成功");

            // 查询所有用户
            var allUsers = DatabaseLibBinding.QueryAll(orm, typeof(User)) as IEnumerable<User>;
            Console.WriteLine($"总用户数: {allUsers?.Count()}");
        }
        finally
        {
            DatabaseLibBinding.CloseConnection(connection);
        }
    }

    /// <summary>
    /// MySQL 操作示例
    /// </summary>
    public static void MySqlExample()
    {
        var connectionString = "Server=localhost;Database=test;User Id=root;Password=password;";
        var connection = DatabaseLibBinding.CreateMySqlConnection(connectionString);
        DatabaseLibBinding.OpenConnection(connection);

        try
        {
            // MySQL 特定操作
            DatabaseLibBinding.ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS users (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Name VARCHAR(100) NOT NULL,
                    Email VARCHAR(255),
                    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                ) ENGINE=InnoDB");

            // 操作方式与 SQLite 相同
            var users = DatabaseLibBinding.ExecuteQuery(connection, "SELECT COUNT(*) as UserCount FROM users");
            Console.WriteLine($"MySQL 用户数量: {users.First().UserCount}");
        }
        finally
        {
            DatabaseLibBinding.CloseConnection(connection);
        }
    }

    /// <summary>
    /// PostgreSQL 操作示例
    /// </summary>
    public static void PostgresExample()
    {
        var connectionString = "Host=localhost;Database=test;Username=postgres;Password=password;";
        var connection = DatabaseLibBinding.CreatePostgresConnection(connectionString);
        DatabaseLibBinding.OpenConnection(connection);

        try
        {
            // PostgreSQL 特定操作
            DatabaseLibBinding.ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS users (
                    Id SERIAL PRIMARY KEY,
                    Name VARCHAR(100) NOT NULL,
                    Email VARCHAR(255),
                    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )");

            // 操作方式与其他数据库相同
            var users = DatabaseLibBinding.ExecuteQuery(connection, "SELECT COUNT(*) as UserCount FROM users");
            Console.WriteLine($"PostgreSQL 用户数量: {users.First().UserCount}");
        }
        finally
        {
            DatabaseLibBinding.CloseConnection(connection);
        }
    }

    /// <summary>
    /// 复杂 ORM 操作示例
    /// </summary>
    public static void AdvancedOrmExample()
    {
        var connection = DatabaseLibBinding.CreateSqliteConnection("Data Source=example.db");
        DatabaseLibBinding.OpenConnection(connection);

        try
        {
            var orm = DatabaseLibBinding.CreateOrm(connection);

            // 创建表
            DatabaseLibBinding.ExecuteNonQuery(connection, @"
                CREATE TABLE IF NOT EXISTS products (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    Price DECIMAL(10,2),
                    Stock INTEGER DEFAULT 0,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                )");

            // 批量插入产品
            var products = new[]
            {
                new Product { Name = "Laptop", Description = "High performance laptop", Price = 999.99m, Stock = 50 },
                new Product { Name = "Mouse", Description = "Wireless mouse", Price = 29.99m, Stock = 200 },
                new Product { Name = "Keyboard", Description = "Mechanical keyboard", Price = 79.99m, Stock = 100 }
            };

            foreach (var product in products)
            {
                DatabaseLibBinding.Insert(orm, product);
            }

            // 查询所有产品
            var allProducts = DatabaseLibBinding.QueryAll(orm, typeof(Product)) as IEnumerable<Product>;
            Console.WriteLine($"产品数量: {allProducts?.Count()}");

            // 更新产品价格
            if (allProducts?.Any() == true)
            {
                var firstProduct = allProducts.First();
                firstProduct.Price *= 0.9m; // 降价10%
                DatabaseLibBinding.Update(orm, firstProduct);
                Console.WriteLine($"产品 {firstProduct.Name} 已降价");
            }
        }
        finally
        {
            DatabaseLibBinding.CloseConnection(connection);
        }
    }
}