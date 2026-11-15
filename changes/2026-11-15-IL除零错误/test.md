# IL 除零错误码统一为 ZERO_DIVISION_ERROR —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL 除零错误被包装成通用 RUNTIME_ERROR」。

## 1. 现象

```old8
a <- 10
b <- 0
c <- a / b
```

| 模式 | 错误 |
|---|---|
| 解释器 | `ZERO_DIVISION_ERROR 除零错误` |
| IL | `RUNTIME_ERROR 除数不能为零` |
| 虚拟机 | `ZERO_DIVISION_ERROR 除零错误` |

`Compiler/Integration/InterpreterIntegrationTests.FullPipeline_RuntimeError_ThrowsAtExecutionStage`
期望 `ZeroDivisionError`，因 IL 包装成通用 `Old8Exception` 而失败（该用例还把
「编译期抛错」与「执行期抛错」写反了）。

## 2. 根因

`Compiler.Compile` 返回的执行委托里 `catch (DivideByZeroException)` 直接抛
`new Old8Exception("RUNTIME_ERROR", "除数不能为零")`，没有使用语言错误类型。

## 3. 修复

1. `Compiler.cs` 的 `DivideByZeroException` 分支改为
   `throw new ZeroDivisionError(new SourcePosition(0, 0))`，与解释器/虚拟机一致；
2. `FullPipeline_RuntimeError_ThrowsAtExecutionStage` 重写为「编译成功、执行阶段抛
   `ZeroDivisionError`」；
3. `FullPipeline_LambdaExecution_WorksCorrectly` 的 lambda 参数补类型注解
   （IL 要求完整注解，属 MODE_SUPPORT 已知限制）。

## 4. 验证

CLI 复核：

| 模式 | `10 / 0` |
|---|---|
| 解释器 | `ZERO_DIVISION_ERROR 除零错误` |
| IL | `ZERO_DIVISION_ERROR 除零错误`（修复前 RUNTIME_ERROR） |
| 虚拟机 | `ZERO_DIVISION_ERROR 除零错误` |

`Compiler.Integration` 2 → 0 失败（53 通过、4 跳过）。整组回归见 `changes/Todo.md`。
