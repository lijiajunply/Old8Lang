# IL 的 match：解构绑定污染外层作用域、guard 读不到外层变量 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL 模式 match 高级模式」的后续修复。

## 1. 现象

```old8
x <- 100
point <- (5, 10)
result <- match point {
    case (x, y) -> x + y
}
final_x <- x       // 期望 100
```

```old8
threshold <- 50
value:int <- 75
result <- match value {
    case x:int if x > threshold -> "Above threshold"
    case x:int if x < threshold -> "Below threshold"
    case _ -> "At threshold"
}
```

| 用例 | `-f` | `-il`（修复前） |
|---|---|---|
| 解构后读外层 `x` | `100` | `x` 已丢失（绑定变量覆盖了外层槽位后又被移除） |
| guard 引用 `threshold` | `Above threshold` | 走不到该分支（变量未定义） |

失败用例：`Match_TupleDeconstruction_VariablesDoNotLeakScope`、`Match_Guard_WithVariableInCondition`。

## 2. 根因

1. 绑定变量直接 `local.AddLocalVar(名字, 新槽位)`，同名外层变量被覆盖；分支结束再
   `RemoveLocalVar`，外层变量连名字都没了。
2. guard 条件由解释器在**子作用域**里求值，该作用域只放了绑定变量，
   `threshold` 这类外层变量不在其中，解释器解析不到。

## 3. 修复

- 绑定前记录被遮蔽的外层槽位与类型，分支结束后恢复（没有同名变量才移除）；
- match 发射前把当前**局部变量**打包成 `names[]` + `values[]`（值类型装箱）传给
  `IlMatchRegistry.TryMatch`，运行期在求值 guard 前把它们塞进子作用域，
  求值完再 `RemoveChildren`。

> 打包时只收局部变量，不收函数形参：形参在不同编译上下文里的 `ldarg` 下标不一致
> （普通函数从 0 开始，类方法与 lambda 的 0 号分别是 `this` / 闭包环境数组），
> 按 `ArgumentIndices` 盲发 `ldarg` 会生成无效 IL——中途版本正是这样让 13 个
> 函数内 match 用例回归为 `invalid program`，已改回只打包局部变量。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| 解构绑定后外层 `x`/`y` | 丢失 | `100` / `200`（与 `-f` 一致） |
| guard 引用外层 `threshold` | 分支走不到 | `Above threshold` |
| 函数内 `match v { case 1 -> ... }` | — | 正常（回归验证） |
| `Compiler.Expressions` 组 | 11 失败 / 221 通过 | **9 失败 / 223 通过** |

其余 18 个 `Compiler` 子组与非 `Compiler` 组逐组不变（Async 43/65、Basic 188/13、
Classes 96/14、Collections 168/6、EdgeCases 77/22、Error 0/12、Exceptions 55/9、
Extern 7/3、FileHeader 29/0、Functions 113/53、Integration 50/3、Linq 45/0、
Modules 263/0、Reflection 17/0、Statements 186/0、Threading 15/0、Types 86/15、
Unit 45/0；Interpreter 1967、VirtualMachine 1083、Parser 889、Libraries 95、TypeSystem 65）。

## 5. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il match_scope.old8
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Expressions.MatchExpressionEnhancedTests"
```
