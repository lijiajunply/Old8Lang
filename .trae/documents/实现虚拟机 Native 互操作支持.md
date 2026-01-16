我已完成对现有代码库的分析，并制定了实现虚拟机原生互操作（Native Interoperability）的详细计划。

该计划将启用以下功能：
1.  **C# DLL 导入**：支持 `native` 语句，允许导入 C# 类库、单个方法或批量导入。
2.  **多语言互操作**：支持 Python (`pythonnet`) 和 JavaScript (`Jint`) 的调用。
3.  **虚拟机指令扩展**：添加新的字节码指令以支持上述功能。

## 计划步骤

### 1. 扩展字节码指令 (OpCode)
-   修改 `Old8Lang/Bytecode/OpCode.cs`
-   添加 `ImportNative` (0xC7) 指令，用于处理 `native` 语句的导入逻辑（类、方法、批量导入）。

### 2. 增强 ExternFunctionWrapper
-   修改 `Old8Lang/Bytecode/ExternFunctionWrapper.cs`
-   实现 `InvokeCSharpDll`：使用反射加载和调用 C# 方法。
-   实现 `InvokePythonScript` / `InvokePythonModule`：集成 Python.NET 支持。
-   实现 `InvokeJavaScript`：集成 Jint 支持。

### 3. 实现虚拟机处理逻辑
-   修改 `Old8Lang/Bytecode/VirtualMachine.cs`
-   实现 `ImportNative` 指令的处理逻辑：
    -   运行时加载 DLL。
    -   解析类和方法。
    -   将导入的函数/类注册到全局变量中。

### 4. 实现字节码生成逻辑
-   修改 `Old8Lang/AST/Visitor/BytecodeVisitor.Statements.cs`
-   实现 `VisitNativeStatement` 方法：
    -   将 `NativeStatement` 编译为 `ImportNative` 指令。
    -   支持单个方法导入、批量导入 (`*`) 和类导入模式。

### 5. 验证与测试
-   创建测试文件 `Old8Lang.Tests/VirtualMachine/NativeImportTest.old8`。
-   测试 `native "Old8LangLib" MathLib *` 等语法在虚拟机模式下的运行情况。
-   验证 C# 互操作功能的正确性。

确认后，我将开始执行此计划。