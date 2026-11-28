# 实施计划：Language Server 与 VS Code 扩展待办清零

## 阶段划分

1. **阶段 1：文档与方案定型**
   - 建立 `changes/2026-11-27-LSP与VSCode扩展完善/{spec,design,plan,task}.md`。
2. **阶段 2：Token 按行索引 + 实时诊断防抖与后台执行（P2-1、P2-2）**
   - 实现 `TokenLineIndex` 并接入 `DocumentParseResult` 及所有位置查找服务/处理器。
   - 实现 `DiagnosticsScheduler` 与 `DocumentManager` 暂存/按需解析，接入 `TextDocumentSyncHandler` 与 `Program.cs`。
   - 编写单元测试并回归既有 `LanguageServer` 测试。
3. **阶段 3：Call Hierarchy 与 Type Hierarchy（P1-2）**
   - 在 `SymbolTableBuilder` 提取 AST 调用关系。
   - 实现 `CallHierarchyHandler` 与 `TypeHierarchyHandler`，在 `Program.cs` 注册。
   - 补齐单元测试与 `LspProtocolEndToEndTests` 协议测试。
4. **阶段 4：VM `--dap` 模式与 VS Code Debug Adapter（未完成-1）**
   - 增强 `FunctionMetadata` / `BytecodeCompiler` / `VMDebugger` / `VirtualMachine.Tooling.cs` 局部变量名、全局变量快照、按行单步与调用栈读取。
   - 实现 `Old8Lang.App/Dap/` 及 `--dap` CLI 命令。
   - 实现 `vscode-old8lang/src/debugAdapter.ts`，更新 `package.json` 与 `src/extension.ts`。
   - 编写 DAP 进程内协议测试与真实进程测试。
5. **阶段 5：Inline Values 与调试状态映射（P1-1）**
   - 在 `DebugProfilerService` 增加变量快照模型及 `old8lang/updateDebugSnapshot` 自定义请求。
   - 实现 `InlineValuesHandler` 并在 `Program.cs` 注册。
   - 在 `debugAdapter.ts` 接入 `DebugAdapterTracker` 同步暂停变量快照。
   - 补齐单元测试与 `LspProtocolEndToEndTests`。
6. **阶段 6：私有成员补全核验与补全测试补齐（未完成-3、未完成-4）**
   - 核查 `CompletionHandler` / `ScopeAnalyzer` / `TypeResolver` 私有与受保护成员可见性规则并修复边界问题。
   - 补齐私有成员、泛型、表达式、Extern、预处理指令、联合/交叉类型、成员链、类型推断、文档注释、并发与确定性模糊测试。
7. **阶段 7：TextMate 语法改进 + Client↔Server E2E + 性能基线（未完成-2、P2-3、P2-4）**
   - 升级 `vscode-old8lang/syntaxes/old8lang.tmLanguage.json`。
   - 新增 `vscode-old8lang/test/` Node E2E 测试（LSP Client↔Server、DAP、Grammar）并配 `npm test`。
   - 新增 C# 子进程级 `LspClientServerProcessEndToEndTests` 与 `LspPerformanceBaselineTests`（含 `PerformanceSmoke` 与 `Performance` 档）。
8. **阶段 8：全量验证、文档与待办更新**
   - 跑 `LanguageServer` / `VirtualMachine` / `Architecture` 默认档测试与 `performance.runsettings` 性能档测试。
   - 跑 `vscode-old8lang` `npm run compile` 与 `npm test`。
   - 更新 `Docs/DEVELOPER_TOOLS.md`、`Docs/CLI_GUIDE.md`、`Docs/CHANGELOG.md`、`changes/Todo.md` 及本 change 的 `test.md`。
