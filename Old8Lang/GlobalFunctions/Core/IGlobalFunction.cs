using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.Core;

/// <summary>
/// 全局函数接口 - 定义所有全局函数的标准行为
/// </summary>
public interface IGlobalFunction
{
    /// <summary>
    /// 函数名称（支持多个别名，例如 "PrintLine" 和 "printLine"）
    /// </summary>
    string[] Names { get; }

    /// <summary>
    /// 参数名称列表（用于支持命名参数）
    /// 如果为 null 或空，则不支持命名参数
    /// </summary>
    string[]? ParameterNames { get; }

    /// <summary>
    /// 最小参数数量
    /// </summary>
    int MinParameterCount { get; }

    /// <summary>
    /// 最大参数数量（-1 表示不限制）
    /// </summary>
    int MaxParameterCount { get; }

    /// <summary>
    /// 解释器模式执行
    /// </summary>
    /// <param name="parameters">参数表达式列表</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="position">源代码位置</param>
    /// <returns>执行结果</returns>
    LangValueType Execute(List<LangExpression> parameters, VariateManager manager, SourcePosition position);

    /// <summary>
    /// 编译器模式生成 IL 代码
    /// </summary>
    /// <param name="parameters">参数表达式列表</param>
    /// <param name="ilGenerator">IL 生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <param name="position">源代码位置</param>
    void GenerateIl(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position);

    /// <summary>
    /// 编译器模式获取返回类型
    /// </summary>
    /// <param name="parameters">参数表达式列表</param>
    /// <param name="local">局部变量管理器</param>
    /// <returns>返回类型</returns>
    Type GetReturnType(List<LangExpression> parameters, LocalManager local);

    /// <summary>
    /// 字节码模式执行（虚拟机模式）
    /// </summary>
    /// <param name="arguments">已求值的参数数组</param>
    /// <returns>执行结果</returns>
    object? ExecuteInVM(object?[] arguments);
}
