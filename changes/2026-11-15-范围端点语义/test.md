# 范围字面量：端点相等且被排除时的方向错误 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1」之后的结果错误核实。

## 1. 现象

```old8
PrintLine([5>~<5])   // 解释器 / IL：[6, 5, 4]；虚拟机：[]
PrintLine([5>~5])    // 解释器 / IL：[6, 5]；虚拟机：[]
PrintLine([5~<5])    // 解释器 / IL：[5, 4]；虚拟机：[]
```

按语法文档，`>` 表示排除起点、`<` 表示排除终点，`[5>~<5]` 是开区间 (5, 5)，
应为空；解析器测试也期望长度为 0，但解释器与 IL 输出 3/2/2。

## 2. 根因

解释器与 IL 各自实现了一遍区间算法（`InterpreterVisitor.VisitRangeLangValue` 与
`RangeLangValue.CreateRangeArray`），逻辑都是：

```csharp
if (!includeStart) startNum++;
if (!includeEnd)   endNum--;
if (startNum > endNum) { 降序 startNum..endNum }
else                   { 升序 startNum..endNum }
```

端点相等时排除会得到 `startNum > endNum`（如 6 > 4），于是被误判成降序范围。
方向应当由**原始端点**决定，排除端点只收窄区间。

## 3. 修复

1. `RangeLangValue.CreateRangeArray` 改为：
   - `start <= end` → 升序，`lo = includeStart ? start : start + 1`，
     `hi = includeEnd ? end : end - 1`；
   - `start > end` → 降序，`hi = includeStart ? start : start - 1`，
     `lo = includeEnd ? end : end + 1`；
   - 收窄后 `lo > hi`（或降序 `hi < lo`）自然得到空数组。
2. `InterpreterVisitor.VisitRangeLangValue` 删除重复算法，直接调用同一个
   `CreateRangeArray`，避免两份实现再次分叉。

涉及文件：

- `Old8Lang.Runtime/AST/Expression/Advanced/Access/RangeLangValue.cs`
- `Old8Lang.Interpreter/Visitors/InterpreterVisitor.Values.cs`

## 4. 验证

### 4.1 新增回归测试

- `Old8Lang.Tests/Interpreter/Expressions/RangeBoundaryTests.cs`（7 组 Theory）：
  `[5>~<5]`、`[5>~5]`、`[5~<5]` 为 0，`[5~5]` 为 1，`[1~<4]` / `[1>~4]` 为 3，
  `[10~1]` 为 10；
- `Old8Lang.Tests/VirtualMachine/Expressions/VMRangeBoundaryTests.cs`（同一组 7 项）。

修复前解释器与 IL 的等端点排他范围全部错误；修复后解释器、IL、虚拟机结果一致。
`Compiler.Collections.RangeTests.RangeEdgeCases_CompilesAndExecutesCorrectly`
由失败转为通过。

### 4.2 用例冲突修正

`Compiler/Collections/Advanced/ExtendedRangeTests.EmptyRange_CompilesAndExecutesCorrectly`
原来按**旧的错误行为**断言 `[1>~<1]` 长度为 3（注释写「2 ~ 0 降序」），
与语法文档和 `RangeEdgeCases` 的期望（0）冲突；已改为断言 0。

### 4.3 回归面

`Compiler.Collections` 失败数 5 → 4；解释器/虚拟机/编译器的范围测试
（`RangeTests`、`ExtendedRangeTests`、`VMRangeTests` 等）共 69 项通过、3 项跳过。
