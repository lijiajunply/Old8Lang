# IL 模式 try/catch 赋值把局部变量槽位换掉，丢失成功分支的值 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1. …再核实其他 IL 结果错误和跨模式差异」。

## 1. 现象

```old8
func f(x:int) -> void {
    if x < 0 { throw "neg" }
}
result1 <- ""
try {
    f(1)
    result1 <- "valid"
} catch (e) {
    result1 <- "caught"
}
PrintLine("[" + result1 + "]")   # -f: [valid] / -il: []
```

| 模式 | 输出 |
|---|---|
| `-f` | `[valid]` |
| `-il`（修复前） | `[]` |
| `-vm` | `[valid]` |

失败用例：`Compiler/Exceptions/ThrowTests.ThrowInFunction_CompilesAndExecutesCorrectly`。

**关键点**：哪怕 catch 分支根本不执行，只要它在源码里出现过，成功分支写入的值就会丢。

## 2. 根因

`EmitDefaultSetValueToIl` / `EmitOperationSetValueToIl` 在「已有局部变量的类型与新值类型
不一致」时，会**删掉旧槽位、按新类型重新声明一个槽位**：

```csharp
if (localVar.LocalType != valueType)
{
    local.RemoveLocalVar(idName);
    localVar = ilGenerator.DeclareLocal(valueType);
    local.AddLocalVar(idName, localVar);
}
```

catch 分支把异常（`ExceptionWrapper`）赋给 `result1`（`string`）时，这条语句在**编译期**
就把 `result1` 重绑定成 `ExceptionWrapper` 槽位。成功分支写入的是那个旧的 string 槽位，
而之后的读取用的是新槽位，于是读到未初始化的 `null`。

## 3. 修复

1. `CatchAssignmentScanner`：扫描语句树，找出**在 try 块和 catch 块里都被赋值**的变量名；
   只在这些变量上放宽类型，避免影响「只写 catch」的常见写法
   （`caughtError <- ""; catch { caughtError <- e }`）——那类变量没有需要保留的成功分支值。
2. `LocalManager.CatchAssignedVariables` 保存这份名单，在顶层脚本、异步入口、具名函数、
   类方法四处编译前填充。
3. 变量首次声明时，若在名单里且初值是引用类型，改用 `object` 槽位；
4. `EmitDefaultSetValueToIl` / `EmitOperationSetValueToIl`：目标是 `object` 槽位时
   装箱后直接写入，不再替换槽位。
5. `ComparisonOpHelper`：相等比较只要任一侧静态类型是 `object` 就走
   `object.Equals`（此前只有 `object` 与非数值混用才走，`object` 与字符串比较是
   `ceq` 引用比较）。装箱也从「只装数值」放宽到「装所有值类型」。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `try { result1 <- "valid" } catch { result1 <- "caught" }` | `[]` | `[valid]`，与解释器一致 |
| 两次 try 分别写 r1 / r2（catch 赋异常） | `r1` 为空 | 与解释器一致 |
| `"success" == result`（result 提升为 object 槽位） | `false` | `true` |
| `x <- 1; x <- 2.5`（值类型动态改宽度） | `2.5` | `2.5`（仍走重绑定，未受影响） |
| `ThrowInFunction_CompilesAndExecutesCorrectly` | 失败 | 通过 |

组级基线对比（19 个 `Compiler` 子组逐组）：

| 组 | 修复前 | 修复后 |
|---|---|---|
| Exceptions | 11 失败 / 53 通过 | **10 失败 / 54 通过** |
| 其余 18 组 | — | 与修复前逐组一致（Async 43/65、Basic 185/16、Classes 95/15、Collections 168/6、EdgeCases 77/22、Error 0/12、Expressions 197/35、Extern 7/3、FileHeader 29/0、Functions 110/56、Integration 50/3、Linq 45/0、Modules 263/0、Reflection 3/14、Statements 183/3、Threading 15/0、Types 50/51、Unit 45/0） |

非 `Compiler` 组全部通过：Interpreter 1967、VirtualMachine 1083、Parser 889、
Libraries 95、LanguageServer、TypeSystem 65、Architecture 73、InstanceMethods 33。

## 5. 验证过程中确认的两点（未在本轮修改）

1. **值类型变量在 catch 里被赋异常仍会丢值**：`x <- 1; try { } catch (e) { x <- e }`
   之后 `x` 读到 `null`（`-f` 为 `1`）。当前只在引用类型初值时提升为 object 槽位；
   值类型槽位保留原有的「按新类型重绑定」行为，因为 `x <- 1; x <- 2.5` 这类动态改数值宽度
   依赖重绑定。
2. **`(-1.0).Sqrt()` 在 IL 返回 NaN 而不抛错**：解释器根本没有 `.Sqrt()` 方法
   （`ATTRIBUTE_ERROR`），文档只写了 `Math.Sqrt(value)`（.NET 语义即 NaN）。
   `EdgeCases/UnexpectedInputsTests.NegativeSquareRoot` 断言会抛异常，
   基线能通过是因为 `caughtError != ""` 走了字符串引用比较（两个不同的 `""` 实例判不等）。
   把相等比较修成按值比较后该用例会失败，因此本轮没有把「只写 catch」的变量提升为 object，
   该用例维持既有结果。此差异已记入 `changes/Todo.md`。

## 6. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il try_assign.old8
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Exceptions.ThrowTests"
```
