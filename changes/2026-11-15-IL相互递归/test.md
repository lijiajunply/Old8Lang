# IL 相互递归与未定义函数 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL 相互递归函数无法编译 / 未定义函数静默返回 null」。

## 1. 现象

```old8
func isEven(n:int) -> bool {
    if n == 0 { return true }
    return isOdd(n - 1)
}
func isOdd(n:int) -> bool {
    if n == 0 { return false }
    return isEven(n - 1)
}
PrintLine(isEven(6))
```

| 模式 | 结果 |
|---|---|
| 解释器 / 虚拟机 | `true` / `true` |
| IL（修复前） | `isOdd` 被当成未注册的类名，`ldnull` 后静默调用 / 或（上一轮加了编译错误后）`CompilerException: 函数或类 'isOdd' 未声明` |

同时 `result <- SomeMethod()` 在 IL 下**静默成功**（返回 null），解释器报
`NAME_ERROR 名称 'SomeMethod' 未定义`。

## 2. 根因

函数体按源码顺序编译：编译 `isEven` 时 `isOdd` 还没有对应委托，
`VisitInstance` 的兜底分支把未知名字当类实例化处理，最终
`ldnull` 返回（后来改为直接抛编译错误，两者都不对）。

## 3. 修复

新增 `IlRuntimeFunctions.InvokeGlobal(name, object[])`：运行期在
`VariateManager.ImportInfos` 里按名字 + 参数个数查找 `FuncLangValue` 并调用，
结果 `Unwrap` 回原生值；找不到抛 `NameError`。

`VisitInstance` 的未知名字兜底改为发射一次该调用（ldstr 名字 + object[] 实参），
返回 `object`。这样：

- 相互递归里后定义的函数在运行期已注册，可以正常调用；
- 真正不存在的函数得到 `NAME_ERROR`（与解释器一致），不再静默 null。

## 4. 验证

### 4.1 CLI 对比

| 表达式 | 解释器 | IL | 虚拟机 |
|---|---|---|---|
| `isEven(6)` / `isOdd(7)`（相互递归） | true / true | true / true | true / true |
| `SomeMethod()` | `NAME_ERROR` | `NAME_ERROR 名称 'SomeMethod' 未定义` | 同左 |

### 4.2 用例重写

`Compiler/Error/Compilation/ErrorCompilationTests.cs` 12 项按当前语义重写：

- 已合法的（完整注解、ToStr 返回 string、`a + b.Length`、相互递归、缺 return）
  改为断言编译+执行成功；
- 名字解析错误（未声明变量、`if a > 0`、`if True`）断言编译期 `CompilerException`；
- 运行期错误（除零、越界、未定义函数）断言执行阶段抛对应语言错误；
- `var` 关键字断言 `Build` 阶段 `SyntaxError`。

`Compiler.Error` 12 → 0 失败。

### 4.3 回归面

整组对比见 `changes/Todo.md`；`Functions` 等组未新增失败。

## 5. 遗留

运行期按名查找只覆盖同步 `FuncLangValue`；异步函数 / 泛型函数的动态按名调用仍走原路径。
