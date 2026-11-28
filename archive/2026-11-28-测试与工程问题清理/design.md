# 设计方案：测试与工程问题清理

## 1. 基线诊断结论（2026-11-28 实测）

首次全量默认档实测（`dotnet test --blame-hang-timeout 30s`）：5123 通过 / 43 失败 / 27 跳过，30 秒无进展中止于 `PythonExternTests.ExecutePython_StandardLibraryModule_WorksCorrectly`。

### 根因定位

1. **全量挂起根因**：`Old8Lang.Runtime/ExternProviders/PythonProvider.cs` 调用 `PythonEngine.Initialize()` 后未调用 `PythonEngine.BeginAllowThreads()`，导致初始化线程永久持有 Python GIL；xUnit 线程池换线程执行下一条 `PythonExternTests` 时在 `using (Py.GIL())` 永久死锁。
2. **43 项失败根因拆解**：
   - 29 项：`FileHeaderConfigTests` 运行 `#!type-inference false` 全局修改 `TypeInferenceConfig.Instance.EnableTypeInference = false` 且未恢复，污染后续 IL 测试；
   - 10 项：`EdgeCaseTests`（9 项，停留在旧 `StringLangValue`/`DoubleLangValue`/`ArrayLangValue` 解析断言，未更新为 `ScalarLiteralExpression`/`ArrayLiteralExpression`）与 `GenericCollectionTypesEdgeCaseTests`（1 项，停留在旧「解释器不校验 `list<int>` 元素类型」断言）；
   - 1 项：`ScalarLiteralExpressionTests.AsyncStateMachine_FindsAwaitInsideSyntaxCollections`；
   - 1 项：`Compiler.Modules.AdvancedImport.ConditionalImportTests.Import_ConditionalWithComplexExpression_ShouldEvaluateExpression`（旧字符串长度比较期望）；
   - 1 项：`Compiler.Threading.ThreadTests.Run_ThreadExecutionOrder_WorksCorrectly`（并发无同步抖动）。
3. **测试脚本基线**：
   - `run_syntax_tests.sh`：5 项真实失败（`test_syntax_structure_error.old8`、`test_lexical_error.old8` 缺末行 `error`；`test_generics.old8` 用 `#` 注释；`test_simple_edge_cases.old8` 与 `test_more_edge_cases.old8` 含非法旧语法）；另脚本对每个文件调用 `dotnet run` 触发重复构建，易与其他任务争抢 MSBuild 锁超时。
   - `run_vm_tests.sh`：仅 `array_sort_vm_test.old8` 1 项真实失败（旧长度排序期望 `a,bb,dd,ccc` 应更新为字典序 `a,bb,ccc,dd`）；`DestructuringAssignmentTest.old8`、`vm_gap_6_error_position.old8`、`legacy/vm_operator_overload.old8`、`legacy/control_flow.old8`、`legacy/vm_decorator.old8` 均已通过。

## 2. 模块与改动设计

### 2.1 解释器缺陷修复（`Old8Lang.Syntax` / `Old8Lang.Runtime` / `Old8Lang.Interpreter`）

