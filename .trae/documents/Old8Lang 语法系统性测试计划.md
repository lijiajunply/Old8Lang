# Old8Lang 语法系统性测试计划

## 测试目标
系统性测试 Old8Lang 的所有语法结构，确保其正确性和完整性。

## 测试范围
根据语法文档，测试以下语法结构：

### 1. 基本语法结构
- [x] 标识符和关键字
- [x] 字面量（整数、浮点数、字符串、字符、布尔值）
- [x] 数据类型（基本类型和复合类型）

### 2. 表达式
- [x] 算术表达式
- [x] 比较表达式
- [x] 逻辑表达式
- [x] 赋值表达式
- [x] 成员访问表达式
- [x] 索引表达式
- [x] 函数调用表达式

### 3. 语句
- [x] 块语句
- [x] 变量声明与赋值
- [x] 控制流语句（if-elif-else、for、while、switch-case-default、for-in）
- [x] 函数声明与调用
- [x] Lambda 表达式
- [x] 类声明与实例化
- [x] 异常处理（try-catch-finally）
- [x] 导入语句

### 4. 特殊语法
- [x] 字符串模板
- [x] 列表推导式
- [x] 范围表达式

## 测试文件结构

创建 `SyntaxTests` 目录，并在其中创建以下测试文件：

1. `basic_syntax.old8` - 测试基本语法结构
2. `expressions.old8` - 测试所有类型的表达式
3. `control_flow.old8` - 测试控制流语句
4. `functions.old8` - 测试函数声明与调用
5. `lambda.old8` - 测试 Lambda 表达式
6. `classes.old8` - 测试类声明与实例化
7. `exception_handling.old8` - 测试异常处理
8. `import_statements.old8` - 测试导入语句
9. `string_templates.old8` - 测试字符串模板
10. `list_comprehensions.old8` - 测试列表推导式
11. `range_expressions.old8` - 测试范围表达式
12. `complex_syntax.old8` - 测试复杂语法组合

## 测试方法

1. 创建上述测试文件，每个文件包含对应语法结构的全面测试用例
2. 使用 Old8Lang.App 进行语法测试：
   ```bash
   dotnet run --project Old8Lang.App -- -s <test-file.old8>
   ```
3. 记录测试结果，确保所有语法结构都能正确解析

## 预期结果

所有测试文件都能通过语法测试，没有语法错误。