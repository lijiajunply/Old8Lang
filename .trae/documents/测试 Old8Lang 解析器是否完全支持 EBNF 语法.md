# 分析 OldParser.cs 并提取 EBNF 规则

## 1. 提取 EBNF 规则

我将从 OldParser.cs 文件中提取所有语法规则，包括：

### 1.1 根规则
- `root = statement*`

### 1.2 语句规则
- `statement = lrBlock | set | arraySetTree | classSetTree | returnTree | ifStatement | forStatement | whileStatement | forInStatement | switchStatement | funcDeclaration | classDeclaration | funcRunStatement | classFuncRunStatement | importStatement | nativeStatement | nativeStatic | nativeClass | plusPlus | minusMinus`

### 1.3 表达式规则
- `expression = binaryExpression | dotExpr | numberOpera1 | numberOpera2 | boolOpera | notBool | minusPrefix | primary`

### 1.4 基本表达式规则
- `primary = stringLiteral | intLiteral | charLiteral | doubleLiteral | ident | trueLiteral | falseLiteral | listInit | instantiate | stringTree | lambda | list | range | array | tuple | dictionary | slice | asStatement`

### 1.5 详细规则提取
我将详细提取每个规则的具体定义，包括操作符优先级、结合性等。

## 2. 与当前 EBNF 比较

我将把提取的 EBNF 规则与当前的 EBNF 语法结构进行比较，分析它们的异同点，包括：

### 2.1 语法结构比较
- 比较根规则、语句规则、表达式规则等的结构
- 比较各种语句类型的定义
- 比较各种表达式类型的定义

### 2.2 操作符比较
- 比较操作符的优先级
- 比较操作符的结合性
- 比较支持的操作符类型

### 2.3 数据类型比较
- 比较支持的数据类型
- 比较复合数据类型的定义

### 2.4 其他特性比较
- 比较函数定义和调用
- 比较类定义和方法调用
- 比较导入语句
- 比较字符串模板
- 比较 Lambda 表达式

## 3. 生成比较报告

我将生成一个详细的比较报告，包括：

### 3.1 相同点
- 列出两者共有的语法规则
- 列出两者共有的特性

### 3.2 不同点
- 列出 OldParser.cs 支持但当前 EBNF 不支持的特性
- 列出当前 EBNF 支持但 OldParser.cs 不支持的特性
- 列出两者定义不同的规则

### 3.3 建议
- 基于比较结果，提出改进当前 EBNF 或解析器的建议
- 指出需要统一的语法规则
- 指出需要添加或修改的特性

## 4. 输出结果

我将以清晰的格式输出提取的 EBNF 规则和比较结果，以便用户理解和使用。