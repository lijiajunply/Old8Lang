using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.StandardLibrary;

/// <summary>
/// DatabaseLib 库测试 - 测试数据库操作功能
/// </summary>
public class DatabaseLibTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_Database_ShouldWorkCorrectly()
    {
        var code = @"
import Database

PrintLine(""Database library imported"")
";
        CreateTempModuleFile("./StandardLibrary/database_test.old8", code);
        var (interpreter, exception) = ExecuteCodeFile("./StandardLibrary/database_test.old8");

        Assert.Null(exception);
        var dbLib = interpreter.Manager.GetValue(new LangId("Database"));
        Assert.NotNull(dbLib);
        Assert.IsAssignableFrom<IModuleValueType>(dbLib);
    }

    [Fact]
    public void CreateInMemoryConnection_ShouldWorkCorrectly()
    {
        var code = @"
import Database

conn <- Database.CreateInMemoryConnection(""testdb"")
PrintLine($""In-memory database connection created"")
";
        CreateTempModuleFile("./StandardLibrary/database_inmemory_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/database_inmemory_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void ExecuteNonQuery_CreateTable_ShouldWorkCorrectly()
    {
        var code = @"
import Database

conn <- Database.CreateInMemoryConnection(""testdb"")
Database.OpenConnection(conn)

createTableSql <- ""CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT, age INTEGER)""
result <- Database.ExecuteNonQuery(conn, createTableSql, null)
PrintLine($""Table created, affected rows: {result}"")

Database.CloseConnection(conn)
";
        CreateTempModuleFile("./StandardLibrary/database_create_table_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/database_create_table_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void ExecuteNonQuery_InsertData_ShouldWorkCorrectly()
    {
        var code = @"
import Database

conn <- Database.CreateInMemoryConnection(""testdb"")
Database.OpenConnection(conn)

// Create table
Database.ExecuteNonQuery(conn, ""CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT, age INTEGER)"", null)

// Insert data
insertSql <- ""INSERT INTO users (id, name, age) VALUES (1, 'Alice', 30)""
result <- Database.ExecuteNonQuery(conn, insertSql, null)
PrintLine($""Data inserted, affected rows: {result}"")

Database.CloseConnection(conn)
";
        CreateTempModuleFile("./StandardLibrary/database_insert_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/database_insert_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void ExecuteQuery_SelectData_ShouldWorkCorrectly()
    {
        var code = @"
import Database

conn <- Database.CreateInMemoryConnection(""testdb"")
Database.OpenConnection(conn)

// Create and populate table
Database.ExecuteNonQuery(conn, ""CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT, age INTEGER)"", null)
Database.ExecuteNonQuery(conn, ""INSERT INTO users (id, name, age) VALUES (1, 'Alice', 30)"", null)
Database.ExecuteNonQuery(conn, ""INSERT INTO users (id, name, age) VALUES (2, 'Bob', 25)"", null)

// Query data
selectSql <- ""SELECT * FROM users""
results <- Database.ExecuteQuery(conn, selectSql, null)
PrintLine($""Query executed, results: {results}"")

Database.CloseConnection(conn)
";
        CreateTempModuleFile("./StandardLibrary/database_query_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/database_query_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void ExecuteScalar_CountRows_ShouldWorkCorrectly()
    {
        var code = @"
import Database

conn <- Database.CreateInMemoryConnection(""testdb"")
Database.OpenConnection(conn)

// Create and populate table
Database.ExecuteNonQuery(conn, ""CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT, age INTEGER)"", null)
Database.ExecuteNonQuery(conn, ""INSERT INTO users (id, name, age) VALUES (1, 'Alice', 30)"", null)
Database.ExecuteNonQuery(conn, ""INSERT INTO users (id, name, age) VALUES (2, 'Bob', 25)"", null)

// Count rows
countSql <- ""SELECT COUNT(*) FROM users""
count <- Database.ExecuteScalar(conn, countSql, null)
PrintLine($""Total users: {count}"")

Database.CloseConnection(conn)
";
        CreateTempModuleFile("./StandardLibrary/database_scalar_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/database_scalar_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Transaction_CommitChanges_ShouldWorkCorrectly()
    {
        var code = @"
import Database

conn <- Database.CreateInMemoryConnection(""testdb"")
Database.OpenConnection(conn)

// Create table
Database.ExecuteNonQuery(conn, ""CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT)"", null)

// Begin transaction
trans <- Database.BeginTransaction(conn)
Database.ExecuteNonQuery(conn, ""INSERT INTO users (id, name) VALUES (1, 'Alice')"", null)
Database.ExecuteNonQuery(conn, ""INSERT INTO users (id, name) VALUES (2, 'Bob')"", null)
Database.CommitTransaction(trans)

PrintLine(""Transaction committed"")

Database.CloseConnection(conn)
";
        CreateTempModuleFile("./StandardLibrary/database_transaction_commit_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/database_transaction_commit_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Transaction_RollbackChanges_ShouldWorkCorrectly()
    {
        var code = @"
import Database

conn <- Database.CreateInMemoryConnection(""testdb"")
Database.OpenConnection(conn)

// Create table
Database.ExecuteNonQuery(conn, ""CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT)"", null)

// Begin transaction
trans <- Database.BeginTransaction(conn)
Database.ExecuteNonQuery(conn, ""INSERT INTO users (id, name) VALUES (1, 'Alice')"", null)
Database.RollbackTransaction(trans)

PrintLine(""Transaction rolled back"")

Database.CloseConnection(conn)
";
        CreateTempModuleFile("./StandardLibrary/database_transaction_rollback_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/database_transaction_rollback_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void ClearMemoryDatabase_ShouldWorkCorrectly()
    {
        var code = @"
import Database

conn <- Database.CreateInMemoryConnection(""testdb"")
Database.OpenConnection(conn)

// Create and populate table
Database.ExecuteNonQuery(conn, ""CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT)"", null)
Database.ExecuteNonQuery(conn, ""INSERT INTO users (id, name) VALUES (1, 'Alice')"", null)

// Clear database
Database.ClearMemoryDatabase(conn)
PrintLine(""Memory database cleared"")

Database.CloseConnection(conn)
";
        CreateTempModuleFile("./StandardLibrary/database_clear_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/database_clear_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void ResetMemoryDatabase_ShouldWorkCorrectly()
    {
        var code = @"
import Database

conn <- Database.CreateInMemoryConnection(""testdb"")
Database.OpenConnection(conn)

// Create table
Database.ExecuteNonQuery(conn, ""CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT)"", null)

// Reset database
Database.ResetMemoryDatabase(conn)
PrintLine(""Memory database reset"")

Database.CloseConnection(conn)
";
        CreateTempModuleFile("./StandardLibrary/database_reset_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/database_reset_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void GetMemoryDatabaseStatistics_ShouldWorkCorrectly()
    {
        var code = @"
import Database

conn <- Database.CreateInMemoryConnection(""testdb"")
Database.OpenConnection(conn)

// Create and populate table
Database.ExecuteNonQuery(conn, ""CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT)"", null)
Database.ExecuteNonQuery(conn, ""INSERT INTO users (id, name) VALUES (1, 'Alice')"", null)

// Get statistics
stats <- Database.GetMemoryDatabaseStatistics(conn)
PrintLine($""Database statistics: {stats}"")

Database.CloseConnection(conn)
";
        CreateTempModuleFile("./StandardLibrary/database_stats_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/database_stats_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void CreateOrm_ShouldWorkCorrectly()
    {
        var code = @"
import Database

conn <- Database.CreateInMemoryConnection(""testdb"")
Database.OpenConnection(conn)

orm <- Database.CreateOrm(conn)
PrintLine($""ORM instance created"")

Database.CloseConnection(conn)
";
        CreateTempModuleFile("./StandardLibrary/database_orm_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/database_orm_test.old8");

        Assert.Null(exception);
    }
}
