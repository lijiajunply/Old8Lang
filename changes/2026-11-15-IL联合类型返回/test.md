# IL 模式联合类型返回值被收窄成第一个分支的类型 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1. …再核实其他 IL 结果错误和跨模式差异」。

## 1. 现象

```old8
func getValue(flag: bool) -> int | string {
    if flag {
        return 456
    } else {
        return "text"
    }
}
result <- getValue(true)
```

| 模式 | 结果 |
|---|---|
| `-f` | 正常 |
| `-il`（修复前） | `CompilerException: [IL 模式错误] 返回类型不匹配 期望返回类型: Int32 实际返回类型: String` |
| `-vm` | 正常 |

失败用例：`Compiler/Types/UnionTypesInterpreterTests.Run_UnionTypeFunctionReturnInt_...`、
`Run_UnionTypeFunctionReturnString_...`。

## 2. 根因

`int | string` 这类注解在 IL 的类型解析里没有对应 CLR 类型，最终返回 `object`；
但两条「函数返回类型」解析路径都有**回退**：注解解析成 `object` 时改用
`FuncLangValue.GetItemType`（按函数体里**第一个** `return` 推断类型）。
于是返回类型被收窄成 `int`，另一个分支 `return "text"` 就被判为类型不匹配。

## 3. 修复

`IlReturnTypeHelper.Resolve`（类方法与顶层函数共用的返回类型校正）新增一条：
注解含顶层 `|`（联合）或 `&`（交叉）时，返回类型固定为 `object`。
联合/交叉类型本来就没有单一 CLR 载体，`object` 承载与解释器的动态语义一致，
也与「可空值类型返回用 object 承载」的处理方式一致。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `-> int | string` 分别返回 `456` / `"text"` | 编译期类型不匹配 | 三模式均正常 |
| `Compiler.Types` 组 | 17 失败 / 84 通过 | **15 失败 / 86 通过** |

其余组与修复前一致：Basic 13/188、Classes 15/95、Expressions 35/197、Functions 56/110。

未修复的一项：`Run_NullableUnionTypeNullAssignment_...`（`a: int? | string? <- null` 之后
`a.ToStr()`）——这是已记录的 `null.ToStr()` 跨模式差异（解释器返回 `Null`，
IL/VM 报「无法在 null 上调用方法」），需要先定语义。

## 5. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il union_return.old8
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Types.UnionTypesInterpreterTests"
```
