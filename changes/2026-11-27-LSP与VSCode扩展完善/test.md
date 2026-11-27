# 测试报告：Language Server 与 VS Code 扩展待办清零

- 测试日期：2026-11-27
- 环境：macOS / .NET SDK 11.0.100（目标框架 `net10.0`）/ Node v26.3.0 / npm 11.16.0

## 1. 测试执行总览

| 测试范围 | 命令 | 通过 | 失败 | 跳过 | 结论 |
|---|---|---:|---:|---:|---|
| `Old8Lang.App` 构建 | `dotnet build Old8Lang.App/Old8Lang.App.csproj --no-restore --nologo -v minimal` | - | 0 错误 / 0 警告 | - | 通过 |
| `LanguageServer` + `VmDapServerTests`（默认档） | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --filter "FullyQualifiedName~Old8Lang.Tests.LanguageServer\|FullyQualifiedName~VmDapServerTests" --nologo -v minimal` | 532 | 0 | 0 | 通过（1s） |
| `VirtualMachine` 全量回归 | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~Old8Lang.Tests.VirtualMachine" --nologo -v minimal` | 1141 | 0 | 0 | 通过（15s） |
| LSP 性能基线（性能档） | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-build --no-restore --settings Old8Lang.Tests/performance.runsettings --filter "FullyQualifiedName~LspPerformanceBaselineTests" --logger "console;verbosity=detailed" --nologo` | 3 | 0 | 0 | 通过（0.99s） |
| `Architecture` 程序集边界 | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~Old8Lang.Tests.Architecture" --nologo -v minimal` | 72 | 1* | 0 | 边界用例全通过（*1 项为既有无关失败，见下节） |
| VS Code 扩展编译与打包 | `cd vscode-old8lang && npm run compile` | - | 0 | - | 通过（30ms，`out/extension.js` 770.3kb） |
| VS Code 扩展无头 E2E & Grammar 测试 | `cd vscode-old8lang && npm test` | 3 组 | 0 | 0 | 全部通过 |

\* `Architecture` 的唯一失败项为 `ScalarLiteralExpressionTests.AsyncStateMachine_FindsAwaitInsideSyntaxCollections`（IL 异步状态机拒绝表达式内部 `await`），属于 `changes/Todo.md` 已登记的既有无关问题；所有项目依赖与程序集边界测试均通过。

## 2. 新增测试覆盖清单

1. **Token 按行索引（P2-1）**：`Old8Lang.Tests/LanguageServer/Services/TokenLineIndexTests.cs`（5 项），验证单调紧凑索引与线性扫描逐 token 一致性、空行/越界回溯、行范围切片、非单调回退与 `Tokens` 重赋失效。
2. **实时诊断防抖与后台执行（P2-2）**：`Old8Lang.Tests/LanguageServer/Services/DiagnosticsSchedulerTests.cs`（3 项），验证 15 次连续快速编辑防抖合并、防抖窗口内 `GetDocument` 按需解析最新文本、关闭文档取消挂起任务并清空诊断。
3. **Call Hierarchy / Type Hierarchy（P1-2）**：
   - `Old8Lang.Tests/LanguageServer/Handlers/Navigation/CallHierarchyHandlerTests.cs`（2 项），覆盖单文件函数、多处调用范围、顶层脚本调用与跨文件类方法调用。
   - `Old8Lang.Tests/LanguageServer/Handlers/Navigation/TypeHierarchyHandlerTests.cs`（1 项），覆盖类 `extends`、接口 `implements`、Mixin `with`、变量位置准备及跨文件 supertypes/subtypes。
