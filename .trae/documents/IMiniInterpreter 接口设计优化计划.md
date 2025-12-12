# IMiniInterpreter 接口设计优化计划

## 1. 接口设计问题分析

### 当前问题
1. **接口定义位置不合理**：IMiniInterpreter 接口定义在 Compiler/LocalManager.cs 文件中，与编译器实现耦合
2. **接口功能不完整**：缺少必要的执行方法和错误处理机制
3. **命名不规范**：方法名和属性名缺乏一致性和描述性
4. **参数传递方式不统一**：部分方法支持文件名，部分不支持
5. **异常处理机制缺失**：接口没有定义统一的异常处理策略
6. **文档不完整**：缺少必要的 XML 文档注释

## 2. 优化方案

### 2.1 接口重新定位
- 将 IMiniInterpreter 接口从 LocalManager.cs 中分离，创建独立的 Interfaces 目录和文件
- 新路径：`/Old8Lang/Old8Lang/Interfaces/IMiniInterpreter.cs`

### 2.2 接口方法优化

#### 2.2.1 核心方法设计
| 当前方法 | 优化后方法 | 说明 |
|----------|------------|------|
| `BlockStatement Build(string code)` | `BlockStatement Parse(string code, string? fileName = null)` | 更清晰的命名，统一支持文件名参数 |
| 无 | `void Execute(BlockStatement block)` | 新增执行方法，用于执行解析后的代码块 |
| 无 | `void Execute(string code, string? fileName = null)` | 新增便捷执行方法，直接执行代码字符串 |
| `Build(string code, string? fileName)` (实现类中的重载) | 合并到 `Parse` 方法 | 统一接口，减少重载 |
| `Tokenize` (实现类中的静态方法) | `IEnumerable<LangToken> Tokenize(string code)` | 提升为接口方法，返回更通用的集合类型 |

#### 2.2.2 属性设计
| 当前属性 | 优化后属性 | 说明 |
|----------|------------|------|
| `AbsUseClass UseClass { get; set; }` | `IOutputProvider OutputProvider { get; set; }` | 更清晰的命名，考虑未来使用接口替代抽象类 |
| `bool IsCompileOptimization { get; set; }` | `bool EnableOptimization { get; set; }` | 更清晰的命名，符合 .NET 命名规范 |
| 无 | `IVariateManager VariateManager { get; }` | 新增属性，提供变量管理器访问 |
| 无 | `string? CurrentSourceCode { get; }` | 新增属性，提供当前源代码访问 |

#### 2.2.3 辅助方法设计
| 方法 | 说明 |
|------|------|
| `string[] GetSourceContext(SourcePosition position)` | 获取错误位置附近的源代码上下文 |
| `IEnumerable<Diagnostic> GetDiagnostics()` | 获取解析和执行过程中的诊断信息 |

### 2.3 异常处理机制

1. **统一异常类型**：定义 `Old8InterpreterException` 作为基异常类
2. **异常分层**：
   - `SyntaxException`：语法错误
   - `RuntimeException`：运行时错误
   - `CompilationException`：编译错误
3. **异常包含上下文信息**：每个异常包含源代码位置和解释器实例

### 2.4 接口文档优化

1. **完整的 XML 文档注释**：为每个方法、属性提供详细说明
2. **参数说明**：清晰描述每个参数的用途和限制
3. **返回值说明**：明确返回值的含义和可能的取值
4. **异常说明**：列出可能抛出的异常类型和触发条件
5. **使用示例**：提供简洁的使用示例代码

### 2.5 实现类调整

1. **LangInterpreter 类**：
   - 调整实现以符合新接口定义
   - 保持向后兼容性（如果需要）
   - 优化错误处理机制

2. **LocalManager 类**：
   - 更新对 IMiniInterpreter 的引用
   - 确保与新接口兼容

### 2.6 测试用例更新

1. **更新现有测试**：确保所有现有测试通过
2. **添加新测试**：测试新接口的所有方法和功能
3. **测试异常处理**：验证异常处理机制的正确性

## 3. 优化后的接口定义

