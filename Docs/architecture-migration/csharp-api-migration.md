# 架构迁移的 C# API 迁移说明

状态：阶段 2 首批，2026-11-14。

本文只记录已发生的公开 C# 源码 API 变更；Old8 源码语法、CLI 参数和三种执行模式的可见行为不变。

| 旧 API | 新 API | 使用方迁移方式 |
|---|---|---|
| `Old8Lang.AST.ILiteralSyntax` | `Old8Lang.Syntax.ILiteralSyntax` | 将 `using Old8Lang.AST;` 改为 `using Old8Lang.Syntax;`；现有 `LiteralValue` 保持不变，新增 `Kind` 和 `Position`。 |
| 无 | `Old8Lang.Syntax.LiteralSyntax` | 面向解析器、语言工具和未来语法节点：用 `LiteralSyntax.String/Char/Bool/Null/Void` 或 `TryParseInteger/TryParseDouble` 创建纯语法数据。不要把它当成可执行运行时值。 |
| 无 | `Old8Lang.Syntax.LiteralKind` | 根据字面量源码类别分支；不要通过 `LangValueType` 子类推断语法类别。 |
| `PrimaryParser.Parse*Literal` 返回标量 `LangValueType` | 返回 `ScalarLiteralExpression` | 从 `Kind`、`LiteralValue` 与 `Position` 读取语法信息；通过 Visitor 求值，不要将解析结果强转为运行时值。字符串的 `LiteralValue` 是解析所得原始文本，运行时求值时沿用原来的转义处理。 |
| `LangExpression`、`OldStatement`、`IOldLangTree`、`MockLangTree`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间不变；直接使用的项目需引用 Syntax，并重新编译既有程序集。`ScalarLiteralExpression` 也位于 Syntax。 |
| Runtime 的 `IVisitor<TResult>` | Syntax 的 `IVisitor<TResult>` 与 Runtime 的 `IRuntimeVisitor<TResult>` | 仅访问已迁入 Syntax 的节点时实现前者；需要访问当前 Runtime 节点的自定义 Visitor 实现后者。旧节点的 `Accept` 暂要求传入 `IRuntimeVisitor`，直到该节点迁入 Syntax；既有自定义 Visitor 必须修改并重新编译。 |
| `LangId`、`ClassMemberId`、`ThisExpression`、`SuperExpression`、`TernaryExpression`、`NamedArgument`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和构造方式不变；直接使用这些类型的项目需引用 Syntax，并重新编译既有程序集。前五个节点通过 Syntax 的 `IVisitor` 分派（`ClassMemberId` 沿用标识符分派）。 |
| `Old8Lang.AST.Expression.GenericParameter`、`GenericConstraint`、`GenericConstraintKind`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间不变；直接使用这些类型的项目需引用 `Old8Lang.Syntax`，已编译的程序集需重新编译。 |
| `FunctionCallExpression`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间与构造方式不变；直接使用的项目需引用 Syntax 并重新编译。原静态方法 `InvokeDynamicDelegate` 已迁为 `Old8Lang.Runtime.DynamicDelegateInvoker.Invoke`。 |
| `AwaitExpression`、`ThrowStatement`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间不变；直接使用的项目需引用 Syntax 并重新编译。`ThrowStatement.Expression` 现公开为只读属性。 |
| `PatternType`、`AccessModifierType`、`ExternType`、`CallingConventionType`、`CSharpImportMode`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和枚举值不变；直接使用这些类型的项目需引用 `Old8Lang.Syntax`，已编译的程序集需重新编译。 |

Runtime 中的 `IntLangValue`、`DoubleLangValue`、`StringLangValue`、`BoolLangValue`、`CharLangValue`、
`NullLangValue`、`VoidLangValue` 暂时仍实现新接口，以降低现有 Visitor/扩展调用方的迁移成本。后续将
在独立 AST 字面量节点稳定后移除这层兼容关系；那一轮会在本文补充构造器和访问策略的替代路径。

仓库内已更新的直接使用方是上述七个运行时值类与架构测试；搜索 `ILiteralSyntax` 可定位外部源码使用方。
