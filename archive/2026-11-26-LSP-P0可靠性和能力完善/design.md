# 设计方案：Language Server P0 可靠性与能力完善

## 1. 受影响程序集与边界检查

严格遵守 `Docs/DEVELOPMENT_WORKFLOW.md §4.1`：
- `Old8Lang.Syntax`：只补充语法节点的 `SourcePosition` 属性与解析器记录逻辑，不引入任何运行时或 LSP 依赖；所有新增构造函数参数均带 `= default` 保持兼容。
- `Old8Lang.LanguageServer`：新增 `Models/SymbolScope.cs`、`Services/TypeResolver.cs`，演进 `SymbolInfo`、`DocumentParseResult`、`DocumentManager`、`SymbolTableBuilder`、`ScopeAnalyzer`、`SymbolFinder`、`MemberChainAnalyzer`、`SemanticAnalyzer` 及各 Handler。
- `Old8Lang.Tests`：新增 Parser 位置回归测试、作用域/继承/诊断专项测试、缺失 Handler 单测以及进程内 JSON-RPC 协议级测试套件。

## 2. 阶段 1：Parser 位置补齐与移除 token 定位变通

### 2.1 `Old8Lang.Syntax` 补充位置
- `BlockStatement`：新增 `public SourcePosition EndPosition { get; }`（构造参数 `SourcePosition endPosition = default`）。
  - `StatementParser.ParseBlock()`：大括号块记录 `{` 为 `position`，`}` 为 `endPosition`；单语句块取该语句的 `Position`。
  - `ClassParser.ParseClassBlock()`：记录 `{` 与 `}`。
  - `StatementParser.ParseTryStatement()`：`finallyBlock` 记录 `{` 与 `}`；`TryStatement` 本身记录 `tryToken`（修复原先用结尾 token 的缺陷）；`catch (e)` 在消费 `e` 前记录 `e` 的位置（修复原先指向 `)` 的缺陷）。
  - `LangParser.ParseProgram()`：非空 token 流以首尾 token 为起止位置。
- `ClassSyntax`：新增 `public SourcePosition NamePosition { get; }`（参数 `SourcePosition namePosition = default`，默认回退 `position`）。`ClassParser.ParseClassDeclaration` / `ParseInterfaceDeclaration` 传入类名 token 位置。
- `ClassParser.ParseClassBlock()`：带修饰符方法构造 `ClassMemberId` 时传入 `funcInit.Syntax.Id.Position`（修复原先误传 `funcInit.Position` 的问题）；`ParseAbstractMethodDeclaration` 为方法名 `LangId` 与 `ClassMemberId` 记录 `methodNamePosition`。
- `StatementParser.ControlFlow.cs`：`ParseForInStatement` / `ParseAsyncForInStatement` 为每个循环变量 `LangId` 记录其 token 位置。
- `UsingStatement`：新增 `public SourcePosition VariablePosition { get; }`，在 `ParseUsingStatement` 记录变量标识符位置。
- `EnumInit`：新增 `public SourcePosition NamePosition { get; }`，在 `ParseEnumDeclaration` 记录枚举名位置。
- `ImportStatement`：新增 `ModuleNamePosition`、`ModuleAliasPosition` 与 `IsQuotedModuleName`，在 `ParseImportStatement` 各分支记录。
- `StatementParser.Assignment.cs`：`ParsePlusPlus` / `ParseMinusMinus` 传递标识符 `SourcePosition`。
- `ExternStatement` / `ExternFunctionDeclaration` / `NativeStatement`：补充 `SourcePosition`（含 `NamePosition`），并在解析时传入。

### 2.2 移除 LSP 中的 token-based location workaround
- `SymbolTableBuilder`：删除 `FindSymbolLocationFromTokens`、`FindVariableLocationFromTokens`、`FindColumnInLine`、`FindColumnInLineForVariable`，统一通过辅助方法 `ToLocation(SourcePosition pos, int length)` 生成 0-based `SourceLocation`（`Line = Math.Max(0, pos.Line - 1)`，`Column = Math.Max(0, pos.Column)`，`EndLine = Line`，`EndColumn = Column + length`）。
- `DocumentLinkHandler`：直接使用 `importStatement.ImportString` 和 `importStatement.ModuleNamePosition` 计算精确链接范围。

## 3. 阶段 2：符号表作用域链

