# 阶段 3.8 LangInterpreter 迁入解释后端与共享契约

日期：2026-11-14 17:09  
范围：[plan.md](../Docs/architecture-migration/plan.md) 阶段 3 的解释器引擎余量。

## 改动

1. `LangInterpreter` 从 `Old8Lang.Runtime/Interpreter/` 迁入 `Old8Lang.Interpreter` 程序集，
   命名空间不变；`Manager`/`TypeAnnotationManager` 由公开只读字段改为只读属性，以满足接口实现。
2. Runtime 新增最小共享契约 `Old8Lang.Interpreter.ILangInterpreter`：
   - `Manager`、`TypeAnnotationManager`、`PerformanceMonitor`、`ModuleCache`、`OutputProvider`；
   - `Build(string, string?)`、`Build(string, string?, PreprocessorSymbols?)`。
3. Runtime 中所有需要解释器状态的类型改为依赖契约：
   - `VariateManager.Interpreter`、`AnyLangValue.Init`、`GenericTypeResolver`；
   - `LocalManager.Interpreter`、`BytecodeCompiler.Interpreter`、`Compiler.Compile` 与
     `GlobalVariableAnalysis`；
   - `DebuggableInterpreter`、反射全局函数与 `TypeValue` 实例方法的 `typeof(...)` 调用。
4. Runtime 的字节码模块加载器不再 `new LangInterpreter(...)`，改用
   `RuntimeVisitors.CreateInterpreterHost(standardLibraries)`；解释后端在
   `InterpreterBackend.Register()` 中注册宿主工厂。不经过 Hosting 的宿主必须显式注册。
5. 架构门禁更新：
   - `LangInterpreter` 必须位于 `Old8Lang.Interpreter`，`ILangInterpreter` 位于 Runtime；
   - Runtime 程序集不引用解释后端；
   - `ModuleCompositionTests` 的「核心程序集不引用官方扩展」改为检查 `VariateManager` 所在程序集。

## 验证

构建（`/usr/local/share/dotnet/dotnet build Old8Lang.sln`）：0 错误。

| 测试组 | 结果 |
| --- | --- |
| Architecture | 56 通过 |
| VirtualMachine | 1083 通过 |
| Interpreter（排除 NetLibTests） | 1959 通过，1 跳过 |
| Compiler.Expressions | 179 通过，41 失败；与 HEAD 基线逐项一致 |

解释器的模块加载、`Build`、输出提供者与类型注解路径均由 Interpreter 组覆盖；VM 组覆盖
字节码模块加载器通过宿主工厂创建解释器的路径。

## 遗留

- `VariateManager`、`ScopeLayer`、`CapturedScope`、`SymbolTableCache` 等解释期状态仍在 Runtime：
  运行时值的 `Dot`/`Run` 与实例方法直接依赖它们，需要更大的接口化改造才能迁出。
- `FuncLangValue.Execution`、`Instance.InstanceMethods`、`AnyLangValue.Reflection`、`SuperProxy`
  等运行时值 partial 仍与解释执行交织。
- `Compiler/` 与 `Bytecode/` 引擎余量同前；阶段 3 不标记完成。
