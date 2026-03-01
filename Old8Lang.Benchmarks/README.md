# Old8Lang.Benchmarks

`Old8Lang.Benchmarks` 用于运行 BenchmarkDotNet 基准、性能验证脚本与报告生成。

## 目录结构

- `Entry/`: 命令入口、参数解析与运行调度
- `Benchmarks/Parser/`: 词法、语法、类型推断与编译相关基准
- `Benchmarks/Interpreter/`: 解释器执行相关基准与基类
- `Benchmarks/VM/`: VM 基准套件、配置、基类与报告生成器
- `Benchmarks/Reflection/`: 反射性能对比基准
- `Benchmarks/Legacy/`: 历史基准（保留兼容）
- `Tools/`: 测试数据生成、性能验证、监控辅助工具
- `Tests/`: xUnit 测试（非 BenchmarkDotNet 基准）
- `TestData/`: 基准与验证使用的 `.old8` 输入文件

## 常用命令

```bash
# 默认：解析器基准
dotnet run --project Old8Lang.Benchmarks

# 验证脚本（非 BenchmarkDotNet）
dotnet run --project Old8Lang.Benchmarks -- --validate

# 反射快速对比
dotnet run --project Old8Lang.Benchmarks -- --quick

# VM 基准
dotnet run --project Old8Lang.Benchmarks -- --vm

# VM 基准 + 报告
dotnet run --project Old8Lang.Benchmarks -- --vm-report

# 扩展 VM 基准 + 报告
dotnet run --project Old8Lang.Benchmarks -- --vm-report-extended

# Quick VM 基准 + 报告
dotnet run --project Old8Lang.Benchmarks -- --vm-report-quick

# Nightly VM 基准 + 报告（回归 FAIL 时返回非零退出码）
dotnet run --project Old8Lang.Benchmarks -- --vm-report-nightly
```

## 输出目录

- BenchmarkDotNet 原始输出：`BenchmarkDotNet.Artifacts/`
- VM 性能报告：`Reports/`
