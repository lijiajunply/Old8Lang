# 强类型集合打印成 .NET 类型名 —— 测试报告

日期：2026-11-14
关联待办：`changes/Todo.md`「优先顺序 1. …再核实其他 IL 结果错误和跨模式差异」。

## 1. 现象

`Range()` 与范围表达式返回的是原生 `int[]`（强类型数组），打印时输出 .NET 类型名：

```old8
r <- [1>~<1]
PrintLine(r)
```

| 模式 | 输出 |
|---|---|
| `-f` | `[2, 1, 0]` |
| `-il` | `System.Int32[]` |
| `-vm` | `System.Int32[]` |

（`PrintLine([1, 2, 3])` 这种列表字面量是 `List<object>`，早已格式化正常，
所以问题只出在**元素类型不是 `object` 的强类型集合**上。）

## 2. 根因

`VMValueFormatter.ToDisplayString` 只匹配 `Dictionary<object, object?>`、`object?[]`、
`List<object?>`，`int[]` / `List<int>` 这类强类型集合落到
`value.ToString()`，于是把 `System.Int32[]` 打印给使用者。
`FormatValueForDisplay`（嵌套元素格式化）同样只认 `object?[]` / `List<object?>`。

此前 Todo 里「IL 直接打印原生列表输出 .NET 类型名」的修复只覆盖了 `object` 元素这一种表示。

## 3. 修复

`Old8Lang.Runtime/Runtime/VMValueFormatter.cs`：

- `ToDisplayString` 在原有 `List<object?>` 之后补充 `Array` 与
  `System.Collections.IList` 两个分支，按语言格式 `[a, b, c]` 递归格式化；
- `FormatValueForDisplay` 的递归条件同步加入 `Array` 与 `IList`，
  使嵌套的强类型集合也按同一口径展开。

`VMValueFormatter` 由 IL 的 `IlPrintHelper` 与虚拟机的 `PrintLine` / `ToStr` 共用，
因此两种模式一起修好。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `VMValueFormatter.ToDisplayString(int[])` | `System.Int32[]` | `[1, 2, 3]` |
| `VMValueFormatter.ToDisplayString(List<int>)` | `System.Collections.Generic.List`1[System.Int32]` | `[1, 2, 3]` |
| `VMValueFormatter.ToDisplayString(string[])` | `System.String[]` | `["a", "b"]`（与解释器的集合元素引号口径一致） |
| `PrintLine([1>~<1])` | `System.Int32[]` | `[2, 1, 0]`（与 `-f` 一致） |

`Range(...)` 在 IL 模式下原本是「返回空数组」的桩实现，本提交之后它会打印成 `[]`；
该缺口在下一个提交（`2026-11-14-IL-Range实现`）里补齐，两处一起才能让
`PrintLine(Range(1, 4, 1))` 三模式一致。

回归：`Compiler.Modules` 263/263、`VirtualMachine` 1083/1083、`Libraries` 95/95、
`InstanceMethods` 33/33 全通过；`Compiler.Collections` 仍为 167 通过 / 7 失败，
`Compiler.Functions` 109/57、`Compiler.Types` 50/51 与修复前一致。
`Interpreter` 组 1968 项中出现 1 项
`TaskAdvancedTests.Run_MultipleAsyncTasksExceptionHandling`，复跑即通过，
属 `changes/Todo.md` 已记录的 `VariateManager.CloneInternal` 并发抖动。

## 5. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il range_display.old8
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -vm range_display.old8
```
