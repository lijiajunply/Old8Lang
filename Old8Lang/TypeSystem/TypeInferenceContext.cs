using System;
using System.Collections.Generic;

namespace Old8Lang.TypeSystem;

/// <summary>
/// 类型推断上下文：存储类型推断过程中的状态信息
/// </summary>
public class TypeInferenceContext
{
    /// <summary>
    /// 类型变量到类型的映射
    /// </summary>
    public Dictionary<string, Type> TypeVariableBindings { get; } = [];

    /// <summary>
    /// 收集到的所有约束
    /// </summary>
    public List<TypeConstraint> Constraints { get; } = [];

    /// <summary>
    /// 函数调用信息：函数名 -> (参数类型列表, 返回类型)
    /// </summary>
    public Dictionary<string, (List<Type> ParamTypes, Type ReturnType)> FunctionCallInfo { get; } = [];

    /// <summary>
    /// 添加约束
    /// </summary>
    public void AddConstraint(TypeConstraint constraint)
    {
        Constraints.Add(constraint);
    }

    /// <summary>
    /// 绑定类型变量
    /// </summary>
    public void BindTypeVariable(string variable, Type type)
    {
        TypeVariableBindings[variable] = type;
    }

    /// <summary>
    /// 查询类型变量的绑定
    /// </summary>
    public Type? GetTypeBinding(string variable)
    {
        return TypeVariableBindings.GetValueOrDefault(variable);
    }

    /// <summary>
    /// 清空上下文
    /// </summary>
    public void Clear()
    {
        TypeVariableBindings.Clear();
        Constraints.Clear();
        FunctionCallInfo.Clear();
    }
}
