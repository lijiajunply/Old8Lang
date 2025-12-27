using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Old8Lang.DatabaseLib;

namespace Old8Lang.Tests.DatabaseLib;

/// <summary>
/// 内存数据库单元测试
/// </summary>
public class InMemoryDatabaseTests
{
    [Fact]
    public void CreateInMemoryConnection_ShouldCreateValidConnection()
    {
        // Act
        var connection = DatabaseLibBinding.CreateInMemoryConnection("test_db");
        
        // Assert
        Assert.NotNull(connection);
        Assert.IsAssignableFrom<InMemoryConnectionWrapper>(connection);
    }

    [Fact]
    public void InMemoryDatabase_BasicOperations_ShouldWorkCorrectly()
    {
        // Arrange
        var connection = DatabaseLibBinding.CreateInMemoryConnection("basic_test");
        DatabaseLibBinding.OpenConnection(connection);

        try
        {
            // Act & Assert - Create table
            DatabaseLibBinding.ExecuteNonQuery(connection, @"
                CREATE TABLE test_users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Email TEXT
                )");

            // Act & Assert - Insert data
            var insertResult = DatabaseLibBinding.ExecuteNonQuery(connection, 
                "INSERT INTO test_users (Name, Email) VALUES (@name, @email)", 
                new { name = "测试用户", email = "test@example.com" });
            Assert.Equal(1, insertResult);

            // Act & Assert - Query data
            var users = DatabaseLibBinding.ExecuteQuery(connection, "SELECT * FROM test_users");
            var usersList = ((IEnumerable<dynamic>)users).ToList();
            Assert.Single(usersList);
            Assert.Equal("测试用户", usersList.First().Name);
            Assert.Equal("test@example.com", usersList.First().Email);

            // Act & Assert - Count data
            var count = DatabaseLibBinding.ExecuteScalar(connection, "SELECT COUNT(*) FROM test_users");
            Assert.Equal(1L, count);
        }
        finally
        {
            DatabaseLibBinding.CloseConnection(connection);
        }
    }

    [Fact]
    public void InMemoryDatabase_Transactions_ShouldWorkCorrectly()
    {
        // Arrange
        var connection = DatabaseLibBinding.CreateInMemoryConnection("transaction_test");
        DatabaseLibBinding.OpenConnection(connection);

        try
        {
            // Create table
            DatabaseLibBinding.ExecuteNonQuery(connection, @"
                CREATE TABLE test_accounts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Balance DECIMAL(10,2) DEFAULT 0.00
                )");

            // Insert initial data
            DatabaseLibBinding.ExecuteNonQuery(connection, 
                "INSERT INTO test_accounts (Name, Balance) VALUES (@name, @balance)", 
                new { name = "账户A", balance = 1000.00m });

            // Act - Successful transaction
            var transaction = DatabaseLibBinding.BeginTransaction(connection);
            try
            {
                DatabaseLibBinding.ExecuteNonQuery(connection, 
                    "UPDATE test_accounts SET Balance = Balance - @amount WHERE Name = @name", 
                    new { name = "账户A", amount = 200.00m });

                DatabaseLibBinding.ExecuteNonQuery(connection, 
                    "INSERT INTO test_accounts (Name, Balance) VALUES (@name, @balance)", 
                    new { name = "账户B", balance = 200.00m });

                DatabaseLibBinding.CommitTransaction(transaction);
            }
            catch
            {
                DatabaseLibBinding.RollbackTransaction(transaction);
                throw;
            }

            // Assert - Check results
            var accounts = DatabaseLibBinding.ExecuteQuery(connection, "SELECT * FROM test_accounts ORDER BY Name");
            var accountsList = ((IEnumerable<dynamic>)accounts).ToList();
            Assert.Equal(2, accountsList.Count);
            
            var accountA = accountsList.First(a => a.Name == "账户A");
            var accountB = accountsList.First(a => a.Name == "账户B");
            Assert.Equal(800.00m, accountA.Balance);
            Assert.Equal(200.00m, accountB.Balance);
        }
        finally
        {
            DatabaseLibBinding.CloseConnection(connection);
        }
    }

