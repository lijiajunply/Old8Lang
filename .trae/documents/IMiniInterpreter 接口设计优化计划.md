# IMiniInterpreter 接口设计优化计划

## 1. 当前 LangInterpreter 类分析

### 1.1 现有实现概述

当前的 `LangInterpreter` 类是 Old8Lang 语言的核心解释器实现，负责代码的解析、执行和状态管理。它包含以下关键组件：

- **变量管理**：通过 `VariateManager` 管理变量和作用域
- **代码解析**：提供 `Build` 方法将源代码转换为抽象语法树
- **词法分析**：包含静态 `Tokenize` 方法用于将代码转换为标记流
- **错误处理**：提供 `GetSourceContext` 方法用于获取错误位置的源代码上下文
- **输出管理**：通过 `UseClass` 属性管理输出方式
- **编译优化**：通过 `IsCompileOptimization` 属性控制是否启用编译优化

### 1.2 设计问题分析

1. **缺少明确的接口定义**：当前没有定义统一的解释器接口，导致实现与使用耦合度高
2. **命名不够清晰**：部分方法和属性命名不够直观，如 `Build` 方法实际是解析功能
3. **参数传递方式不统一**：`Build` 方法有重载，但静态 `Tokenize` 方法没有类似设计
4. **静态方法设计不合理**：`Tokenize` 作为静态方法，无法访问实例状态
5. **功能划分不清晰**：解释器类承担了过多职责，包括解析、执行和状态管理

## 2. IMiniInterpreter 接口设计方案

### 2.1 接口核心设计原则

- **单一职责**：每个方法只负责一项功能
- **清晰命名**：方法和属性命名应直观反映其功能
- **灵活性**：支持多种使用场景和扩展方式
- **可测试性**：便于单元测试和集成测试
- **向后兼容**：考虑现有代码的迁移成本

### 2.2 接口定义

```csharp
public interface IMiniInterpreter
{
    /// <summary>
    /// 获取或设置变量管理器
    /// </summary>
    VariateManager VariateManager { get; }
    
    /// <summary>
    /// 获取或设置输出提供者
    /// </summary>
    AbsUseClass OutputProvider { get; set; }
    
    /// <summary>
    /// 获取或设置是否启用编译优化
    /// </summary>
    bool EnableCompileOptimization { get; set; }
    
    /// <summary>
    /// 将源代码解析为抽象语法树
    /// </summary>
    /// <param name="code">要解析的源代码</param>
    /// <param name="fileName">可选的文件名，用于错误报告</param>
    /// <returns>解析后的抽象语法树</returns>
    BlockStatement Parse(string code, string? fileName = null);
    
    /// <summary>
    /// 将源代码转换为标记流
    /// </summary>
    /// <param name="code">要标记化的源代码</param>
    /// <returns>标记流</returns>
    IEnumerable<LangToken> Tokenize(string code);
    
    /// <summary>
    /// 获取错误位置附近的源代码上下文
    /// </summary>
    /// <param name="position">错误位置</param>
    /// <returns>错误位置附近的源代码上下文</returns>
    string[] GetSourceContext(SourcePosition position);
}
```

### 2.3 接口方法详细设计

| 方法名 | 签名 | 功能说明 |
|--------|------|----------|
| `VariateManager` | `VariateManager VariateManager { get; }` | 提供对变量管理器的访问，用于管理变量和作用域 |
| `OutputProvider` | `AbsUseClass OutputProvider { get; set; }` | 控制解释器的输出方式，默认使用控制台输出 |
| `EnableCompileOptimization` | `bool EnableCompileOptimization { get; set; }` | 控制是否启用编译优化，提高执行效率 |
| `Parse` | `BlockStatement Parse(string code, string? fileName = null)` | 将源代码解析为抽象语法树，支持指定文件名用于错误报告 |
| `Tokenize` | `IEnumerable<LangToken> Tokenize(string code)` | 将源代码转换为标记流，用于词法分析 |
| `GetSourceContext` | `string[] GetSourceContext(SourcePosition position)` | 获取错误位置附近的源代码上下文，便于调试和错误报告 |

### 2.4 实现类设计

基于当前的 `LangInterpreter` 类，优化后的实现类设计如下：

