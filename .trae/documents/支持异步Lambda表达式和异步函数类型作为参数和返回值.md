# 支持异步Lambda表达式、异步函数类型和await func()语法

## 实现目标
1. 支持异步Lambda表达式，语法：`async () -> expression` 或 `async (params) -> { ... }`
2. 支持异步函数类型作为参数和返回值
3. 支持await func()或await c.func()语法

## 实现步骤

### 1. 修改解析器支持异步Lambda表达式

#### PrimaryParser.cs
- 在`ParseLambdaOrTuple`方法中添加对`async`关键字的支持
- 当检测到`async`关键字时，创建`AsyncFuncLangValue`而不是`FuncLangValue`
- 支持两种异步Lambda形式：
  - 无参数：`async () -> expression` 或 `async () -> { ... }`
  - 有参数：`async (params) -> expression` 或 `async (params) -> { ... }`

### 2. 确保await表达式支持函数调用

#### PrimaryParser.cs
- 检查当前`await`表达式的解析逻辑，确保它能够正确处理`await func()`或`await c.func()`语法
- 确认`ParseExpression()`方法能够正确解析函数调用表达式

### 3. 支持异步函数类型作为参数和返回值

#### 类型系统修改
- 确保函数类型检查能够正确处理异步函数类型
- 支持将异步函数类型作为参数传递给其他函数
- 支持将异步函数类型作为返回值从函数返回

#### 编译模式支持
- 修改`SetValueToIl`方法，支持异步函数类型的IL生成
- 确保异步Lambda表达式在编译模式下能够正确转换为委托

### 4. 测试用例编写

#### 语法测试
- 编写异步Lambda表达式的语法测试用例
- 编写await func()语法的测试用例

#### 解释模式测试
- 编写异步Lambda表达式在解释模式下的运行测试
- 编写异步函数类型作为参数和返回值的测试
- 编写await func()语法的运行测试

#### 编译模式测试
- 编写异步Lambda表达式在编译模式下的运行测试
- 编写await func()语法在编译模式下的运行测试

## 实现细节

### 1. 异步Lambda表达式解析
- 在`ParseLambdaOrTuple`方法开头检查`async`关键字
- 如果检测到`async`，则跳过关键字并创建`AsyncFuncLangValue`
- 否则创建普通的`FuncLangValue`

### 2. await func()语法支持
- 确保`await`关键字后的表达式能够正确解析函数调用
- 检查`AwaitExpression.Run`方法，确保它能够正确处理`TaskLangValue`

### 3. 异步函数类型处理
- 确保`AsyncFuncLangValue`能够正确作为参数传递
- 确保`AsyncFuncLangValue`能够正确作为返回值返回
- 支持异步函数类型的类型注解

### 4. 编译模式支持
- 为异步Lambda表达式生成适当的IL代码
- 支持异步函数委托类型

## 预期效果
- 能够编写和运行异步Lambda表达式
- 能够将异步函数作为参数传递给其他函数
- 能够从函数返回异步函数
- 能够使用await func()或await c.func()语法
- 支持解释模式和编译模式

## 实现文件
- `/Users/luckyfish/Documents/Project/RiderProjects/Old8Lang/Old8Lang/LangParser/Parsers/PrimaryParser.cs` - 修改解析器支持异步Lambda和await func()语法
- `/Users/luckyfish/Documents/Project/RiderProjects/Old8Lang/Old8Lang/AST/Expression/Value/FuncLangValue.cs` - 确保支持异步函数类型作为参数和返回值
- `/Users/luckyfish/Documents/Project/RiderProjects/Old8Lang/Old8Lang/AST/Expression/Value/AsyncFuncLangValue.cs` - 确保异步函数类型能够正确处理

## 测试文件
- 语法测试：`/Users/luckyfish/Documents/Project/RiderProjects/Old8Lang/SyntaxTests/async_lambda_syntax_test.old8`
- 解释模式测试：`/Users/luckyfish/Documents/Project/RiderProjects/Old8Lang/InterpreterTests/async_lambda_test.old8`
- 编译模式测试：`/Users/luckyfish/Documents/Project/RiderProjects/Old8Lang/CompilerTests/async_lambda_test.old8`