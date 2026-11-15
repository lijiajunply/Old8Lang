# 虚拟机 `in` 运算符未实现 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「虚拟机 `in` 运算符未实现」。

## 1. 现象

```old8
PrintLine(1 in [1, 2, 3])        // -vm 输出 [1, 2, 3]（右操作数本身）
PrintLine("a" in {"a": 1})       // -vm 输出 {"a": 1}
```

解释器与 IL 模式都返回 `true`。

## 2. 根因

`BytecodeVisitor.VisitOperation` 的二元运算符 switch 没有 `LangTokenType.In` 分支，
落入 `default: Emit(OpCode.Nop)`；左右操作数都已压栈且没有人消费，
表达式结果自然就是栈顶的右操作数，后面的 `PrintLine` 打印出集合。

## 3. 修复

1. `OpCode` 比较运算段新增 `In = 0x26`；
2. `BytecodeVisitor` 增加 `case LangTokenType.In: Emit(OpCode.In);`；
3. VM 指令分派把 `OpCode.In` 交给比较指令处理；
4. `VirtualMachine.Instructions.Comparison.cs` 新增 `ContainsIn(value, collection)`：
   - 右侧是 `ILangList`（ListLangValue / ArrayLangValue / DictionaryLangValue /
     StringLangValue 等）→ 直接调用其 `In`，与解释器完全同一套语义；
   - 右侧是原生 `string` → 字符按单字符、字符串按子串（序数）判断；
   - 右侧是原生 `IDictionary` → 按键查找；
   - 其他 `IEnumerable` → 逐项按 `Equals` 查找；
   - `null` 或不支持的类型 → `false`。

涉及文件：

- `Old8Lang.Runtime/Bytecode/Core/OpCode.cs`
- `Old8Lang.VirtualMachine/Visitors/BytecodeVisitor.Expressions.Operation.cs`
- `Old8Lang.VirtualMachine/VM/Instructions/VirtualMachine.Instructions.Main.cs`
- `Old8Lang.VirtualMachine/VM/Instructions/VirtualMachine.Instructions.Comparison.cs`

## 4. 验证

### 4.1 新增回归测试

`Old8Lang.Tests/VirtualMachine/Expressions/VMInOperatorTests.cs`（11 项）：

- 数组、列表、字典、字符串字符/子串共 10 组 `in` 结果；
- `if 2 in [1, 2, 3]` 作为条件分支。

修复前 10 项失败（只有一项期望 false 的用例碰巧通过）；修复后 11 项全部通过。

### 4.2 CLI 对比

| 表达式 | 解释器 | IL | 虚拟机（修复后） |
|---|---|---|---|
| `1 in [1, 2, 3]` | true | true | true |
| `6 in [1, 2, 3]` | false | false | false |
| `10 in {10, 20}` | true | true | true |
| `"name" in {"name": 1}` | true | true | true |
| `'h' in "hello"` | true | true | true |
| `"ell" in "hello"` | true | true | true |

`TestFiles/CompilerTests/12_in_expression.old8` 在 `-vm` 下各测试行与解释器一致
（仅布尔显示大小写沿用虚拟机自己的 `True/False`）。

### 4.3 回归面

`dotnet test --filter "FullyQualifiedName~Old8Lang.Tests.VirtualMachine"` 1096 项通过
（原 1085 + 新增 11）；`Compiler.Linq` 49 项、`Compiler.Expressions` 与修复前一致（9 失败）。

## 5. 遗留

字节码文件格式新增了 0x26 操作码；旧版本生成的 `.o8bc` 不受影响（不会包含该值），
但新版本字节码在旧运行时上执行会报未知指令，属正常的版本兼容边界。
