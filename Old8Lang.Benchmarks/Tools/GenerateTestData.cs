namespace Old8Lang.Benchmarks.Tools;

/// <summary>
/// 用于生成测试数据的临时程序
/// </summary>
public class GenerateTestDataProgram
{
    public static void Main(string[] args)
    {
        Console.WriteLine("开始生成测试数据...");

        var baseDir = Path.GetDirectoryName(typeof(GenerateTestDataProgram).Assembly.Location);
        var testDataDir = Path.Combine(baseDir!, "TestData");

        TestDataGenerator.GenerateTestScript(500, Path.Combine(testDataDir, "small_script_500.old8"));
        TestDataGenerator.GenerateTestScript(3000, Path.Combine(testDataDir, "medium_project_3000.old8"));
        TestDataGenerator.GenerateTestScript(5000, Path.Combine(testDataDir, "large_script_5000.old8"));

        Console.WriteLine("测试数据生成完成！");
    }
}
