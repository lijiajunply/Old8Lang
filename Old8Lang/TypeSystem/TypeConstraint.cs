using System;
using System.Collections.Generic;
using Old8Lang.AST;

namespace Old8Lang.TypeSystem;

/// <summary>
/// 类型约束种类
/// </summary>
public enum TypeConstraintKind
{
    /// <summary>
    /// 相等约束：T = SomeType
    /// </summary>
    Equality,

    /// <summary>
    /// 子类型约束：T <: SomeType
    /// </summary>
    Subtype,

    /// <summary>
    /// 调用约束：从函数调用推断
    /// </summary>
    Call,

    /// <summary>
    /// 赋值约束：从赋值操作推断
    /// </summary>
    Assignment,

    /// <summary>
    /// 返回约束：从return语句推断
    /// </summary>
    Return
}

/// <summary>
/// 类型约束：表示类型推断过程中的一个约束条件
/// </summary>
public class TypeConstraint
{
    /// <summary>
    /// 约束类型
    /// </summary>
    public TypeConstraintKind Kind { get; }

    /// <summary>
    /// 被约束的类型变量名
    /// </summary>
    public string TypeVariable { get; }

    /// <summary>
    /// 约束目标类型（可能为null，表示需要推断）
    /// </summary>
    public Type? TargetType { get; set; }

    /// <summary>
    /// 约束来源位置
    /// </summary>
    public SourcePosition Position { get; }

    /// <summary>
    /// 置信度（0.0-1.0）：表示此约束的可靠程度
    /// </summary>
    public double Confidence { get; set; }

    public TypeConstraint(
        TypeConstraintKind kind,
        string typeVariable,
        Type? targetType,
        SourcePosition position,
        double confidence = 1.0)
    {
        Kind = kind;
        TypeVariable = typeVariable;
        TargetType = targetType;
        Position = position;
        Confidence = confidence;
    }

    public override string ToString()
    {
        var targetStr = TargetType?.Name ?? "?";
        var op = Kind switch
        {
            TypeConstraintKind.Equality => "=",
            TypeConstraintKind.Subtype => "<:",
            _ => "~"
        };
        return $"{TypeVariable} {op} {targetStr} (conf: {Confidence:F2})";
    }
}
