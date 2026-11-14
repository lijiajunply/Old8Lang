# 阶段 3.6 DotOperator IL 辅助迁入 IL 后端

日期：2026-11-14 17:03  
范围：[plan.md](../Docs/architecture-migration/plan.md) 阶段 3 的引擎余量。

## 改动

1. 新增 Runtime 共享类型解析 `InstanceMethodTypeResolver`（`Old8Lang.AST.Expression.OperationHelpers`）：
   - `InferInstanceMethodReturnType`、`ResolveInstanceMethod`；
   - `MapToLangValueType`、`TryGetEquivalentLangValueType`。
   IL 发射与 Runtime 类型推断必须使用同一套实例方法解析规则，因此这四段与类型/注册表
   相关的逻辑留在 Runtime，不发射 IL。
2. `DotOperatorILHelper`（约 1400 行 IL 发射，含成员访问、静态类调用、索引/字段/属性访问）
   迁入 `Old8Lang.Compiler/OperationHelpers/`，命名空间不变。
3. `TypeInferenceVisitor` 改为调用 `InstanceMethodTypeResolver.InferInstanceMethodReturnType`；
   `DotOperatorILHelper` 内部改为调用共享 `ResolveInstanceMethod`。

至此 `Old8Lang.Runtime/Execution/OperationHelpers/` 只剩共享的
`InstanceMethodTypeResolver.cs`，全部 IL 运算发射代码位于 `Old8Lang.Compiler`。

## 验证

构建（`/usr/local/share/dotnet/dotnet build Old8Lang.sln`）：0 错误。

| 测试组 | 结果 |
| --- | --- |
| Architecture | 55 通过 |
| VirtualMachine | 1083 通过 |
| Interpreter（排除 NetLibTests） | 1959 通过，1 跳过 |
| Compiler.Expressions | 179 通过，41 失败；与 HEAD 基线逐项一致 |

DotOperator 覆盖的比较、逻辑、成员访问、字段/属性、索引与方法调用用例均包含在
`Compiler.Expressions`；失败集合与迁移前完全相同。

## 遗留

- `Compiler/` 引擎（`Compiler.Compile`、`GlobalVariableAnalysis`、特化/验证辅助）仍与
  运行时值 partial（`FuncLangValue.Compiler` 等）交织；需要先把 partial 改为后端扩展方法。
- 解释与 VM 引擎余量同前；阶段 3 不标记完成。
