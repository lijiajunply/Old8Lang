# 需求规格：Language Server 与 VS Code 扩展待办清零

- 日期：2026-11-27
- 来源：`changes/Todo.md`「Language Server 与 VS Code 扩展」节全部未完成项（P1、P2 及「尚未完成」共 10 项）

## 1. 目标与范围

完成 `changes/Todo.md` 中「Language Server 与 VS Code 扩展」的全部剩余条目：

### P1：尚未实现的 LSP 能力
1. **Inline Values (`textDocument/inlineValue`)**：实现标准 LSP 3.17 Inline Values 处理器，并与可用的调试运行时状态建立协议映射（既支持标准 LSP ↔ DAP 的 `InlineValueVariableLookup` / `InlineValueEvaluatableExpression` 映射，也支持通过运行时变量快照返回 `InlineValueText`）。
2. **Call Hierarchy & Type Hierarchy**：基于 OmniSharp 0.19.9 协议 API 实现 `textDocument/prepareCallHierarchy`、`callHierarchy/incomingCalls`、`callHierarchy/outgoingCalls` 及 `textDocument/prepareTypeHierarchy`、`typeHierarchy/supertypes`、`typeHierarchy/subtypes`，支持单文件与跨文件（`WorkspaceIndex`）。
3. 同步完成：协议注册、能力声明、单元测试、协议级集成测试与文档更新。

### P2：性能与工程质量
4. **Token 按行索引**：为 `DocumentParseResult` 建立按行索引结构 `TokenLineIndex`，消除 `SymbolFinder`、`MemberChainAnalyzer`、`SelectionRangeBuilder`、`SignatureHelpHandler`、`InlayHintHandler`、`SemanticTokensHandler` 等路径的全量线性扫描。
5. **实时诊断防抖与后台执行**：对 `didChange` 引入按 URI 的防抖调度与后台任务执行，支持快速连续编辑时的取消合并、按需同步解析（避免补全/悬停读到陈旧文本）、保存即刷新与关闭即清理诊断。
6. **Language Client ↔ Language Server 端到端测试**：新增真实子进程 stdio 通信的 Client ↔ Server E2E 测试（含 Node/TS 扩展端 `vscode-jsonrpc` 测试与 C# 进程级 E2E 测试），覆盖 LSP 核心请求、新增 Hierarchy/InlineValue、防抖诊断及 DAP 调试会话。
7. **大文件与实时诊断延迟性能基线**：新增性能基线与粗阈值烟雾测试（按 `Docs/TESTING_GUIDE.md §1.1` 标 `[Trait("Category", "Performance")]` / `[Trait("Category", "PerformanceSmoke")]`），覆盖 2,000~5,000 行大文件解析、索引查找加速比及实时诊断防抖延迟。

### 尚未完成
8. **Debug Adapter（DAP）**：
   - 运行时侧：在 `Old8Lang.App` 新增 `--dap` 模式，基于 `VirtualMachine` + `VMDebugger` 实现标准 Debug Adapter Protocol 服务端（行断点、函数断点、条件断点、单步/步入/步出/继续/暂停、调用栈、局部变量与全局变量查看、表达式求值、标准输出捕获、终止会话）。
   - 扩展侧：在 `vscode-old8lang` 新增 `src/debugAdapter.ts`，配置 `package.json` 的 `debuggers`/`breakpoints`/`old8lang.debugFile` 贡献点，并通过 `DebugAdapterTracker` 将暂停态变量快照同步到 LSP 以联动 Inline Values。
9. **TextMate 语法高亮改进**：升级 `vscode-old8lang/syntaxes/old8lang.tmLanguage.json`，完善文档注释（`///` + JavaDoc/Sphinx/Google/中文标签 + 行内代码）、字符串插值（转义 `{{`/`}}`、嵌套 `{...}`、格式说明符）、预编译/文件头指令、装饰器与嵌套括号/泛型结构，并补充自动化语法规则验证。
10. **私有成员补全核验与补全测试补齐**：
    - 核验并补齐私有/受保护/公开成员在不同访问位置（本类实例、同类其他实例、子类 `this`/`super`、类外部、静态访问、方法内裸名称）的补全可见性与单元测试。
    - 核查并补齐泛型、表达式、Extern、预处理指令、联合/交叉类型、成员链、类型推断、文档注释的补全测试断言，并新增并发、性能与确定性模糊测试。

## 2. 明确不做的部分

- 不改动 `BytecodeFile` 二进制 `.o8c` 格式版本号（局部变量名调试元数据保存在内存/可选 `DebugInfo` 中，不破坏既有 `.o8c` 序列化测试）。
- 不引入需要真 GUI / Electron 窗口才能运行的 VS Code 集成测试依赖，确保全部测试可在无头 CI 与命令行直接运行。
- 不修改与本任务无关的 IL/VM/解释器既有待办缺陷。

## 3. 验收标准

1. `Old8Lang.sln` 构建零错误；`Old8Lang.Tests` 的 `LanguageServer`、`VirtualMachine`（调试相关）、`Architecture` 全通过。
2. `performance.runsettings` 性能档下新增 LSP 性能基线用例全部通过并记录实测数据。
3. `vscode-old8lang` 的 `npm run compile` 与 `npm test`（含 LSP Client↔Server E2E、DAP E2E、Grammar 验证）全部通过。
4. `Docs/DEVELOPER_TOOLS.md`、`Docs/CLI_GUIDE.md`、`Docs/CHANGELOG.md` 与 `changes/Todo.md` 已同步更新，`test.md` 记录完整测试结果。
