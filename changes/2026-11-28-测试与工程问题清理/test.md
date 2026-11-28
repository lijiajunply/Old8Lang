# 测试报告：测试与工程问题全面清理（2026-11-28）

## 1. 测试结论

`changes/Todo.md` 中 `## 测试与工程问题` 全部 **23 项未勾选条目**已全部完成并闭环验证。
解决方案构建 0 错误，架构边界测试、全量默认档单元测试（无挂起、0 失败）、性能档 `SpawnTests`、语法脚本套件与虚拟机脚本套件全部 **100% 通过**。

## 2. 最终实测汇总

| 验证项 | 命令 | 结果 | 耗时 |
|--------|------|------|------|
| 解决方案构建 | `dotnet build Old8Lang.sln --nologo -v minimal` | **0 错误**（52 条既有警告，0 新增） | 5.5 s |
| 架构与边界测试 | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-build --nologo --filter "FullyQualifiedName~Old8Lang.Tests.Architecture"` | **73 通过 / 0 失败 / 0 跳过** | 197 ms |
| 全量默认档单元测试 | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-build --nologo --blame-hang-timeout 30s` | **7,256 通过 / 0 失败 / 26 跳过**（总计 7,282） | 47 s |
| 性能档 `SpawnTests` | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-build --nologo --settings Old8Lang.Tests/performance.runsettings --filter "FullyQualifiedName~SpawnTests"` | **13 通过 / 0 失败 / 0 跳过** | 1 s |
| 语法脚本测试集 | `cd TestFiles && ./run_syntax_tests.sh` | **89 通过 / 0 失败** | - |
| 虚拟机脚本测试集 | `cd TestFiles && ./run_vm_tests.sh` | **79 通过 / 0 失败** | - |

> 对比改造前基线：
> - 全量默认档原在 30s 无进展处被 `PythonExternTests` 永久挂起（中止前 5,123 通过 / 43 失败），现 **7,256 通过 / 0 失败** 稳定完成；
> - Architecture 原 72 通过 / 1 失败，现 **73/73**；
> - `run_syntax_tests.sh` 原 84 通过 / 5 失败，现 **89/89**；
> - `run_vm_tests.sh` 原 78 通过 / 1 失败，现 **79/79**。

## 3. 关键修复与新增回归清单

### 3.1 解释器缺陷与边角缺口
1. **循环内 `return` 死循环**：`WhileStatementRuntimeExtensions`、`ForInStatementRuntimeExtensions`、`InterpreterVisitor.Statements.VisitForStatement` 在循环体执行后补齐 `manager.IsReturn` 检查。
2. **三层及以上继承连续 `super` 栈溢出**：`VariateManager.CurrentExecutingClassName` + `AnyLangValue` + `SuperProxy` 改按当前执行方法声明类逐级向上解析直接父类。
3. **集合/元组字面量 AST 单实例复用**：移除 `InterpreterVisitor.SyntaxAdapters.cs` 的 4 个 `ConditionalWeakTable` 缓存，更新 `ScalarLiteralExpressionTests` 4 项断言为每次求值独立实例。
4. **模块导入作用域隔离**：`ModuleSystemService.CreateIsolatedModuleManager` 在独立模块作用域执行被导入模块，并将顶层函数绑定到所属模块作用域（别名导入完全隔离，普通导入同步注入导出符号保持直接调用与再导出语义）。
5. **`MaxRecursionDepth` 与托管栈容量**：降至 `200` 并加入 `RuntimeHelpers.TryEnsureSufficientExecutionStack()` 守卫，移除 `CircularReferenceTests` 的手动 `RecursionDepth = 990` 规避代码。
6. **边角缺口**：非函数值调用抛 `InvalidOperationError`（并解包 `LazySymbolProxy`）、`EnumLangValue` 六种算术解包、三模式 LINQ 链式 `into ... into ...`、`where T: U` 继承约束回归。
7. 新增回归：`Old8Lang.Tests/Interpreter/EdgeCases/InterpreterDefectsRegressionTests.cs`（9 项全通过）。

### 3.2 IL 与跨模式缺陷
1. **IL 表达式内部 `await` 提升**：新增 `Old8Lang.Compiler/AsyncGeneration/AsyncExpressionAwaitLowerer.cs`，在状态机分析前按从左到右求值顺序提升临时变量并保留原 `AwaitExpression` 实例映射；同步修复 `AwaitExpression` 对非 Task 值类型参数调用 `ToAwaitableTask` 前缺失 `box` 的问题。
2. **IL `object` 操作数动态算术与回写**：新增 `Old8Lang.Runtime/InstanceMethods/Core/IlDynamicArithmetic.cs`，接入 `NumericBinaryOpHelper`（`+ - * / % ^`）与 `TypeInferenceVisitor`，并在 `CompilerVisitor.SetValueToIl.cs`、`CompilerVisitor.Statements.cs`（异步状态机字段）与 `LocalManager.DefineVariable` 补齐 `CoerceDynamic` 写回转换；`ComparisonOpHelper` 补齐 `object` 与数值的动态顺序比较。
3. **IL 值类型变量 `catch` 赋值**：`CatchAssignmentScanner` 扩展覆盖 try 前/try 内与 catch 内均被赋值的变量，`CompilerVisitor.SetValueToIl.cs` 移除 `!valueType.IsValueType` 限制。
4. **三模式线程异常口径统一**：`ThreadLangValue.RethrowThreadException` 解包 `TargetInvocationException` 并以 `ExceptionDispatchInfo` 重抛原始 `Old8Exception` / `LangThrownException`。
5. **并发全局函数 IL 参数审计**：完成 `Old8Lang.Runtime/GlobalFunctions/Implementations/Concurrency/` 全 9 文件 44 处 `int` 参数 `ConcurrencyIlHelpers.EmitLoadIntArgument` 拆箱，并修复 `ChannelTryReceiveFunction` IL 返回未展开 `ChannelReceiveResult` 结构体的缺陷。
6. **既定边界固化**：IL 未定义变量/成员编译期检查与 IL 32 位 `unchecked Int32` 溢出边界写入 `Docs/MODE_SUPPORT.md`；`ArithmeticTests.VeryLargeNumbers_CompilesAndExecutesCorrectly` 解除 `Skip`。
7. 新增/扩展回归：`Old8Lang.Tests/Compiler/Integration/IlAndCrossModeDefectsRegressionTests.cs`、`Old8Lang.Tests/Compiler/Threading/ConcurrencyIdArgumentIlTests.cs`、`Old8Lang.Tests/Compiler/Modules/StandardLibraryImportTests.cs`。

### 3.3 测试隔离、抖动、外部依赖与脚本修复
1. **Python GIL 多线程死锁**：`PythonProvider` 在 `PythonEngine.Initialize()` 后立即调用 `PythonEngine.BeginAllowThreads()`，关闭前调用 `EndAllowThreads`。
2. **全局配置与调用栈并发隔离**：`IlCompilerOptions` 与 `TypeInferenceConfig` 改为 `AsyncLocal` 隔离（`FileHeaderConfigTests` 补 `IDisposable` 复位）；`Old8Exception` 异步调用栈改为不可变持久化链表；`RuntimeGlobals` 改用 `ConcurrentDictionary` 并在 `SpawnFunction.SpawnHelper` 保护写回竞态。
3. **消除外网依赖与抖动**：新增 `LoopbackHttpTestServer` 替代 `NetLibTests`（解释器/IL 两侧）对 `jsonplaceholder.typicode.com` 的外网请求；`SerializationLibTests` 使用独立 GUID 临时路径；`TaskAPITests`、`AsyncStateMachineResumeTests`、`ThreadTests` 改用 `TaskCompletionSource` / `Task.Completed()` / `await` / `Mutex + Join` 消除竞态与计时抖动。
4. **`SpawnTests` 整理**：补 `[Collection("Sequential")]` 与 `try/finally` 恢复 `IlVerificationEnabled`，13 项含 `Sleep` 用例标 `[Trait("Category", "Performance")]`，新增 1 项默认档无 `Sleep` 冒烟用例。
5. **过时测试与脚本收敛**：同步更新 `EdgeCaseTests`、`GenericCollectionTypes*Tests`、`ConditionalImportTests`、`TaskAPITests`（4 处旧 `int()` 误调）、5 个 `TestFiles/SyntaxTests/*.old8`、`TestFiles/VirtualMachine/array_sort_vm_test.old8`，以及 `run_syntax_tests.sh` / `run_vm_tests.sh`（单次预构建 + `--no-build`）。
