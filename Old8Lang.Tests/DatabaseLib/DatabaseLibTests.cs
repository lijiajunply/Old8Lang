using Xunit;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Old8Lang.DatabaseLib;

namespace Old8Lang.Tests.DatabaseLib;

/// <summary>
/// 数据库库单元测试
/// </summary>
public class DatabaseLibTests : IDisposable
{
    private const string TestConnectionString = "Data Source=:memory:";
    private readonly object _connection;

    public DatabaseLibTests()
    {
        _connection = DatabaseLibBinding.CreateSqliteConnection(TestConnectionString);
        DatabaseLibBinding.OpenConnection(_connection);
        
        // 创建测试表
        DatabaseLibBinding.ExecuteNonQuery(_connection, @"
            CREATE TABLE TestUsers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT,
                Age INTEGER
            )");

        DatabaseLibBinding.ExecuteNonQuery(_connection, @"
            CREATE TABLE TestProducts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Price DECIMAL(10,2),
                Stock INTEGER DEFAULT 0
            )");
    }

    public void Dispose()
    {
        DatabaseLibBinding.CloseConnection(_connection);
    }

    [Fact]
    public void CreateSqliteConnection_ShouldCreateValidConnection()
    {
        // Act
        var connection = DatabaseLibBinding.CreateSqliteConnection(TestConnectionString);
        
        // Assert
        Assert.NotNull(connection);
        Assert.IsAssignableFrom<SqliteConnectionWrapper>(connection);
    }

