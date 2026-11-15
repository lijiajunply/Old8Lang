# IL 模式 LINQ `where` 与非流式子句组合生成无效 IL —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1. 排查会使测试进程崩溃的 IL 问题」、
「IL LINQ 其他查询路径待复核」。

## 1. 现象

语料扫描的最后一个 `invalid program` 是 `TestFiles/CompilerTests/linq_join_compiler.old8`：

```old8
left <- {1, 2, 3, 4, 5}
right <- {2, 3, 4}
result <- from x in left join y in right on x == y where x > 2 select x
```

`-il` 下 JIT 报 `Common Language Runtime detected an invalid program`。
`orderby` 不在末尾时同样触发：

```old8
numbers <- {3, 1, 2}
result <- from x in numbers orderby x where x > 1 select x
```

而 `where ... orderby`（orderby 在末尾、走流式路径）一直正常，因此问题只出现在
**非流式查询路径**（`GenerateWhereClauseIL`）。

## 2. 根因

LINQ 有两条 IL 生成路径：`let/where + select` 走 `GenerateStreamingQuery`，
含 join / 非末尾 orderby 的查询走旧的手写循环 `GenerateWhereClauseIL`。
后者有两处会生成无效 IL：

### 2.1 条件比较直接发 `cgt`

非流式路径里范围变量是 `LangValueType` 局部变量。`x > 2` 编译时
`ComparisonOpHelper` 只识别 `AnyLangValue`（`LangValueType` 的子类），
不识别 `LangValueType` 本身，于是走普通数值分支：

```
ldloc x        // LangValueType 引用
ldc.i4.2       // int32
cgt            // 引用与 int32 比较 → 无效 IL
```

同时 `ConvertToLangValueType` 用 `GetConstructor([typeof(int)])` 构造语言值，
而 `IntLangValue` 的构造函数带可选的 `SourcePosition` 参数（两个参数），
查不到构造函数时 `Emit(Newobj, null)` 直接抛 `ArgumentNullException`。

### 2.2 条件值没有按类型装箱

`GenerateIsTruthy` 直接调用 `LinqCompilerHelper.IsTruthy(LangValueType?)`。
语言值比较返回的是原生 `bool`（`ComparisonOpHelper` 的返回值），
把它传给 `LangValueType` 形参同样生成无效 IL；`where true` 这类字面量条件必现。

## 3. 修复

1. `ComparisonOpHelper` 的六个比较分支把判断从 `AnyLangValue` 放宽到
   `typeof(LangValueType).IsAssignableFrom(leftType)`，语言值统一走
   `Greater/Less/GreaterEqual/LessEqual/Equal` 方法；
2. `ConvertToLangValueType` 改为「值类型先 box，再调用 `IlValueBridge.Wrap(object)`」，
   覆盖基础类型、集合、元组与原生对象，不再依赖不存在的单参数构造函数；
3. `GenerateWhereClauseIL` 入口先调用 `LinqCompilerHelper.ConvertToEnumerable`，
   让上游 join/orderby 的栈类型统一成 `IEnumerable`；
4. `GenerateIsTruthy` 增加条件类型参数：值类型先 `box`，改用
   `LinqStreamingHelper.IsTruthy(object?)`（运行期会 `Unwrap` 语言值）。

涉及文件：

- `Old8Lang.Compiler/OperationHelpers/ComparisonOpHelper.cs`
- `Old8Lang.Compiler/Linq/LinqExpression.Compiler.cs`

## 4. 验证

### 4.1 新增回归测试

`Old8Lang.Tests/Compiler/Linq/LinqJoinWhereTests.cs`（4 项）：

- `JoinThenWhere_FiltersByOuterVariable`：join 后 where 过滤外层变量，结果 `[3, 4]`；
- `WhereThenJoin_FiltersOuterSourceBeforeJoin`：where 在 join 前，结果 `[2, 3, 4]`；
- `JoinThenWhere_LiteralConditionCompiles`：`where true` 不再生成无效 IL；
- `OrderByThenWhere_AppliesFilter`：非末尾 orderby + where，结果 `[2, 3]`（此前不报错但不过滤）。

修复前 4 项全部失败（无效 IL / 过滤不生效），修复后 4 项全部通过。

### 4.2 CLI 对比

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `join ... where x > 2` | 无效 IL | `[3, 4]` |
| `where x > 1 join ...` | 无效 IL | `[2, 3, 4]` |
| `join ... where true` | 无效 IL | `[2, 3]` |
| `orderby x where x > 1` | 编译通过但不过滤 `[1,2,3]` | `[2, 3]` |
| `join ... select`（无 where） | 正常 | 正常 |
| `where ... orderby`（流式） | 正常 | 正常 |

### 4.3 语料扫描

`TestFiles/CompilerTests/` 157 个文件逐个 `-il` 运行：
**段错误 0、超时 0、invalid program 0**（本轮之前剩 1 个）。

### 4.4 回归面

`Compiler.Linq` 45 项全通过；`Compiler.Expressions`（9 失败）、`Types`（15 失败）、
`Classes`（14 失败）、`Collections`（6 失败）、`Basic`（13 失败）、`Functions`（53 失败）
均与修复前一致。

## 5. 遗留（同一文件暴露的语义问题，未在本轮修改）

1. **join 结果只保留外层元素**：`select y` / `where y > ...` 里的内层范围变量
   取到的是 join 循环结束后的最后一个值，而不是当前配对的内层元素。
   例如 `join ... where y > 2 select x` 在 IL 下是 `[2, 3, 4]`，解释器是 `[3, 4]`。
2. **Group Join（`join ... into g`）结果与解释器不一致**：
   `linq_join_compiler.old8` 测试 3 在解释器为 `[[1, 1], [2, 2, 2], [3]]`，
   IL 为 `[[3], [3], [3]]`。
3. `linq_join_compiler.old8` 现在能跑完，但上述两项使它仍不是三模式等价用例。