```csharp
namespace Old8Lang.Interfaces;

/// <summary>
/// 定义 Old8Lang 解释器的核心功能接口
/// </summary>
public interface IMiniInterpreter
{
    /// <summary>
    /// 获取或设置输出提供程序，用于处理 I/O 操作
    /// </summary>
    AbsUseClass OutputProvider { get; set; }
    
    /// <summary>
    /// 获取或设置是否启用编译优化
    /// </summary>
    bool EnableOptimization { get; set; }
    
    /// <summary>
    /// 获取变量管理器，用于管理解释器中的变量
    /// </summary>
    VariateManager VariateManager { get; }
    
    /// <summary>
    /// 获取当前正在处理的源代码
    /// </summary>
    string? CurrentSourceCode { get; }
    
    /// <summary>
    /// 将源代码解析为抽象语法树
    /// </summary>
    /// <param name="code">要解析的源代码</param>
    /// <param name="fileName">可选的文件名，用于错误报告</param>
    /// <returns>解析后的代码块语句</returns>
    /// <exception cref="SyntaxException">当源代码存在语法错误时抛出</exception>
    BlockStatement Parse(string code, string? fileName = null);
    
    /// <summary>
    /// 执行解析后的代码块
    /// </summary>
    /// <param name="block">要执行的代码块语句</param>
    /// <exception cref="RuntimeException">当执行过程中发生运行时错误时抛出</exception>
    void Execute(BlockStatement block);
    
    /// <summary>
    /// 直接执行源代码字符串
    /// </summary>
    /// <param name="code">要执行的源代码</param>
    /// <param name="fileName">可选的文件名，用于错误报告</param>
    /// <exception cref="SyntaxException">当源代码存在语法错误时抛出</exception>
    /// <exception cref="RuntimeException">当执行过程中发生运行时错误时抛出</exception>
    void Execute(string code, string? fileName = null);
    
    /// <summary>
    /// 将源代码转换为标记列表
    /// </summary>
    /// <param name="code">要标记化的源代码</param>
    /// <returns>标记列表</returns>
    IEnumerable<LangToken> Tokenize(string code);
    
    /// <summary>
    /// 获取错误位置附近的源代码上下文
    /// </summary>
    /// <param name="position">源代码位置</param>
    /// <returns>包含错误位置附近代码的字符串数组</returns>
    string[] GetSourceContext(SourcePosition position);
    
    /// <summary>
    /// 获取解析和执行过程中的诊断信息
    /// </summary>
    /// <returns>诊断信息列表</returns>
    IEnumerable<Diagnostic> GetDiagnostics();
}
```

## 4. 实现步骤

1. **创建新的接口文件**：在 Interfaces 目录下创建 IMiniInterpreter.cs
2. **定义新接口**：根据优化方案实现新的接口定义
3. **更新实现类**：修改 LangInterpreter 类以实现新接口
4. **更新依赖类**：修改 VariateManager、LocalManager 等依赖类
5. **更新测试用例**：确保现有测试通过，添加新测试
6. **更新文档**：添加完整的 XML 文档注释和使用示例
7. **验证兼容性**：确保现有代码能够平滑过渡到新接口

## 5. 预期效果

1. **接口设计更清晰**：方法和属性命名更具描述性，功能划分更合理
2. **使用更便捷**：提供多种执行方式，简化调用流程
3. **扩展性更强**：支持更多的输出提供者和变量管理方式
4. **错误处理更完善**：统一的异常处理机制，便于调试和错误报告
5. **文档更完整**：详细的 XML 文档注释，支持智能提示和文档生成
6. **一致性更好**：统一的命名规范和参数传递方式

## 6. 向后兼容性考虑

1. **保留原有接口**：考虑到可能存在外部依赖，可以暂时保留原有接口，让 LangInterpreter 同时实现两个接口
2. **添加适配器**：提供从旧接口到新接口的适配器，便于现有代码迁移
3. **渐进式迁移**：允许逐步迁移到新接口，不强制一次性修改所有代码

## 7. 代码质量提升

1. **遵循 .NET 设计规范**：使用一致的命名和设计模式
2. **提高可测试性**：接口设计便于单元测试和集成测试
3. **减少耦合度**：接口与实现分离，便于扩展和替换
4. **增强可读性**：清晰的文档和命名，便于理解和使用

通过以上优化，IMiniInterpreter 接口将变得更加易用、灵活和强大，能够更好地支持 Old8Lang 的解释和编译功能，同时降低开发者的学习和使用成本。