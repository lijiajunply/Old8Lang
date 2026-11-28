# 设计方案：Language Server 与 VS Code 扩展待办清零

## 1. 受影响的程序集与模块

严格遵循程序集分层约束（`Docs/DEVELOPMENT_WORKFLOW.md §4.1`）：
- `Old8Lang.Runtime`：`FunctionMetadata` 新增非序列化运行期调试元数据 `LocalVariableNames`（`Dictionary<int, string>`），不改变 `.o8c` 二进制布局。
- `Old8Lang.VirtualMachine`：
  - `BytecodeCompiler` 在 `DeclareLocalVariable` 时将用户局部变量名写入 `_currentFunction.LocalVariableNames`（跳过 `<...>` / `$...` 编译器临时变量）。
  - `VMDebugger` 扩展按行单步能力（`StepOver(int? currentLine)`、`StepInto(int? currentLine)`、`SetLineBreakpoint` / `ClearLineBreakpoints` / 条件表达式支持），保留旧无参方法签名兼容。
  - `VirtualMachine.Tooling.cs` 增强 `GetFrameVariables(frame)`（返回参数 + 命名局部变量），新增 `GetGlobalVariables()`、`GetCallStackSnapshot()`、`ClearAllBreakpoints()`、`EvaluateSimpleExpression(...)` 等调试辅助方法。
- `Old8Lang.App`：新增 `Dap/` 目录（`DapCommand.cs`、`DapServer.cs`、`DapProtocol.cs`）并在 `Program.cs` 注册 `--dap` 命令。
- `Old8Lang.LanguageServer`：
  - `Models/TokenLineIndex.cs`：按行索引结构。
  - `Models/DocumentParseResult.cs`：挂载 `TokenLineIndex`（与 `Tokens` 同步）。
  - `Models/SymbolInfo.cs`：扩充 `CallSite`（`ReceiverName`、`IsMemberCall`）并在 `SymbolTableBuilder` 构建 AST 调用图与顶层调用点。
  - `Services/DiagnosticsScheduler.cs`：按 URI 防抖与后台诊断调度器，配合 `DocumentManager` 暂存未解析文本及按需刷新。
  - `Services/DebugProfilerService.cs` & `Models/ProfilerModels.cs`：新增调试变量快照存储（`DebugVariableSnapshot`）与 `old8lang/updateDebugSnapshot` 请求。
  - `Handlers/CallHierarchyHandler.cs`、`Handlers/TypeHierarchyHandler.cs`、`Handlers/InlineValuesHandler.cs`：实现三个新标准 LSP 处理器并在 `Program.cs` 注册。
  - `Handlers/CompletionHandler.cs` & `Services/ScopeAnalyzer.cs`：核查并完善私有/受保护成员与外部/裸名称补全可见性。
- `vscode-old8lang`：
  - `src/debugAdapter.ts`：DAP 描述符工厂、调试配置提供器、DAP 通信跟踪器（同步变量快照到 LSP）。
  - `src/extension.ts`：注册调试工厂、`old8lang.debugFile` 命令。
  - `package.json`：贡献 `debuggers`、`breakpoints`、`old8lang.debugFile`、`test` 脚本。
  - `syntaxes/old8lang.tmLanguage.json`：完善插值、文档注释、指令、装饰器与嵌套结构。
  - `test/`：Node 无头 E2E 测试（LSP Client↔Server、DAP Client↔Server、Grammar 验证）。
- `Old8Lang.Tests`：单元测试、协议 E2E 测试、子进程 E2E 测试、DAP 单元测试、补全增强与模糊/并发测试、性能基线测试。

## 2. 关键技术设计

### 2.1 Token 按行索引 (`TokenLineIndex`)
- `DocumentParseResult.Tokens` 改为属性，赋值时按需/自动失效缓存，`TokenIndex` 延迟或即时构建 `TokenLineIndex`。
- `TokenLineIndex` 扫描一次 tokens（O(N)），为连续 1-based 行号构建紧凑 `LineSpan[]`（`StartIndex`, `Count`）；同时保留非单调行号的稀疏回退表以防手工构造测试数据。
- 提供：
  - `FindTokenAtPosition(int lspLine, int lspColumn)` -> `(LangToken Token, int Index)?`（O(1) 定位行 + O(log K) 二分列）。
  - `FindLastTokenAtOrBefore(int lspLine, int lspColumn)` -> `int`（O(1) 定位目标行并向前回溯最近非空行）。
  - `GetTokenRangeForLines(int startLspLine, int endLspLine)` -> `(int Start, int EndExclusive)`（范围查询如 `SemanticTokensRange`、`InlayHint`、`InlineValue` 只遍历可见行）。
- 接入点：`SymbolFinder`、`MemberChainAnalyzer`、`SelectionRangeBuilder`、`SignatureHelpHandler`、`InlayHintHandler`、`SemanticTokensHandler`。

### 2.2 实时诊断防抖与后台执行 (`DiagnosticsScheduler`)
- `DocumentManager` 增加 `StageDocumentText(uri, text)`（O(1) 记录最新版本文本与版本号 `long Version`）与 `EnsureUpToDate(uri)`；当补全/悬停等请求到达而防抖尚未触发时，`GetDocument(uri)` 可立即解析当前暂存文本并缓存结果，防止读到旧文本，同时避免后台任务重复解析同一版本。
- `DiagnosticsScheduler`：
  - 每个 URI 维护独立 `CancellationTokenSource` 和单调递增版本号。
  - `Schedule(uri, text, onPublished)`：取消该 URI 先前的防抖/分析任务，在 `Task.Run` 后台延迟 `DebounceDelay`（生产默认 `150ms`），随后解析并仅在版本仍为最新且文档未关闭时发布诊断。
  - `FlushAsync(uri)`（用于 `didOpen` / `didSave`）：立即取消挂起防抖并执行一次解析发布。
  - `Cancel(uri)`（用于 `didClose`）：取消挂起任务并发布空诊断列表清除编辑器波浪线。
  - 兼容测试：直接 `new TextDocumentSyncHandler(documentManager, facade)` 未注入调度器时默认使用 `DebounceDelay = TimeSpan.Zero` 且等待本次发布完成，保持既有单元测试零改动通过；`Program.cs` 注册异步防抖调度器。

