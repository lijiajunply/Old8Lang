# 实施计划：测试与工程问题清理

## 阶段 1：基线建档与方案定稿（当前）
- 完成全量默认档、`Architecture`、`run_syntax_tests.sh`、`run_vm_tests.sh` 基线采集。
- 输出 `spec.md`、`design.md`、`plan.md`、`task.md`。

## 阶段 2：解释器缺陷与边角缺口修复
1. `WhileStatementRuntimeExtensions.RunStandard`（及循环族）检查 `manager.IsReturn`。
2. `SuperProxy` + `VariateManager` 修复三层及以上继承连续 `super` 调用。
3. `InterpreterVisitor.SyntaxAdapters` 移除可变集合/元组字面量实例缓存，更新 `ScalarLiteralExpressionTests`。
4. `ModuleSystemService` 隔离别名/普通本地模块导入作用域。
5. `VariateManager.MaxRecursionDepth` 安全阈值 + `TryEnsureSufficientExecutionStack` 守卫。
6. `VisitInstance` 非函数值调用报错、`EnumLangValue` 算术解包、LINQ 链式 `into`（三模式）、`where T: U` 回归。
- 阶段验证：跑相关解释器/Architecture 回归测试。

## 阶段 3：IL 与跨模式缺陷修复
1. `AsyncStateMachineGenerator` AST 表达式内 `await` 提升，验证 `ScalarLiteralExpressionTests` 与新回归。
2. `IlDynamicArithmetic` + `NumericBinaryOpHelper` + `TypeInferenceVisitor` + `EmitOperationSetValueToIl` 动态写回收敛。
3. `CatchAssignmentScanner` + `CompilerVisitor.SetValueToIl` 值类型 catch 变量保留。
4. 三模式 `Thread.Join()` / `Thread.Result` 异常统一。
5. `Concurrency/` 全部 9 文件约 40+ 处 `int` 参数改用 `ConcurrencyIlHelpers.EmitLoadIntArgument`。
- 阶段验证：跑 `Compiler` 各主组、跨模式与 `Architecture` 测试。

## 阶段 4：测试隔离、抖动、脚本与覆盖补齐
1. `PythonProvider.BeginAllowThreads()` 消除 GIL 死锁；`IlCompilerOptions` & `TypeInferenceConfig` 改为 `AsyncLocal`。
2. 修复 `TaskAPITests`、`ThreadTests`、`SerializationLibTests`、`NetLibTests`（本地环回 HTTP 服务）、`SpawnTests` 分档与恢复、`ConditionalImportTests`、`EdgeCaseTests`、`GenericCollectionTypesEdgeCaseTests`。
3. 补齐 `Old8Lang.Tests/Compiler/Modules/StandardLibraryImportTests.cs` 及并发动态实参回归。
4. 修复 `TestFiles/SyntaxTests/` 5 个用例、`TestFiles/VirtualMachine/array_sort_vm_test.old8`，优化测试脚本预构建。
- 阶段验证：跑 `./run_syntax_tests.sh` 与 `./run_vm_tests.sh`。

## 阶段 5：全量验证、文档与待办收敛
1. `dotnet build Old8Lang.sln`（0 警告 0 错误）。
2. `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --filter "FullyQualifiedName~Old8Lang.Tests.Architecture"`。
3. `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj`（全量默认档）完整通过 0 失败。
4. 性能档复核 `SpawnTests`。
5. 更新 `Docs/MODE_SUPPORT.md`、`changes/Todo.md` 与 `changes/2026-11-28-测试与工程问题清理/test.md`。
