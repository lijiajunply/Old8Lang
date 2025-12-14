using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

/// <summary> 
/// 类型模板类，用于存储类的定义信息 
/// </summary> 
public class TypeTemplate(
    string className,
    Dictionary<ClassMemberId, LangExpression> variates,
    Dictionary<ClassMemberId, LangExpression> staticVariates,
    string? parentClassName = null,
    SourcePosition position = default)
    : ImportInfo(position)
{
    public readonly string ClassName = className;
    public readonly Dictionary<ClassMemberId, LangExpression> Variates = variates;
    public readonly Dictionary<ClassMemberId, LangExpression> StaticVariates = staticVariates;
    public readonly string? ParentClassName = parentClassName;

    public override string ToString() => ParentClassName == null
        ? $"TypeTemplate({ClassName})"
        : $"TypeTemplate({ClassName} extends {ParentClassName})";

    /// <summary>
    /// 递归获取所有父类的成员变量和方法
    /// </summary>
    /// <param name="manager">变量管理器，用于获取父类信息</param>
    /// <param name="type">当前类型模板</param>
    /// <param name="allVariates">用于存储所有成员的字典</param>
    private void GetAllParentMembers(LangParser.VariateManager manager, TypeTemplate type,
        Dictionary<ClassMemberId, LangExpression> allVariates)
    {
        // 如果有父类，递归获取父类的所有成员
        if (type.ParentClassName != null)
        {
            if (manager.GetAny(new LangId(type.ParentClassName)) is TypeTemplate parentType)
            {
                // 先递归获取祖父类的成员
                GetAllParentMembers(manager, parentType, allVariates);

                // 然后添加直接父类的成员
                foreach (var parentMember in parentType.Variates.Where(parentMember =>
                             !allVariates.ContainsKey(parentMember.Key)))
                {
                    allVariates[parentMember.Key] = parentMember.Value;
                }
            }
        }
    }

    /// <summary>
    /// 创建类的实例
    /// </summary>
    /// <param name="manager">变量管理器，用于获取父类信息</param>
    /// <returns>类的实例</returns>
    public AnyLangValue CreateInstance(LangParser.VariateManager manager)
    {
        // 合并所有祖先类和子类的成员
        var allVariates = new Dictionary<ClassMemberId, LangExpression>();

        // 递归获取所有父类的成员
        GetAllParentMembers(manager, this, allVariates);

        // 然后添加子类的成员，子类成员会覆盖父类同名成员
        foreach (var member in Variates)
        {
            allVariates[member.Key] = member.Value;
        }

        // 创建一个新的AnyLangValue实例，传递合并后的所有成员变量和方法
        var instance = new AnyLangValue(new LangId(ClassName), allVariates, Position);

        return instance;
    }

    public override LangValueType Run(LangParser.VariateManager manager)
    {
        return this;
    }

    /// <summary>
    /// 处理静态成员访问和静态方法调用
    /// </summary>
    /// <param name="right">要访问的成员或方法</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>访问结果</returns>
    public LangValueType Dot(LangExpression right, LangParser.VariateManager manager)
    {
        return right switch
        {
            LangId id => GetStaticMember(id, manager),
            Instance instance => CallStaticMethod(instance, manager),
            _ => throw new InvalidOperationException($"不支持的静态成员访问: {right.GetType().Name}")
        };
    }

    /// <summary>
    /// 获取静态成员
    /// </summary>
    /// <param name="id">成员名称</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>成员值</returns>
    private LangValueType GetStaticMember(LangId id, LangParser.VariateManager manager)
    {
        // 查找实际的 ClassMemberId（包含修饰符信息）
        ClassMemberId? actualMemberId = null;
        LangExpression? expr = null;

        // 1. 先查找当前类的静态成员
        foreach (var (memberId, memberExpr) in StaticVariates)
        {
            if (memberId.IdName == id.IdName)
            {
                actualMemberId = memberId;
                expr = memberExpr;
                break;
            }
        }

        // 2. 如果没找到，查找父类的静态成员
        if (actualMemberId == null && ParentClassName != null)
        {
            if (manager.GetAny(new LangId(ParentClassName)) is TypeTemplate parentType)
            {
                return parentType.GetStaticMember(id, manager);
            }
        }

        // 3. 如果没找到，查找当前类的实例成员
        if (actualMemberId == null)
        {
            foreach (var (memberId, memberExpr) in Variates)
            {
                if (memberId.IdName == id.IdName)
                {
                    actualMemberId = memberId;
                    expr = memberExpr;
                    break;
                }
            }
        }

        // 4. 检查是否找到成员
        if (actualMemberId == null || expr == null)
        {
            throw new NameError(this, id.IdName);
        }

        // 5. 检查访问权限
        bool isPrivate = actualMemberId.HasModifier(AccessModifierType.Private);
        
        // 直接检查成员是否是私有的，如果是，外部无法访问
        // 静态成员的私有访问控制：只有类内部可以访问
        if (isPrivate)
        {
            // 检查是否在类内部访问（通过检查当前作用域是否有类上下文）
            bool isInClassContext = manager.IsClass;
            if (!isInClassContext)
            {
                throw new NameError(this, id.IdName);
            }
        }

        // 6. 执行并返回结果
        return expr.Run(manager);
    }

    /// <summary>
    /// 调用静态方法
    /// </summary>
    /// <param name="instance">方法调用实例</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>方法调用结果</returns>
    private LangValueType CallStaticMethod(Instance instance, LangParser.VariateManager manager)
    {
        // 查找静态方法
        foreach (var (key, value) in StaticVariates)
        {
            if (key.IdName == instance.Id.IdName && value is FuncLangValue func)
            {
                return func.Run(manager, instance.Ids);
            }
        }

        // 查找父类的静态方法
        if (ParentClassName != null)
        {
            if (manager.GetAny(new LangId(ParentClassName)) is TypeTemplate parentType)
            {
                return parentType.CallStaticMethod(instance, manager);
            }
        }

        // 查找实例方法（如果存在的话）
        foreach (var (key, value) in Variates)
        {
            if (key.IdName == instance.Id.IdName && value is FuncLangValue func)
            {
                // 静态调用实例方法，传入null作为this
                return func.Run(manager, instance.Ids);
            }
        }

        throw new NameError(this, instance.Id.IdName);
    }
}