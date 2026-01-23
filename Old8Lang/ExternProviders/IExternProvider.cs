using System.Reflection.Emit;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.ExternProviders;

/// <summary>
/// 外部语言提供者接口
/// 用于统一不同语言（C/C++、Python、JavaScript 等）的 extern 函数导入
/// </summary>
public interface IExternProvider
{
    /// <summary>
    /// 解释模式执行：加载外部函数并注册到变量管理器
    /// </summary>
    /// <param name="source">外部资源路径（DLL 路径、脚本文件、模块名等）</param>
    /// <param name="functions">要导入的函数声明列表</param>
    /// <param name="defaultCallingConvention">默认调用约定（主要用于 P/Invoke）</param>
    /// <param name="manager">变量管理器，用于注册导入的函数</param>
    void LoadFunctions(
        string source,
        List<ExternFunctionDeclaration> functions,
        CallingConventionType defaultCallingConvention,
        VariateManager manager);

    /// <summary>
    /// 编译模式：生成 IL 代码
    /// </summary>
    /// <param name="source">外部资源路径</param>
    /// <param name="functions">要导入的函数声明列表</param>
    /// <param name="defaultCallingConvention">默认调用约定</param>
    /// <param name="ilGenerator">IL 生成器</param>
    /// <param name="localManager">局部变量管理器</param>
    void GenerateIL(
        string source,
        List<ExternFunctionDeclaration> functions,
        CallingConventionType defaultCallingConvention,
        ILGenerator ilGenerator,
        LocalManager localManager);

    /// <summary>
    /// 是否支持编译模式
    /// 注意：某些动态语言（如 Python）可能只支持解释模式
    /// </summary>
    bool SupportsCompilation { get; }
}
