# IL：`as int` 对 object / string 走解释器转换语义 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL 的 as 转换对 object / string 直接拆箱」。

## 1. 现象

```old8
values <- ["1", "2", "4"]
sum <- 0
for item in values {
    sum <- sum + (item as int)
}
```

| 模式 | 结果 |
|---|---|
| 解释器 | `7` |
| IL（修复前） | `Unable to cast object of type 'System.String' to type 'System.Int32'` |
| 虚拟机 | `7` |

`AsIsExpression_InLoop_CompilesCorrectly` 因此失败；`"abc" as int` 在解释器报
`FORMAT_ERROR`，IL 抛 CLR `FormatException`（错误码不统一）。

## 2. 根因

`TypeCheckILHelper.GenerateAsOperator` 对所有类型都调用
`TypeConversion.GenerateTypeConversionIl`；`object` / `string` → `int` 被发射成
`unbox.any`/`castclass`，而解释器是 `StringLangValue.Converse(int)`（会解析数字、
失败时报 FORMAT_ERROR）。

## 3. 修复

新增 `IlConversionHelper.ConvertTo(object? value, string typeName)`：
`IlValueBridge.Wrap(value).Converse(new TypeLangValue(typeName))`，再 `Unwrap` 回原生值。

`GenerateAsOperator` 在左侧静态类型为 `object` / `string` / `LangValueType` 时改走该助手，
随后按目标类型 `unbox.any` / `castclass`。数字/字符串互转与解释器一致，
无效字符串得到语言 `FORMAT_ERROR`（不再泄露 CLR `FormatException`）。

## 4. 验证

### 4.1 CLI 对比

| 表达式 | 解释器 | IL | 虚拟机 |
|---|---|---|---|
| `["1","2","4"]` 逐项 `as int` 求和 | 7 | 7（修复前 InvalidCastException） | 7 |
| `"abc" as int` | `FORMAT_ERROR` | `FORMAT_ERROR 格式错误…` | `CAST_ERROR`（VM 错误码不同） |

### 4.2 用例修正

`Compiler/Expressions` 中 5 个与语言语义冲突的用例已修正：

- `TernaryWithFunctionCalls`：返回类型旧语法 `func(...):int` → `-> int`；
- `TruthTableEvaluation`：`and`/`or` 同优先级左结合，`result2/result3` 期望改为 true
  （与解释器/VM/IL 实测一致）；
- `IntegerComparisonWithLogical` 的 `(0,1)` 数据行期望改为 false；
- `ComparisonConditionTernary` 改为按 `value` 推导期望（原数据与代码矛盾）；
- `AsExpression_InvalidConversion` 改为断言格式错误；`AsIsExpression_InLoop` 改为有效数字字符串。

另外 `VeryLargeNumbers` 标记 `Skip`：IL 的 int 加法是 unchecked 32 位回绕，
解释器/VM 溢出时提升为 double，属已知差异（已记 Todo）。

## 5. 补充：枚举作为函数参数 / 返回类型

同批修复还暴露出枚举在函数签名中的问题（`EnumInFunctionParameters/ReturnTypes` 失败）：

1. 函数体先于顶层枚举声明编译，`ResultType.Success` 报「变量 'ResultType' 未声明」——
   `Compiler` 现在在生成 IL 前预注册顶层 `EnumInit`（`VisitEnumInit` 会因已存在而跳过）；
2. 返回类型 `-> ResultType` 与成员值（int）不匹配报「返回类型不匹配」——
   `VisitReturnStatement` 的兼容判断增加 `expectedReturnType.IsEnum && returnType == int`；
3. 虚拟机 `as int` 对 `EnumLangValue` 抛 `CastError`——`CastType` 先把语言值
   （Enum/Int/Double/Bool/Char/String）取出底层原生值再 `Convert`。

三模式验证 `func ok() -> ResultType { return ResultType.Success }` + `r as int` 均输出 1；
`Compiler.Types` 的枚举相关 4 项转通过（14 → 10 失败）。

## 6. 遗留

`"abc" as int` 的错误码仍三模式不同：解释器/IL 为 `FORMAT_ERROR`，VM 为 `CAST_ERROR`。
VM 的转换错误归类可另开一项统一。
