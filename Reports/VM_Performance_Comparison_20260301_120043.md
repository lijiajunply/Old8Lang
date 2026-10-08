# VM 性能对比报告

- 生成时间: 2026-03-01 12:00:43 +0800
- Baseline: `Reports/VM_Performance_Report_20260301_115322.txt`
- Current: `Reports/VM_Performance_Report_20260301_120003.txt`

## 结果总览

| 场景 | Baseline Avg(ms) | Current Avg(ms) | Avg 变化(ms) | Avg 提升(%) | Baseline P95(ms) | Current P95(ms) | P95 提升(%) | Baseline 分配(bytes) | Current 分配(bytes) | 分配变化(%) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| ArithmeticLoop | 30.508 | 33.995 | 3.487 | -11.43% | 33.145 | 48.678 | -46.86% | 3603560 | 3603560 | 0.00% |
| DenseFunctionCall | 115.061 | 115.804 | 0.743 | -0.65% | 116.368 | 118.543 | -1.87% | 8403608 | 8403608 | 0.00% |
| DefaultAndNamedArgs | 54.248 | 50.392 | -3.856 | 7.11% | 62.429 | 54.011 | 13.48% | 9834776 | 9834776 | 0.00% |

## 验收阈值检查

- ArithmeticLoop 吞吐提升 >= 10%: -11.43% => FAIL
- DenseFunctionCall 吞吐提升 >= 20%: -0.65% => FAIL
- DenseFunctionCall 分配下降 >= 15%: 0.00% => FAIL

## 备注

- 吞吐提升按时间下降比例计算: (Baseline - Current) / Baseline。
- 分配变化为正表示分配下降，为负表示分配上升。
