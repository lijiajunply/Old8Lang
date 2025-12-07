# 记录所有 OldExpr 的 Position 计划

## 问题分析
- OldExpr 类已经定义了 Position 属性，用于存储源代码位置信息
- 所有继承自 OldExpr 的类都接受 SourcePosition 参数，但默认值为 default
- 在 LangParser.cs 文件中，很多地方创建 OldExpr 实例时没有传递 Position 参数，导致使用默认值

## 解决方案
修改 LangParser.cs 文件，确保所有 OldExpr 实例在创建时都传递正确的 Position 参数。

## 实现步骤

### 1. 修改表达式解析方法
- **ParseBinaryExpression**：为每个 Operation 实例传递操作符的 Position
- **ParseDotExpr**：为每个 Operation 实例传递 Dot 操作符的 Position
- **ParseNumberOpera1**：为每个 Operation 实例传递 + 或 - 操作符的 Position
- **ParseNumberOpera2**：为每个 Operation 实例传递 * 或 / 操作符的 Position
- **ParseBoolOpera**：为每个 Operation 实例传递 and/or/xor 操作符的 Position

### 2. 修改 Primary 解析方法
- **ParsePrimary**：为 NOT 操作符的 Operation 实例传递 Not 关键字的 Position
- **ParsePrimary**：为前缀 minus 表达式的 Operation 实例传递 Minus 操作符的 Position

### 3. 修改字面量解析方法
- **ParseStringLiteral**：为 StringValue 实例传递字符串字面量的 Position
- **ParseDoubleLiteral**：为 DoubleValue 实例传递数字字面量的 Position
- **ParseIdentifier**：为 OldId 实例传递标识符的 Position
- **ParseBoolLiteral**：为 BoolValue 实例传递布尔字面量的 Position

### 4. 修改复合表达式解析方法
- **ParseList**：为 ListValue 实例传递 list 关键字的 Position
- **ParseAs**：为 AsValue 实例传递标识符的 Position
- **ParseDictionary**：为 DictionaryValue 和 TupleValue 实例传递相关 token 的 Position
- **ParseArrayOrRange**：为 ArrayValue 和 RangeValue 实例传递相关 token 的 Position
- **ParseLambdaOrTuple**：为 FuncValue 和 TupleValue 实例传递相关 token 的 Position
- **ParseStringTree**：为 StringTreeList 实例传递 $ 符号的 Position
- **ParseInstantiate**：为 Instance 实例传递标识符的 Position
- **ParseListInitOrSlice**：为 RangeValue 和 OldItem 实例传递相关 token 的 Position

### 5. 修改语句解析方法
- **ParseReturnStatement**：为 ReturnStatement 实例传递 Return 关键字的 Position
- **ParseSet**：为 SetStatement 实例传递标识符的 Position
- **ParseCaseBlock**：为 OldCase 实例传递 Case 关键字的 Position

## 技术细节
- 使用 `CurrentToken` 或 `operatorToken` 来获取当前解析位置的 Line 和 Column
- 创建 `SourcePosition` 实例时传递正确的 Line 和 Column 值
- 确保所有 OldExpr 派生类都能正确接收和存储 Position 参数

## 预期效果
- 所有 OldExpr 实例都将包含正确的源代码位置信息
- 有助于调试和错误报告
- 为后续的静态分析和代码生成提供准确的位置信息