# IL 模式类方法实参与同步生成器生成无效 IL —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1. 排查会使测试进程崩溃的 IL 问题」。

## 1. 现象

`TestFiles/CompilerTests/` 语料扫描剩余两类「invalid program」：

- `15_class_declaration.old8`：无类型注解的类方法（`func add(a, b)`）；
- `generator_basic/debug/forin_simple/param_test/simple.old8`、`sync_iterator_test.old8`：
  同步生成器（`func ... { yield ... }`）。

最小复现：

```old8
// 类方法实参
class Calculator {
    func add(a, b) { return a + b }
}
calc <- Calculator()
PrintLine(calc.add(10, 5))

// 同步生成器
func simpleGen() {
    yield 1
}
gen <- simpleGen()
```

两者在 IL 模式下都被 JIT 判为无效 IL。

## 2. 根因

### 2.1 类方法调用没有按形参类型转换实参

`DotOperatorILHelper.GenerateRegularInstanceMethodCall` 先加载接收者，再按
「原始类型」压入全部实参（注释写明「不装箱，保持原始类型」），最后直接 `callvirt`。
未标注类型的参数在 CLR 签名里是 `object`，于是栈上的 `int` 被当作 `object` 传入，
JIT 拒绝该方法。

### 2.2 同步生成器的 `yield` 不发射任何指令

`VisitYieldStatement` 是空实现（注释「后续实现」），既不返回生成器对象也不跳过后续语句，
整个函数体因此与调用约定不符，生成无效 IL。

## 3. 修复

1. `GenerateRegularInstanceMethodCall` 在 `callvirt` 前调用新增的
   `EmitInstanceArgumentConversions`：需要转换时把已压栈的实参逆序落到局部变量，
   再按形参类型重新加载（值类型 → `object` 装箱、`object` → 值类型拆箱、数值宽度转换）。
   引用类型之间刻意**不做** `castclass`：集合字面量的静态类型是 `List<object>`，
   形参可能写 `list<int>`，强行转换会在运行期抛 `InvalidCastException`，而这是既有宽松行为。
2. `VisitYieldStatement` 在非异步状态机上下文抛 `CompilerException`
   「IL 模式暂不支持同步生成器（yield）」；异步生成器路径保持原样
   （目前可编译，语义仍不完整）。

涉及文件：

- `Old8Lang.Compiler/OperationHelpers/DotOperatorILHelper.cs`
- `Old8Lang.Compiler/Visitors/CompilerVisitor.Statements2.cs`

## 4. 验证

### 4.1 新增回归测试

- `Old8Lang.Tests/Compiler/Classes/ClassMethodArgumentConversionTests.cs`（2 项）：
  无注解参数接收 int 实参、带注解参数保持精确匹配；
- `Old8Lang.Tests/Compiler/Collections/GeneratorIlUnsupportedTests.cs`（2 项）：
  同步生成器给出明确错误且不是 `InvalidProgramException`，异步生成器仍可编译。

修复前的 CLI 复现（`calc.add(10, 5)` 与 `func gen() { yield 1 }`）都是
「Common Language Runtime detected an invalid program」；修复后 4 项测试全部通过。

### 4.2 CLI 对比

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `calc.add(10, 5)`（无注解参数） | 无效 IL | `15` |
| `calc.add(a:int, b:int)` | 正常 | 正常 |
| `15_class_declaration.old8` | 无效 IL | 跑完，退出码 0 |
| `func gen() { yield 1 }` | 无效 IL | 明确编译错误 |
| `async func gen() -> stream { yield 1 }` | 正常（语义不完整） | 不变 |

### 4.3 回归面

`Compiler.Classes` 14 失败（与基线一致，另有新增用例通过）；
`Compiler.Types` 曾因对引用类型发 `castclass` 由 15 增到 16 失败，收紧规则后恢复为 15。
其余子组见 `changes/Todo.md` 记录的整组对比。

## 5. 遗留

1. 未注解参数收到 `double` / `string` 时，方法体里的 `a + b` 仍会撞上
   「IL 算术对 object 操作数一律按 int 拆箱」的独立缺口（`changes/Todo.md` 已有条目），
   因此本轮只覆盖 int 实参。
2. 异步生成器的 `yield` 目前被静默忽略（不生成迭代逻辑），语义仍不完整。
3. `from x in list1 join y in list2 on x == y where x > 2 select x` 仍生成无效 IL，
   已单独记录到 `changes/Todo.md`。