| 缺陷 | 受影响文件 | 方案 |
|---|---|---|
| `while` 内 `if return` 死循环 | `Old8Lang.Runtime/Execution/LegacyNodeExtensions/ControlFlow/WhileStatementRuntimeExtensions.cs`（同步排查 `ForStatement`/`ForInStatement`） | 循环体执行后立即检查 `if (manager.IsReturn) break;` |
| 三层继承 `super` 栈溢出 | `Old8Lang.Runtime/Interpreter/VariateManager.cs`、`Old8Lang.Runtime/Execution/Nodes/SuperProxy.cs`、`AnyLangValue.cs`、`ClassInitRuntimeExtensions` | 在 `VariateManager` 上维护当前正在执行的方法所属类元数据（`CurrentExecutingClassName` / `CurrentClassMetadata`），`SuperProxy.GetDirectParentMetadata()` 优先取当前执行类的 `ParentClassName`，调用父类方法时切换为父类元数据并在 `finally` 恢复 |
| 集合/元组字面量 AST 单实例复用 | `Old8Lang.Interpreter/Visitors/InterpreterVisitor.SyntaxAdapters.cs`、`Old8Lang.Tests/Architecture/ScalarLiteralExpressionTests.cs` | 移除 `ArrayLiteralValues`/`ListLiteralValues`/`DictionaryLiteralValues`/`TupleLiteralValues` 四个弱表缓存，每次求值创建独立可变容器实例；更新 `ScalarLiteralExpressionTests` 4 项断言验证「每次求值返回独立实例且内容正确」 |
| 别名/普通导入污染调用者作用域 | `Old8Lang.Runtime/ModuleSystem/Core/ModuleSystemService.cs` | 在独立子作用域（`AddChildren`/`RemoveChildren` 或独立模块作用域）内执行 `import "mod"` / `import "mod" as alias`，只提取本模块新增符号，更新模块内函数的 `CapturedScope` 保留模块顶层变量，调用者作用域只登记模块对象 |
| `MaxRecursionDepth = 1000` 触发 SOE | `Old8Lang.Runtime/Interpreter/VariateManager.cs` | 将 `MaxRecursionDepth` 调整为安全阈值（如 150），并结合 `RuntimeHelpers.TryEnsureSufficientExecutionStack()` 在触达托管栈底前提前抛 `RecursionError` |
| 非函数值调用不报错 | `Old8Lang.Interpreter/Visitors/InterpreterVisitor.Values.cs` | `VisitInstance` 在未命中任何可调用类型（函数、类模板、委托、异步函数、静态方法包装、`NativeAnyLangValue`、`NativeStaticAny` 等）时抛 `InvalidOperationError` |
| LINQ 链式 `into` | `Old8Lang.Syntax/AST/Expressions/Linq/QueryContinuation.cs`、`Old8Lang.Syntax/LangParser/Parsers/LinqParser.cs`、解释器/IL/VM LINQ 执行器 | 为 `QueryContinuation` 增加可选 `Continuation` 字段，解析器在 `select`/`group` 后遇到 `into` 递归解析后续延续，三模式按链依次执行 |
| `EnumLangValue` 算术运算 | `Old8Lang.Runtime/AST/Expression/Value/Special/EnumLangValue.cs`、`IntLangValue.cs`、`DoubleLangValue.cs` | `EnumLangValue` 六种算术方法自动解包右操作数 `EnumLangValue`，`IntLangValue`/`DoubleLangValue` 同样兼容右操作数 `EnumLangValue` |
| `where T: U` 约束 | `Old8Lang.Runtime/TypeSystem/GenericConstraintValidator.cs` | 已有 `ValidateTypeNameConstraint` 兜底，补用户类继承正反回归测试核验 |

### 2.2 IL 与跨模式修复（`Old8Lang.Runtime` / `Old8Lang.Compiler` / `Old8Lang.VirtualMachine`）

1. **表达式内部 `await` 支持（AST 提升）**：
   - 在 `Old8Lang.Compiler/AsyncGeneration/AsyncStateMachineGenerator.cs` 中新增 AST 预降级遍 `LowerExpressionAwaits(BlockStatement)`：当语句中的表达式在非栈空位置包含 `AwaitExpression` 时，按从左到右求值顺序把前序子表达式与 `AwaitExpression` 提升为语句前的临时赋值 `SetStatement(new LangId("__async_tmp_N"), expr)`，**直接复用原始 `AwaitExpression` 实例**。
   - 效果：`1 + await f()`、`PrintLine(await f())`、集合/元组/字典/索引/切片/字符串模板中的 `await` 均可正确编译与运行，且 `ScalarLiteralExpressionTests.AsyncStateMachine_FindsAwaitInsideSyntaxCollections` 原样通过！
   - `catch` 过滤器与 `defer` 中的 `await` 继续给出明确 `CompilerException`。
2. **IL `object` 操作数动态算术 + `EmitOperationSetValueToIl` 写回收敛**：
   - 在 `Old8Lang.Runtime/InstanceMethods/Core/IlDynamicArithmetic.cs`（或 `IlValueBridge`）提供六种运算（`+ - * / % ^`）的运行期语言值分派（自动处理 `int`/`double`/`string`/`char`/`List`/`Array` 及溢出提升，返回原生解包值）。
   - 在 `NumericBinaryOpHelper` 与 `TypeInferenceVisitor` 中：当任一操作数为 `typeof(object)` 时调用 `IlDynamicArithmetic.Evaluate*` 并推断返回 `typeof(object)`。
   - 关键防回归：在 `CompilerVisitor.SetValueToIl.cs` 的 `EmitOperationSetValueToIl` 中补齐与 `EmitDefaultSetValueToIl` 一致的 `CoerceDynamic` 路径——当动态 `object` 结果写回已有具体类型槽（如 `sum <- sum + item` 中的 `int` 槽）时，运行期收敛回槽位类型而不替换 `LocalBuilder`。
