namespace Old8Lang.Benchmarks.Entry;

/// <summary>
/// 命令行参数解析器。
/// </summary>
internal static class BenchmarkArgumentParser
{
    public static BenchmarkCommand Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            return BenchmarkCommand.DefaultParserBenchmark;
        }

        return args[0] switch
        {
            "--validate" => BenchmarkCommand.Validate,
            "--quick" => BenchmarkCommand.QuickCompare,
            "--vm" => BenchmarkCommand.Vm,
            "--vm-report" => BenchmarkCommand.VmReport,
            "--vm-extended" => BenchmarkCommand.VmExtended,
            "--vm-report-extended" => BenchmarkCommand.VmReportExtended,
            "--vm-quick" => BenchmarkCommand.VmQuick,
            "--vm-report-quick" => BenchmarkCommand.VmReportQuick,
            "--vm-nightly" => BenchmarkCommand.VmNightly,
            "--vm-report-nightly" => BenchmarkCommand.VmReportNightly,
            _ => BenchmarkCommand.DefaultParserBenchmark
        };
    }
}
