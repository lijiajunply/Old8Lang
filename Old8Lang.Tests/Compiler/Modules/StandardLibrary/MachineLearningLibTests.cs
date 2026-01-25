using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Modules.StandardLibrary;

/// <summary>
/// MachineLearningLib 库测试 - 测试机器学习功能
/// </summary>
[Collection("Sequential")]
public class MachineLearningLibTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_MachineLearning_ShouldWorkCorrectly()
    {
        var code = @"
import MachineLearning

PrintLine(""MachineLearning library imported"")
";
        CreateTempModuleFile("./StandardLibrary/ml_test.old8", code);
        var (interpreter, exception) = ExecuteCodeFile("./StandardLibrary/ml_test.old8");

        Assert.Null(exception);
        var mlLib = interpreter.Manager.GetValue(new LangId("MachineLearning"));
        Assert.NotNull(mlLib);
        Assert.IsAssignableFrom<IModuleValueType>(mlLib);
    }

    [Fact]
    public void InitMlContext_ShouldWorkCorrectly()
    {
        var code = @"
import MachineLearning

MachineLearning.InitMlContext(null)
PrintLine(""ML Context initialized"")
";
        CreateTempModuleFile("./StandardLibrary/ml_init_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/ml_init_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void InitMlContext_WithSeed_ShouldWorkCorrectly()
    {
        var code = @"
import MachineLearning

MachineLearning.InitMlContext(42)
PrintLine(""ML Context initialized with seed 42"")
";
        CreateTempModuleFile("./StandardLibrary/ml_init_seed_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/ml_init_seed_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void LoadDataFromCsv_ShouldLoadData()
    {
        var code = @"
import MachineLearning
import File

// Create sample CSV file
csvPath <- ""./test_ml_data.csv""
csvContent <- ""Label,Feature1,Feature2
1,2.5,3.5
0,1.2,2.1
1,3.3,4.2""
File.FileWrite(csvPath, csvContent)

// Initialize ML context and load data
MachineLearning.InitMlContext(null)
data <- MachineLearning.LoadDataFromCsv(csvPath, true, "","")
PrintLine($""Data loaded from CSV"")

// Clean up
File.DeleteFile(csvPath)
";
        CreateTempModuleFile("./StandardLibrary/ml_loadcsv_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/ml_loadcsv_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void SplitData_ShouldSplitIntoTrainAndTest()
    {
        var code = @"
import MachineLearning
import File

// Create sample CSV file
csvPath <- ""./test_ml_split.csv""
csvContent <- ""Label,Feature1,Feature2
1,2.5,3.5
0,1.2,2.1
1,3.3,4.2
0,1.8,2.8
1,4.1,5.3""
File.FileWrite(csvPath, csvContent)

// Initialize ML context and load data
MachineLearning.InitMlContext(42)
data <- MachineLearning.LoadDataFromCsv(csvPath, true, "","")

// Split data
splitResult <- MachineLearning.SplitData(data, 0.3)
PrintLine($""Data split into train and test sets"")

// Clean up
File.DeleteFile(csvPath)
";
        CreateTempModuleFile("./StandardLibrary/ml_split_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/ml_split_test.old8");

        Assert.Null(exception);
    }
}
