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

## 回归门禁与噪声容忍度

`--vm-report-quick` / `--vm-report-nightly` 在生成报告后做回归门禁：出现 `FAIL` 场景时返回非零退出码。
判定使用「基线报告的均值」（`OLD8LANG_VM_REPORT_BASELINE` 指定的文件，否则取 Reports 目录中最新同名报告，
最后回退 `Reports/Baselines/*.json`），并按场景分类取固定阈值：

| 分类 | 阈值 |
|------|------|
| Throughput / Latency / LargeFile | 8% |
| Allocation | 5% |
| Edge / Core | 10% |
| Concurrency | 12% |

同机连跑时并发场景的中位数本身就会波动（实测可达 12% 量级），固定阈值容易把噪声判成回归。
因此当**基线和当前报告都带 `StdDevMs`** 时，额外计算一个「噪声容忍度」：

```text
NoiseFloor(%) = min(15, 2 × sqrt(cv_baseline² + cv_current²) × 100)
有效阈值      = 分类阈值 + NoiseFloor
```

其中 `cv = StdDevMs / MeanMs`，系数 2 约对应 95% 置信区间，上限 15 个百分点避免高方差场景完全失去门禁作用。
报告 Markdown/JSON 都有 `NoiseFloor(%)` / `NoiseFloorPercent` 字段；门禁失败摘要会同时打印
`Threshold=` 与 `NoiseFloor=`，便于区分「真回归」与「噪声」。

> 静态基线（`Reports/Baselines/vm_quick_baseline.json`）只记录均值，没有 `StdDevMs`，
> 此时 `NoiseFloor` 为 `N/A`，退化为固定阈值。CI 缓存的基线是完整报告，会启用噪声容忍度。
