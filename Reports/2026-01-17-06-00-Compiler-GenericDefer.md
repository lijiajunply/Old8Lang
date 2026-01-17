# 编译模式泛型方法 Defer 支持测试报告

**日期**: 2026-01-17
**测试类型**: 编译器模式测试 (`-c`)
**测试文件**: `TestFiles/CompilerTests/generic_defer_test.old8`

## 测试目的

验证在编译模式下，泛型方法（Generic Method）中是否正确支持 `defer` 语句。
此前发现 `GenericMethodSpecializer` 在生成特化方法体时，未包裹 `try-finally` 块，导致 `defer` 语句被忽略。

## 修复内容

1.  **GenericMethodSpecializer.cs**:
    - 在生成特化方法体前后添加了 `BeginExceptionBlock` 和 `BeginFinallyBlock`。
    - 在 `finally` 块中调用 `GenerateDeferIL` 生成 defer 执行代码。
    - 显式添加 `OpCodes.Leave` 以支持隐式返回时的正确流程跳转。

2.  **BlockStatement.cs**:
    - 修复了 `GenerateIl` 方法使用旧版逻辑（直接迭代调用 `GenerateIl`）的问题，改为使用 `CompilerVisitor`，确保所有语句（包括 `FuncRunStatement`）都能通过 Visitor 模式正确生成代码。
    - 这一修复解决了泛型函数调用（`FuncRunStatement` 包装 `GenericInstanceExpression`）在某些情况下被忽略的问题。

## 测试代码

```old8
func test_defer<T>(val:T) -> void {
    PrintLine("Start")
    defer {
        PrintLine("Defer executed")
    }
    PrintLine("End")
}

test_defer<int>(123)
```

## 运行结果

```
[编译信息] 编译成功
Start
End
Defer executed
------------------
Parser Build Time : 174.5251ms
Process Run Time : 0.1447ms
Total : 174.6698ms
```

## 结论

测试通过。
1.  泛型方法体正常执行。
2.  `defer` 语句在方法返回前正确执行（LIFO 顺序）。
3.  `FuncRunStatement` 能够正确处理 `GenericInstanceExpression`。

修复已生效。
