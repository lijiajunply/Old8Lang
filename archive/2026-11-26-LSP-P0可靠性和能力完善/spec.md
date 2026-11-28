# 规格说明：Language Server 与 VS Code 扩展 P0 可靠性与能力完善

## 1. 背景与目标

解决 `changes/Todo.md` 中「Language Server 与 VS Code 扩展」节全部 5 项 P0 问题，使 LSP 在真实编辑器场景下具备可靠的位置基准、词法作用域隔离、继承/静态成员访问类型推断、语义诊断与跨文件分析，并为所有已注册 Handler 提供稳定的单元测试和协议级回归保护。

## 2. 范围（对应 Todo.md 5 项 P0）

1. **P0-5：评估并逐步替换 token-based location workaround；Parser 修复后保留回归测试再移除**
   - 补齐 `Old8Lang.Syntax` 解析器缺失的标识符与块范围位置（`ClassSyntax.NamePosition`、`BlockStatement.EndPosition`、`ClassFuncInitStatement` 成员名位置、`ForInStatement`/`AsyncForInStatement` 循环变量位置、`TryStatement` 异常变量与语句起始位置、`UsingStatement` 变量位置、`EnumInit.NamePosition`、`ImportStatement` 模块名/别名位置、`ExternStatement`/`NativeStatement` 位置、`PlusPlus`/`MinusMinus` 位置）。
   - 移除 `SymbolTableBuilder` 中的 `FindSymbolLocationFromTokens`、`FindVariableLocationFromTokens`、`FindColumnInLine`、`FindColumnInLineForVariable`，以及 `DocumentLinkHandler.FindImportRange` 的启发式估算，全部改由 AST 精确 `SourcePosition` 提供。
   - 保留针对 Parser 与 LSP 位置的回归测试（同名多处声明、带类型注解变量、类/接口/枚举/导入/成员方法/属性）。

2. **P0-2：为符号表引入作用域链，正确处理局部变量、变量遮蔽和同名符号**
   - 引入词法作用域树 `SymbolScope`（挂在 `DocumentParseResult.RootScope`），覆盖 Global、Class、Function、AsyncFunction、Method、Block（if/elif/else/while/switch/case/try/finally/defer/select）、Loop（for/for-in/async-for-in）、Catch、Using、Lambda。
   - 顶层 `DocumentParseResult.SymbolTable` 只保留全局符号（不再把函数内局部变量污染到顶层），同作用域重复声明在 `AllDeclarations` 完整保留。
   - 作用域链支持按 LSP 0-based `(line, column)` 定位最内层作用域、按链向上查找（内层遮蔽外层）、以及获取可见符号集合。
   - 接入 `ScopeAnalyzer`、`SymbolFinder`、`DefinitionHandler`、`HoverHandler`、`ReferencesHandler`、`RenameHandler`、`DocumentHighlightHandler`、`CompletionHandler`、`SemanticTokensHandler`、`MemberChainAnalyzer`、`SemanticAnalyzer`。

3. **P0-3：完善成员访问的类型推断，覆盖继承成员和静态成员补全**
   - `SymbolInfo` 记录类继承链信息（`BaseClassName`、`InterfaceNames`、`MixinNames`、`IsInterface`、`IsAbstract`、`IsMixin`）。
   - 支持类字段/属性从初始化表达式推断类型，支持多级继承/Mixin/接口成员聚合（子类成员覆盖父类同名成员，防循环继承）。
   - `MemberChainAnalyzer` 改为作用域感知，支持局部变量、函数参数、`this.`、`super.`、静态访问 `ClassName.`、属性链 `obj.b.c.`、方法调用链 `obj.getB().getC().`、泛型类型参数剥离（`Box<int>` → `Box`）、可空后缀剥离（`User?` → `User`）、跨文件类查找。
   - 静态访问（`ClassName.`）仅补全静态成员（含继承的静态成员）；实例访问补全可见成员（`this.` / 类内部允许私有与受保护成员，`super.` 指向父类受保护与公共成员）；移除 `CompletionHandler` / `MemberChainAnalyzer` 中破坏 JSON-RPC stdout 的 `Console.WriteLine`。
   - 解除并修通现有两个被 Skip 的测试（`ChainOfPropertyAccess_ShouldComplete`、`NullableTypeParameter_ShouldComplete`）。

4. **P0-4：改善诊断：类型不匹配、未使用变量、成员不存在，以及跨文件语义分析**
   - 重构 `SemanticAnalyzer` 为基于 AST 与作用域链的分析器（可选注入 `WorkspaceIndex`）：
     - 作用域感知未定义符号检查与同作用域重复定义检查；
     - 类型不匹配诊断（显式类型注解赋值、函数/方法 return 类型与声明不匹配、调用参数数量与显式类型不匹配）；
     - 未使用变量诊断（`Warning` 级别，忽略 `_` 前缀，仅对局部/块级变量报告，避免顶层脚本脚本变量误报）；
     - 成员不存在与访问违规诊断（已知用户类上的不存在成员、通过类名访问实例成员、类外部访问私有成员）；
     - 跨文件语义分析（通过 `WorkspaceIndex` 和 `ModuleResolver` 解析 `import` 与工作区顶层符号：识别已导入/工作区声明的符号与类，检测选择性导入缺失符号和本地缺失模块）。
   - 在 `Program.cs` 中将 `WorkspaceIndex` 接入 `DocumentManager` 及需要跨文件解析的 Handler（`HoverHandler`、`CompletionHandler`、`SignatureHelpHandler` 等）。

5. **P0-1：为所有已注册 Handler 补齐稳定的单元测试和协议级测试**
   - 覆盖 `Program.cs` 注册的全部 26 个 Handler（含 `TextDocumentSyncHandler` 真实触发通知、`ReferencesHandler` 独立单测、以及此前 0 单测的 8 个 Debug/Profiler Handler）。
   - 新增进程内 JSON-RPC 协议级测试框架（基于 `System.IO.Pipelines.Pipe` 直接驱动真实 `OmniSharp.Extensions.LanguageServer.Server.LanguageServer`），逐一验证 `initialize` 能力协商、`didOpen`/`didChange`/`publishDiagnostics` 推送、全部 18 个标准 LSP Handler、全部 8 个 `old8lang/*` 自定义 RPC Handler、`didClose` 与 `shutdown`。
   - 校验 `vscode-old8lang` 编译无误。

## 3. 明确不做（超出 P0 范围）

- P1 的 Inline Values、Call Hierarchy / Type Hierarchy；
- P2 的按行 token 索引、防抖后台诊断、大文件性能基准；
- DAP 调试适配器实现与 TextMate 语法高亮重构。

## 4. 验收标准

- `Old8Lang.Tests` 下 `LanguageServer`、`Parser`、`Architecture` 测试全部通过，0 失败，原有 2 个 Skip 的 LanguageServer 用例转通过；
- `SymbolTableBuilder` 不再存在任何基于 token 扫描猜测位置的 workaround；
- `Program.cs` 注册的 26 个 Handler 全部拥有单元测试与协议级 JSON-RPC 测试；
- `vscode-old8lang` 执行 `npm run compile` 通过；
- `changes/Todo.md` 对应 5 项 P0 标记完成并在 `test.md` 留下完整实测记录。
