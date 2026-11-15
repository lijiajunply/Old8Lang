# IL 泛型可空参数（`T?`）的构造/调用实参转换 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1. 排查会使测试进程崩溃的 IL 问题」。
数据来源：`TestFiles/InterpreterTests` 语料扫描（`generic_nullable_simple.old8`）。

## 1. 现象

```old8
class Box<T?> {
    value: T?
    func init(v: T?) { this.value <- v }
    func getValue() -> T? { return this.value }
}
box <- Box<int>(5)
PrintLine(box.getValue())
```

`-f` 输出 5，`-il` 生成无效 IL（`Common Language Runtime detected an invalid program`）。
泛型函数同样：

```old8
func identity<T?>(v: T?) -> T? { return v }
identity<int>(456)     // -il 无效 IL
```

另外 `Box(123)`（不写类型参数、由构造函数实参推断）在 IL 下静默得到 `null`，
随后调用实例方法报「无法在 null 上调用方法 'getValue'」。

## 2. 根因

1. `GenericTypeResolver.ResolveType("T?")` 找不到映射（映射里只有 `T`），
   最终落到默认分支返回 `object`；于是特化后的 init 形参是 `object`。
2. `HandleGenericClassInstantiation` / `HandleGenericFunctionCall` 按实参的**原始类型**
   压栈（`int`），随后直接 `newobj`/`Call` 特化方法，`object` 形参拿到未装箱的 int
   → 无效 IL。
3. 不写类型参数时 `local.ClassVar` 里没有泛型类名，实例化兜底发 `ldnull` 返回 null。

## 3. 修复

1. 在 `GenericInstanceExpressionRuntimeExtensions` 增加
   `EmitInitArgumentConversions`：需要时把已压栈的实参落局部变量，再按形参类型
   重新加载（值类型 → object 装箱、object → 值类型拆箱、数值宽度转换）；
   泛型类 `init` 与泛型函数调用都接入。
2. 类实例化兜底：若名字在 `local.GenericClasses` 里（是泛型类但没写类型参数），
   抛 `CompilerException`「IL 模式暂不支持由构造函数实参推断泛型类的类型参数；
   请显式写成 Box<int>(...)」，不再静默返回 null。

涉及文件：

- `Old8Lang.Runtime/Execution/LegacyNodeExtensions/Expressions/GenericInstanceExpressionRuntimeExtensions.cs`
- `Old8Lang.Compiler/Visitors/CompilerVisitor.ExpressionsAndValues.cs`

## 4. 验证

### 4.1 新增回归测试

`Old8Lang.Tests/Compiler/Types/GenericNullableIlTests.cs`（4 项）：

- `GenericClassWithNullableParameter_StoresValue`
- `GenericClassWithNullableParameter_AcceptsNull`
- `GenericFunctionWithNullableParameter_BoxesValueArgument`
- `GenericClassWithoutTypeArguments_ReportsClearError`

前三项修复前生成无效 IL；第四项修复前在运行期 NRE，现在编译期明确报错。

### 4.2 CLI

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `Box<int>(7).getValue()` | 无效 IL | `7` |
| `Box<int>(null).hasValue()` | 无效 IL | `false` |
| `identity<int>(456)` | 无效 IL | `456` |
| `Box(123)` | 静默 null → NRE | 编译期明确错误 |
| 普通 `Box<T>`（无 `?`） | 正常 | 正常 |

### 4.3 语料

`generic_nullable_simple.old8` 不再产生无效 IL；因为文件使用 `Box(123)` 推断形式，
现在会得到上述明确编译错误（该特性在 IL 模式本来就不支持）。

## 5. 遗留

1. 泛型类/函数的 `T?` 目前按 `object` 承载（可空语义由 boxed 值/空引用表示），
   没有使用 `Nullable<T>`；
2. 「由构造函数实参推断泛型类类型参数」在 IL 模式仍未实现，只是改为明确报错。
