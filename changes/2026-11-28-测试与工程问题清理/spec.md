# 需求规格：测试与工程问题清理

日期：2026-11-28  
范围：`changes/Todo.md` 中「测试与工程问题」章节的全部 23 项未勾选条目（含其中记录的解释器、IL、跨模式、测试稳定性与工程基线问题）。

## 1. 目标

1. **全量默认档测试稳定完成且 0 失败**：`dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj` 不再因 Python.NET GIL 死锁、全局编译配置泄漏或外网限流而挂起/失败。
2. **修复全部可复现的解释器实现缺陷**：
   - `while` 循环内 `if ... return` 死循环（`WhileStatementRuntimeExtensions.RunStandard` 漏检 `manager.IsReturn`）；
   - 三层及以上继承连续 `super` 调用栈溢出；
   - 集合/元组字面量按 AST 节点复用单一可变实例；
   - `import "mod" as alias` 与普通导入未隔离模块作用域；
   - `MaxRecursionDepth = 1000` 高于托管栈导致不可捕获 `StackOverflowException`；
   - 边角缺口：调用非函数值不报错、LINQ 链式 `into ... into ...`、`where T: U` 用户类继承约束回归、`EnumLangValue + EnumLangValue` 算术运算。
3. **修复或明确收敛 IL 与跨模式语义条目**：
   - 支持 IL 异步表达式内部 `await`（AST 临时变量提升，保留原始 `AwaitExpression` 引用），使 `ScalarLiteralExpressionTests.AsyncStateMachine_FindsAwaitInsideSyntaxCollections` 转绿并支持 `1 + await f()` / `PrintLine(await f())` / 集合内 `await`；
   - 支持 IL `object` 操作数动态算术（`func process(v) { return v + 1 }; process(3.5)`），同时在 `EmitOperationSetValueToIl` 补动态结果收敛写回，不破坏循环累加；
   - 修复 IL 值类型变量在 `catch` 中被赋异常丢失成功分支值（`x <- 1; try { ... } catch (e) { x <- e }`）；
   - 统一三模式线程异常 `Thread.Join()` / `Thread.Result` 抛出的原始语言错误值口径；
   - 明确并文档化 IL 编译期未定义变量/成员检查与 IL 32 位定型整数溢出边界（写入 `Docs/MODE_SUPPORT.md`）。
4. **清理全部测试抖动、隔离、分档、脚本与覆盖缺口**：
   - 消除 `TaskAPITests.TaskAPI_WhenAny_HandlesFirstCompletedTask`、`ThreadTests.Run_ThreadExecutionOrder_WorksCorrectly` 抖动；
   - 修复 `SerializationLibTests` 共用临时文件与 `NetLibTests` 依赖外部 `jsonplaceholder.typicode.com`；
   - 整理 `SpawnTests` 的 `IlVerificationEnabled` 恢复与性能分档；
   - 审计并修复 `Concurrency/` 下全部约 40 处 IL `int` 实参拆箱，补齐 `StandardLibraryImportTests.cs` 及并发/端到端回归；
   - 修复 `TestFiles/SyntaxTests/` 5 项失败与 `TestFiles/VirtualMachine/` 失败用例，确保 `./run_syntax_tests.sh` 与 `./run_vm_tests.sh` 全部通过。
5. **更新文档与待办**：勾选 `changes/Todo.md`「测试与工程问题」全部条目，同步校正 `Docs/MODE_SUPPORT.md` 并记录完整测试报告到本目录 `test.md`。

## 2. 约束与不做的事

- 严守 `Old8Lang.Core → Syntax → Runtime → Interpreter/Compiler/VirtualMachine → Hosting` 程序集边界，所有变更后通过 `Old8Lang.Tests.Architecture`。
- 不修改与本任务无关的「VM 性能」「LSP」「长期研究与调试方向」章节内容。
- IL 模式不把静态 `int + int` 退化为装箱 `object`（保持值类型热路径性能）；未定义变量/成员保持 IL 编译期检查语义并在 `Docs/MODE_SUPPORT.md` 明确口径。

## 3. 验收标准

- `dotnet build Old8Lang.sln` 0 错误 0 警告。
- `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --filter "FullyQualifiedName~Old8Lang.Tests.Architecture"` 全部通过。
- `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj`（全量默认档）完整结束、无挂起、0 失败。
- `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --settings Old8Lang.Tests/performance.runsettings --filter "FullyQualifiedName~SpawnTests"` 全部通过。
- `cd TestFiles && ./run_syntax_tests.sh` 89/89 全通过；`./run_vm_tests.sh` 79/79 全通过。
- `changes/Todo.md`「测试与工程问题」章节全部 `[x]`，并落盘 `changes/2026-11-28-测试与工程问题清理/test.md`。