4. **Inline Values 与调试变量快照（P1-1）**：`Old8Lang.Tests/LanguageServer/Handlers/Visualization/InlineValuesHandlerTests.cs`（2 项），验证无快照时输出 `InlineValueVariableLookup` / `InlineValueEvaluatableExpression` 及 `StoppedLocation` 截断，有快照时输出 `InlineValueText` 并在恢复后清空回退。
5. **VM DAP 服务端（未完成-1）**：`Old8Lang.Tests/VirtualMachine/Debugger/VmDapServerTests.cs`（2 项），覆盖 `initialize`/`launch`/`setBreakpoints`/条件断点 `i == 3`/`configurationDone`/`stopped`/`threads`/`stackTrace`/`scopes`/`variables`（局部变量 + 全局变量）/`evaluate`/`next`（按行单步）/`continue`/`terminated`/`disconnect`。
6. **私有成员补全与补全深度/并发/模糊测试（未完成-3、未完成-4）**：
   - `Old8Lang.Tests/LanguageServer/Completion/Context/CompletionHandler_PrivateMemberTests.cs`（6 项）
   - `Old8Lang.Tests/LanguageServer/Completion/Features/CompletionHandler_AdvancedCoverageTests.cs`（6 项）
   - `Old8Lang.Tests/LanguageServer/Completion/Core/CompletionHandler_StressAndFuzzTests.cs`（2 项，含 8 线程并发与 60 组确定性模糊用例）
   - 启用 `CompletionHandler_ContextAwareTests` 私有成员隐藏断言。
7. **协议与进程级端到端测试（P2-3）**：
   - 扩充 `Old8Lang.Tests/LanguageServer/Protocol/LspProtocolEndToEndTests.cs`，覆盖全部 30 个已注册 Handler（含 CallHierarchy、TypeHierarchy、InlineValues、`old8lang/updateDebugSnapshot`）。
   - 新增 `Old8Lang.Tests/LanguageServer/Protocol/LspClientServerProcessEndToEndTests.cs`（1 项），启动真实 `Old8Lang.LanguageServer.dll` 子进程通过 OS stdio 验证完整会话。
   - 新增 `vscode-old8lang/test/run-tests.js`（`npm test`），基于 `vscode-jsonrpc` 验证：
     - TextMate grammar 规则（`///` 文档注释、插值 `{{`/`}}` 转义与嵌套花括号、预编译指令、装饰器、嵌套泛型）
     - 真实 `Old8Lang.LanguageServer.dll` stdio Client↔Server E2E（含私有成员过滤、防抖诊断、Call/Type Hierarchy、Inline Values）
     - 真实 `Old8Lang.App.dll --dap` stdio DAP Client↔Server E2E（含断点、局部变量、求值、stdout `output` 事件与 `terminated`）。

## 3. 性能基线实测数据（P2-4）

来源：`LspPerformanceBaselineTests` 在 `performance.runsettings` 下实测输出：

| 场景 | 文档规模 | Token 数 | 解析+语义诊断均值 | 线性查找 500 次 | 按行索引查找 500 次 | 索引加速比 | 补全延迟 |
|---|---|---:|---:|---:|---:|---:|---:|
| 大文件基线（1,000 行） | 1,000 行 | 5,642 | 7.0 ms | 7.54 ms | 0.04 ms | **171.0x** | 7.92 ms |
| 大文件基线（2,000 行） | 2,000 行 | 11,284 | 17.9 ms | 15.17 ms | 0.05 ms | **289.0x** | 0.38 ms |
| 大文件基线（5,000 行） | 5,000 行 | 28,210 | 63.4 ms | 41.18 ms | 0.11 ms | **382.4x** | 1.23 ms |

烟雾测试（默认档）：
- 2,000 行解析 + 1,026 次索引查找：`parse+semantic = 30 ms`，`indexedLookups = 0.35 ms`。
- 1,000 行连续 15 次快速编辑防抖：`scheduleAll = 0.44 ms`（非阻塞），最终稳定发布 `51 ms`（含 40 ms 防抖窗口），发布次数合并为 `1` 次。

## 4. 结论

`changes/Todo.md` 中「Language Server 与 VS Code 扩展」节全部 10 项未完成条目（P1、P2 与「尚未完成」）已全部实现并通过单元、协议、子进程 E2E、性能基线及扩展侧测试验收。
