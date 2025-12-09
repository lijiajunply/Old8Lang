# 实现 break 和 continue 语句支持

## 1. 添加 Token 类型

* 在 `LangTokenType` 枚举中添加 `Break` 和 `Continue` 类型

* 在 `KeywordType` 枚举中添加对应的关键字类型

## 2. 更新语法规则

* 在 `Old8Lang.ebnf` 中扩展 `statement` 定义，添加 `breakStatement` 和 `continueStatement`

* 定义 `breakStatement` 和 `continueStatement` 规则

## 3. 创建语句类

* 创建 `BreakStatement` 类，继承自 `OldStatement`

* 创建 `ContinueStatement` 类，继承自 `OldStatement`

* 实现 Run 和 GenerateIl 方法

## 4. 修改解析器

* 在 `LangParser.cs` 中添加对 break 和 continue 关键字的识别

* 添加解析 break 和 continue 语句的方法

## 5. 更新循环语句的解释器实现

* 修改 `ForStatement.Run` 方法，支持 break 和 continue

* 修改 `WhileStatement.Run` 方法，支持 break 和 continue

* 修改 `ForInStatement.Run` 方法，支持 break 和 continue

* 使用异常或特殊返回值来处理跳转

## 6. 更新循环语句的编译器实现

* 修改 `ForStatement.GenerateIl` 方法，添加 break 和 continue 标签

* 修改 `WhileStatement.GenerateIl` 方法，添加 break 和 continue 标签

* 修改 `ForInStatement.GenerateIl` 方法，添加 break 和 continue 标签

## 7. 添加测试用例

* 创建 `break_continue_test.old8` 测试文件

* 测试 break 在 for、while 和 for-in 循环中的功能

* 测试 continue 在 for、while 和 for-in 循环中的功能

## 8. 运行测试验证

* 运行语法测试确保解析正确

* 运行解释模式测试确保功能正常

* 运行编译模式测试确保编译正确

