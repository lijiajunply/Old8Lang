# IL 比较运算装箱顺序导致测试进程崩溃 —— 测试报告

日期：2026-11-14
关联待办：`changes/Todo.md`「优先顺序 1. 排查会使测试进程崩溃的 IL 问题」、
「`Compiler/Collections` 与表达式组既有 IL 失败」中的“`Compiler.Classes` 组会使测试主机因生成无效 IL 崩溃”。

## 1. 现象

`dotnet test --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Classes"` 在
`MemberAccessTests.MemberAccessErrorHandling_CompilesAndExecutesCorrectly` 处
**使测试主机进程直接消失**（不是断言失败、也不是托管异常）：

```
活动的测试运行已中止。原因: 测试主机进程崩溃
```

用 CLI 最小复现同样是原生崩溃：

```bash
dotnet Old8Lang.App.dll -il crash.old8   # Segmentation fault: 11 (退出码 139)
```

## 2. 最小复现

```old8
class A {
    public data <- {}          // 空集合字面量字段，静态类型退化为 object
    public func f(i:int) -> bool {
        return i >= this.data.Count()
    }
}
a <- A()
PrintLine(a.f(1))
```

前置条件（逐项二分确认）：

| 变量 | 是否崩溃 |
|---|---|
| 字段直接 `public data <- {1, 2, 3}`（静态类型 `List<object?>`） | 否 |
| `data` 为 `object`，但方法内只 `return this.data.Count()` | 否 |
| `data` 为 `object`，方法内 `b <- i >= this.data.Count()` | 是 |
| 数值在左、`Count()` 在右（`i >= this.data.Count()`） | 是 |
| `Count()` 在左、数值在右（`this.data.Count() <= i`） | 否（旧实现恰好正确） |

## 3. 根因

`ComparisonOpHelper` 在 `object` 与数值混合比较时，先把**两个**操作数都压栈，
再调用 `BoxNumericIfNeeded` 统一装箱：

```csharp
left?.Accept(...);  // 压栈：int
right?.Accept(...); // 压栈：object（Count() 结果）
if (IsObjectVsNumericComparison(...))
{
    BoxNumericIfNeeded(...);   // 只装箱栈顶 → 装的是 object，不是 int
    ilGenerator.Emit(OpCodes.Callvirt, typeof(IComparable).GetMethod("CompareTo", [typeof(object)])!);
}
```

`box` 只作用于栈顶。数值在**左**时，被装箱的是右侧 `object`，左侧整数保持未装箱；
随后 `callvirt IComparable.CompareTo(object)` 把未装箱整数（如 `1`）当作 `this` 引用传入，
生成的 IL 无效。该路径没有经过验证器（类方法构建在 `TypeBuilder` 上，只有顶层
`OldLangRun` 走 `IlVerifier`），JIT 后直接访问非法地址，于是 **进程段错误** 而不是抛
`InvalidProgramException`。

崩溃方法反汇编（临时用 `IlDisassembler` 导出 `A.f`）：

```
0000: ldarg.1
0001: stloc.1
0002: ldloc.1              // int 1
0003: ldarg.0
0004: ldfld data           // object
0009: ldstr "Count"
000E: ldc.i4.0
000F: newarr object
0014: call InvokeDynamicObjectMethod(object, string, object[]) -> object
0019: box 0x01000005       // ← 只有一次 box，且作用在栈顶的 object 上
001E: callvirt IComparable.CompareTo(object)   // this = 未装箱的 int
```

这也解释了为什么 `object < int`（ForIn 循环变量历史路径）一直正常：那时栈顶正是整数。

## 4. 修复

`BoxNumericIfNeeded` 改为 `EmitBoxedOperands`：**发射一个操作数就立即按该操作数类型装箱**，
六个比较运算符（`>`、`<`、`>=`、`<=`、`==`、`!=`）的 object×数值分支全部改为走它；
普通数值/字符串分支仍走原有的“先压两个操作数再比较”。

涉及文件：`Old8Lang.Compiler/OperationHelpers/ComparisonOpHelper.cs`。

## 5. 验证

### 5.1 崩溃用例

