# IL 可空值类型返回 `null` 触发 NullReferenceException —— 测试报告

日期：2026-11-14
关联待办：`changes/Todo.md`「IL 可空返回类型 `int?` 返回 `null` 触发 `NullReferenceException`」。

## 1. 现象

```old8
class A {
    public func f(x:int) -> int? {
        if x < 0 {
            return null
        }
        return x
    }
}
a <- A()
PrintLine(a.f(1))
PrintLine(a.f(-1))
```

| 模式 | 结果 |
|---|---|
| `-f` | `1` / `Null` |
| `-il` | `1` / `RUNTIME_ERROR: Object reference not set to an instance of an object` |
| `-vm` | `1` / `null` |

失败用例：`Compiler/Classes/MemberAccessTests.MemberAccessErrorHandling_CompilesAndExecutesCorrectly`
（`getSafeData` 返回 `int?`，越界时 `return null`），三模式里只有 IL 报错。

## 2. 根因

`TypeInferenceVisitor` 对类型注解的处理明确写着「可空类型被视为其基础类型」：

```csharp
if (typeName.EndsWith('?'))
{
    typeName = typeName[..^1].Trim();
}
```

于是 `-> int?` 在 IL 里就是 `int`。`VisitReturnStatement` 看到返回表达式推断为 `object`
（`null` 字面量）、期望类型是值类型时执行：

```csharp
ilGenerator.Emit(OpCodes.Unbox_Any, expectedReturnType);   // unbox.any int32
```

而 `null` 压栈的是空引用，`unbox.any` 对空引用直接抛 NRE——这是空引用拆箱的固有行为，
不是丢失的检查。

## 3. 修复

新增 `Old8Lang.Runtime/Execution/LegacyNodeExtensions/Declarations/IlReturnTypeHelper.cs`：
把「单个可空值类型」的**返回类型**统一改用 `object` 承载。

- 具体值走装箱，`null` 就是空引用，与「可空」语义一致；
- 引用类型（`string?`）与联合类型（`int? | string?`）本来就能表示 `null`，不受影响；
- 只作用于函数返回类型，不影响参数、字段与局部变量的既有映射。

接入两处返回类型解析：

- `ClassInitRuntimeExtensions.DefineMethodInternal`（类方法）；
- `CompilerVisitor.VisitFuncInit`（顶层函数）。

调用点无需额外处理：顶层函数的返回类型来自 `DelegateVar` 里 `DynamicMethod.ReturnType`，
类方法的返回类型来自 `Type.GetMethod(...).ReturnType`，两者都会自动看到 `object`。

## 4. 验证

### 4.1 定向

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `A.f(1)` / `A.f(-1)`（`int?` 返回具体值 / null） | `1` / NRE | `1` / `null`，与 `-vm` 一致 |
| `func f(b:bool) -> int? { if b { return 1 } return null }` | `1` / NRE | `1` / 空值分支正常 |
| `x:int? <- null; x == null` | 正常 | 正常（局部变量路径未改动） |
| `func f() -> string? { return null }` | 正常 | 正常（引用类型不受影响） |
| `MemberAccessTests.MemberAccessErrorHandling_...` | 失败 | 通过 |
| `MemberAccessTests` 整组 | 8 通过 / 1 失败 | **9 通过 / 0 失败** |

### 4.2 基线对比（`Old8Lang.Tests.Compiler` 19 个子组）

| 组 | 修复前（通过/失败） | 修复后（通过/失败） |
|---|---|---|
| Classes | 91 / 19 | **92 / 18** |
| 其余 18 组 | — | 与修复前逐组一致（Async 43/65、Basic 185/16、Collections 165/9、EdgeCases 77/22、Error 0/12、Exceptions 53/11、Expressions 191/41、Extern 7/3、FileHeader 29/0、Functions 109/57、Integration 50/3、Linq 45/0、Modules 263/0、Reflection 3/14、Statements 183/3、Types 50/51、Unit 45/0） |

`Compiler.Threading` 在一次逐组运行中出现过 1 项失败，单独连续复跑 3 次均为 15/15 通过，
属既有的并发时序抖动（`changes/Todo.md` 已记录同类条目），与本次改动无关。

## 5. 验证过程中确认的跨模式差异（未在本轮修改）

1. **`null` 的显示大小写**：`PrintLine(null)`、集合/字典里的 `null` 元素
   —— 解释器输出 `Null`，IL 与虚拟机输出 `null`；
   `null.ToStr()` 在解释器是 `Null`，而 `NullLangValue.Converse(String)` 又返回 `null`。
   文档 `Docs/CHANGELOG.md` / `Docs/MODE_SUPPORT.md` 里写的是 `Task(Completed: Null)`。
2. **`null.ToStr()`**：解释器返回 `Null`，IL 报
   `无法在 null 上调用方法 'ToStr'`，虚拟机报 `NULL_REFERENCE_ERROR: ToStr`。

两条都已记入 `changes/Todo.md`，等确定统一口径后再改。

## 6. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il nullable.old8
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Classes.MemberAccessTests"
```
