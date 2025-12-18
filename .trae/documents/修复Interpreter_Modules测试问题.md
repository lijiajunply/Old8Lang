## 问题分析

通过运行测试和查看代码，我发现`ImportTests`测试失败的主要原因是导入语句解析器不支持某些高级导入语法，特别是：

1. **模块别名语法**：如 `import "math" as m` - 这是导致大部分测试失败的主要原因
2. **其他高级导入语法**：条件导入、动态导入等

## 修复方案

### 1. 修改导入语句解析器

需要扩展 `StatementParser.ParseImportStatement()` 方法，添加对模块别名语法的支持：

- 在解析完模块名后，检查是否有 `as` 关键字
- 如果有 `as` 关键字，解析别名并将其添加到导入语句中

### 2. 更新导入语句AST结构

检查 `ImportStatement` 类，确保它支持模块别名：

- 可能需要添加 `Alias` 属性
- 确保导入语句执行时正确处理别名

### 3. 修复导入路径处理

确保导入语句能够正确处理相对路径和各种模块名格式。

### 4. 修复条件导入支持

确保导入语句可以在条件语句（如 if）中正常工作。

## 修复步骤

1. 查看 `ImportStatement` 类的定义，了解其结构
2. 修改 `StatementParser.ParseImportStatement()` 方法，添加对 `as` 别名语法的支持
3. 更新 `ImportStatement` 类，添加别名支持
4. 运行测试，验证修复效果
5. 根据需要进一步调整和修复其他问题

## 预期结果

修复后，所有 `ImportTests` 测试应该能够通过，特别是：
- `Import_WithAlias_ImportsModuleWithAlias`
- `Import_NamespaceImport_ImportsUnderNamespace`
- `Import_DynamicImport_ImportsModuleDynamically`
- `Import_ReimportSameModule_HandlesReimporting`
- 其他相关测试