    [Fact]
    public void InMemoryDatabase_OrmOperations_ShouldWorkCorrectly()
    {
        // Arrange
        var connection = DatabaseLibBinding.CreateInMemoryConnection("orm_test");
        DatabaseLibBinding.OpenConnection(connection);

        try
        {
            // Create table
            DatabaseLibBinding.ExecuteNonQuery(connection, @"
                CREATE TABLE test_products (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Price DECIMAL(10,2),
                    Stock INTEGER DEFAULT 0
                )");

            // Act & Assert - ORM operations
            var orm = DatabaseLibBinding.CreateOrm(connection);
            var product = new TestProductEntity 
            { 
                Name = "测试产品", 
                Price = 99.99m, 
                Stock = 50 
            };

            // Insert
            var insertResult = DatabaseLibBinding.Insert(orm, product);
            Assert.Equal(1, insertResult);
            Assert.True(product.Id > 0);

            // Query by ID
            var foundProduct = DatabaseLibBinding.QueryById(orm, typeof(TestProductEntity), product.Id) as TestProductEntity;
            Assert.NotNull(foundProduct);
            Assert.Equal(product.Id, foundProduct.Id);
            Assert.Equal("测试产品", foundProduct.Name);
            Assert.Equal(99.99m, foundProduct.Price);
            Assert.Equal(50, foundProduct.Stock);

            // Update
            foundProduct!.Price = 89.99m;
            foundProduct.Stock = 45;
            var updateResult = DatabaseLibBinding.Update(orm, foundProduct);
            Assert.Equal(1, updateResult);

            // Verify update
            var updatedProduct = DatabaseLibBinding.QueryById(orm, typeof(TestProductEntity), product.Id) as TestProductEntity;
            Assert.Equal(89.99m, updatedProduct!.Price);
            Assert.Equal(45, updatedProduct.Stock);

            // Query all
            var allProducts = DatabaseLibBinding.QueryAll(orm, typeof(TestProductEntity)) as List<TestProductEntity>;
            Assert.Single(allProducts);

            // Delete
            var deleteResult = DatabaseLibBinding.Delete(orm, foundProduct);
            Assert.Equal(1, deleteResult);

            // Verify delete
            var deletedProduct = DatabaseLibBinding.QueryById(orm, typeof(TestProductEntity), product.Id) as TestProductEntity;
            Assert.Null(deletedProduct);
        }
        finally
        {
            DatabaseLibBinding.CloseConnection(connection);
        }
    }

    [Fact]
    public void InMemoryDatabase_StatisticsAndManagement_ShouldWorkCorrectly()
    {
        // Arrange
        var connection = DatabaseLibBinding.CreateInMemoryConnection("management_test");
        DatabaseLibBinding.OpenConnection(connection);

        try
        {
            // Create multiple tables
            DatabaseLibBinding.ExecuteNonQuery(connection, @"
                CREATE TABLE customers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL
                )");

            DatabaseLibBinding.ExecuteNonQuery(connection, @"
                CREATE TABLE orders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CustomerId INTEGER,
                    Amount DECIMAL(10,2)
                )");

            // Insert test data
            DatabaseLibBinding.ExecuteNonQuery(connection, 
                "INSERT INTO customers (Name) VALUES (@name)", 
                new { name = "客户A" });

            DatabaseLibBinding.ExecuteNonQuery(connection, 
                "INSERT INTO customers (Name) VALUES (@name)", 
                new { name = "客户B" });

            DatabaseLibBinding.ExecuteNonQuery(connection, 
                "INSERT INTO orders (CustomerId, Amount) VALUES (@customerId, @amount)", 
                new { customerId = 1, amount = 100.00m });

            // Act - Get statistics
            var stats = DatabaseLibBinding.GetMemoryDatabaseStatistics(connection);

            // Assert
            Assert.Equal(2, stats.TotalTables);
            Assert.Equal(3, stats.TotalRecords);
            Assert.True(stats.IsOpen);
            Assert.Contains("customers", stats.TableRecords.Keys);
            Assert.Contains("orders", stats.TableRecords.Keys);
            Assert.Equal(2, stats.TableRecords["customers"]);
            Assert.Equal(1, stats.TableRecords["orders"]);

            // Act & Assert - Clear all data
            DatabaseLibBinding.ClearMemoryDatabase(connection);
            var clearedStats = DatabaseLibBinding.GetMemoryDatabaseStatistics(connection);
            Assert.Equal(2, clearedStats.TotalTables); // Tables still exist
            Assert.Equal(0, clearedStats.TotalRecords); // But no records

            // Act & Assert - Reset database
            DatabaseLibBinding.ResetMemoryDatabase(connection);
            var resetStats = DatabaseLibBinding.GetMemoryDatabaseStatistics(connection);
            Assert.Equal(0, resetStats.TotalTables); // No tables
            Assert.Equal(0, resetStats.TotalRecords); // No records
        }
        finally
        {
            DatabaseLibBinding.CloseConnection(connection);
        }
    }
}

/// <summary>
/// 测试产品实体
/// </summary>
[Table("test_products")]
public class TestProductEntity
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