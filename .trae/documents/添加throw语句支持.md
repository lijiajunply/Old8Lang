# 添加throw语句支持的实现计划

## 1. 修改语法规则
- 在 `Old8Lang.ebnf` 文件的 `statement` 规则中添加 `throwStatement`
- 定义 `throwStatement` 规则：`throwStatement = "throw" expression ;`

## 2. 创建ThrowStatement AST节点
- 在 `AST/Statement` 目录下创建 `ThrowStatement.cs` 文件
- 继承自 `OldStatement` 类
- 包含一个 `Expression` 属性，用于存储要抛出的异常表达式

## 3. 修改解析器
- 在 `LangParser.cs` 的 `ParseStatement` 方法中添加对 `throw` 关键字的处理
- 添加 `ParseThrowStatement` 方法，用于解析throw语句
- 在statement规则的注释中添加throwStatement

## 4. 修改词法分析器
- 确保 `throw` 被识别为关键字

## 5. 修改解释器执行逻辑
- 在执行相关的类中添加对 `ThrowStatement` 的执行逻辑
- 当执行throw语句时，使用 `CustomError` 类抛出相应的异常

## 6. 添加测试用例
- 在 `SyntaxTests` 目录下添加throw语句的语法测试
- 在 `InterpreterTests` 目录下添加throw语句的解释模式测试

## 7. 更新语法文档
- 在 `Old8Lang_Grammar.md` 文件中添加throw语句的语法说明

## 实现步骤
1. 首先修改语法规则文件，添加throw语句的语法定义
2. 创建ThrowStatement AST节点类
3. 修改词法分析器，将throw添加为关键字
4. 修改解析器，添加throw语句的解析逻辑
5. 修改解释器，添加throw语句的执行逻辑，使用CustomError抛出异常
6. 添加语法测试用例，验证语法解析正确性
7. 添加解释模式测试用例，验证执行正确性
8. 更新语法文档

## 预期效果
- 支持 `throw "error message"` 语法
- 支持 `throw expression` 语法，其中expression可以是任意表达式
- 在try-catch块中可以捕获throw抛出的异常
- 支持在函数中throw异常，函数调用者可以捕获
- 语法测试通过
- 解释模式测试通过

## 错误处理
- 当执行throw语句时，将使用 `CustomError` 类创建并抛出异常
- 异常将包含错误信息、位置信息和源代码上下文
- 异常可以被try-catch块捕获和处理