### 3.1 `SymbolScope` 数据模型（`Models/SymbolScope.cs`）
```csharp
public enum ScopeKind { Global, Class, Function, Method, Block, Loop, Catch, Using, Lambda }

public class SymbolScope
{
    public ScopeKind Kind { get; init; }
    public SymbolScope? Parent { get; init; }
    public List<SymbolScope> Children { get; } = [];
    public SourceLocation Range { get; init; } // 0-based inclusive start..end
    public Dictionary<string, SymbolInfo> Symbols { get; } = new();
    public List<SymbolInfo> AllDeclarations { get; } = [];
    public SymbolInfo? OwnerSymbol { get; init; }
}
```
- `SymbolInfo` 新增 `public SymbolScope? DeclaringScope { get; set; }`，标识该符号声明所在作用域（区分顶层符号、局部符号、参数、成员）。
- `SymbolScope` 提供：
  - `ContainsPosition(int line, int column)`（0-based）
  - `FindInnermostScope(int line, int column)`：递归优先命中更具体的子作用域；对没有显式范围的节点合理回退。
  - `Resolve(string name, int? line = null, int? column = null)`：从当前作用域沿 `Parent` 向上查找；在函数/块作用域内若提供位置，只返回声明位置不晚于当前位置的局部变量（函数/类/参数在同一作用域内可前向可见）。
  - `GetVisibleSymbols(int line, int column)`：从最内层作用域向上汇总，内层同名符号覆盖外层，并在类实例方法内自动提供 `this`（以及有父类时的 `super`）。
  - `FindScopedReferences(DocumentParseResult doc, SymbolInfo target)`：
    - 若 `target` 是局部/参数符号：只在 `target.DeclaringScope` 范围内查找标识符，跳过被内层同名声明遮蔽的子作用域以及 `.member` 成员访问；
    - 若 `target` 是类成员：结合接收者类型或类体范围定位；
    - 若 `target` 是全局符号：在整个文件内查找，但跳过被局部/参数同名变量遮蔽的作用域。

### 3.2 `SymbolTableBuilder` 双输出
- `Build(BlockStatement ast)` 保持返回全局符号表（`Dictionary<string, SymbolInfo>`），同时暴露 `RootScope` 属性及 `BuildWithScope(BlockStatement ast)`，由 `DocumentManager` 和 `WorkspaceIndex` 写入 `DocumentParseResult.RootScope`。
- 递归构建所有语句块的嵌套作用域：
  - `FuncInit` / `AsyncFuncInit`：在父作用域注册函数符号，创建 `ScopeKind.Function` 子作用域注册参数并遍历 `Body`；
  - `ClassInit`：在父作用域注册类符号（含父类/接口/Mixin 元数据），创建 `ScopeKind.Class` 子作用域，成员方法各自创建 `ScopeKind.Method` 子作用域并遍历方法体；
  - `EnumInit`：注册枚举符号及其枚举成员；
  - `IfStatement` / `WhileStatement` / `ForStatement` / `ForInStatement` / `AsyncForInStatement` / `TryStatement` / `UsingStatement` / `SwitchStatement` / `SelectStatement` / `DeferStatement` / Lambda 表达式：按块建立子作用域并注册循环变量、`catch` 异常变量、`using` 资源变量、lambda 参数。
  - 变量赋值 `SetStatement`：若当前作用域链（同一函数内）已声明该变量且非新类型注解声明，则视为重新赋值而非重复定义；否则在当前作用域声明。

## 4. 阶段 3：成员访问类型推断与继承/静态补全

### 4.1 `TypeResolver` 服务（`Services/TypeResolver.cs`）
- 规范化类型名：去除可空 `?`、去除泛型参数 `<...>`、去除前导 `static `。
- `ResolveClass(string typeName, DocumentParseResult? doc, WorkspaceIndex? workspaceIndex)`：先查当前文档符号表，再查工作区索引。
- `GetAllMembers(SymbolInfo classSymbol, DocumentParseResult? doc, WorkspaceIndex? workspaceIndex, bool staticOnly, AccessContext accessContext)`：
  - 沿 `BaseClassName` 向上递归（使用 `HashSet<string>` 防环），合并 `MixinNames` 和 `ImplementsNames`，派生类成员覆盖基类同名成员；
  - `AccessContext` 区分：外部访问（仅 `Public`）、子类内部（`Public` + `Protected`）、本类内部（全部）；
  - `staticOnly = true` 时（如 `ClassName.`）仅返回 `IsStatic == true` 的成员；
  - 实例访问时返回实例成员（保留兼容性：旧测试 `inner.getStaticValue()` 期望实例上也能补全静态成员，因此实例访问不隐藏公开静态成员，而类名访问严格只展示静态成员）。
- `InferExpressionType(LangExpression expr, SymbolScope scope, DocumentParseResult? doc, WorkspaceIndex? workspaceIndex)`：
  - 字面量（`ScalarLiteralExpression` / `*LangValue` / 列表/数组/字典/范围/元组/模板字符串）、标识符（作用域链解析）、`this`（当前类）、`super`（父类）、类实例化（`Instance` / `CallExpression` / `GenericInstanceExpression` / `FunctionCallExpression`）、成员访问链 `Operation(Dot)`（支持继承字段/方法返回类型、静态成员）、函数调用返回类型、二元/一元/三元运算类型推断。
