# 实现新的 import 语法支持

## 1. 修改 EBNF 语法规则

首先修改 `Old8Lang.ebnf` 文件，更新 `importStatement` 规则，支持以下语法：
```
importStatement = "import" ( importSpecifier "from" )? ( identifier | STRING ) ;
importSpecifier = "{" importItem ( "," importItem )* "}" | identifier;
importItem = identifier ( "as" identifier )?;
```

## 2. 更新 AST 结构

修改 `ImportStatement` 类，添加以下属性：
- `ImportSpecifiers`：用于存储导入的指定项（命名导入和重命名导入）
- `FromClause`：用于标记是否使用了 `from` 子句
- `DefaultImport`：用于存储默认导入的名称

## 3. 修改解析器

更新 `ParseImportStatement` 方法，实现以下逻辑：
1. 解析 `import` 关键字
2. 检查是否有导入指定项（`{...}` 或标识符）
3. 如果有导入指定项，解析 `from` 关键字
4. 解析模块名（标识符或字符串）
5. 根据解析结果创建 `ImportStatement` 对象

## 4. 更新解释器实现

修改 `ImportStatement.Run` 方法，支持：
- 命名导入：只导入指定的函数或变量
- 重命名导入：将导入的项重命名为指定名称
- 默认导入：将整个模块导入为指定名称

## 5. 更新编译器实现

修改 `ImportStatement.GenerateIl` 方法，确保在编译模式下也能正确处理新的 import 语法。

## 6. 编写测试用例

创建测试文件，测试以下情况：
- `import { func1, func2 } from "module"`
- `import { A as B } from "module"`
- `import module as B`
- `import { A, B as C } from module`

## 7. 更新文档

更新 `Old8Lang_Grammar.md` 文件，添加新的 import 语法说明。

## 实现步骤

1. 修改 `Old8Lang.ebnf` 语法规则
2. 更新 `ImportStatement` 类
3. 实现新的 `ParseImportStatement` 方法
4. 更新解释器和编译器逻辑
5. 编写测试用例
6. 运行测试，确保所有功能正常工作
7. 更新文档

这个计划将确保 Old8Lang 支持所有要求的 import 语法，同时保持向后兼容。