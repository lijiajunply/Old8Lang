# IL 类型错误运行期化（含无效 IL）—— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL 类型不匹配：无效 IL / 编译期拒绝 / 静默写入」。

## 1. 现象

| 代码 | 解释器 | IL（修复前） | VM |
|---|---|---|---|
| `add(5, "hello")`（`add(a:int,b:int)`） | 运行期 `TYPE_ERROR 参数 'b' 类型不匹配` | **无效 IL**，编译期 `IL代码验证失败` | 运行期 TYPE_ERROR |
| `func get() -> int { return "hello" }` | 运行期 TYPE_ERROR | 编译期 `返回类型不匹配` | 运行期 TYPE_ERROR |
| `x:int <- "hello"` | 运行期 `TYPE_ERROR 变量 'x' 赋值类型不匹配` | 静默接受并打印 hello | 运行期 TYPE_ERROR |
| `"hello" * 2` | `hellohello`（解释器允许字符串重复） | 打印垃圾整数 `1560537200` | 运行期 `无法对类型 String 和 Int 执行乘法` |
| `[1,2,3] * 2` | 运行期 INVALID_OPERATION | 打印垃圾整数 `-1123519184` | 运行期 INVALID_OPERATION |

## 2. 根因

1. 函数调用点直接把实参按原类型 `call` 进编译后的委托：`int add(int,int)` 收到 string，
   栈类型不匹配，JIT 报 invalid program（会让整个编译流程失败）；
2. 返回类型不匹配在编译期用 `ReportError` 拒绝，与解释器/VM 的运行期 TYPE_ERROR 不一致；
3. 带注解的局部赋值没有运行期类型检查，string 被写进 int 槽位；
4. `NumericBinaryOpHelper.GenerateMultiplication` 对非数值类型仍发 `mul`，把引用当整数乘，
   产生垃圾结果而不是错误。

## 3. 修复

- 新增 `IlTypeErrors`：`ThrowParameterMismatch` / `ThrowReturnMismatch` /
  `ThrowAssignmentMismatch` / `ThrowInvalidOperation`，统一抛 `TypeError`（TYPE_ERROR）
  或 `InvalidOperationError`，消息与解释器/VM 对齐。
- `VisitInstance` 在装载实参**之前**做静态类型预检；不兼容时发射
  `ThrowParameterMismatch` + 返回值占位，得到运行期 TYPE_ERROR 而不是无效 IL
  （允许数值/字符隐式转换、值类型→string、object 互通）。
- `VisitReturnStatement`：不兼容的返回类型不再 `ReportError`，改为运行期 TypeError。
- `VisitSetStatement`：带注解的赋值若静态类型不兼容（非数值转换），运行期 TypeError，
  并声明占位局部变量保证后续读取可编译。
- `GenerateMultiplication`：非数值操作数发射 `ThrowInvalidOperation`（String/Array…），
  不再发 `mul`；`object` 仍走旧的动态拆箱路径（否则未注解参数的算术会误报）。
- 枚举成员在 IL 里是 int32：参数预检允许 enum ↔ int；空列表/空字典字面量不参与
  注解赋值检查（由后面的专门分支生成目标容器类型）。

## 4. 用例迁移

- `TypeSystemTests` 两个“编译错误”用例改为 `Assert.ThrowsAny<Old8Exception>`（运行期 TypeError）；
- `TypeMismatchAssignment` 改为带注解的初始赋值不匹配（动态重赋值在解释器里是允许的）；
- `UndefinedVariable` / `UndefinedClassMember` 改为断言编译期错误（IL 要求先声明，
  解释器/VM 为运行期错误，属模式差异）；`MissingTypeAnnotation` 改为 CompilerException；
- `FunctionReturnTypeMismatch_ThrowsCompileException` 改为“编译成功、运行期 TypeError 且消息含
  返回值类型不匹配”。

## 5. 验证

- CLI：`add(5,"hello")`、`get()` 返回 string、`x:int <- "hello"`、`"hello"*2`、`[1,2,3]*2`
  在 IL 下都给出与 VM 一致的运行期错误，不再出现无效 IL 或垃圾整数；
- `Compiler.Basic` 11 → **0**（201 通过 / 2 跳过）；
- 整组回归见 `changes/Todo.md`。
