# Old8Lang Language Server 与 VS Code 扩展路线图

本文档只记录当前仍需完成的工作。已实现的能力以代码和 `Program.cs` 的注册为准，避免把历史任务重新列为待办。

最后核对：2026-10-24

## Language Server 当前状态

### 已实现并已注册

以下 Handler 均位于 `Old8Lang.LanguageServer/Handlers/`，并已在 `Program.cs` 中注册：

| 能力 | 实现 |
| --- | --- |
| 文档同步 | `TextDocumentSyncHandler` |
| 自动补全 | `CompletionHandler` |
| 跳转到定义 | `DefinitionHandler`（顶层符号可跨文件） |
| 查找引用 | `ReferencesHandler`（顶层符号可跨文件） |
| 重命名 | `RenameHandler`（顶层符号跨文件） |
| 悬停提示 | `HoverHandler` |
| 文档符号 / 大纲 | `DocumentSymbolHandler` |
| 签名帮助 | `SignatureHelpHandler` |
| 文档格式化、范围格式化 | `DocumentFormattingHandler` + `FormattingService` |
| 代码操作 | `CodeActionHandler` |
| 工作区符号 | `WorkspaceSymbolHandler`（包含未打开的磁盘文件） |
| 文档高亮 | `DocumentHighlightHandler` |
| 折叠范围 | `FoldingRangeHandler` |
| 语义高亮 | `SemanticTokensHandler` |
| 文档链接 | `DocumentLinkHandler` |
| 代码镜头 | `CodeLensHandler`（函数/类/方法引用计数） |
| 选择范围 | `SelectionRangeHandler` |
| 参数名内联提示 | `InlayHintHandler` |
| 调试与性能分析扩展请求 | `DebugProfilerHandler.cs` 中的 profiling/debug handlers |

配套能力：

- `DocumentManager` 负责文档解析、缓存和诊断推送。
- `SemanticAnalyzer` 已集成未定义符号、重复定义等基础诊断。
- `SymbolTableBuilder` / `SymbolFinder` 支持函数、异步函数、类、变量及类成员的查找。
- `WorkspaceIndex` 提供工作区级索引：扫描工作区根目录下的 `*.old8`，按修改时间/长度缓存，
  并用已打开文档覆盖磁盘版本；没有根目录时退化为仅已打开文档。
- token-based location finding 已用于弥补部分 AST 位置问题。

### 尚未实现

- **Inline Values**：需要与暂停中的调试运行时交换变量值。当前运行时没有 DAP 服务器，
  `DebugProfilerService` 只保存会话和事件队列，不提供可引用的变量快照，因此暂不实现。
- **Call Hierarchy / Type Hierarchy**：协议类型在 0.19.9 中已提供（见下），属于后续功能项，
  当前没有对应的数据模型和 UI 验证，暂不实现。
- 完整的作用域链建模：`SymbolTableBuilder` 的顶层符号表已可用，但局部变量遮蔽、
  块级作用域仍未进入模型；引用/重命名对局部符号仍采用文件内 token 匹配。

### 依赖能力核对（2026-10-24）

已直接检查 `OmniSharp.Extensions.LanguageServer 0.19.9` 的程序集，确认以下协议类型存在：

- `InlayHint`、`InlayHintsHandlerBase`、`IInlayHintsHandler`（已用于 `InlayHintHandler`）
- `CallHierarchyItem`、`CallHierarchyPrepareHandlerBase`、`CallHierarchyIncoming/OutgoingHandlerBase`
- `TypeHierarchyItem`、`TypeHierarchyPrepareHandlerBase`、`TypeHierarchySubtypes/SupertypesHandlerBase`

因此此前“0.19.9 不提供 Inlay Hint / Call Hierarchy / Type Hierarchy API”的记录有误，已更正。
`SymbolInfo` 中的 `Calls` / `CalledBy` 字段可以继续作为 Call Hierarchy 的数据模型基础。

## 真实待办

### P0：可靠性与已有能力完善

- [ ] 为所有已注册 Handler 补齐稳定的单元测试和协议级测试（新增的 CodeLens / SelectionRange /
      InlayHint / WorkspaceIndex 已有单元测试，其余 Handler 仍需体系化回归）。
