using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Core;

/// <summary>
/// 实例方法接口 - 定义所有实例方法的标准行为
/// </summary>
public interface IInstanceMethod
{
    /// <summary>
    /// 方法名称（支持多个别名，例如 "Add" 和 "add"）
    /// </summary>
    string[] Names { get; }

    /// <summary>
    /// 目标类型（此方法适用的类型）
    /// </summary>
    Type TargetType { get; }

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
    /// <param name="instance">实例对象</param>
    /// <param name="parameters">参数表达式列表</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="position">源代码位置</param>
    /// <returns>执行结果</returns>
    LangValueType Execute(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position);

    /// <summary>
    /// 编译器模式生成 IL 代码
    /// </summary>
    /// <param name="instance">实例表达式</param>
    /// <param name="parameters">参数表达式列表</param>
    /// <param name="ilGenerator">IL 生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <param name="position">源代码位置</param>
    void GenerateIl(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position);

    /// <summary>
    /// 编译器模式获取返回类型
    /// </summary>
    /// <param name="instanceType">实例类型</param>
    /// <param name="parameters">参数表达式列表</param>
    /// <param name="local">局部变量管理器</param>
    /// <returns>返回类型</returns>
    Type GetReturnType(Type instanceType, List<LangExpression> parameters, LocalManager local);

    /// <summary>
    /// 字节码模式执行（虚拟机模式）
    /// </summary>
    /// <param name="instance">实例对象</param>
    /// <param name="arguments">已求值的参数数组</param>
    /// <returns>执行结果</returns>
    object? ExecuteInVM(object? instance, object?[] arguments);
}
