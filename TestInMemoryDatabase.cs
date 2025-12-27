using System;
using Old8Lang.DatabaseLib;

// 测试实体类
public class TestUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class Program
{
    public static void Main()
    {
        Console.WriteLine("=== 测试内存数据库功能 ===");
        
        // 1. 创建内存数据库连接
        var connection = DatabaseLibBinding.CreateInMemoryConnection("test_db");
        Console.WriteLine("✓ 成功创建内存数据库连接");
        
        // 2. 创建表
        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS TestUser (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            )";
        
        DatabaseLibBinding.ExecuteNonQuery(connection, createTableSql);
        Console.WriteLine("✓ 成功创建 TestUser 表");
        
        // 3. 创建 ORM 实例
        var orm = DatabaseLibBinding.CreateOrm(connection);
        Console.WriteLine("✓ 成功创建 ORM 实例");
        
        // 4. 插入测试数据
        var testUser = new TestUser
        {
            Name = "测试用户1",
            Email = "test1@example.com",
            CreatedAt = DateTime.Now
        };
        
        var insertResult = ((OrmWrapper)orm).Insert(testUser);
        Console.WriteLine($"✓ 成功插入用户，影响的行数: {insertResult}");
        
        var testUser2 = new TestUser
        {
            Name = "测试用户2", 
            Email = "test2@example.com",
            CreatedAt = DateTime.Now
        };
        
        var insertResult2 = ((OrmWrapper)orm).Insert(testUser2);
        Console.WriteLine($"✓ 成功插入第二个用户，影响的行数: {insertResult2}");
        
        // 5. 查询所有用户
        var allUsers = ((OrmWrapper)orm).QueryAll<TestUser>();
        Console.WriteLine($"✓ 查询到 {allUsers.Count()} 个用户:");
        foreach (var user in allUsers)
        {
            Console.WriteLine($"  - ID: {user.Id}, 姓名: {user.Name}, 邮箱: {user.Email}");
        }
        
        // 6. 根据条件查询
        var filteredUsers = ((OrmWrapper)orm).QueryWhere<TestUser>("Name LIKE '%测试用户1%'");
        Console.WriteLine($"✓ 条件查询结果: {filteredUsers.Count()} 个用户");
        
        // 7. 更新用户
        if (allUsers.Any())
        {
            var firstUser = allUsers.First();
            firstUser.Email = "updated@example.com";
            var updateResult = ((OrmWrapper)orm).Update(firstUser);
            Console.WriteLine($"✓ 更新用户，影响的行数: {updateResult}");
        }
        
        // 8. 根据 ID 查询
        if (allUsers.Any())
        {
            var firstUser = allUsers.First();
            var userById = ((OrmWrapper)orm).QueryById<TestUser>(firstUser.Id);
            if (userById != null)
            {
                Console.WriteLine($"✓ 根据 ID 查询用户: {userById.Name}, 邮箱: {userById.Email}");
            }
        }
        
        // 9. 测试内存数据库特定功能
        if (connection is InMemoryConnectionWrapper memoryConn)
        {
            var stats = memoryConn.GetStatistics();
            Console.WriteLine($"✓ 内存数据库统计: 表数量 {stats.TablesCount}, 总记录数 {stats.TotalRecords}");
            
            // 清空数据
            memoryConn.Clear();
            Console.WriteLine("✓ 清空内存数据库");
            
            var statsAfterClear = memoryConn.GetStatistics();
            Console.WriteLine($"✓ 清空后统计: 表数量 {statsAfterClear.TablesCount}, 总记录数 {statsAfterClear.TotalRecords}");
        }
        
        Console.WriteLine("=== 内存数据库功能测试完成 ===");
    }
}