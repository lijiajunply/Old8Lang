# 移除 import ... as ... 语法支持

## 1. 概述

根据用户要求，需要移除对 `import ... as ...` 语法的支持，即不再支持将整个模块导入为指定名称的语法，但仍保留命名导入和重命名导入的支持（`import { A as B } from "module"`）。

## 2. 修改内容

### 2.1 语法规则修改

- 修改 `Old8Lang.ebnf` 文件，调整 `importSpecifier` 规则，移除对 `identifier` 作为单独导入指定项的支持
- 保持 `importItem` 规则不变，仍支持命名导入中的重命名

### 2.2 AST 结构修改

- 修改 `ImportStatement` 类，移除 `defaultImport` 参数和属性
- 移除相关的构造函数参数和逻辑

### 2.3 解析器修改

- 修改 `StatementParser.cs` 中的 `ParseImportStatement` 方法，移除处理 `import module as alias` 语法的逻辑
- 移除对 `as` 关键字在默认导入场景下的处理

### 2.4 测试用例修改

- 更新或删除使用 `import ... as ...` 语法的测试用例
- 确保语法测试只包含支持的语法

### 2.5 文档修改

- 更新 `Old8Lang_Grammar.md` 文件，移除 `import ... as ...` 语法的说明
- 调整相关章节，确保文档与实际实现一致

## 3. 实现步骤

1. 修改 `Old8Lang.ebnf` 文件，更新 importStatement 和 importSpecifier 规则
2. 修改 `ImportStatement` 类，移除 defaultImport 属性和相关逻辑
3. 修改 `StatementParser.cs` 中的 `ParseImportStatement` 方法，移除处理 `import module as alias` 语法的逻辑
4. 更新测试用例，移除 `import ... as ...` 语法的测试
5. 更新 `Old8Lang_Grammar.md` 文档
6. 运行测试，确保所有修改正确

## 4. 预期结果

- 不再支持 `import module as alias` 语法
- 不再支持 `import "module" as alias` 语法
- 仍支持 `import { A as B } from "module"` 语法
- 仍支持传统的 `import module` 和 `import "module"` 语法
- 所有现有测试（除了专门测试 `import ... as ...` 语法的测试）都能通过

## 5. 注意事项

- 确保修改不会影响其他导入语法的正常工作
- 确保向后兼容，不破坏现有代码
- 彻底测试所有修改，确保没有引入新的错误