### 2.3 Call Hierarchy 与 Type Hierarchy
- **调用图提取**：在 `SymbolTableBuilder` 遍历函数体/方法体/顶层语句时，扫描表达式内的 `FunctionCallExpression` 与 `Operation(Dot)` 调用点，记录到当前宿主函数/方法的 `SymbolInfo.Calls`（以及 `DocumentParseResult.TopLevelCalls`），每条 `CallSite` 包含被调名称、接收者名称（若有）与精确 0-based `SourceLocation`。
- **CallHierarchyHandler**（实现 `CallHierarchyHandlerBase` 或拆分三个接口 `ICallHierarchyPrepareHandler` / `ICallHierarchyIncomingHandler` / `ICallHierarchyOutgoingHandler`）：
  - `Prepare`：定位函数/方法/构造器符号，返回带 `Data` 元数据的 `CallHierarchyItem`。
  - `IncomingCalls`：遍历当前文档与 `WorkspaceIndex` 全部文件的函数/方法及顶层调用，匹配调用目标并按调用者分组返回 `CallHierarchyIncomingCall`。
  - `OutgoingCalls`：读取目标函数/方法的 `Calls`，按作用域/接收者类型/工作区解析被调符号并按被调者分组返回 `CallHierarchyOutgoingCall`。
- **TypeHierarchyHandler**（实现 `ITypeHierarchyPrepareHandler` / `ITypeHierarchySupertypesHandler` / `ITypeHierarchySubtypesHandler`）：
  - `Prepare`：定位类/接口/Mixin/枚举（或 `this`/`super` 指向的类型）。
  - `Supertypes`：解析 `BaseClassName`、`InterfaceNames`、`MixinNames`（含跨文件）。
  - `Subtypes`：扫描当前文档与 `WorkspaceIndex` 所有类型声明，找出直接继承、实现或混入该类型的所有子类型。

### 2.4 VM DAP (`--dap`) 与 Inline Values 联动
- **DAP 协议传输**：标准 `Content-Length: N\r\n\r\n{json}` 帧读写，基于 `Stream` 实现，便于 `Old8Lang.App --dap` 使用 stdio 以及测试用内存 `Pipe` 驱动。
- **调试会话生命周期**：
  1. `initialize` → 返回能力 + 发送 `initialized` 事件。
  2. `launch` → 记录 `program`、`cwd`、`stopOnEntry`、`noDebug`，解析并编译为 `BytecodeFile`（确保每条指令带行号、函数带 `LocalVariableNames`）。
  3. `setBreakpoints` / `setFunctionBreakpoints` → 映射到 `VirtualMachine.AddBreakpointsAtLine` / `AddFunctionBreakpoint`，返回 `verified: true` 及实际行号；支持条件断点表达式求值。
  4. `configurationDone` → 在后台线程启动 `vm.Execute()`（若 `stopOnEntry` 则首条可执行指令暂停），重定向输出为 DAP `output` 事件。
  5. 暂停时 `DebugPaused` 回调阻塞等待控制信号（`ManualResetEventSlim`），主消息循环处理 `threads` / `stackTrace` / `scopes` / `variables` / `evaluate` / `next` / `stepIn` / `stepOut` / `continue` / `disconnect` / `terminate`。
- **Inline Values 协议映射**：
  - `InlineValuesHandler` 实现 OmniSharp `IInlineValuesHandler`。
  - 确定分析范围：`request.Range` 截断到 `request.Context.StoppedLocation`（只展示已执行到暂停点之前/所在行的可见变量）。
  - 若 `DebugProfilerService` 存在该 URI（及帧）的活跃变量快照（由 DAP Tracker 通过 `old8lang/updateDebugSnapshot` 同步或由 LSP 调试会话提供），则对命中的变量直接返回 `InlineValueText { Text = $"{name} = {value}" }`。
  - 其余可见局部变量/参数返回 `InlineValueVariableLookup { VariableName = name, CaseSensitiveLookup = true }`，成员访问表达式返回 `InlineValueEvaluatableExpression { Expression = "obj.prop" }`，由 VS Code 自动向活跃 DAP 会话求值。

### 2.5 TextMate Grammar 改进
- `///` 文档注释独立规则，早于 `//`，内部高亮 `@param`/`@return`/`@throws`/`:param:`/`:return:`/`Args:`/`Returns:`/`参数:`/`返回:` 等多风格标签与行内反引号代码。
- 字符串插值 `$"..."`：优先匹配 `{{`、`}}` 转义花括号与 `\.` 转义字符；插值 `{...}` 内支持嵌套花括号（字典/列表字面量）与可选格式说明符（如 `:F2`）。
- 补充 `#!directive` 文件头指令、`#if`/`#elif`/`#else`/`#endif`/`#define`/`#undef` 预编译指令、`@decorator` 装饰器、完整类型/运算符/数字字面量（十六进制/二进制）及嵌套泛型/括号作用域。
