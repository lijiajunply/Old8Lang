# IL 模式 `in` 运算符生成无效 IL —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1. 排查会使测试进程崩溃的 IL 问题」。

## 1. 现象

用 IL 模式批量跑 `TestFiles/CompilerTests/` 时，`12_in_expression.old8` 被
IL 验证器判为无效 IL：

```
[编译错误] IL验证错误 [IL001]: 无效的IL代码: Common Language Runtime detected an invalid program.
```

最小复现（拼接里嵌一个字典 `in`）：

```old8
dict <- {"a": 1}
PrintLine("x" + ("a" in dict))
```

`-f` 输出 `xTrue`，IL 模式直接生成无效 IL。

## 2. 三个根因

### 2.1 `in` 的类型推断返回左操作数类型

`TypeInferenceVisitor.VisitOperation` 的「比较/逻辑运算符返回 bool」列表漏了
`LangTokenType.In`，于是落到末尾的 `return leftType`：

- `1 in list` 被推断成 `int`；
- `"a" in dict` 被推断成 `string`。

字符串拼接分支按这个类型决定是否装箱：

```csharp
operation.Right?.Accept(...);          // 实际发的是 bool
if (rightType != typeof(string))       // 推断成 string 时直接跳过装箱
    ilGenerator.Emit(OpCodes.Box, rightType!);
ilGenerator.Emit(OpCodes.Call, string.Concat(object, object));
```

于是 `string.Concat(object, object)` 收到未装箱的 `bool` → 无效 IL；
推断成 `int` 时会给 `bool` 发 `box int`，拼出错误的 `"x1"` 而不是 `"xTrue"`。

### 2.2 `Contains(T)` / `ContainsKey(TKey)` 收到装箱的 object

`GenerateInOperator` 先把左值统一装箱成 `object` 再压集合，随后：

```csharp
SwapStackOrder(...);              // 栈上变成 [collection, value(object)]
listType.GetMethod("Contains", [elementType])  // 形参是 int / string...
ilGenerator.Emit(OpCodes.Callvirt, containsMethod);
```

`List<int>.Contains(int)`、`Dictionary<string,…>.ContainsKey(string)` 拿到的却是
栈上的 `object`，同样生成无效 IL（数组分支走 `Array.IndexOf(object)` 不受影响）。

### 2.3 通用回退「什么都不发射却声明返回 bool」

集合类型不是 List/Array/Dictionary/String 时走 `TryGenerateGenericContains`；
找不到 `Contains(object)` 重载时返回 `false`，调用方却仍按「栈上有一个 bool」继续使用，
栈深度失衡，也会生成无效 IL。

## 3. 修复

1. `TypeInferenceVisitor.VisitOperation` 把 `LangTokenType.In` 归入恒返回 `bool` 的分支；
2. `InOperatorHelper.GenerateListContains` / `GenerateDictionaryContainsKey` 增加
   `EmitValueConversion`：按元素/键类型 `unbox.any`（数值宽度不同时先按实际装箱类型拆箱再转换），
   引用类型则 `castclass`；
3. 通用回退改为调用运行期 `InOperatorHelper.RuntimeContains(value, collection)`：
   字符串按字符/子串语义，其它 `IEnumerable` 按语言相等语义（`IlValueBridge.Wrap(...).Equal`）
   逐个比较，保证栈上一定有一个 bool。

涉及文件：

- `Old8Lang.Runtime/AST/Visitor/TypeInferenceVisitor.ExpressionsAndValues.cs`
- `Old8Lang.Compiler/OperationHelpers/InOperatorHelper.cs`

## 4. 验证

### 4.1 新增回归测试

`Old8Lang.Tests/Compiler/Expressions/InExpressionCompilerTests.cs` 新增 4 项：

- `InExpression_ListWithValueTypeElements_UsesElementConversion`
- `InExpression_DictionaryWithStringKeys_UsesKeyConversion`
- `InExpression_AsStringConcatOperand_DoesNotProduceInvalidIl`
- `InExpression_DictionaryInsideConcat_DoesNotProduceInvalidIl`

用 `git stash` 回退两处源码后，其中 3 项失败（`AsStringConcatOperand` 当时只是
拼出错误文本、不抛异常，故不失败）；恢复后 4 项全部通过。

### 4.2 CLI 对比

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `12_in_expression.old8` | 无效 IL | 跑完，退出码 0 |
| `"x" + (1 in {1,2})` | 拼出 `x1` | `xTrue` |
| `"x" + ("a" in {"a":1})` | 无效 IL | `xTrue` |
| `10 in {10,20,30}` | 无效 IL | `true` |
| `"name" in {"name":"Alice","age":30}` | 无效 IL | `true` |
| `1 in [1,2,3]` | 正常 | 正常 |

### 4.3 语料扫描

`TestFiles/CompilerTests/` 157 个文件逐个 `-il` 运行：本轮修复前 9 个文件报
「invalid program」，修复后剩 1 个（`linq_join_compiler.old8`，见遗留）。

## 5. 遗留

1. `'h' in "abc"`（右侧是**字符串字面量**）在 IL 下仍返回 `false`，解释器为 `true`；
   右侧换成变量 `str` 则正常（`'h' in str` → true）。属字面量接收者的类型推断/分派问题，
   未在本轮修复。
2. 字符串拼接里的 `bool` 显示为 `True`/`False`，解释器为 `true`/`false`
   （`string.Concat(object,object)` 调用的是 CLR `ToString`），属跨模式显示差异。
3. `from x in list1 join y in list2 on x == y where x > 2 select x`
   仍生成无效 IL（`linq_join_compiler.old8`），已单独记录到 `changes/Todo.md`。
