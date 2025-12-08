# Old8Lang解释模式系统性测试计划

## 测试目标
确保Old8Lang解释器能够正确处理所有语法结构，包括基本类型、表达式、语句、函数、类、异常处理和特殊语法。

## 测试范围
根据Old8Lang语法文档，测试将覆盖以下语法结构：

1. **基本数据类型和字面量**
   - 整数、浮点数、字符串、布尔值、字符
   - 数组、列表、字典、元组、范围

2. **表达式**
   - 算术表达式（+、-、*、/、%）
   - 比较表达式（==、!=、<、>、<=、>=）
   - 逻辑表达式（and、or、xor、not）
   - 赋值表达式（<-）
   - 成员访问表达式（.）
   - 索引表达式（[]）
   - 函数调用表达式（()）

3. **语句**
   - 块语句（{}）
   - 变量声明与赋值（:类型 <- 值）
   - 控制流语句（if-elif-else、for、while、for-in、switch）

4. **函数**
   - 函数声明（func）
   - 函数调用
   - Lambda表达式

5. **类**
   - 类声明（class）
   - 类实例化
   - 成员访问（字段和方法）

6. **异常处理**
   - try-catch-finally

7. **特殊语法**
   - 字符串模板（$()）
   - 列表推导式
   - 范围表达式（[1~10]）

## 测试文件结构
将为每种语法结构创建一个单独的测试文件，文件命名遵循`语法类型_test.old8`格式，所有测试文件将放在`InterpreterTests`目录下。

## 测试方法
1. 为每种语法结构创建测试文件
2. 使用解释模式运行测试：`dotnet run --project Old8Lang.App -- -f <测试文件路径>`
3. 验证输出结果是否符合预期
4. 记录测试结果

## 测试文件列表
1. `basic_types_test.old8` - 基本数据类型测试
2. `expressions_test.old8` - 表达式测试
3. `control_flow_test.old8` - 控制流语句测试
4. `functions_test.old8` - 函数测试
5. `lambda_test.old8` - Lambda表达式测试
6. `class_test.old8` - 类测试
7. `exception_test.old8` - 异常处理测试
8. `string_template_test.old8` - 字符串模板测试
9. `list_comprehension_test.old8` - 列表推导式测试
10. `range_test.old8` - 范围表达式测试
11. `complex_expression_test.old8` - 复杂表达式测试
12. `mixed_syntax_test.old8` - 混合语法测试

## 测试执行步骤
1. 创建所有测试文件
2. 依次运行每个测试文件
3. 验证输出结果
4. 生成测试报告

## 预期结果
所有测试文件应能在解释模式下成功执行，输出结果符合预期，无语法错误或运行时错误。