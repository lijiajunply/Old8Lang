using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.Utilities;
using System.Collections.Concurrent;
using System.Reflection;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 原生静态类 映射
/// </summary>
/// <param name="className"></param>
/// <param name="classType"></param>
public class NativeStaticAny(string className, Type classType) : ImportInfo
{
    public readonly string ClassName = className;

    // 成员信息缓存 - 按类型缓存，所有同类型实例共享
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, MemberInfo?>> MemberCache = new();

    /// <summary>
    /// 获取缓存的成员信息（属性、字段或方法）
    /// </summary>
    private MemberInfo? GetCachedMember(string memberName)
    {
        // 获取或创建该类型的缓存字典
        var typeCache = MemberCache.GetOrAdd(classType, _ => new ConcurrentDictionary<string, MemberInfo?>());

        // 从缓存中获取或查询成员
        return typeCache.GetOrAdd(memberName, name =>
        {
            // 依次尝试：属性 -> 字段 -> 方法
            MemberInfo? member = classType.GetProperty(name);
            if (member != null) return member;

            member = classType.GetField(name);
            if (member != null) return member;

            member = classType.GetMethod(name);
            return member; // 如果都找不到，返回 null
        });
    }

    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        if (dotExpression is LangId id)
        {
            // 使用缓存获取成员
            var member = GetCachedMember(id.IdName);

            // 尝试访问属性
            if (member is PropertyInfo prop)
            {
                return ObjToValue(prop.GetValue(null)!);
            }

            // 尝试访问字段
            if (member is FieldInfo field)
            {
                return ObjToValue(field.GetValue(null)!);
            }

            throw new AttributeError(this, id.IdName, ClassName);
        }

        if (dotExpression is Instance instance)
        {
            // 使用缓存获取方法
            var member = GetCachedMember(instance.Id.IdName);
            if (member is not MethodInfo method)
                throw new AttributeError(this, instance.Id.IdName, ClassName);

            var a = Apis.ListToObjects(instance.Ids.OfType<LangValueType>().ToList()).ToArray();

            // 使用委托缓存优化静态方法调用
            var invoke = MethodInvokerCache.Invoke(method, null, a);
            return ObjToValue(invoke!);
        }

        throw new InvalidOperationError(this, "不支持的点操作表达式类型");
    }

    public override TResult Accept<TResult>(Visitor.IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("NativeStaticAny 暂不支持 Visitor 模式访问");
    }
}