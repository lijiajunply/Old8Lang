# 编译器待办事项清单

## 高优先级
- [x] **修复 `if` 块中的 `return` 语句**
  - **问题**：如果未正确使用 `Ret` 处理，`if` 块中的 `return` 可能会导致“无效程序”或堆栈不平衡。
  - **状态**：✅ 已修复。
  - **详情**：
    - 在 `Compiler.cs` 中添加了主程序返回标签 `ReturnLabel`。
    - 修复了 `ReturnStatement.cs`，确保在 `try-catch-finally` 块（以及顶层代码）中使用 `Leave` 指令代替 `Ret` 指令。
    - 修复了当返回值局部变量为空但表达式产生值时的堆栈平衡问题（通过 `Pop` 指令）。

- [x] **实现 `try-catch-finally`**
  - **状态**：✅ 已完成。
  - **详情**：
    - 在 `CompilerVisitor` 中完整实现了 `TryStatement` 和 `ThrowStatement` 的 IL 生成。
    - 支持多个 `catch` 块和类型匹配。
    - 支持异常过滤器 (`where` 子句)，通过在 catch 块内部进行条件检查和跳转实现。
    - 修复了 `ExceptionHelper` 以正确处理异常类型匹配。
    - 支持 `finally` 块的正确执行。
  - **参考**：`TryStatement.cs`, `ThrowStatement.cs`。

## 中优先级
- [x] **实现泛型函数支持**
  - **状态**：✅ 已完成。
  - **详情**：
    - 修复 `FuncInit` 以延迟泛型函数的代码生成。
    - 修复 `GenericMethodSpecializer` 以处理 `ReturnValueLocal` 和 `ReturnLabel`。
    - 已验证显式类型参数（例如 `func<int>()`）。
    - *注意*：编译模式下尚不支持泛型函数的隐式类型推断。

- [x] **实现泛型类支持**
  - **状态**：✅ 已完成。
  - **详情**：
    - 修复 `FuncInit`、`GenericMethodSpecializer` 和 `GenericClassSpecializer` 以传播 `GenericClasses` 上下文。
    - 在 `GenericInstanceExpression` 中实现了泛型类实例化的 `init` 方法调用。
    - 已验证基本泛型类（例如 `Box<T>`）。
    - *注意*：具有多个参数的复杂泛型类可能仍有问题。

- [x] **优化 `switch` 语句**
  - **状态**：✅ 已完成（整数优化）。
  - **详情**：
    - 在 `SwitchStatement.cs` 中实现了 `TryGenerateIntSwitch` 方法。
    - 对于整数类型的 switch，当 case 值足够密集时（密度 > 0.5 且范围 < 2048），使用 IL `Switch` 指令（跳转表）代替 `if-else` 链。
    - 字符串优化暂时保留为未来工作（目前使用 `string.Equals`）。

## 低优先级
- [x] **支持泛型方法中的 `defer`**
  - **状态**：✅ 已完成。
  - **详情**：
    - 在 `GenericMethodSpecializer` 中添加了 `try-finally` 块生成逻辑。
    - 修复了 `BlockStatement` 以使用 `CompilerVisitor`，确保泛型函数调用（`FuncRunStatement`）被正确处理。
  - **参考**：`GenericMethodSpecializer.cs`

- [x] **优化变量访问**
  - **当前**：直接 IL 参数访问 (`Ldarg`/`Starg`)。
  - **目标**：数组索引或直接 IL 局部变量映射。
  - **状态**：✅ 已完成。
  - **详情**：
    - 在 `LocalManager` 中添加了 `ArgumentIndices` 映射。
    - 修改 `FuncInit` 不再将参数复制到局部变量，而是注册到 `ArgumentIndices`。
    - 修改 `LangId` 和 `LangExpression` (`SetValueToIl`) 以优先检查 `ArgumentIndices` 并使用 `Ldarg`/`Starg` 指令。
    - 修复了 `LangId` 中对 `this` 的处理。
    - 更新了 `CompilerVisitor` 以保持逻辑一致。

## 已知问题
- 泛型函数调用的隐式类型推断（例如 `identity(1)`）在编译模式下会抛出错误。
