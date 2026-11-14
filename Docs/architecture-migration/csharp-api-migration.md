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
| 解析出的标识符索引 `LangListItem` | Syntax 的 `IndexExpression` | 从 `Target`、`Key` 和 `Position` 读取语法信息，通过 Visitor 执行；旧运行时类型暂予保留。 |
| 解析出的切片 `SliceLangValue` | Syntax 的 `SliceExpression` | 从 `Id`、`Start`、`End`、`Step` 读取语法信息，通过 Visitor 访问；旧运行时类型暂予保留。 |
| 解析出的区间 `RangeLangValue` | Syntax 的 `RangeLiteralExpression` | 从 `Start`、`End`、`IncludeStart`、`IncludeEnd` 读取语法信息，通过 Visitor 求值；运行时辅助类型暂予保留。 |
| 解析出的元组 `TupleLangValue` | Syntax 的 `TupleLiteralExpression` | 从 `Elements` 和 `FieldNames` 获取语法信息；通过 Visitor 获取运行时元组。解构目标也使用该语法节点。 |
| `PrimaryParser.ParseListOrDictionary` 返回 `LangValueType`，列表字面量为 `ListLangValue` | 返回 `LangExpression`，列表和字典字面量分别为 Syntax 的 `ListLiteralExpression`、`DictionaryLiteralExpression` | 使用 `Elements` 或 `Entries` 读取源码结构；通过 Visitor 获取运行时集合。 |
| `PrimaryParser.ParseArrayOrRange` 返回 `LangValueType`，数组字面量为 `ArrayLangValue` | 返回 `LangExpression`，数组字面量为 Syntax 的 `ArrayLiteralExpression` | 使用 `Elements` 读取数组元素语法；通过 Visitor 获取运行时数组，其他区间或推导式仍保持各自节点类型。 |
| `Operation`（Runtime 程序集） | 同名类型（Syntax 程序集） | 引用 Syntax 并重新编译。原 `Type` 编译期缓存已移入 Runtime，不再是语法节点 API；需推断类型时使用对应后端的类型推断服务。 |
| `FunctionCallExpression`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间与构造方式不变；直接使用的项目需引用 Syntax 并重新编译。原静态方法 `InvokeDynamicDelegate` 已迁为 `Old8Lang.Runtime.DynamicDelegateInvoker.Invoke`。 |
| `AwaitExpression`、`ThrowStatement`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间不变；直接使用的项目需引用 Syntax 并重新编译。`ThrowStatement.Expression` 现公开为只读属性。 |
| `BreakStatement`、`ContinueStatement`、`ReturnStatement`、`YieldStatement`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和构造方式不变；直接使用的项目需引用 `Old8Lang.Syntax` 并重新编译。自定义 `IVisitor<TResult>` 实现需补四个 `Visit*` 方法。`BreakStatement`/`ContinueStatement` 的子语句索引器未实现，现抛 `NotSupportedException`（原为运行时的 `InvalidOperationError`）；这两个节点没有子语句，`Count == 0`，正常遍历不会触发。 |
| `BlockStatement`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和构造方式不变；直接使用的项目需引用 `Old8Lang.Syntax` 并重新编译。自定义 `IVisitor<TResult>` 实现需补 `VisitBlockStatement`。块内声明分类改由标记接口完成，不再依赖具体声明节点类型。 |
| `BlockStatement.ExecuteDeclarations/ExecuteModule/GenerateImportIl/ToAnyData/ToStaticData`（公开实例方法） | `BlockStatementRuntimeExtensions`（Runtime 公开扩展方法） | 引用 Runtime 并保留 `using Old8Lang.AST.Statement;` 后，原调用写法 `block.ExecuteModule(manager)` 仍然有效；扩展方法依赖解释器/IL/运行时值，按设计归 Runtime。 |
| 无 | `IBlockDeclaration`、`IBlockImportDeclaration`（Syntax） | 新增语法层标记接口：`FuncInit`、`ClassInit`、`AsyncFuncInit`、`NativeStatement`、`ExternStatement` 实现前者；`ImportStatement` 实现后者并保留 `IsDynamicImport`。自定义声明节点若要参与块内声明/导入分类，需实现对应接口。 |
| `PatternType`、`AccessModifierType`、`ExternType`、`CallingConventionType`、`CSharpImportMode`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和枚举值不变；直接使用这些类型的项目需引用 `Old8Lang.Syntax`，已编译的程序集需重新编译。 |

Runtime 中的 `IntLangValue`、`DoubleLangValue`、`StringLangValue`、`BoolLangValue`、`CharLangValue`、
`NullLangValue`、`VoidLangValue` 暂时仍实现新接口，以降低现有 Visitor/扩展调用方的迁移成本。后续将
在独立 AST 字面量节点稳定后移除这层兼容关系；那一轮会在本文补充构造器和访问策略的替代路径。

仓库内已更新的直接使用方是上述七个运行时值类与架构测试；搜索 `ILiteralSyntax` 可定位外部源码使用方。
