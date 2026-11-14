# IL 模式 `Range()` 恒返回空数组 —— 测试报告

日期：2026-11-14
关联待办：`changes/Todo.md`「优先顺序 1. …再核实其他 IL 结果错误和跨模式差异」。

## 1. 现象

```old8
PrintLine(Range(1, 4, 1))
PrintLine(Len(Range(1, 4, 1)))
PrintLine(Range(5, 1, -1))
```

| 模式 | 输出 |
|---|---|
| `-f` | `[1, 2, 3]` / `3` / `[5, 4, 3, 2]` |
| `-il`（修复前） | `System.Int32[]` / `0` / `System.Int32[]` |
| `-vm` | `[1, 2, 3]` / `3` / `[5, 4, 3, 2]` |

IL 里的 `Range` 是明显的桩实现：

```csharp
protected override void GenerateIlInternal(...)
{
    // IL 模式暂不支持动态构建 Range，返回空数组
    ilGenerator.Emit(OpCodes.Ldc_I4_0);
    ilGenerator.Emit(OpCodes.Newarr, typeof(int));
}
```

因此 `Len(Range(...))` 恒为 0，任何用 `Range` 生成序列的 IL 程序结果都不对。

## 2. 修复

`Old8Lang.Runtime/GlobalFunctions/Implementations/UtilityFunctions.cs`：

- `GenerateIlInternal` 逐个压入三个参数（值类型先 `box`），再调用新增的
  `RangeFunction.BuildIlRange(object?, object?, object?)`；
- `BuildIlRange` 复用虚拟机与解释器同一个 `BuildRangeIntegers` 实现
  （`Convert.ToInt32` 取参 → 构建 → `ToArray()`），保证三个模式同一套边界与步长规则，
  包括 `step = 0` 时抛 `InvalidOperationError`。

## 3. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `Range(1, 4, 1)` | `[]`（打印 `System.Int32[]`） | `[1, 2, 3]` |
| `Len(Range(1, 4, 1))` | `0` | `3` |
| `Range(5, 1, -1)`（负步长） | `[]` | `[5, 4, 3, 2]` |
| `Range(0, 0, 1)`（空区间） | `[]` | `[]` |

三模式输出逐行一致。

回归：`Compiler.Collections` 167 通过 / 7 失败、`Compiler.Functions` 109/57、
`Compiler.Types` 50/51、`Compiler.Modules` 263/0，与修复前一致；
`VirtualMachine` 1083/1083、`Libraries` 95/95 全通过。

## 4. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il range.old8
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -vm range.old8
```
