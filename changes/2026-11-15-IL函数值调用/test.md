# IL 模式调用 object 静态类型的函数值静默返回 null —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL 模式 lambda 的闭包函数值支持」相关缺口。

## 1. 现象

```old8
func mk2() -> function {
    return () -> {
        return (x:int) -> x * 2
    }
}
f2 <- mk2()
m2 <- f2()
PrintLine(m2(5))    # -f: 10    -il: null
```

| 模式 | 结果 |
|---|---|
| `-f` | `10` |
| `-il`（修复前） | `null`（不报错，静默返回 null） |
| `-vm` | `10` |

失败用例：`ClosureTests.ClosureReturningClosure_...`（形如 `return () -> { return (x:int) -> x * factor }`）。

## 2. 根因（三处）

1. **函数值调用落到「按类名构造实例」兜底**：`VisitInstance` 只在名字是
   **委托类型**的局部变量时才走快路径（`TryEmitLocalDelegateCall`）；函数返回值、
   集合里取出的函数值静态类型是 `object`，匹配不到委托，也不在 `DelegateVar` 里，
   于是进入「当作类名构造」分支——找不到类就发 `ldnull` 直接返回，结果静默变成 null。
2. **嵌套闭包取外层捕获值时用 `castclass`**：环境数组是 `object[]`，
   捕获的 `int` 是装箱值，`castclass int` 得到垃圾位模式
   （`x * factor` 曾输出 `1195142656`）；值类型必须 `unbox.any`。
3. （上一轮已修）形参捕获与 lambda 注解白名单。

## 3. 修复

- `IlValueBridge.InvokeFunctionValue(object?, object?[])`：按运行期实际类型调用函数值
  （`Delegate` / `IlDelegateFuncLangValue`），非函数值报错而不是返回 null；
- `VisitInstance`：在「按类名构造实例」之前，若名字是**已存在的局部变量**，
  发射「加载该值 + 装箱实参数组 + `InvokeFunctionValue`」；
- `EmitLambdaClosure`：读取外层捕获值时，值类型走 `Unbox_Any`，引用类型才 `castclass`
  （`this` 分支同样修正）。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| 嵌套闭包返回函数 `m2(5)` | `null` | `10` |
| 嵌套闭包捕获外层形参 `x * factor` | 垃圾整数 | `56`（与 `-f`、`-vm` 一致） |
| 单层捕获形参 `x + n` | 变量未声明（上一轮已修） | `15` |
| `ClosureTests.ClosureReturningClosure_...` | 失败 | 通过 |
| `Compiler.Functions` 组 | 54 失败 / 112 通过 | **53 失败 / 113 通过** |

其余 18 个 `Compiler` 子组逐组一致（Async 43/65、Basic 188/13、Classes 95/15、
Collections 168/6、EdgeCases 77/22、Error 0/12、Exceptions 54/10、Expressions 197/35、
Extern 7/3、FileHeader 29/0、Integration 50/3、Linq 45/0、Modules 263/0、
Reflection 17/0、Statements 186/0、Threading 15/0、Types 86/15、Unit 45/0）；
非 `Compiler` 组全部通过（Interpreter 1967、VirtualMachine 1083、Parser 889、Libraries 95）。

## 5. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il closure_value.old8
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Functions.ClosureTests"
```
