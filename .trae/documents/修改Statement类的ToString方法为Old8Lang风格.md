## 修改计划

我需要将 `/Users/luckyfish/Documents/Project/RiderProjects/Old8Lang/Old8Lang/AST/Statement/` 下所有类的 `ToString` 方法修改为生成 Old8Lang 风格的代码字符串。根据对代码库和 Old8Lang 语法的分析，以下是需要修改的内容：

### 1. 变量赋值语句 (SetStatement)

* **当前**：`var {Id} = {Value};` (C# 风格)

* **修改后**：`{Id} <- {Value}` (Old8Lang 风格，使用 `<-` 赋值，无分号)

### 2. 条件语句 (IfStatement)

* **当前**：`if {ifBlock} else if{Apis.ListToString(elifBlock)} 
  else {{ {elseBlockStatement} }}` (C# 风格，使用 `else if`)

* **修改后**：`if {ifBlock} {string.Join(" ", elifBlock.Select(elif => $"elif {elif}"))} {elseBlockStatement != null ? $"else {elseBlockStatement}" : ""}` (Old8Lang 风格，使用 `elif`)

### 3. 条件块 (OldIf)

* **当前**：`({expr})
   {{ {blockStatement} }}` (条件用括号包裹)

* **修改后**：`{expr}
  {{ {blockStatement} }}` (去掉条件表达式的括号)

### 4. For 循环语句 (ForStatement)

* **当前**：`for({setStatement} ; {expr} ; {statement})
  {{ {blockStatement} }}` (C# 风格，使用分号分隔)

* **修改后**：`for {setStatement}, {expr}, {statement}
  {{ {blockStatement} }}` (Old8Lang 风格，使用逗号分隔)

### 5. While 循环语句 (WhileStatement)

* **当前**：`while({expr}){blockStatement}` (C# 风格，条件用括号包裹)

* **修改后**：`while {expr}
  {{ {blockStatement} }}` (去掉条件表达式的括号，添加换行)

### 6. Return 语句 (ReturnStatement)

* **当前**：`return {returnExpr};` (C# 风格，带分号)

* **修改后**：`return {returnExpr}` (去掉分号)

### 7. 导入语句 (ImportStatement)

* **当前**：`using {importString}` (C# 风格)

* **修改后**：`import {importString}` (Old8Lang 风格，使用 `import` 关键字)

### 8. 对象属性赋值 (OtherVariateChanging)

* **当前**：`{id}.{sumId} = {expr}` (C# 风格，使用 `=` 赋值)

* **修改后**：`{id}.{sumId} <- {expr}` (Old8Lang 风格，使用 `<-` 赋值)

### 9. 其他类

* **FuncInit, ClassInit, NativeStatement, FuncRunStatement**：这些类的 `ToString` 方法依赖于其他对象的 `ToString` 方法，需要确保依赖对象的 `ToString` 方法也生成 Old8Lang 风格的代码

## 实现细节

### 1. SetStatement.cs

```csharp
// 当前
public override string ToString() => $"var {Id} = {Value};";

// 修改后
public override string ToString() => $"{Id} <- {Value}";
```

### 2. IfStatement.cs

```csharp
//
```

