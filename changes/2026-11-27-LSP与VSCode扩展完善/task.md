# 执行清单：Language Server 与 VS Code 扩展待办清零

## 阶段 1：方案与规划
- [x] 建立 `spec.md`、`design.md`、`plan.md`、`task.md`

## 阶段 2：Token 按行索引 + 实时诊断防抖（P2-1、P2-2）
- [x] 实现 `TokenLineIndex` 并接入 `DocumentParseResult`
- [x] 将 `SymbolFinder`、`MemberChainAnalyzer`、`SelectionRangeBuilder`、`SignatureHelpHandler`、`InlayHintHandler`、`DocumentHighlightHandler` 迁移到按行索引
- [x] 实现 `DiagnosticsScheduler` 与 `DocumentManager` 暂存/按需刷新机制
- [x] 接入 `TextDocumentSyncHandler` 与 `Program.cs`，补齐防抖、取消与关闭清理测试

## 阶段 3：Call Hierarchy & Type Hierarchy（P1-2）
- [x] 在 `SymbolTableBuilder` 与 `DocumentParseResult`/`WorkspaceIndex` 提取函数/方法/顶层 AST 调用点
- [x] 实现 `CallHierarchyHandler` 并注册到 `Program.cs`
- [x] 实现 `TypeHierarchyHandler` 并注册到 `Program.cs`
- [x] 补齐单元测试与 `LspProtocolEndToEndTests` 覆盖

## 阶段 4：VM `--dap` 模式与 VS Code Debug Adapter（未完成-1）
- [x] 为 VM 补齐局部变量名调试元数据、全局变量快照、按行单步与调用栈帧快照
- [x] 实现 `VmDapServer` 与 `Old8Lang.App` 的 `--dap` 命令注册
- [x] 编写 DAP 协议单元测试与集成测试（`VmDapServerTests`）
- [x] 在 `vscode-old8lang` 实现 `src/debugAdapter.ts`、更新 `package.json` 调试贡献点与 `src/extension.ts`

## 阶段 5：Inline Values 与调试变量映射（P1-1）
- [x] 扩展 `DebugProfilerService` 与 `ProfilerModels` 支持变量快照和 `old8lang/updateDebugSnapshot`
- [x] 实现 `InlineValuesHandler` 并注册到 `Program.cs`
- [x] 在 `vscode-old8lang/src/debugAdapter.ts` 通过 `DebugAdapterTracker` 同步变量快照
- [x] 补齐 `InlineValuesHandlerTests` 与 `LspProtocolEndToEndTests`

## 阶段 6：私有成员补全与补全测试补齐（未完成-3、未完成-4）
- [x] 核查并完善私有/受保护/公开成员在各访问上下文的补全行为及悬空 `.` 符号表容错恢复
- [x] 新增 `CompletionHandler_PrivateMemberTests`
- [x] 强化泛型、表达式、Extern、预处理指令、联合/交叉类型、成员链、类型推断、文档注释补全测试（`CompletionHandler_AdvancedCoverageTests`）
- [x] 新增并发与确定性模糊测试（`CompletionHandler_StressAndFuzzTests`）

## 阶段 7：TextMate 语法改进 + Client↔Server E2E + 性能基线（未完成-2、P2-3、P2-4）
- [x] 改进 `vscode-old8lang/syntaxes/old8lang.tmLanguage.json`（插值、文档注释、指令、装饰器、嵌套结构）
- [x] 新增 `vscode-old8lang/test/run-tests.js` 无头端到端测试（LSP Client↔Server、DAP、Grammar）
- [x] 新增 C# 子进程级 `LspClientServerProcessEndToEndTests`
- [x] 新增 `LspPerformanceBaselineTests`（`PerformanceSmoke` 与 `Performance` 双档）

## 阶段 8：验证与文档
- [x] 运行 `dotnet test`（LanguageServer、DAP、VirtualMachine、Architecture、Performance 档）
- [x] 运行 `vscode-old8lang` `npm run compile` 与 `npm test`
- [x] 更新 `Docs/DEVELOPER_TOOLS.md`、`Docs/CLI_GUIDE.md`、`Docs/CHANGELOG.md`、`changes/Todo.md`
- [x] 编写 `test.md` 测试报告
