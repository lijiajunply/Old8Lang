# 测试报告：Language Server 与 VS Code 扩展 P0 可靠性与能力完善

日期：2026-11-26

## 1. 测试范围与实测命令

| 范围 | 命令 | 结果 |
|------|------|------|
| LanguageServer 全量单测 + 协议级测试 | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --filter "FullyQualifiedName~Old8Lang.Tests.LanguageServer"` | **500 通过、0 失败、0 跳过**（基线 474 通过、2 跳过） |
| LanguageServer + Parser 联合回归 | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --filter "FullyQualifiedName~Old8Lang.Tests.LanguageServer\|FullyQualifiedName~Old8Lang.Tests.Parser"` | **1393 通过、0 失败、0 跳过** |
| 词法 / Token / 位置全仓回归 | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~Position\|FullyQualifiedName~Lexical\|FullyQualifiedName~Token"` | **133 通过、0 失败、1 跳过** |
| Architecture + Error 边界与错误回归 | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --filter "FullyQualifiedName~Old8Lang.Tests.Architecture\|FullyQualifiedName~Old8Lang.Tests.Error"` | **109 通过、1 已知既有失败**（`ScalarLiteralExpressionTests.AsyncStateMachine_FindsAwaitInsideSyntaxCollections`，已记录于 `changes/Todo.md` 第 245 行，与本任务无关） |
| LanguageServer 无增量全量构建 | `dotnet build Old8Lang.LanguageServer/Old8Lang.LanguageServer.csproj --no-restore --no-incremental -v minimal` | **0 错误；`Core`/`Syntax`/`LanguageServer` 均 0 警告** |
| VS Code 扩展 TypeScript + esbuild 构建 | `cd vscode-old8lang && npm run compile` | **构建通过**（`out/extension.js` 生成成功） |

## 2. 5 项 P0 验收明细

### P0-5：Parser 位置修复与移除 token-based location workaround
- 修复 `Old8Lang.Core/LangParser/LangToken.cs` 字符串与字符字面量 token 列号落到闭合引号的缺陷（改为起始引号行列）。
- 在 `Old8Lang.Syntax` 补齐 `BlockStatement.EndPosition`、`ClassSyntax.NamePosition`、`EnumInit.NamePosition`、`UsingStatement.VariablePosition`、`ImportStatement.ModuleNamePosition`/`ModuleAliasPosition`/`IsQuotedModuleName`、`ExternFunctionDeclaration.NamePosition`、`NativeStatement.NamePosition`；修复 `ClassParser`（带修饰符/抽象方法 `ClassMemberId` 位置）、`StatementParser`（`for-in`、`try`/`catch`/`finally`、`switch`、`++`/`--`、`extern`/`native`）与 `LangParser.ParseProgram` 位置记录。
- 移除 `SymbolTableBuilder` 中的 `FindSymbolLocationFromTokens`、`FindVariableLocationFromTokens`、`FindColumnInLine`、`FindColumnInLineForVariable` 以及 `DocumentLinkHandler.FindImportRange` 启发式估算。
- 回归测试：`PositionBasisTests` 新增 `SymbolTableBuilder_WithoutTokens_UsesAccurateAstPositions`、`Parser_RecordsBlockEndPositionsAndControlFlowBindingPositions`、`DocumentLinkHandler_UsesExactImportModulePositionFromAst`，全部通过。

### P0-2：符号表作用域链
- 新增 `Old8Lang.LanguageServer/Models/SymbolScope.cs`（覆盖 `Global`/`Class`/`Function`/`Method`/`Block`/`Loop`/`Catch`/`Using`/`Lambda`）并挂载于 `DocumentParseResult.RootScope` 与 `IndexedFile.RootScope`。
- `SymbolTableBuilder` 不再将函数/方法内局部变量污染到顶层 `SymbolTable`，完整遍历普通函数、异步函数、类方法及控制流/Lambda 块，并在各作用域 `AllDeclarations` 保留同作用域重复声明。
- `ScopeAnalyzer`、`SymbolFinder`、`DefinitionHandler`、`ReferencesHandler`、`RenameHandler`、`DocumentHighlightHandler`、`HoverHandler`、`CompletionHandler`、`SemanticTokensHandler`、`MemberChainAnalyzer`、`SemanticAnalyzer` 全面接入作用域链。
- 解除原有跳过用例 `CompletionHandler_GenericsTests.NullableTypeParameter_ShouldComplete` 并通过；新增 `ScopeChainTests` 4 项（局部隔离、变量遮蔽的跳转/悬停/重命名、同名兄弟函数高亮隔离、异步函数及嵌套 `for-in`/`try-catch` 作用域），全部通过。

### P0-3：成员访问类型推断、继承与静态成员补全
- `SymbolInfo` 扩展 `BaseClassName`、`InterfaceNames`、`MixinNames`、`IsInterface`、`IsAbstract`、`IsMixin`、`GenericParameters`、`HasExplicitTypeAnnotation`；类字段支持从初始化表达式推断类型。
- 新增 `Old8Lang.LanguageServer/Services/TypeResolver.cs`，支持多级继承链（防循环继承）、Mixin 与接口成员聚合、访问修饰符上下文判定（外部/子类/本类）与静态/实例区分。
- `MemberChainAnalyzer`、`CompletionHandler`、`SymbolFinder`、`SignatureHelpHandler` 支持 `this.`、`super.`、`ClassName.` 静态访问、多级属性/方法链及跨文件类；彻底清除 `Old8Lang.LanguageServer` 中向 stdout 写日志的 `Console.WriteLine`（0 处残留）。
- 解除原有跳过用例 `CompletionHandler_MemberChainTests.ChainOfPropertyAccess_ShouldComplete` 并通过；新增 `MemberTypeInferenceTests` 4 项（继承/Mixin/静态/`this`/`super`/访问修饰符、属性初始化推断与成员签名帮助、跨文件类补全与悬停、循环继承保护），全部通过。

### P0-4：诊断改善与跨文件语义分析
- 重构 `SemanticAnalyzer` 为 AST + 作用域链 + `WorkspaceIndex` 驱动：
  - 同作用域重复定义（`Error`）与作用域感知未定义符号（`Error`）；
  - 显式类型注解赋值、函数/方法 `return`、调用参数数量与类型不匹配（`Error`，兼容数值宽化 `int -> double`、可空 `T?`、继承子类型、联合/交叉类型、泛型参数）；
  - 未使用局部变量（`Warning`，忽略 `_` 前缀与顶层脚本变量）；
  - 成员不存在、通过类名静态访问实例成员、类外部访问私有成员（`Error`）；
  - 跨文件导入（标准库、本地相对文件、选择性导入、工作区顶层符号）与缺失导出/缺失本地模块诊断。
- `Program.cs` 启动后自动调用 `documentManager.AttachWorkspaceIndex(workspaceIndex)`。
- 回归测试：`SemanticAnalyzerTests` 扩展为 7 项，全面验证上述诊断与无误报兼容性，全部通过。

### P0-1：全部 26 个已注册 Handler 的单元测试与协议级测试
- 补齐 `DebugProfilerHandlersTests`（覆盖 `StartProfiling`/`StopProfiling`/`GetProfilingStatus`/`StartDebugging`/`StopDebugging`/`AddBreakpoint`/`RemoveBreakpoint`/`DebugControl` 全部 8 个自定义 Handler）、`ReferencesHandlerTests`（`ScopedReferencesHandlerTests`）与 `TextDocumentSyncHandlerTests`（真实 `DidOpen`/`DidChange`/`DidSave`/`DidClose` 与 `PublishDiagnostics` 验证）。
- 新增 `Old8Lang.Tests/LanguageServer/Protocol/LspProtocolEndToEndTests.cs`：通过内存双向 `System.IO.Pipelines.Pipe` 直接运行与 `Program.cs` 注册一致的真实 OmniSharp `LanguageServer`，通过标准 `Content-Length` JSON-RPC 协议帧完成 `initialize`/`initialized` 握手并验证全部 **26 个已注册 Handler** 与 `publishDiagnostics` 推送、`didChange`/`didClose`/`shutdown`。

## 3. 结论

`changes/Todo.md` 中「Language Server 与 VS Code 扩展」节全部 5 项 P0 问题已完成实施并通过自动化回归验证。
