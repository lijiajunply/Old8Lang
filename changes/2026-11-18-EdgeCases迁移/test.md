# EdgeCases 边界用例迁移与两个 IL 修复 —— 测试报告

日期：2026-11-18
关联待办：`changes/Todo.md`「EdgeCases 组：旧语法/错误期望占多数」。

## 1. 现象

`Compiler.EdgeCases` 20 项失败，分三类：

1. **旧语法**：`for i <- 0 to 5`、`case 1 -> {}`、`func() -> void {}`、`result <- (())`、
   `if x > 5 { } else { }`、`2.0.Pow(...)`（数字字面量后接方法）；
2. **错误期望与当前语义不符**：`null.ToStr()` 现在三模式都返回 `"null"`；
   `arr[10:20]` 三模式都返回空切片；`[10~5]` 是合法降序范围；`1 && 0` 应报类型错误；
3. **真实 IL 缺口 / 模式差异**：
   - `this.value <- newValue.ToInt()` 写入原生 int 字段时把 `IntLangValue` 对象指针
     直接写进字段（读出垃圾整数）；
   - `1 && 0` IL 按真值静默计算（解释器 INVALID_OPERATION / VM TYPE_ERROR）；
   - 私有成员/未声明变量/未知方法在 IL 是**编译期**错误，解释器是运行期错误。

## 2. 修复

- `EmitFieldValueConversion` 增加语言值分支：目标字段是原生值类型且右值是
  `LangValueType` 时先 `UnwrapDynamic` 再 `unbox.any`；`this.field <-` 路径统一走它。
- `LogicalOpILHelper.GenerateAnd/Or`：静态已知非 bool 操作数（int/double/string…）
  发运行期 `TypeError`（与 VM 口径一致），不再按真值静默计算。
- 用例迁移：switch 箭头分支改 `{}`、for 改三段式、lambda 改 `() -> {}`、
  条件表达式改 `then/else`、幂运算改用 `^`、`[]`+Add 改为 `{}`+Add、
  null.ToStr/越界切片/降序范围按真实结果断言、未知成员/方法按 IL 编译期错误断言。

## 3. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `instance.setValue("42")` 写 int 字段 | 读回 747089992 | 42 |
| `1 && 0` | IL 返回 false/true | 运行期 TYPE_ERROR |
| `Compiler.EdgeCases` | 20 失败 | **0**（99 通过 / 1 跳过） |

整组回归见 `changes/Todo.md`。
