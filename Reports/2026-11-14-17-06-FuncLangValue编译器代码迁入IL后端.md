# 阶段 3.7 FuncLangValue 的 IL lambda 辅助迁入 IL 后端

日期：2026-11-14 17:06  
范围：[plan.md](../Docs/architecture-migration/plan.md) 阶段 3 的运行时值 partial 清理。

## 改动

1. 把 `Execution/Nodes/FuncLangValue.Compiler.cs` 中的 IL 代码迁入
   `Old8Lang.Compiler/Compilers/FuncLangValueIlCompilerExtensions.cs`，改为扩展方法：
   - `BuildLambdaDynamicMethod`、`EmitLambdaClosure`（lambda 方法体与闭包槽位发射）；
   - `GetCapturedLocalValues`、`CapturedLocalValue`、`EmitDefaultReturn`（闭包捕获与默认返回）。
2. Runtime 只保留共享成员，并把文件改名为
   `AST/Expression/Value/Functions/FuncLangValue.Shared.cs`：
   - `GetItemType`（运行时类型推断与 IL 编译共用的返回类型推断）；
   - `ToString` 覆写。
3. 调用方 `CompilerVisitor.Values/SetValueToIl` 的写法不变，扩展方法通过既有
   `using Old8Lang.AST.Expression.Value;` 解析。
4. Runtime 的 `FuncLangValue.Compiler` partial 不再包含 IL 发射代码；lambda IL 逻辑全部
   位于 `Old8Lang.Compiler`。

## 验证

构建（`/usr/local/share/dotnet/dotnet build Old8Lang.sln`）：0 错误。

| 测试组 | 结果 |
| --- | --- |
| Architecture | 55 通过 |
| VirtualMachine | 1083 通过 |
| Interpreter（排除 NetLibTests） | 1959 通过，1 跳过 |
| Compiler.Expressions | 179 通过，41 失败；与 HEAD 基线逐项一致 |
| `FullyQualifiedName~Lambda` | 124 通过，4 失败，1 跳过；与 HEAD 基线完全一致 |

说明：4 项 Lambda 失败均为测试仍使用旧语法（`->` 位置、未类型注解的参数）或既有 IL
契约问题，在未改动的 HEAD 上以同样数量、同样用例失败，不是本子阶段引入。

## 遗留

- `FuncLangValue.Execution`（解释执行 900 行）、`Instance.InstanceMethods`、
  `AnyLangValue.Reflection`、`SuperProxy` 等运行时值 partial 仍待按后端拆分。
- `Compiler/` 引擎其余部分（`Compiler.Compile`、`GlobalVariableAnalysis`、特化/验证辅助）
  仍与运行时值交织；解释与 VM 引擎余量同前，阶段 3 不标记完成。