- 同步改进 `MemberChainAnalyzer`：
  - 接收 `DocumentParseResult`（或保留旧构造器兼容）、`WorkspaceIndex?`；
  - 使用 `RootScope` 在光标位置解析首标识符、`this`、`super`、函数调用起点（`createUser().`）；
  - 返回 `MemberChainResult(SymbolInfo TargetClass, bool IsStaticAccess, SymbolInfo? EnclosingClass)`，供 `CompletionHandler` 精确过滤静态/可见性，并同步用于 `SymbolFinder` 和 `SignatureHelpHandler`。

## 5. 阶段 4：语义诊断与跨文件语义分析

- `SemanticAnalyzer` 重构为遍历 AST + `RootScope` + 可选 `WorkspaceIndex`：
  1. **同作用域重复定义**（`Error`）：检查每个作用域的 `AllDeclarations` 中同名重复的函数/类/枚举/方法/字段/参数声明。
  2. **跨文件导入解析**：解析 `ImportStatement`：
     - 标准库（`StandardLibraryRegistry` / `NativeLibraryRegistry`）注册导入的模块名或选择性导入符号；
     - 本地/工作区文件（通过 `ModuleResolver` 或同目录/工作区路径在 `WorkspaceIndex` 中定位）：将导出符号（全量或选择性导入）并入当前文件可见导入集合；若本地相对路径文件不存在则报 `Warning`（仅在当前文档 URI 对应真实磁盘目录存在时报告，避免内存虚拟 URI 测试误报）；若选择性导入的符号在目标文件不存在则报 `Error`。
     - 同时若存在 `WorkspaceIndex`，允许解析工作区内其他文件声明的顶层符号（与现有 `CrossFileNavigationTests` 语义保持一致）。
  3. **未定义符号**（`Error`）：基于 AST 表达式遍历（不误伤成员名、命名参数名、字典键、类型注解内置类型、全局函数、导入符号、`this`/`super`）。
  4. **成员不存在 / 静态误用 / 访问修饰符违规**（`Error` / `Warning`）：
     - 当 `left.member` 的 `left` 推断为已知用户定义类时，用 `TypeResolver.FindMember` 检查成员是否存在（含继承/Mixin/接口）；不存在报 `Error: 类型 'C' 不存在成员 'm'`；
     - `ClassName.instanceMember` 报 `Error: 不能通过类 'C' 静态访问实例成员 'm'`；
     - 类外部访问 `private` 成员报 `Error: 无法访问类 'C' 的私有成员 'm'`。
  5. **类型不匹配**（`Warning` / `Error`，用 `Error` 于明确违背显式注解）：
     - 显式类型注解赋值 `x: int <- "str"` 或对已标注类型的变量重新赋不兼容类型；
     - 函数/方法 `return expr` 与显式非 `void`/`any` 返回类型不兼容；
     - 调用已知用户函数/方法时参数数量不匹配或显式标注参数类型不匹配。
     - 兼容规则：`any`/`var`/未知类型始终放行；`int` 可赋给 `double`；`null` 可赋给可空 `T?` 或类/集合/对象类型；派生类可赋给基类/接口；联合类型 `A | B` 任一分支匹配即兼容；泛型参数名（如 `T`）在泛型类/函数体内放行。
  6. **未使用变量**（`Warning`）：
     - 跟踪局部作用域（函数/方法/块内）声明的局部变量读写次数；未读且不以 `_` 开头的局部变量报告 `DiagnosticSeverity.Warning`（`未使用的变量 'x'`）。
- `DocumentManager`：
  - 新增可选 `WorkspaceIndex?` 引用（支持构造函数或 `AttachWorkspaceIndex` 避免循环依赖：`Program.cs` 中创建完两者后绑定，且 `DocumentManager.UpdateDocument` 将 `WorkspaceIndex` 传入 `SemanticAnalyzer`）。

## 6. 阶段 5：全量 Handler 单测与协议级测试

- 新增 `Old8Lang.Tests/LanguageServer/Handlers/DebugProfilerHandlersTests.cs`：覆盖全部 8 个 Debug/Profiler Handler（正常流程、缺失会话、无效命令、异常边界）。
- 新增 `Old8Lang.Tests/LanguageServer/Handlers/Navigation/ReferencesHandlerTests.cs` 与增强 `TextDocumentSyncHandlerTests.cs`（用 `Moq` 验证 `DidOpen`/`DidChange`/`DidSave`/`DidClose` 与 `PublishDiagnostics`）。
- 新增 `Old8Lang.Tests/LanguageServer/Protocol/LspProtocolEndToEndTests.cs`：
  - 使用 `System.IO.Pipelines.Pipe` 启动真实 `OmniSharp.Extensions.LanguageServer.Server.LanguageServer`，注册与 `Program.cs` 完全一致的全部 26 个 Handler；
  - 编写轻量 `LspJsonRpcTestClient` 收发标准 `Content-Length` JSON-RPC 帧；
  - 验证 `initialize` → `initialized` → `textDocument/didOpen` → 接收 `textDocument/publishDiagnostics` → 逐一发送 18 种标准 LSP 请求 + 8 种 `old8lang/*` 自定义请求 → `textDocument/didChange` → `textDocument/didClose` → `shutdown`。
