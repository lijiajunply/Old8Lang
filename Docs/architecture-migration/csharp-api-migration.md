# 架构迁移的 C# API 迁移说明

状态：阶段 2 首批、阶段 3 后端拆分，2026-11-14。

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
| `IfStatement`、`IfChild`、`DeferStatement`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和构造方式不变；直接使用的项目需引用 `Old8Lang.Syntax` 并重新编译。自定义 `IVisitor<TResult>` 实现需补 `VisitIfStatement`、`VisitDeferStatement`；`IfChild` 是内部辅助节点，仍不支持 Visitor 访问。不再使用的 `IfChild.GenerateConditionIl` 已随节点迁移移除。 |
| `WhileStatement`、`UsingStatement`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和构造方式不变；直接使用的项目需引用 `Old8Lang.Syntax` 并重新编译。自定义 `IVisitor<TResult>` 实现需补 `VisitWhileStatement`、`VisitUsingStatement`。生成器上下文执行与资源释放辅助移入 Runtime 的 `WhileStatementRuntimeExtensions`/`UsingStatementRuntimeExtensions`（内部扩展方法），`UsingStatement.DisposeResource` 不再公开。 |
| `SetStatement`、`ForStatement`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和构造方式不变；直接使用的项目需引用 `Old8Lang.Syntax` 并重新编译。自定义 `IVisitor<TResult>` 实现需补 `VisitSetStatement`、`VisitForStatement`。原 internal 字段 `_leftExpression` 仍不公开，调用方改用既有公开属性 `LeftExpression`；`IsSimpleCountingLoop`、`SetStatement` 的全局槽位/数值转换辅助移入 Runtime 扩展类。 |
| `MatchCase`、`TuplePattern`、`TuplePatternElement`、`RangePattern`、`SelectCase`、`ImportItem`、`ClassFieldSetStatement`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和构造方式不变；直接使用的项目需引用 `Old8Lang.Syntax` 并重新编译。`MatchCase.IsMatch` 由实例方法改为 Runtime 内部扩展方法 `MatchCaseRuntimeExtensions.IsMatch`；仓库内调用方保持不变。 |
| `FuncRunStatement`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间不变；直接使用的项目需引用 `Old8Lang.Syntax` 并重新编译。四个按表达式类型区分的构造器合并为单一 `FuncRunStatement(LangExpression, SourcePosition)`，原有实参类型（`Instance`、`Operation`、`AwaitExpression`、`GenericInstanceExpression`）均可隐式转换；`Expression` 属性保留。自定义 `IVisitor<TResult>` 实现需补 `VisitFuncRunStatement`。 |
| `SwitchStatement`、`CaseStatement`、`TryStatement`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和构造方式不变；直接使用的项目需引用 `Old8Lang.Syntax` 并重新编译。`SwitchStatement` 的 `SwitchExpression`/`SwitchCaseList`/`DefaultBlockStatement` 由 internal 改为公开只读属性；自定义 `IVisitor<TResult>` 实现需补 `VisitSwitchStatement`、`VisitCaseStatement`、`VisitTryStatement`。`TryStatement.IsMatch` 移入 Runtime 的 `TryStatementRuntimeExtensions`。 |
| `EnumInit`、`SelectStatement`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和构造方式不变；直接使用的项目需引用 `Old8Lang.Syntax` 并重新编译。自定义 `IVisitor<TResult>` 实现需补 `VisitEnumInit`、`VisitSelectStatement`。Select 的通道轮询/生成器路径辅助移入 Runtime 的 `SelectStatementRuntimeExtensions`。 |
| 解析器合成字面量（`++/--` 的 `IntLangValue(1)`、未初始化字段的 `NullLangValue`、字符串模板片段、动态导入字符串、空 `return` 的 `VoidLangValue`） | `ScalarLiteralExpression`（Syntax） | 解析结果统一为语法节点；求值后仍是原来的运行时值类型。依赖解析树节点的 C# 代码应按 `ScalarLiteralExpression.Kind` 判断，而不是按 `IntLangValue`/`NullLangValue`/`VoidLangValue` 子类判断。 |
| `ForInStatement`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和构造方式不变；直接使用的项目需引用 `Old8Lang.Syntax` 并重新编译。自定义 `IVisitor<TResult>` 实现需补 `VisitForInStatement`。解释执行的生成器/流/集合分支与 IL 字典遍历辅助移入 `ForInStatementRuntimeExtensions`。 |
| `AsyncForInStatement`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和构造方式不变；直接使用的项目需引用 `Old8Lang.Syntax` 并重新编译。`Expression`/`Body` 由 internal 改为公开只读属性（另有公开 `Id`）；Visitor 使用公开 `Ids`。自定义 `IVisitor<TResult>` 实现需补 `VisitAsyncForInStatement`。异步流/生成器/列表分支与 IL 生成移入 `AsyncForInStatementRuntimeExtensions`。 |
| `ListComprehension`（Runtime 程序集，`LangValueType` 子类） | 同名类型（Syntax 程序集，`LangExpression` 子类） | 解析树中的列表推导式不再是运行时值：它只表示语法，求值由 Visitor 产出列表。直接使用的项目需引用 `Old8Lang.Syntax` 并重新编译；自定义 `IVisitor<TResult>` 实现需补 `VisitListComprehension`。原 `CheckCondition`/`ProcessNestedLoops` 移入 Runtime 的 `ListComprehensionRuntimeExtensions`。 |
| `MatchExpression`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和构造方式不变；直接使用的项目需引用 `Old8Lang.Syntax` 并重新编译。自定义 `IVisitor<TResult>` 实现需补 `VisitMatchExpression`。 |
| `GenericInstanceExpression`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和构造方式不变；直接使用的项目需引用 `Old8Lang.Syntax` 并重新编译。自定义 `IVisitor<TResult>` 实现需补 `VisitGenericInstanceExpression`。类型注解转换、类型映射与泛型调用/实例化 IL 辅助移入 Runtime 的 `GenericInstanceExpressionRuntimeExtensions`。 |
| `StringTemplateValue`（Runtime 程序集，`LangValueType` 子类） | 同名类型（Syntax 程序集，`LangExpression` 子类） | 解析树中的字符串模板不再是运行时值；求值由 Visitor 产出字符串。直接使用的项目需引用 `Old8Lang.Syntax` 并重新编译；自定义 `IVisitor<TResult>` 实现需补 `VisitStringTemplateValue`。 |
| 解析器产出的 lambda（`FuncLangValue`/`AsyncFuncLangValue`） | Syntax 的 `LambdaExpression` | 解析树中的 lambda 只承载语法（返回类型注解、参数、函数体、是否 async）；求值/编译时经 `LambdaExpressionRuntimeFactory` 转换为原来的运行时可调用值。自定义 `IVisitor<TResult>` 实现需补 `VisitLambdaExpression`。依赖解析树节点类型判断 lambda 的 C# 代码应改判 `LambdaExpression`，而不是 `FuncLangValue`。 |
| `FunctionDecorator`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间 `Old8Lang.AST` 与构造方式不变；直接使用的项目需引用 `Old8Lang.Syntax` 并重新编译。 |
| Linq 子句类型（`LinqClause`、`FromClause`、`WhereClause`、`SelectClause`、`OrderByClause`、`LetClause`、`JoinClause`、`GroupByClause`、`QueryContinuation`；Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间 `Old8Lang.AST.Expression.Linq` 与构造方式不变；直接使用的项目需引用 `Old8Lang.Syntax` 并重新编译。`LinqExpression` 及其 IL 编译器 partial 仍在 Runtime。 |
| `FuncInit(FuncLangValue)`、`AsyncFuncInit(AsyncFuncLangValue)`（Runtime 程序集） | `FuncInit(FunctionSyntax)`、`AsyncFuncInit(FunctionSyntax)`（Syntax 程序集） | 直接构造函数声明的项目需改为先建 `FunctionSyntax`（函数名/返回类型注解、参数、函数体、泛型、装饰器、文档注释、是否 async）。`FuncInit.FuncValue`/`AsyncFuncInit.AsyncFuncValue` 字段被移除：引用 Runtime 并保留 `using Old8Lang.AST.Statement;` 后改用公开扩展方法 `GetFuncValue()`/`GetAsyncFuncValue()`；同一语法节点返回同一运行时可调用实例。`FuncInit.GetItemType` 移除，改用 `FuncLangValue.GetItemType`；`ValidateParamsParameter` 改为公开方法。自定义 `IVisitor<TResult>` 实现需补 `VisitFuncInit`、`VisitAsyncFuncInit`。 |
| 无 | `FunctionSyntax`（Syntax） | 函数声明的纯语法模型；装饰器与文档注释由工厂复制到运行时函数值。 |
| `ClassInit(TypeTemplate)`、`ClassFuncInitStatement(ClassMemberId, FuncLangValue)`（Runtime 程序集） | `ClassInit(ClassSyntax)`、`ClassFuncInitStatement(ClassMemberId, FunctionSyntax)`（Syntax 程序集） | 直接构造类声明的项目需先建 `ClassSyntax`（类名、类体、父类、mixin/接口、泛型、文档注释）。`ClassInit.AnyValue` 移除：引用 Runtime 并保留 `using Old8Lang.AST.Statement;` 后改用公开扩展 `GetTypeTemplate()`（同一语法节点返回同一 TypeTemplate）；`RegisterTypeToTypeSystem` 也改为扩展方法。`ClassFuncInitStatement.FuncValue` 改用 `GetFuncValue()`。`ClassInitRuntimeExtensions.CreateForTemplate` 供内部嵌套类重算使用。自定义 `IVisitor<TResult>` 实现需补 `VisitClassInit`。 |
| 无 | `ClassSyntax`（Syntax） | 类/接口/混入声明的纯语法模型。 |
| `ExtensionDeclaration`（Runtime 程序集，`List<FuncLangValue>`） | 同名类型（Syntax 程序集，`List<FunctionSyntax>`） | 构造参数中的扩展方法改为语法模型；引用 Runtime 后可用 `GetExtensionMethods()` 取得缓存的运行时函数值列表。`ExtensionDeclaration.ResolveTargetType` 移为 `ExtensionDeclarationRuntimeExtensions.ResolveTargetType`。自定义 `IVisitor<TResult>` 实现需补 `VisitExtensionDeclaration`。 |
| `NativeStatement`、`ExternStatement`、`ExternFunctionDeclaration`、`CSharpDllImportDeclaration`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和构造方式基本不变；`NativeStatement` 的 `FuncValue` 改为 `Function`（`FunctionSyntax?`），签名 `(…, FuncInit)` 改为 `(…, FunctionSyntax)`，运行时签名可用 `GetFuncValue()` 取得。`ExternStatement` 的内部字段改为公开只读属性 `DllName`/`Functions`/`DefaultCallingConvention`/`ExternType`。自定义 `IVisitor<TResult>` 实现需补 `VisitNativeStatement`、`VisitExternStatement`。 |
| 解析器产出的简单调用 `Instance(Id, args, namedArgs)` | Syntax 的 `CallExpression` | 解析树中的 `identifier(...)` 只承载调用语法；求值/编译时经 `CallSyntaxAdapter.Normalize`/`ToInstance` 适配为既有 `Instance`。Dot 分派、静态类调用、闭包捕获、泛型推断与 LSP 成员分析均按此适配。依赖解析树节点类型判断调用的代码应同时接受 `Instance` 与 `CallExpression`。 |
| `AsyncStreamExpression`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和构造方式不变；`_block` 字段改为公开只读属性 `Block`。自定义 `IVisitor<TResult>` 实现需补 `VisitAsyncStreamExpression`。 |
| `ImportStatement`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和构造方式不变；模块加载逻辑移入 `ImportStatementRuntimeExtensions`。原内部字段改为公开只读属性 `ImportString`/`FromClause`/`Specifiers`/`ModuleAlias`/`IsLazy`/`IsSelective`/`DynamicModuleExpression`（`Get*` 方法保留）。`ImportStatement.ModuleService` 改为 `ImportStatementRuntimeExtensions.ModuleService`；`HandleLazyImport`/`RegisterModuleValue`/`HandleDynamicImport` 改为扩展方法。自定义 `IVisitor<TResult>` 实现需补 `VisitImportStatement`。 |
| `LinqExpression`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间与构造方式不变；IL/Streaming 编译逻辑（约 1200 行）移入 Runtime 的 `LinqExpressionRuntimeExtensions`。自定义 `IVisitor<TResult>` 实现需补 `VisitLinqExpression`。 |
| `LangParser`、`PrimaryParser`、`StatementParser`、`ExpressionParser`、`FunctionParser`、`ClassParser`、`LinqParser`、`ExtensionParser`、`ParserBase`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间 `Old8Lang.LangParser` 不变；直接使用解析器的项目需引用 `Old8Lang.Syntax` 并重新编译。`ScopeLayer` 留在 `Old8Lang.Runtime`。 |
| 无 | `IBlockDeclaration`、`IBlockImportDeclaration`（Syntax） | 新增语法层标记接口：`FuncInit`、`ClassInit`、`AsyncFuncInit`、`NativeStatement`、`ExternStatement` 实现前者；`ImportStatement` 实现后者并保留 `IsDynamicImport`。自定义声明节点若要参与块内声明/导入分类，需实现对应接口。 |
| `PatternType`、`AccessModifierType`、`ExternType`、`CallingConventionType`、`CSharpImportMode`（Runtime 程序集） | 同名类型（Syntax 程序集） | 命名空间和枚举值不变；直接使用这些类型的项目需引用 `Old8Lang.Syntax`，已编译的程序集需重新编译。 |