3. **IL 值类型变量在 `catch` 中被赋异常**：
   - 更新 `CatchAssignmentScanner`：收集「在 `catch` 块内被赋值，且在 `catch` 块外（try 块内或 try 块外）也被赋值」的所有变量名。
   - 移除 `CompilerVisitor.SetValueToIl.cs` 中的 `!valueType.IsValueType` 限制（同时覆盖 `EmitDefaultSetValueToIl` 与 `EmitOperationSetValueToIl`），使此类变量统一用 `object` 槽位承载（读写装箱/动态算术与比较无缝衔接）。
4. **线程异常消息口径三模式统一**：
   - 统一约定：线程内 `throw <value>` 后，调用 `thread.Join()` / `thread.Result` 在三模式下均重新抛出原始语言错误（`LangThrownException` / `Old8Exception`），被语言层 `try/catch (e)` 捕获后，`e` 一致为原始抛出的值（如 `"Task failed"`），不再额外拼接 `"线程执行异常: CustomError: ..."` 或暴露 CLR 包装类。
   - 修改 `ThreadLangValue.Join` / `ThreadLangValue.Result`、`VMThreadLangValue.Join`（`ResourceManager.JoinThread`）以及 IL 的 `ExceptionWrapper.Bind` / `Spawn` 异常传递路径。
5. **未定义变量/成员与 IL 32 位整数溢出语义定案**：
   - 在 `Docs/MODE_SUPPORT.md` 明确记录：IL 静态编译模式刻意在编译期拒绝未定义变量/成员；IL `int` 算术保持原生 32 位性能语义（超大整数需用 `double` 字面量，如 `2147483647.0`），并更新 `ArithmeticTests.VeryLargeNumbers_CompilesAndExecutesCorrectly` 为清晰验证该行为与 `double` 大数计算（消除跳过或明确回归）。

### 2.3 测试隔离、稳定化与覆盖补齐

1. **进程级状态隔离**：
   - `IlCompilerOptions` 与 `TypeInferenceConfig` 各开关改为 `AsyncLocal` 承载（保持公开静态属性签名 100% 兼容），同时在 `FileHeaderConfigTests` 与 `SpawnTests` 补充 `try/finally` 恢复。
   - `PythonProvider.LoadFunctions` 在 `PythonEngine.Initialize()` 后加线程安全锁并调用 `PythonEngine.BeginAllowThreads()`，彻底消除多线程 GIL 死锁。
2. **消除抖动与外部依赖**：
   - `TaskAPITests.TaskAPI_WhenAny_HandlesFirstCompletedTask` 改用确定性 `TaskCompletionSource` 控制先完成任务。
   - `Compiler/Threading/ThreadTests.Run_ThreadExecutionOrder_WorksCorrectly`（及 Interpreter 同名用例）改用 `Mutex` 同步并 `Join()` 等待全部线程完成。
   - 两组 `SerializationLibTests` 使用各自 `TempDirectory` 下的独立路径并在 `try/finally` 安全清理。
   - 两组 `NetLibTests` 引入本地环回测试 HTTP 服务器（`TcpListener` / `HttpListener` 监听 `127.0.0.1:0`），消除对 `jsonplaceholder.typicode.com` 的外部网络依赖。
   - `SpawnTests` 按 `Docs/TESTING_GUIDE.md` 对使用 `Sleep` 的并发用例标注 `[Trait("Category", "Performance")]`，并确保无 `IlVerificationEnabled` 状态污染。
3. **并发全局函数 IL `int` 拆箱与标准库覆盖**：
   - 审查并修改 `Old8Lang.Runtime/GlobalFunctions/Implementations/Concurrency/` 全部 9 个文件约 40+ 处 `int` 参数改用 `ConcurrencyIlHelpers.EmitLoadIntArgument`。
   - 补齐 `Old8Lang.Tests/Compiler/Modules/StandardLibraryImportTests.cs` 与并发动态实参拆箱测试。
4. **旧测试文件与脚本修复**：
   - 更新 `EdgeCaseTests.cs`（适配 `ScalarLiteralExpression`/`ArrayLiteralExpression`）与 `GenericCollectionTypesEdgeCaseTests.cs`（适配解释器集合元素校验）及 `Compiler.Modules.AdvancedImport.ConditionalImportTests.cs`（字典序期望）。
   - 修复 `TestFiles/SyntaxTests/` 5 个 `.old8` 文件与 `TestFiles/VirtualMachine/array_sort_vm_test.old8`，优化 `run_syntax_tests.sh` 与 `run_vm_tests.sh` 先构建一次再 `--no-build` 运行。
