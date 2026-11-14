using Old8Lang.AST.Statement;
using Old8Lang.LangParser;
using Old8Lang.Profiler;
using Old8Lang.TypeSystem;

namespace Old8Lang.Interpreter;

/// <summary>
/// 解释器宿主的共享契约。
/// </summary>
/// <remarks>
/// Runtime 中的运行时值、模块加载器与实例方法需要在解释执行期访问解释器状态
/// （作用域、输出、类型注解与源码解析入口），但 Runtime 不能在编译期引用解释后端程序集。
/// 因此这里只暴露最小成员，具体实现 <c>LangInterpreter</c> 位于 <c>Old8Lang.Interpreter</c>。
/// </remarks>
public interface ILangInterpreter
{
    /// <summary>解释器的根变量管理器。</summary>
    VariateManager Manager { get; }

    /// <summary>类型注解管理器。</summary>
    TypeAnnotationManager TypeAnnotationManager { get; }

    /// <summary>可选的性能监控器。</summary>
    IPerformanceMonitor? PerformanceMonitor { get; }

    /// <summary>模块缓存（模块加载与编译期预处理共享）。</summary>
    Dictionary<string, BlockStatement> ModuleCache { get; }

    /// <summary>当前宿主使用的输出提供者。</summary>
    AbsOutputProvider OutputProvider { get; }

    /// <summary>解析源码并返回程序根块。</summary>
    BlockStatement Build(string code, string? fileName = null);

    /// <summary>解析源码并应用预编译符号。</summary>
    BlockStatement Build(string code, string? fileName, PreprocessorSymbols? preprocessorSymbols);
}