Runtime 中的 `IntLangValue`、`DoubleLangValue`、`StringLangValue`、`BoolLangValue`、`CharLangValue`、
`NullLangValue`、`VoidLangValue` 暂时仍实现新接口，以降低现有 Visitor/扩展调用方的迁移成本。后续将
在独立 AST 字面量节点稳定后移除这层兼容关系；那一轮会在本文补充构造器和访问策略的替代路径。

仓库内已更新的直接使用方是上述七个运行时值类与架构测试；搜索 `ILiteralSyntax` 可定位外部源码使用方。

## 阶段 3：执行后端程序集拆分（2026-11-14）

三个执行后端的 Visitor 已从 Runtime 迁出，Runtime 只保留 `RuntimeVisitors` 工厂契约与共享执行状态。
`Old8HostBuilder` 构造时显式注册三个后端；不经过 Hosting 的宿主必须自行注册所需后端。

| 旧 API | 新 API | 使用方迁移方式 |
|---|---|---|
| `Old8Lang.AST.Visitor.InterpreterVisitor`（Runtime 程序集） | 同名类型（`Old8Lang.Interpreter` 程序集） | 引用 `Old8Lang.Interpreter`，组合根调用 `InterpreterBackend.Register()`。需要在 Runtime 内部或自定义代码中按 manager 求值时，改用 `RuntimeVisitors.CreateInterpreter(manager)`，返回 `IRuntimeVisitor<LangValueType>`。 |
| `Old8Lang.AST.Visitor.CompilerVisitor`（Runtime 程序集） | 同名类型（`Old8Lang.Compiler` 程序集） | 引用 `Old8Lang.Compiler`，组合根调用 `CompilerBackend.Register()`。按 IL 生成器与局部管理器执行时使用 `RuntimeVisitors.CreateCompiler(ilGenerator, local)`，返回 `IRuntimeVisitor<object?>`。 |
| `Old8Lang.AST.Visitor.BytecodeVisitor`（Runtime 程序集） | 同名类型（`Old8Lang.VirtualMachine` 程序集），并实现 `IBytecodeVisitor` | 引用 `Old8Lang.VirtualMachine`，组合根调用 `VirtualMachineBackend.Register()`。`BytecodeCompiler` 经 `RuntimeVisitors.CreateBytecode(compiler)` 获取 `IBytecodeVisitor`，再读取 `GetInstructions()`/`MaxStackSize`。注册类位于 `Old8Lang.BytecodeBackend` 命名空间，避免 `Old8Lang.VirtualMachine` 命名空间与 `Bytecode.VM.VirtualMachine` 类型同名。 |
| 无 | `Old8Lang.AST.Visitor.RuntimeVisitors`（Runtime） | 三个后端的工厂契约；`HasInterpreter`/`HasCompiler`/`HasBytecode` 用于诊断组合根是否注册。未注册时抛出带后端指引的 `InvalidOperationException`。 |
| 无 | `Old8Lang.AST.Visitor.IBytecodeVisitor`（Runtime） | 字节码后端的最小契约：`IRuntimeVisitor<Instruction?>` + 指令列表 + 最大栈深。自定义字节码后端可实现该接口并注册。 |
| Runtime 的 internal 执行辅助（迁移期） | `InternalsVisibleTo` 解释、IL、VM 三个后端程序集 | 后端程序集暂可访问 Runtime internal 成员；第三方扩展不应依赖这些成员。后续收敛为公开契约时会在本文补充替代路径。 |