```csharp
public class LangInterpreter : IMiniInterpreter
{
    /// <summary>
    /// 变量管理器
    /// </summary>
    public VariateManager VariateManager { get; }
    
    /// <summary>
    /// 源代码
    /// </summary>
    private string? SourceCode { get; set; }
    
    /// <summary>
    /// 输出提供者
    /// </summary>
    public AbsUseClass OutputProvider { get; set; }
    
    /// <summary>
    /// 是否启用编译优化
    /// </summary>
    public bool EnableCompileOptimization { get; set; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public LangInterpreter()
    {
        VariateManager = new VariateManager();
        VariateManager.Interpreter = this;
        VariateManager.LangInfo ??= Apis.ReadJson();
        OutputProvider = new ConsoleUse();
    }
    
    /// <summary>
    /// 将源代码解析为抽象语法树
    /// </summary>
    /// <param name="code">要解析的源代码</param>
    /// <param name="fileName">可选的文件名，用于错误报告</param>
    /// <returns>解析后的抽象语法树</returns>
    public BlockStatement Parse(string code, string? fileName = null)
    {
        SourceCode = code;
        VariateManager.Path = fileName ?? string.Empty;
        
        // 设置当前解释器，以便在错误处理中使用
        Old8Exception.CurrentInterpreter = this;
        
        var tokens = LangTokenizer.Tokenize(code);
        if (tokens == null) throw new SyntaxError(new SourcePosition(1, 1), "语法出错");
        
        return new LangParser(tokens, code, fileName).ParseProgram();
    }
    
    /// <summary>
    /// 将源代码转换为标记流
    /// </summary>
    /// <param name="code">要标记化的源代码</param>
    /// <returns>标记流</returns>
    public IEnumerable<LangToken> Tokenize(string code)
    {
        return LangTokenizer.Tokenize(code);
    }
    
    /// <summary>
    /// 获取错误位置附近的源代码上下文
    /// </summary>
    /// <param name="position">错误位置</param>
    /// <returns>错误位置附近的源代码上下文</returns>
    public string[] GetSourceContext(SourcePosition position)
    {
        if (string.IsNullOrEmpty(SourceCode))
        {
            return Array.Empty<string>();
        }
        
        var lines = SourceCode.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var contextLines = new List<string>();
        
        // 获取错误行前后的上下文，最多显示3行上下文
        var safeLine = Math.Max(1, position.Line);
        var zeroBasedLine = safeLine - 1;
        var startLine = Math.Max(0, zeroBasedLine - 2);
        var endLine = Math.Min(lines.Length - 1, zeroBasedLine + 1);
        
        for (var i = startLine; i <= endLine; i++)
        {
            contextLines.Add(lines[i]);
        }
        
        return contextLines.ToArray();
    }
}
```

## 3. 优化前后对比

| 对比项 | 优化前 | 优化后 |
|--------|--------|--------|
| 接口定义 | 无明确接口 | 定义了 `IMiniInterpreter` 接口 |
| 方法命名 | `Build` | `Parse`（更清晰地表达解析功能） |
| Tokenize 方法 | 静态方法，返回 `List<LangToken>` | 实例方法，返回 `IEnumerable<LangToken>`（更通用） |
| 属性命名 | `Manager` | `VariateManager`（更具描述性） |
| 属性命名 | `UseClass` | `OutputProvider`（更清晰表达用途） |
| 属性命名 | `IsCompileOptimization` | `EnableCompileOptimization`（更符合 .NET 命名规范） |
| 参数传递 | 部分方法支持文件名，部分不支持 | 统一支持文件名参数，使用可选参数 |

## 4. 实现步骤

1. **定义 IMiniInterpreter 接口**：创建新的接口文件，定义上述接口
2. **修改 LangInterpreter 类**：实现新接口，优化方法和属性命名
3. **更新依赖关系**：确保 `VariateManager` 等依赖类与新接口兼容
4. **更新测试用例**：确保现有测试通过，添加针对新接口的测试
5. **添加 XML 文档注释**：为接口和实现类添加完整的文档注释
6. **验证兼容性**：确保现有代码能够平滑过渡到新接口

## 5. 预期效果

1. **接口设计更清晰**：明确的接口定义，方法和属性命名更具描述性
2. **使用更便捷**：统一的参数传递方式，支持可选参数
3. **扩展性更强**：通过接口实现，便于扩展和替换不同的解释器实现
4. **可测试性更高**：接口设计便于单元测试和集成测试
5. **代码质量提升**：遵循 .NET 设计规范，提高代码的可读性和可维护性
6. **向后兼容**：通过优化现有类，确保现有代码能够平滑过渡

## 6. 向后兼容性考虑

1. **保留原有方法**：在实现类中保留原有 `Build` 方法，通过调用新的 `Parse` 方法实现，确保现有代码继续工作
2. **添加过时标记**：为原有方法添加 `[Obsolete]` 标记，提示开发者使用新方法
3. **提供迁移指南**：在文档中说明如何从旧方法迁移到新方法

## 7. 代码质量提升

1. **遵循 .NET 设计规范**：使用一致的命名和设计模式
2. **提高可测试性**：接口设计便于单元测试和集成测试
3. **减少耦合度**：接口与实现分离，便于扩展和替换
4. **增强可读性**：清晰的文档和命名，便于理解和使用
5. **提高可维护性**：明确的职责划分，便于后续修改和扩展

## 8. 总结

通过定义 `IMiniInterpreter` 接口，我们可以将解释器的功能与实现分离，提高代码的可扩展性、可测试性和可维护性。优化后的设计遵循了 .NET 设计规范，提供了更清晰的命名和更统一的参数传递方式，同时考虑了向后兼容性，确保现有代码能够平滑过渡。