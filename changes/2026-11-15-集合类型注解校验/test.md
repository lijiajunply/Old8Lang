# 带元素类型的集合注解在 IL 模式被误判「实际值不是列表」 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1. …再核实其他 IL 结果错误和跨模式差异」。

## 1. 现象

```old8
func f() -> int {
    intList: list<int> <- {1, 2, 3}
    return intList.Count()
}
```

| 模式 | 结果 |
|---|---|
| `-f` | `3` |
| `-il`（修复前） | `CompilerException: 变量 'intList' 类型注解为 list<int>,但实际值不是列表。期望类型: list<int>, 实际类型: any` |
| `-vm` | `3` |

`array<int>`、`dict<string, int>` 同样失败。这一条让 `Compiler.Types` 里
`GenericCollectionTypesCompilerTests` 的「带元素类型的集合」用例整批失败。

## 2. 根因

`CollectionTypeValidator.ValidateListElements` 等三个校验方法只认解释器的**运行时值**节点：

```csharp
if (value is not ListLangValue listValue) { throw new CompilerException("... 实际值不是列表"); }
```

而 IL 模式下 `{1, 2, 3}` / `[4, 5, 6]` / `{"a": 1}` 是**字面量语法节点**
`ListLiteralExpression` / `ArrayLiteralExpression` / `DictionaryLiteralExpression`，
于是全部落进「实际值不是列表」分支；`GetExpressionTypeName` 也把它们归为 `any`。

## 3. 修复

`Old8Lang.Runtime/TypeSystem/CollectionTypeValidator.cs`：

- 三个校验方法同时接受运行时值与字面量语法节点，统一取出元素/键值列表再校验；
- 元素类型不匹配时的错误消息保持不变（`列表元素类型不匹配: 第 i 个元素…`）；
- 「把元素类型写回 AST 节点」只对解释器的运行时值执行（字面量节点没有该字段）；
- `GetExpressionTypeName` 补充三种字面量节点的名称（`list` / `array` / `dict`）。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `list<int> <- {1, 2, 3}` + `Count()` | CompilerException | `3`，与 `-f`/`-vm` 一致 |
| `array<int> <- [4, 5, 6]` | CompilerException | `3` |
| `dict<string, int> <- {"a": 1}` | CompilerException | `1` |
| `Compiler.Types` 组 | 51 失败 / 50 通过 | **17 失败 / 84 通过** |
| `Compiler.Basic` 组（TypeSystemTests 里的数组注解用例） | 16 失败 / 185 通过 | **13 失败 / 188 通过** |

其余 17 个 `Compiler` 子组与修复前逐组一致（Async 43/65、Classes 95/15、
Collections 168/6、EdgeCases 77/22、Error 0/12、Exceptions 54/10、Expressions 197/35、
Extern 7/3、FileHeader 29/0、Functions 110/56、Integration 50/3、Linq 45/0、
Modules 263/0、Reflection 3/14、Statements 186/0、Threading 15/0、Unit 45/0）。

非 `Compiler` 组：Interpreter 1967、VirtualMachine 1083、Parser 889、Libraries 95、
LanguageServer、TypeSystem 65、Architecture 73 全部通过
（解释器组首次运行有 1 项 `TaskAdvancedTests` 并发抖动，复跑通过）。

## 5. 遗留

`GenericCollectionTypesCompilerTests` 里还有 6 项 `*TypeMismatch_ThrowsCompilerException`：
用例期望编译期报错（如 `list<int> <- {1, "hello", 3}` 应抛「列表元素类型不匹配」），
但实测解释器与 IL 都接受这段代码（返回 `1`），只有虚拟机报 `list<int>` 类型不匹配。
需要在「集合注解是否严格校验元素类型」上先定语义，再统一实现与断言。

## 6. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il generic_list.old8
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Types.GenericCollectionTypesCompilerTests"
```
