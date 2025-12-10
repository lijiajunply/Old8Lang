# 实现try-catch-finally语句的IL生成

## 1. 了解当前实现
- 已经存在`TryStatement`类，包含了try块、catch块列表和finally块
- 解释模式的`Run`方法已经实现
- 编译模式的`GenerateIl`方法为空，需要实现

## 2. 实现思路
- 使用ILGenerator的异常处理API：`BeginExceptionBlock`、`BeginCatchBlock`、`BeginFinallyBlock`和`EndExceptionBlock`
- 为每个catch块生成相应的IL代码，包括异常类型匹配和异常变量处理
- 生成finally块的IL代码
- 确保异常处理流程正确

## 3. 具体实现步骤
1. 在`TryStatement.cs`文件中修改`GenerateIl`方法
2. 使用`BeginExceptionBlock`开始异常处理块
3. 生成try块的IL代码
4. 对于每个catch块：
   - 使用`BeginCatchBlock`定义异常类型
   - 如果有异常变量，将其添加到局部变量管理器
   - 生成catch块的IL代码
5. 如果有finally块：
   - 使用`BeginFinallyBlock`定义finally块
   - 生成finally块的IL代码
6. 使用`EndExceptionBlock`结束异常处理块

## 4. 测试计划
- 使用已有的`CompilerTests/17_exception_handling.old8`测试文件
- 运行编译模式测试，验证try-catch-finally语句的IL生成是否正确
- 检查测试结果，确保所有异常处理场景都能正确执行

## 5. 预期结果
- 编译模式下，try-catch-finally语句能够正确生成IL代码
- 异常能够被正确捕获和处理
- finally块总是被执行
- 嵌套的try-catch-finally语句能够正确工作

## 6. 注意事项
- 确保异常类型匹配逻辑与解释模式一致
- 处理好异常变量的作用域
- 确保IL代码的正确性和完整性
- 测试各种异常处理场景，包括正常执行、抛出异常、捕获异常等