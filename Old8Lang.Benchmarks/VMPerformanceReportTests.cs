using Xunit;

namespace Old8Lang.Benchmarks;

public class VMPerformanceReportTests : IDisposable
{
    private readonly string _tempRoot;

    public VMPerformanceReportTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "vm-report-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public void GenerateFromBenchmarkArtifacts_WithValidCsv_GeneratesMarkdownAndJson()
    {
        var artifactsDir = Path.Combine(_tempRoot, "artifacts");
        var reportsDir = Path.Combine(_tempRoot, "reports");
        Directory.CreateDirectory(artifactsDir);
        Directory.CreateDirectory(reportsDir);

        var csvPath = Path.Combine(artifactsDir, "Old8Lang.Benchmarks.VMModePerformanceBenchmarks-report.csv");
        File.WriteAllText(csvPath, BuildSampleCsv());

        var (markdownPath, jsonPath) = VMPerformanceReport.GenerateFromBenchmarkArtifacts(artifactsDir, reportsDir);

        Assert.True(File.Exists(markdownPath));
        Assert.True(File.Exists(jsonPath));

        var markdown = File.ReadAllText(markdownPath);
        Assert.Contains("Old8Lang VM Performance Report", markdown);
        Assert.Contains("VM_ArithmeticLoop", markdown);
        Assert.Contains("30.508", markdown);
        Assert.Contains("3690045", markdown);
        Assert.Contains("N/A", markdown); // P95 默认不可用
    }

    [Fact]
    public void GenerateFromBenchmarkArtifacts_WithNaValues_OutputsNaAndWarningsSection()
    {
        var artifactsDir = Path.Combine(_tempRoot, "artifacts-na");
        var reportsDir = Path.Combine(_tempRoot, "reports-na");
        Directory.CreateDirectory(artifactsDir);
        Directory.CreateDirectory(reportsDir);

        var csvPath = Path.Combine(artifactsDir, "Old8Lang.Benchmarks.VMModePerformanceBenchmarks-report.csv");
        File.WriteAllText(csvPath, BuildCsvWithNa());

        var (markdownPath, jsonPath) = VMPerformanceReport.GenerateFromBenchmarkArtifacts(artifactsDir, reportsDir);
        var markdown = File.ReadAllText(markdownPath);
        var json = File.ReadAllText(jsonPath);

        Assert.Contains("N/A", markdown);
        Assert.Contains("VM_DefaultAndNamedArgs", markdown);
        Assert.Contains("\"Scenario\": \"VM_DefaultAndNamedArgs\"", json);
    }

    [Fact]
    public void GenerateFromBenchmarkArtifacts_WhenCsvMissing_ThrowsFileNotFoundException()
    {
        var artifactsDir = Path.Combine(_tempRoot, "missing-artifacts");
        var reportsDir = Path.Combine(_tempRoot, "missing-reports");
        Directory.CreateDirectory(artifactsDir);
        Directory.CreateDirectory(reportsDir);

        var exception = Assert.Throws<FileNotFoundException>(() =>
            VMPerformanceReport.GenerateFromBenchmarkArtifacts(artifactsDir, reportsDir));

        Assert.Contains("未找到 VM BenchmarkDotNet 报告 CSV", exception.Message);
    }

    private static string BuildSampleCsv()
    {
        return string.Join(
            Environment.NewLine,
            "Method,Job,Runtime,WarmupCount,IterationCount,Mean,StdDev,Allocated",
            "VM_ArithmeticLoop,DefaultJob,.NET 10.0,3,8,30.508 ms,1.337 ms,3603.56 KB",
            "VM_DenseFunctionCall,DefaultJob,.NET 10.0,3,8,115.061 ms,2.113 ms,8403.61 KB",
            "VM_DefaultAndNamedArgs,DefaultJob,.NET 10.0,3,8,54.248 ms,3.667 ms,9834.78 KB");
    }

    private static string BuildCsvWithNa()
    {
        return string.Join(
            Environment.NewLine,
            "Method,Job,Runtime,WarmupCount,IterationCount,Mean,StdDev,Allocated",
            "VM_ArithmeticLoop,DefaultJob,.NET 10.0,3,8,30.000 ms,1.100 ms,3500 KB",
            "VM_DefaultAndNamedArgs,DefaultJob,.NET 10.0,3,8,NA,NA,NA");
    }

    public void Dispose()
    {
        if (!Directory.Exists(_tempRoot))
        {
            return;
        }

        try
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // 忽略清理失败，避免影响测试结果
        }
    }
}
