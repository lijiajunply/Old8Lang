# 任务清单：测试与工程问题清理

## 阶段 1：基线建档
- [x] 采集 Architecture、全量默认档、`run_syntax_tests.sh`、`run_vm_tests.sh` 基线
- [x] 完成 `spec.md`、`design.md`、`plan.md`、`task.md`

## 阶段 2：解释器缺陷与边角缺口
- [x] 修复 `while` / `for` / `for-in` 内 `return` 死循环（`WhileStatementRuntimeExtensions` 等）
- [x] 修复三层及以上继承连续 `super` 调用栈溢出
- [x] 移除解释器集合/元组字面量 AST 单实例缓存并更新 `ScalarLiteralExpressionTests`
- [x] 隔离 `import "mod" as alias` 与普通导入的模块执行作用域
- [x] 修复 `MaxRecursionDepth` 高于托管栈容量问题（降为 200 并加 `TryEnsureSufficientExecutionStack`）
- [x] 修复非函数值调用、`EnumLangValue` 算术、LINQ 链式 `into`（三模式），核验 `where T: U` 回归

## 阶段 3：IL 与跨模式缺陷
- [x] 实现 IL 异步表达式内部 `await` 提升，修复 `AsyncStateMachine_FindsAwaitInsideSyntaxCollections`
- [x] 实现 IL `object` 操作数动态算术与 `EmitOperationSetValueToIl` / 异步字段写回收敛及动态数值比较
- [x] 修复 IL 值类型变量在 `catch` 中被赋异常丢值
- [x] 统一三模式线程异常 `Join()` / `Result` 口径
- [x] 审计并修复 `Concurrency/` 全部 9 文件 IL `int` 参数拆箱及 `ChannelTryReceive` 结构体展开

## 阶段 4：测试隔离、抖动、脚本与覆盖补齐
- [x] 修复 `PythonProvider` 多线程 GIL 死锁（`BeginAllowThreads` / `EndAllowThreads`）
- [x] `IlCompilerOptions` 与 `TypeInferenceConfig` 改为 `AsyncLocal` 隔离全局状态，并在 `FileHeaderConfigTests` 补 `IDisposable` 复位
- [x] 修复 `Old8Exception` 异步调用栈并发竞态（改为不可变链表）与 `RuntimeGlobals` 并发读写（`ConcurrentDictionary`）
- [x] 修复 `TaskAPITests`、`AsyncStateMachineResumeTests`、`ThreadTests`、`SerializationLibTests`、`NetLibTests`（进程内 `LoopbackHttpTestServer`）
- [x] 整理 `SpawnTests` 状态恢复与性能分档
- [x] 修复 `ConditionalImportTests`、`EdgeCaseTests`、`GenericCollectionTypes*Tests`、`ArithmeticTests`
- [x] 补齐 `Compiler/Modules/StandardLibraryImportTests.cs` 及全部回归测试
- [x] 修复 `TestFiles/SyntaxTests/` 5 项、`TestFiles/VirtualMachine/array_sort_vm_test.old8` 及脚本

## 阶段 5：全量验证与文档更新
- [x] 全量默认档、Architecture、性能档、`run_syntax_tests.sh`、`run_vm_tests.sh` 全部通过
- [x] 更新 `Docs/MODE_SUPPORT.md`、`changes/Todo.md` 与本目录 `test.md`
