## 修改计划

1. **修改 `BlockStatement.ToCode()` 方法**
   - 当前功能：生成 C# 风格的代码
   - 修改目标：生成 Old8Lang 风格的代码
   - 关键语法调整：
     - 移除 C# 特定的 `using System;` 语句
     - 移除 `static class Program` 类定义
     - 移除 `public static void Main(string[] args)` 方法定义
     - 保留原始的 Old8Lang 语句结构
     - 确保生成的代码符合 Old8Lang 语法规范

2. **删除 `Compiler.CompileByCode()` 方法**
   - 该方法使用 C# 编译器编译由 `ToCode()` 生成的 C# 代码
   - 由于 `ToCode()` 方法将被修改为生成 Old8Lang 代码，该方法将不再有用
   - 从 `Compiler` 类中完全移除该方法

3. **验证修改**
   - 确保修改后的代码能够正常编译
   - 确保项目的其他部分不受影响

## 实现细节

### 1. 修改 `BlockStatement.ToCode()` 方法
```csharp
// 当前实现（生成C#代码）
public string ToCode()
{
    var sb = new StringBuilder();
    var import = ImportStatements.OfType<ImportStatement>().ToList();
    var func = ImportStatements.Where(x => x is ClassInit or FuncInit).ToList();
    sb.AppendLine("using System;");
    foreach (var importStatement in import)
        sb.AppendLine(importStatement.ToString());
    sb.AppendLine("static class Program");
    sb.AppendLine("{");
    foreach (var statement in func)
        sb.AppendLine(statement.ToString());
    sb.AppendLine("public static void Main(string[] args)");
    sb.AppendLine("{");
    foreach (var statement in OtherStatements)
        sb.AppendLine(statement.ToString());
    sb.AppendLine("}");
    sb.AppendLine("}");
    return sb.ToString();
}

// 修改后实现（生成Old8Lang代码）
public string ToCode()
{
    var sb = new StringBuilder();
    // 直接输出所有语句，保持Old8Lang风格
    foreach (var statement in ImportStatements)
        sb.AppendLine(statement.ToString());
    foreach (var statement in OtherStatements)
        sb.AppendLine(statement.ToString());
    return sb.ToString();
}
```

### 2. 删除 `Compiler.CompileByCode()` 方法
```csharp
// 从Compiler类中删除以下方法
public static void CompileByCode(BlockStatement statement)
{
    var code = statement.ToCode();
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    // ... 其余实现 ...
}
```

## 预期结果

- `BlockStatement.ToCode()` 方法将生成符合 Old8Lang 语法的代码
- `Compiler.CompileByCode()` 方法将被完全移除
- 项目能够正常编译和运行
- 所有相关功能保持正常工作