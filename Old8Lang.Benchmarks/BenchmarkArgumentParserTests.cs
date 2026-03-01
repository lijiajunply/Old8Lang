using Xunit;

namespace Old8Lang.Benchmarks;

public class BenchmarkArgumentParserTests
{
    [Theory]
    [InlineData(new string[] { }, BenchmarkCommand.DefaultParserBenchmark)]
    [InlineData(new[] { "--validate" }, BenchmarkCommand.Validate)]
    [InlineData(new[] { "--quick" }, BenchmarkCommand.QuickCompare)]
    [InlineData(new[] { "--vm" }, BenchmarkCommand.Vm)]
    [InlineData(new[] { "--vm-report" }, BenchmarkCommand.VmReport)]
    [InlineData(new[] { "--vm-extended" }, BenchmarkCommand.VmExtended)]
    [InlineData(new[] { "--vm-report-extended" }, BenchmarkCommand.VmReportExtended)]
    [InlineData(new[] { "--vm-quick" }, BenchmarkCommand.VmQuick)]
    [InlineData(new[] { "--vm-report-quick" }, BenchmarkCommand.VmReportQuick)]
    [InlineData(new[] { "--vm-nightly" }, BenchmarkCommand.VmNightly)]
    [InlineData(new[] { "--vm-report-nightly" }, BenchmarkCommand.VmReportNightly)]
    [InlineData(new[] { "--unknown" }, BenchmarkCommand.DefaultParserBenchmark)]
    public void Parse_ReturnsExpectedCommand(string[] args, BenchmarkCommand expected)
    {
        var actual = BenchmarkArgumentParser.Parse(args);

        Assert.Equal(expected, actual);
    }
}