    [Fact]
    public void ExecuteNonQuery_ShouldInsertRecord()
    {
        // Act
        var result = DatabaseLibBinding.ExecuteNonQuery(_connection, 
            "INSERT INTO TestUsers (Name, Email, Age) VALUES (@name, @email, @age)", 
            new { name = "Test User", email = "test@example.com", age = 25 });

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void ExecuteQuery_ShouldReturnRecords()
    {
        // Arrange
        DatabaseLibBinding.ExecuteNonQuery(_connection, 
            "INSERT INTO TestUsers (Name, Email, Age) VALUES (@name, @email, @age)", 
            new { name = "Test User", email = "test@example.com", age = 25 });

        // Act
        var results = DatabaseLibBinding.ExecuteQuery(_connection, "SELECT * FROM TestUsers");

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal("Test User", results.First().Name);
        Assert.Equal("test@example.com", results.First().Email);
        Assert.Equal(25, results.First().Age);
    }

    [Fact]
    public void ExecuteScalar_ShouldReturnSingleValue()
    {
        // Arrange
        DatabaseLibBinding.ExecuteNonQuery(_connection, 
            "INSERT INTO TestUsers (Name, Email, Age) VALUES (@name, @email, @age)", 
            new { name = "Test User", email = "test@example.com", age = 25 });

        // Act
        var count = DatabaseLibBinding.ExecuteScalar(_connection, "SELECT COUNT(*) FROM TestUsers");

        // Assert
        Assert.Equal(1L, count);
    }

    [Fact]
    public void Transaction_Commit_ShouldPersistChanges()
    {
        // Act
        var transaction = DatabaseLibBinding.BeginTransaction(_connection);
        
        try
        {
            DatabaseLibBinding.ExecuteNonQuery(_connection, 
                "INSERT INTO TestUsers (Name, Email, Age) VALUES (@name, @email, @age)", 
                new { name = "Transaction User", email = "trans@example.com", age = 30 });
            
            DatabaseLibBinding.CommitTransaction(transaction);
        }
        catch
        {
            DatabaseLibBinding.RollbackTransaction(transaction);
            throw;
        }

        // Assert
        var count = DatabaseLibBinding.ExecuteScalar(_connection, "SELECT COUNT(*) FROM TestUsers");
        Assert.Equal(1L, count);
    }

    [Fact]
    public void Transaction_Rollback_ShouldNotPersistChanges()
    {
        // Act
        var transaction = DatabaseLibBinding.BeginTransaction(_connection);
        
        try
        {
            DatabaseLibBinding.ExecuteNonQuery(_connection, 
                "INSERT INTO TestUsers (Name, Email, Age) VALUES (@name, @email, @age)", 
                new { name = "Rollback User", email = "rollback@example.com", age = 35 });
            
            DatabaseLibBinding.RollbackTransaction(transaction);
        }
        catch
        {
            DatabaseLibBinding.RollbackTransaction(transaction);
            throw;
        }

        // Assert
        var count = DatabaseLibBinding.ExecuteScalar(_connection, "SELECT COUNT(*) FROM TestUsers");
        Assert.Equal(0L, count);
    }

    [Fact]
    public void Orm_Insert_ShouldAddRecord()
    {
        // Arrange
        var orm = DatabaseLibBinding.CreateOrm(_connection);
        var user = new TestUser { Name = "ORM User", Email = "orm@example.com", Age = 40 };

        // Act
        var result = DatabaseLibBinding.Insert(orm, user);

        // Assert
        Assert.Equal(1, result);
        Assert.True(user.Id > 0);
    }

    [Fact]
    public void Orm_QueryById_ShouldReturnRecord()
    {
        // Arrange
        var orm = DatabaseLibBinding.CreateOrm(_connection);
        var user = new TestUser { Name = "Query User", Email = "query@example.com", Age = 45 };
        DatabaseLibBinding.Insert(orm, user);

        // Act
        var foundUser = DatabaseLibBinding.QueryById(orm, typeof(TestUser), user.Id) as TestUser;

        // Assert
        Assert.NotNull(foundUser);
        Assert.Equal(user.Id, foundUser.Id);
        Assert.Equal("Query User", foundUser.Name);
        Assert.Equal("query@example.com", foundUser.Email);
        Assert.Equal(45, foundUser.Age);
    }

    [Fact]
    public void Orm_Update_ShouldModifyRecord()
    {
        // Arrange
        var orm = DatabaseLibBinding.CreateOrm(_connection);
        var user = new TestUser { Name = "Update User", Email = "update@example.com", Age = 50 };
        DatabaseLibBinding.Insert(orm, user);

        // Act
        user.Name = "Updated User";
        user.Age = 55;
        var result = DatabaseLibBinding.Update(orm, user);

        // Assert
        Assert.Equal(1, result);
        var foundUser = DatabaseLibBinding.QueryById(orm, typeof(TestUser), user.Id) as TestUser;
        Assert.NotNull(foundUser);
        Assert.Equal("Updated User", foundUser.Name);
        Assert.Equal(55, foundUser.Age);
    }

    [Fact]
    public void Orm_Delete_ShouldRemoveRecord()
    {
        // Arrange
        var orm = DatabaseLibBinding.CreateOrm(_connection);
        var user = new TestUser { Name = "Delete User", Email = "delete@example.com", Age = 60 };
        DatabaseLibBinding.Insert(orm, user);

        // Act
        var result = DatabaseLibBinding.Delete(orm, user);

        // Assert
        Assert.Equal(1, result);
        var foundUser = DatabaseLibBinding.QueryById(orm, typeof(TestUser), user.Id) as TestUser;
        Assert.Null(foundUser);
    }

    [Fact]
    public void Orm_QueryAll_ShouldReturnAllRecords()
    {
        // Arrange
        var orm = DatabaseLibBinding.CreateOrm(_connection);
        
        DatabaseLibBinding.Insert(orm, new TestUser { Name = "User 1", Email = "user1@example.com", Age = 25 });
        DatabaseLibBinding.Insert(orm, new TestUser { Name = "User 2", Email = "user2@example.com", Age = 30 });

        // Act
        var allUsers = DatabaseLibBinding.QueryAll(orm, typeof(TestUser)) as List<TestUser>;

        // Assert
        Assert.NotNull(allUsers);
        Assert.Equal(2, allUsers.Count);
    }
}

/// <summary>
/// 测试用户实体
/// </summary>
[Table("TestUsers")]
public class TestUser
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    public int Age { get; set; }
}

/// <summary>
/// 测试产品实体
/// </summary>
[Table("TestProducts")]
public class TestProduct
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }
    
    public int Stock { get; set; }
}