# 执行清单：Language Server P0 可靠性与能力完善

## 阶段 0：规划与留痕
- [x] 创建 `spec.md`、`design.md`、`plan.md`、`task.md`

## 阶段 1：Parser 位置修复与移除 token 定位变通（P0-5）
- [x] 修复 `LangToken` 字符串/字符字面量起始列号缺陷，并为 `BlockStatement`、`ClassSyntax`、`EnumInit`、`ImportStatement`、`UsingStatement`、`ExternStatement`、`NativeStatement` 补齐精确位置属性
- [x] 修复 `ClassParser`、`StatementParser`、`LangParser` 中的位置记录缺陷
- [x] 移除 `SymbolTableBuilder` 中的 `FindSymbolLocationFromTokens` / `FindVariableLocationFromTokens` / `FindColumnInLine*`
- [x] 更新 `DocumentLinkHandler` 直接使用 AST 的 `ImportStatement` 位置与模块名
- [x] 添加 Parser 与 LSP 精确位置回归测试并通过验证

## 阶段 2：符号表作用域链（P0-2）
- [x] 新增 `SymbolScope` 模型并在 `DocumentParseResult` / `IndexedFile` 暴露 `RootScope`
- [x] 更新 `SymbolTableBuilder` 构建完整词法作用域树（含 async func、类方法、控制流块、循环/catch/using/lambda）且不污染顶层 `SymbolTable`
- [x] 更新 `ScopeAnalyzer`、`SymbolFinder`、`DefinitionHandler`、`HoverHandler`、`ReferencesHandler`、`RenameHandler`、`DocumentHighlightHandler`、`SemanticTokensHandler` 接入作用域链
- [x] 解除 `NullableTypeParameter_ShouldComplete` Skip 并新增作用域/遮蔽/同名符号测试（`ScopeChainTests`）

## 阶段 3：成员访问类型推断、继承与静态补全（P0-3）
- [x] 在 `SymbolInfo` 记录类继承/接口/Mixin 元数据，推断类字段初始值类型
- [x] 新增 `TypeResolver` 处理多级继承、Mixin、接口、静态/实例与可见性成员解析及表达式类型推断
- [x] 升级 `MemberChainAnalyzer`、`CompletionHandler`、`SymbolFinder`、`SignatureHelpHandler` 支持继承、`this`/`super`、静态成员、多级属性/方法链及跨文件类
- [x] 移除 `CompletionHandler` 与 `MemberChainAnalyzer` 中向 stdout 写日志的 `Console.WriteLine`
- [x] 解除 `ChainOfPropertyAccess_ShouldComplete` Skip 并新增继承/静态/链式推断测试（`MemberTypeInferenceTests`）

## 阶段 4：诊断改进与跨文件语义分析（P0-4）
- [x] 重构 `SemanticAnalyzer` 为 AST + 作用域链 + `WorkspaceIndex` 驱动
- [x] 实现同作用域重复定义检测与作用域感知未定义符号检测
- [x] 实现类型不匹配诊断（显式注解赋值、return、函数调用实参）
- [x] 实现未使用局部变量诊断（Warning，忽略 `_` 前缀）
- [x] 实现成员不存在、静态误用、私有成员外部访问诊断
- [x] 实现跨文件导入与工作区符号/类型语义分析并在 `Program.cs` / `DocumentManager` 完成接线
- [x] 新增诊断与跨文件语义分析单元测试（`SemanticAnalyzerTests`）

## 阶段 5：全量 Handler 单测与协议级测试（P0-1）及最终验收
- [x] 补齐 `DebugProfilerHandler`（8 个 Handler）单元测试（`DebugProfilerHandlersTests`）
- [x] 补齐 `ReferencesHandler` 与 `TextDocumentSyncHandler` 完整行为单测
- [x] 新增基于 `System.IO.Pipelines.Pipe` 的进程内 JSON-RPC 协议级测试覆盖全部 26 个注册 Handler（`LspProtocolEndToEndTests`）
- [x] 验证 `vscode-old8lang` 编译构建（`npm run compile`）
- [x] 运行 `LanguageServer`、`Parser`、`Architecture`、`Error`、词法/位置测试并记录 `test.md`
- [x] 更新 `changes/Todo.md` 标记 5 项 P0 完成
