# 重构计划：删除 OperationType 并全面使用 LangTokenType

## 1. 分析映射关系
- 已在 `LangTokenType.cs` 的 `GetGeneric` 方法中定义了 `LangTokenType` 到 `OperationType` 的映射关系
- 需要将所有 `OperationType` 的使用替换为对应的 `LangTokenType` 值

## 2. 修改文件

### 2.1 修改 `Operation.cs`
- 将类中的 `OperationType` 类型替换为 `LangTokenType`
- 更新 `OperaToString` 方法中的条件判断，使用 `LangTokenType` 枚举值
- 更新 `Run` 方法中的所有 `OperationType` 引用
- 更新 `OutputType` 和 `LoadIlValue` 方法中的所有 `OperationType` 引用

### 2.2 修改 `SetStatement.cs`
- 更新 `Run` 方法中的 `OperationType.CONCAT` 引用为 `LangTokenType.Dot`
- 更新 `GenerateIl` 方法中的 `OperationType.CONCAT` 引用为 `LangTokenType.Dot`

### 2.3 修改 `LangParser.cs`
- 将所有 `OperationType` 枚举值替换为对应的 `LangTokenType` 枚举值
- 例如：`OperationType.PLUS` → `LangTokenType.Plus`
- 更新所有 `Operation` 构造函数调用，使用 `LangTokenType` 枚举值

### 2.4 删除 `OperationType.cs`
- 确认所有 `OperationType` 引用都已替换为 `LangTokenType` 后，删除该文件

## 3. 验证修改
- 运行项目构建，确保没有编译错误
- 运行测试，确保功能正常

## 4. 重构步骤
1. 首先修改 `Operation.cs` 文件，将 `OperationType` 替换为 `LangTokenType`
2. 然后修改 `SetStatement.cs` 文件
3. 接着修改 `LangParser.cs` 文件
4. 最后删除 `OperationType.cs` 文件
5. 运行构建和测试，验证重构是否成功