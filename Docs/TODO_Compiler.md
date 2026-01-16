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

- [ ] **优化 `switch` 语句**
  - **状态**：目前实现为 `if-else` 链。可以使用整数/字符串的 `Switch` 指令进行优化。

## 低优先级
- [ ] **支持泛型方法中的 `defer`**
  - **问题**：`GenericMethodSpecializer` 目前没有为 `defer` 将主体包装在 `try-finally` 中。
  - **影响**：`defer` 语句将被忽略或在泛型函数中导致错误。

- [ ] **优化变量访问**
  - **当前**：字典查找。
  - **目标**：数组索引或直接 IL 局部变量映射。

## 已知问题
- 泛型函数调用的隐式类型推断（例如 `identity(1)`）在编译模式下会抛出错误。
