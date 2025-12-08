# Old8Lang解释器系统性测试计划

## 测试目标
使用解释模式对Old8Lang的每一种语法结构进行全面测试，确保它们能够正确执行。

## 测试范围
根据Old8Lang.ebnf文件，测试以下语法结构：

### 语句类型
1. lrBlock (括号块)
2. set (赋值语句)
3. ifStatement (if语句)
4. forStatement (for循环)
5. whileStatement (while循环)
6. forInStatement (for-in循环)
7. switchStatement (switch语句)
8. funcDeclaration (函数声明)
9. classDeclaration (类声明)
10. funcRunStatement (函数调用)
11. classFuncRunStatement (类方法调用)
12. importStatement (导入语句)
13. nativeStatement (原生语句)
14. nativeStatic (原生静态)
15. nativeClass (原生类)
16. plusPlus (自增)
17. minusMinus (自减)
18. returnStatement (返回语句)
19. tryStatement (try语句)

### 表达式类型
1. binaryExpression (二元表达式)
2. dotExpr (点表达式)
3. numberOpera1 (加减运算)
4. numberOpera2 (乘除运算)
5. boolOpera (布尔运算)
6. primary (基本表达式)
   - stringLiteral (字符串字面量)
   - doubleLiteral (双精度字面量)
   - intLiteral (整数字面量)
   - charLiteral (字符字面量)
   - identifier (标识符)
   - trueLiteral (true字面量)
   - falseLiteral (false字面量)
   - listInit (列表初始化)
   - instantiate (实例化)
   - stringTree (字符串树)
   - lambda (lambda表达式)
   - list (列表)
   - range (范围)
   - array (数组)
   - tuple (元组)
   - dictionary (字典)
   - slice (切片)
   - asStatement (类型转换)
   - notPrefix (not前缀)
   - minusPrefix (减号前缀)

## 测试文件结构
1. 创建InterpreterTests目录
2. 为每种语法结构创建独立的测试文件
3. 在Old8Lang.XUnitTests项目中创建InterpreterTests类
4. 每个测试方法对应一个测试文件

## 测试实现步骤
1. 创建InterpreterTests目录
2. 为每种语法结构创建.old8测试文件
3. 在测试文件中使用PrintLine函数打印结果，方便验证
4. 创建InterpreterTests类，使用XUnit框架编写测试用例
5. 运行测试，验证每种语法结构的正确性

## 测试文件命名规范
- 语句类型：`test_statement_xxx.old8`
- 表达式类型：`test_expression_xxx.old8`
- 基本类型：`test_primary_xxx.old8`

## 测试执行
使用Visual Studio或dotnet命令行工具运行测试，确保所有测试都能通过。