行为不变的验证：解释器 1959/1960（排除并发访问外部 HTTP 服务的 `NetLibTests`，1 跳过）、VM 1082、
Architecture 55 通过；Compiler.Expressions 的 41 项失败与迁移前 HEAD 逐项一致，属 `Todo.md`
记录的 IL 既有问题。

### 阶段 3.10：VM 引擎与字节码编译器（2026-11-14）

| 旧 API | 新 API | 使用方迁移方式 |
|---|---|---|
| `Old8Lang.Bytecode.VM.VirtualMachine`（Runtime 程序集） | 同名类型（`Old8Lang.VirtualMachine` 程序集） | 引用 `Old8Lang.VirtualMachine`；命名空间与构造方式不变。Runtime 侧只保留 `IVirtualMachine` 契约。 |
| `Old8Lang.Bytecode.BytecodeCompiler`（Runtime 程序集） | 同名类型（`Old8Lang.VirtualMachine` 程序集） | 引用 `Old8Lang.VirtualMachine`；实现 `IBytecodeCompiler`，`RuntimeVisitors.CreateBytecode` 接收该接口。 |
| `Old8Lang.Bytecode.ModuleSystem.ModuleLoader`/`ModuleRegistry`、`Bytecode.Debugger.VMDebugger`、`Bytecode.Profiler.VMProfiler`、`Bytecode.Interop.ExternFunctionWrapper`、`Bytecode.VmStaticClassRegistry` | 同名类型（`Old8Lang.VirtualMachine` 程序集） | 引用 VM 后端并重新编译；命名空间不变。 |
| `VMContext.CurrentVM`、`VMReflectionHelper` 的 `VirtualMachine` 参数 | `IVirtualMachine`（Runtime） | 共享运行时只依赖接口；需要具体虚拟机能力时由 VM 后端实现。 |
| 无 | `IBytecodeCompiler`（Runtime） | 字节码编译器的最小标记契约，用于 Runtime 与 VM 后端解耦。 |

