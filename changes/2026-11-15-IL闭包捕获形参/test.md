# IL 模式 lambda 无法捕获函数形参 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL 模式 lambda 的闭包函数值支持」相关缺口。

## 1. 现象

```old8
func makeAdder(n:int) -> function {
    return (x:int) -> x + n
}
add5 <- makeAdder(5)
PrintLine(add5(10))
```

| 模式 | 结果 |
|---|---|
| `-f` | `15` |
| `-il`（修复前） | `CompilerException: 变量 'n' 未声明` |
| `-vm` | `15` |

同类失败：`ClosureWithHigherOrderFunction`、`ClosureReturningClosure`。
另外 lambda 的参数注解只允许固定白名单，`(input:array)` 直接报语法错误
（`不支持的类型注解: array`）。

## 2. 根因

1. **形参不在捕获列表里**：`GetCapturedLocalValues` 只遍历 `LocalVar`（局部变量），
   而函数形参登记在 `ArgumentIndices` 里、没有 `LocalBuilder`，因此闭包环境数组里没有形参，
   lambda 体内引用形参就报「变量未声明」。
2. **嵌套闭包不传递外层捕获**：内层 lambda 需要引用外层闭包已捕获的变量时，
   捕获列表同样只从 `LocalVar` 收集，取不到外层 `CapturedLocals`。
3. **lambda 注解白名单过窄**：`ParseLambdaParameter` 只认
   `int/double/string/bool/char/void/list/dict/any`，`array`、`function` 等注解被拒。

## 3. 修复

- `GetCapturedLocalValues`：追加 `ArgumentIndices` 中的形参（类型取 `LocalVarTypes`）
  与外层 `CapturedLocals` 中的变量，登记为「无 `LocalBuilder` 的捕获槽位」；
- `EmitLambdaClosure`：无 `LocalBuilder` 时，先按形参索引发 `ldarg`，
  再按外层捕获索引发 `ldarg.0 / ldelem.ref` 取值，最后统一装箱后写入闭包环境数组；
- 解析器白名单补上 `array`、`dictionary`、`object`、`function`
  （与 IL/解释器已支持的注解保持一致），错误提示同步更新。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `(x:int) -> x + n`（捕获形参 `n`） | 变量 'n' 未声明 | `15`，三模式一致 |
| `(input:array) -> ...` | 语法错误「不支持的类型注解」 | 可以解析（后续行为见 §5） |
| `ClosureTests.BasicClosure_...` | 失败 | 通过 |
| `ClosureTests.ClosureWithHigherOrderFunction_...` | 失败 | 通过 |
| `Compiler.Functions` 组 | 56 失败 / 110 通过 | **54 失败 / 112 通过** |

其余 18 个 `Compiler` 子组逐组一致（Async 43/65、Basic 188/13、Classes 95/15、
Collections 168/6、EdgeCases 77/22、Error 0/12、Exceptions 54/10、Expressions 197/35、
Extern 7/3、FileHeader 29/0、Integration 50/3、Linq 45/0、Modules 263/0、Reflection 17/0、
Statements 186/0、Threading 15/0、Types 86/15、Unit 45/0）；
非 `Compiler` 组全部通过（Interpreter 1967、VirtualMachine 1083、Parser 889、Libraries 95）。

## 5. 遗留

- `ClosureReturningClosure`（形如 `return () -> { return (x:int) -> x * factor }`）仍返回 `null`：
  内层 lambda 的捕获列表已补上外层捕获变量，但「外层 lambda 调用后返回函数值」这条链路仍未贯通。
- `ClosureWithArrayOperations` 依赖 `input.Length` 属性形式，解释器本身不支持（`NAME_ERROR`），
  需要先统一 `.Length` 属性的语义。

## 6. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il closure_param.old8
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Functions.ClosureTests"
```
