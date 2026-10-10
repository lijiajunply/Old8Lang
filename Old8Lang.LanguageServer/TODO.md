# Old8Lang Language Server 与 VS Code 扩展路线图

本文档只记录当前仍需完成的工作。已实现的能力以代码和 `Program.cs` 的注册为准，避免把历史任务重新列为待办。

最后核对：2026-10-10

## Language Server 当前状态

### 已实现并已注册

以下 Handler 均位于 `Old8Lang.LanguageServer/Handlers/`，并已在 `Program.cs` 中注册：

| 能力 | 实现 |
| --- | --- |
| 文档同步 | `TextDocumentSyncHandler` |
| 自动补全 | `CompletionHandler` |
| 跳转到定义 | `DefinitionHandler` |
| 查找引用 | `ReferencesHandler` |
| 重命名 | `RenameHandler` |
| 悬停提示 | `HoverHandler` |
| 文档符号 / 大纲 | `DocumentSymbolHandler` |
| 签名帮助 | `SignatureHelpHandler` |
| 文档格式化、范围格式化 | `DocumentFormattingHandler` + `FormattingService` |
| 代码操作 | `CodeActionHandler` |
| 工作区符号 | `WorkspaceSymbolHandler` |
| 文档高亮 | `DocumentHighlightHandler` |
| 折叠范围 | `FoldingRangeHandler` |
| 语义高亮 | `SemanticTokensHandler` |
| 文档链接 | `DocumentLinkHandler` |
| 调试与性能分析扩展请求 | `DebugProfilerHandler.cs` 中的 profiling/debug handlers |

配套能力也已经存在：

- `DocumentManager` 负责文档解析、缓存和诊断推送。
- `SemanticAnalyzer` 已集成未定义符号、重复定义等基础诊断。
- `SymbolTableBuilder` / `SymbolFinder` 支持函数、异步函数、类、变量及类成员的查找。
- token-based location finding 已用于弥补部分 AST 位置问题。

### 尚未实现

这些能力目前没有对应 Handler，也没有在 `Program.cs` 注册：

- Code Lens
- Inline Values
- Selection Range

### 当前依赖限制

项目使用 `OmniSharp.Extensions.LanguageServer` 0.19.9。该版本的协议模型不提供以下 LSP 能力所需的 API：

- Inlay Hint
- Call Hierarchy
- Type Hierarchy

在升级 OmniSharp 或替换 LSP 库前，不安排上述三项的实现。`SymbolInfo` 中的调用关系字段仅作为未来升级后的数据模型预留，当前不表示功能已经可用。

## 真实待办

### P0：可靠性与已有能力完善

- [ ] 为所有已注册 Handler 补齐稳定的单元测试和协议级测试。
- [ ] 解决跨文件索引，支持跨文件查找引用、重命名和工作区符号。
- [ ] 为符号表引入作用域链，正确处理局部变量、变量遮蔽和同名符号。
- [ ] 完善成员访问的类型推断，覆盖继承成员和静态成员补全。
- [ ] 改善诊断：类型不匹配、未使用变量、成员不存在，以及跨文件语义分析。
- [ ] 评估并逐步替换 token-based location workaround；Parser 修复后保留回归测试再移除。

### P1：尚未实现的 LSP 能力

- [ ] 实现 Code Lens（引用计数或其他可验证的代码镜头信息）。
- [ ] 实现 Inline Values，并与可用的调试运行时状态建立协议映射。
- [ ] 实现 Selection Range，至少覆盖 token、表达式、语句块的父级选择范围。

实现新 Handler 时需要同时完成：协议注册、能力声明（如需要）、单元测试、集成测试和文档更新。

### P2：性能与工程质量

- [ ] 缓存未修改文档的符号表，并探索增量更新。
- [ ] 为 token 按行建立索引，降低位置查找的线性扫描成本。
- [ ] 为实时诊断增加防抖和后台执行，避免大文件编辑卡顿。
- [ ] 增加 Language Client ↔ Language Server 的端到端测试。
- [ ] 增加大文件和实时诊断延迟的性能基线。

## VS Code 扩展（`vscode-old8lang/`）

### 当前状态

- 基础扩展、TextMate 语法高亮和 Language Client 已存在。
- Language Client 会启动 `server/Old8Lang.LanguageServer`，并使用 `old8lang.languageServer.path` 覆盖默认路径。
- LSP 已提供的补全、诊断、跳转、引用、重命名、悬停等能力由 VS Code 客户端自动消费。

### 尚未完成

- [ ] 添加 `snippets/old8lang.json`，并在 `package.json` 注册函数、类、条件、循环和异常处理片段。
- [ ] 实现 Debug Adapter；当前扩展没有 `debugAdapter.ts`、调试贡献点或 DAP 配置。
- [ ] 增加任务支持（运行、编译、测试），并在 `package.json` 注册 `taskDefinitions`。
- [ ] 补充编译器路径、运行参数、格式化选项和诊断级别等扩展配置，并做输入校验。
- [ ] 用 `vsce package` 验证打包，补齐跨平台安装文档；是否发布 Marketplace 另行决定。

语法高亮的字符串插值、文档注释和嵌套结构仍可作为独立的 grammar 改进项，但不应再与 Language Server Handler 的实现状态混写。

## 已知限制

- 当前引用查找和重命名主要面向单文档；跨文件能力依赖待实现的 workspace index。
- 符号表仍有部分作用域限制，局部变量同名时可能产生覆盖或误匹配。
- 成员访问的完整类型语义、继承成员和静态成员支持仍不完整。
- 诊断目前不是完整的类型检查器，不能替代编译器的类型验证。
- Language Server 的 profiling/debug 请求是自定义扩展请求，不等同于 VS Code Debug Adapter。

## 验证入口

```bash
# 构建 Language Server
dotnet build Old8Lang.LanguageServer/Old8Lang.LanguageServer.csproj

# 运行 Language Server 单元测试
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --filter "FullyQualifiedName~LanguageServer"

# 编译 VS Code 扩展
cd vscode-old8lang
npm run compile
```

测试结果按仓库约定写入根目录 `Reports/`。新增语言语法时仍须遵循“语法 → 解释器 → IL → VM”的测试顺序。