字节码格式与共享执行模型（`BytecodeFile`、`Instruction`、`OpCode`、`CallFrame`、`FunctionMetadata`、
`ClassMetadata`、闭包类型）仍在 `Old8Lang.Runtime`；`ModuleDependency`/`ExportedSymbol` 作为
`BytecodeFile` 的格式类型留在 Runtime，模块加载实现位于 VM 后端。

### 阶段 3.12：IL 编译器驱动（2026-11-14）

| 旧 API | 新 API | 使用方迁移方式 |
|---|---|---|
| `Old8Lang.Compiler.Compiler`（Runtime 程序集） | 同名类型（`Old8Lang.Compiler` 程序集） | 引用 `Old8Lang.Compiler` 并重新编译；`Compile`、`LogLevel`、`CurrentLogLevel` 等公开 API 不变。 |
| `Old8Lang.Compiler.GlobalVariableAnalysis`、`Old8Lang.Compiler.Verification.ILVerifier` | 同名类型（`Old8Lang.Compiler` 程序集） | 引用 IL 后端；命名空间不变。`GlobalVariableScope` 仍在 Runtime，供 `LocalManager` 共享。 |
| 无 | `Old8Lang.Compiler.IlCompilerOptions`（Runtime） | 共享编译开关与文件头指令入口；`Compiler` 的同名属性委托到该类型。 |