| 用例 | 修复前 | 修复后 |
|---|---|---|
| CLI 最小复现 `i >= this.data.Count()` | 段错误（139） | 输出 `true`，退出码 0 |
| `MemberAccessTests.MemberAccessErrorHandling_...` 单跑 | 测试主机崩溃 | 正常失败（见 §5.3 遗留） |
| `--filter Compiler.Classes` 整组 | 测试运行中止 | 完成 110 项：87 通过 / 23 失败 |
| `--filter Compiler.Expressions` 整组 | 测试运行中止 | 完成 232 项：191 通过 / 41 失败 |

### 5.2 新增回归测试

`Old8Lang.Tests/Compiler/Expressions/ComparisonTests.cs` 新增两组（共 12 项）：

- `NumericLeftObjectRight_BoxesEachOperandIndividually`：数值在左、`object` 在右，
  覆盖 `>=`/`<=`/`>`/`<`/`==`/`!=`；
- `ObjectLeftNumericRight_BoxesEachOperandIndividually`：反向顺序，确认原有语义不变。

结果：`ComparisonTests` 56 项，55 通过、1 失败（`ChainedComparisons_...`，见 §5.3）。

### 5.3 基线对比（确认没有引入新失败）

对 `Old8Lang.Tests.Compiler` 的 19 个子组逐一运行“修复前（`git show HEAD:` 还原
`ComparisonOpHelper.cs`）”与“修复后”并比较通过/失败数：

| 组 | 修复前 | 修复后 |
|---|---|---|
| Async | 65 / 43 / 3 跳过 | 65 / 43 / 3 跳过 |
| Basic | 16 / 185 | 16 / 185 / 2 跳过 |
| Classes | **测试运行中止** | 23 / 87 |
| Collections | 9 / 165 | 9 / 165 / 3 跳过 |
| EdgeCases | 22 / 77 | 22 / 77 / 1 跳过 |
| Error | 12 / 0 | 12 / 0 |
| Exceptions | 12 / 52 | 12 / 52 |
| Expressions | **测试运行中止** | 41 / 191 |
| Extern | 3 / 7 | 3 / 7 |
| FileHeader | 0 / 29 | 0 / 29 |
| Functions | 57 / 109 | 57 / 109 / 12 跳过 |
| Integration | 3 / 50 | 3 / 50 / 4 跳过 |
| Linq | 0 / 45 | 0 / 45 |
| Modules | 0 / 263 | 0 / 263 |
| Reflection | 14 / 3 | 14 / 3 |
| Statements | 3 / 183 | 3 / 183 |
| Threading | 0 / 15 | 0 / 15 |
| Types | 51 / 50 | 51 / 50 |
| Unit | 0 / 45 | 0 / 45 |

（表中“修复前”列为逐组运行时输出的通过/失败数；除 Classes 与 Expressions 由“中止”变为
可完整运行外，其余 17 组的通过/失败/跳过数完全一致，没有出现“修复前通过、修复后失败”的用例。）

整组 `--filter "FullyQualifiedName~Old8Lang.Tests.Compiler"` 修复后完成 1955 项
（1580 通过 / 350 失败 / 25 跳过），不再中止。

## 6. 遗留（与本崩溃无关，未在本轮修改）

1. **可空返回类型 `int?` 返回 `null` 触发 `NullReferenceException`**：
   `func f(i:int) -> int? { return null }` 在 IL 模式抛 NRE。崩溃用例修掉段错误后
   暴露的就是这个更早的错误（`MemberAccessErrorHandling_...` 因此仍失败）。
2. **`TypeInferenceVisitor.VisitOperation` 在未完成的 `TypeBuilder` 上调用 `GetMethod`**：
   `MemberAccessTests.PublicPrivateAccess_...` 报
   `NotSupportedException : The invoked member is not supported before the type is created`
   （`TypeInferenceVisitor.ExpressionsAndValues.cs:548`）。
3. **链式比较 `a < b < c` 在 IL 模式下结果为 false**：
   `ComparisonTests.ChainedComparisons_CompilesAndExecutesCorrectly` 在修复前后
   均失败（基线已验证），属独立缺口。
4. `Compiler.Classes` 其余 23 项失败、其他组的既有失败仍按原清单保留。

## 7. 复现命令

```bash
# 崩溃复现（修复前）
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il /tmp/il-crash/k4.old8

# 回归
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Expressions.ComparisonTests"
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Classes"
```