- [x] 解决跨文件索引，支持跨文件查找引用、重命名和工作区符号。
- [ ] 为符号表引入作用域链，正确处理局部变量、变量遮蔽和同名符号。
- [ ] 完善成员访问的类型推断，覆盖继承成员和静态成员补全。
- [ ] 改善诊断：类型不匹配、未使用变量、成员不存在，以及跨文件语义分析。
- [ ] 评估并逐步替换 token-based location workaround；Parser 修复后保留回归测试再移除。

### P1：尚未实现的 LSP 能力

- [x] 实现 Code Lens（引用计数）。
- [ ] 实现 Inline Values，并与可用的调试运行时状态建立协议映射（依赖 DAP 或变量快照）。
- [x] 实现 Selection Range，覆盖 token、括号表达式、语句、声明块和文档。
- [ ] 实现 Call Hierarchy / Type Hierarchy（协议 API 已可用，见上）。

实现新 Handler 时需要同时完成：协议注册、能力声明（如需要）、单元测试、集成测试和文档更新。

### P2：性能与工程质量

- [x] 缓存未修改文档的符号表（`WorkspaceIndex` 按修改时间/长度复用解析结果）。
- [ ] 为 token 按行建立索引，降低位置查找的线性扫描成本。
- [ ] 为实时诊断增加防抖和后台执行，避免大文件编辑卡顿。
- [ ] 增加 Language Client ↔ Language Server 的端到端测试。
- [x] 增加工作区索引的性能基线（2026-10-24 实测：615 个 `.old8` 首次建索引约 1.35s，
      2 秒后的增量重扫约 28ms，命中缓存的引用查找约 3ms）。
- [ ] 增加大文件和实时诊断延迟的性能基线。

## VS Code 扩展（`vscode-old8lang/`）

### 当前状态

- 基础扩展、TextMate 语法高亮和 Language Client 已存在。
- Language Client 会按“可执行文件 → `.dll` → 源码项目 `dotnet run`”的顺序自动探测服务器，
  并支持 `old8lang.languageServer.path` 覆盖默认路径。
- LSP 已提供的补全、诊断、跳转、引用、重命名、悬停、代码镜头、选择范围、内联提示等能力
  由 VS Code 客户端自动消费。
- 代码片段、任务提供器、命令、配置校验和 VSIX 打包均已接入。

### 已完成（0.3.0）

- [x] 添加 `snippets/old8lang.json`，并在 `package.json` 注册函数、类、条件、循环和异常处理片段。
- [x] 增加任务支持（解释 / IL / VM / 语法检查），并在 `package.json` 注册 `taskDefinitions`。
- [x] 补充编译器路径、运行参数、诊断级别等扩展配置，并做输入校验。
- [x] 用 `vsce package` 验证打包（`npm run package:vsix`，产物 `old8lang-0.3.0.vsix`）。

### 尚未完成

- [ ] 实现 Debug Adapter；当前扩展没有 `debugAdapter.ts`、调试贡献点或 DAP 配置。
      运行时目前只有 VM 的交互式终端调试器（`-vm --debug-session`）和 LSP 自定义调试请求，
      需要先确定 DAP 与运行时的对接方式（建议新增一个 `--dap` 模式），再实现适配器。
- [ ] 语法高亮的字符串插值、文档注释和嵌套结构可作为独立的 grammar 改进项。

## 已知限制

- 跨文件索引是 token 级的：若另一个文件自己声明了同名顶层符号，会被视为独立程序并整体跳过，
  避免把互不相关的脚本算进引用。
- 方法、参数等文件内作用域的符号仍只在当前文件内查找引用和重命名。
- 成员访问的完整类型语义、继承成员和静态成员支持仍不完整。
- 诊断目前不是完整的类型检查器，不能替代编译器的类型验证。
- Language Server 的 profiling/debug 请求是自定义扩展请求，不等同于 VS Code Debug Adapter。

## 验证入口

```bash
# 构建 Language Server
dotnet build Old8Lang.LanguageServer/Old8Lang.LanguageServer.csproj

# 运行 Language Server 单元测试
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --filter "FullyQualifiedName~LanguageServer"

# 编译 VS Code 扩展（tsc 类型检查 + esbuild 打包）
cd vscode-old8lang
npm run compile

# 打包 VSIX（会先 dotnet publish Language Server 到 server/）
npm run package:vsix
```

测试结果按仓库约定写入根目录 `Reports/`。新增语言语法时仍须遵循“语法 → 解释器 → IL → VM”的测试顺序。
