using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary> 
/// 类型模板类，用于存储类的定义信息 
/// </summary> 
public class TypeTemplate(
    string className,
    Dictionary<ClassMemberId, LangExpression> variates,
    Dictionary<ClassMemberId, LangExpression> staticVariates,
    string? parentClassName = null,
    bool isMixin = false,
    List<string>? mixinNames = null,
    List<string>? implementsNames = null,
    bool isInterface = false,
    bool isAbstract = false,
    SourcePosition position = default)
    : ImportInfo(position)
{
    public readonly string ClassName = className;
    public readonly Dictionary<ClassMemberId, LangExpression> Variates = variates;
    public readonly Dictionary<ClassMemberId, LangExpression> StaticVariates = staticVariates;
    public readonly string? ParentClassName = parentClassName;
    public readonly bool IsMixin = isMixin;
    public readonly List<string> MixinNames = mixinNames ?? [];
    public readonly List<string> ImplementsNames = implementsNames ?? [];
    public readonly bool IsInterface = isInterface;
    public readonly bool IsAbstract = isAbstract;

    /// <summary>
    /// 存储运行时的静态变量值，支持在静态方法调用之间保持状态
    /// </summary>
    private readonly Dictionary<string, LangValueType> _staticVariableValues = [];

    /// <summary>
    /// 获取静态变量的当前值，如果已修改则返回存储的值，否则返回初始值
    /// </summary>
    /// <param name="variableName">变量名</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>静态变量的值</returns>
    private LangValueType GetStaticVariableValue(string variableName, VariateManager manager)
    {
        if (_staticVariableValues.TryGetValue(variableName, out var storedValue))
        {
            return storedValue;
        }

        // 如果没有存储的值，查找初始值
        foreach (var (key, value) in StaticVariates)
        {
            if (key.IdName == variableName && value is not FuncLangValue)
            {
                // 创建一个临时管理器来初始化静态变量
                var tempManager = manager.NewManger();
                var initialValue = value.Run(tempManager);
                _staticVariableValues[variableName] = initialValue;
                return initialValue;
            }
        }

        throw new NameError(this, variableName);
    }

    public override string ToString()
    {
        var baseStr = IsInterface ? $"InterfaceTemplate({ClassName})" : IsMixin ? $"MixinTemplate({ClassName})" : IsAbstract ? $"AbstractTypeTemplate({ClassName})" : $"TypeTemplate({ClassName})";

        if (ParentClassName != null)
        {
            baseStr += $" extends {ParentClassName}";
        }

        if (ImplementsNames.Count > 0)
        {
            baseStr += $" implements {string.Join(", ", ImplementsNames)}";
        }

        if (MixinNames.Count > 0)
        {
            baseStr += $" with {string.Join(", ", MixinNames)}";
        }

        return baseStr;
    }

    /// <summary>
    /// 递归获取所有父类、mixin和接口的成员变量和方法
    /// </summary>
    /// <param name="manager">变量管理器，用于获取父类、mixin和接口信息</param>
    /// <param name="type">当前类型模板</param>
    /// <param name="allVariates">用于存储所有成员的字典</param>
    private void GetAllParentMembers(VariateManager manager, TypeTemplate type,
        Dictionary<ClassMemberId, LangExpression> allVariates)
    {
        // 如果有父类，递归获取父类的所有成员
        if (type.ParentClassName != null)
        {
            if (manager.GetAny(new LangId(type.ParentClassName)) is TypeTemplate parentType)
            {
                // 先递归获取祖父类的成员
                GetAllParentMembers(manager, parentType, allVariates);

                // 然后添加直接父类的成员，允许子类方法覆盖父类方法
                foreach (var parentMember in parentType.Variates)
                {
                    allVariates[parentMember.Key] = parentMember.Value;
                }
            }
        }
        
        // 处理所有实现的接口
        foreach (var interfaceName in type.ImplementsNames)
        {
            if (manager.GetAny(new LangId(interfaceName)) is TypeTemplate interfaceType)
            {
                // 递归获取接口的成员（接口可以继承其他接口）
                GetAllParentMembers(manager, interfaceType, allVariates);
                
                // 添加当前接口的成员
                foreach (var interfaceMember in interfaceType.Variates.Where(interfaceMember =>
                             !allVariates.ContainsKey(interfaceMember.Key)))
                {
                    allVariates[interfaceMember.Key] = interfaceMember.Value;
                }
            }
        }
        
        // 处理所有mixin类
        foreach (var mixinName in type.MixinNames)
        {
            if (manager.GetAny(new LangId(mixinName)) is TypeTemplate mixinType)
            {
                // 递归获取mixin的父类和mixin成员
                GetAllParentMembers(manager, mixinType, allVariates);
                
                // 添加当前mixin的成员
                foreach (var mixinMember in mixinType.Variates.Where(mixinMember =>
                             !allVariates.ContainsKey(mixinMember.Key)))
                {
                    allVariates[mixinMember.Key] = mixinMember.Value;
                }
            }
        }
    }

    /// <summary>
    /// 创建类的实例
    /// </summary>
    /// <param name="manager">变量管理器，用于获取父类信息</param>
    /// <returns>类的实例</returns>
    public AnyLangValue CreateInstance(VariateManager manager)
    {
        // 合并所有祖先类和子类的成员
        var allVariates = new Dictionary<ClassMemberId, LangExpression>();

        // 递归获取所有父类的成员
        GetAllParentMembers(manager, this, allVariates);

        // 添加子类的成员，重载列表已在解析阶段创建
        foreach (var member in Variates)
        {
            allVariates[member.Key] = member.Value;
        }

        // 创建一个新的AnyLangValue实例，传递合并后的所有成员变量和方法
        var instance = new AnyLangValue(new LangId(ClassName), allVariates, Position);

        // 关键：将类型模板添加到实例的管理器中，这样实例就能查找自己的类型信息
        instance.Manager.AddClassAndFunc(this);

        // 将全局的类型模板（TypeTemplate）和其他ImportInfo添加到实例的Manager中
        // 这样实例的方法就可以访问全局定义的类（如Engine、Wheel等）
        instance.Manager.AddImportInfoRange(manager.ImportInfos);

        return instance;
    }

    public override LangValueType Run(VariateManager manager)
    {
        return this;
    }

    /// <summary>
    /// 处理静态成员访问和静态方法调用
    /// </summary>
    /// <param name="right">要访问的成员或方法</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>访问结果</returns>
    public override LangValueType Dot(LangExpression right, VariateManager manager)
    {
        return right switch
        {
            LangId id => GetStaticMember(id, manager),
            Instance instance => TryCreateInstanceOrCallStaticMethod(instance, manager),
            _ => throw new InvalidOperationException($"不支持的静态成员访问: {right.GetType().Name}")
        };
    }

    /// <summary>
    /// 获取静态成员
    /// </summary>
    /// <param name="id">成员名称</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>成员值</returns>
    private LangValueType GetStaticMember(LangId id, VariateManager manager)
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

        // 3. 如果没找到，查找嵌套类
        if (actualMemberId == null)
        {
            foreach (var (memberId, memberExpr) in Variates)
            {
                if (memberId.IdName == id.IdName && memberExpr is TypeTemplate)
                {
                    // 找到嵌套类，执行并返回类型模板
                    return memberExpr.Run(manager);
                }
            }
        }

        // 4. 如果没找到，查找当前类的实例成员
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
    private LangValueType CallStaticMethod(Instance instance, VariateManager manager)
    {
        // 查找静态方法
        foreach (var (key, value) in StaticVariates)
        {
            if (key.IdName == instance.Id.IdName && value is FuncLangValue func)
            {
                // 创建一个临时的变量管理器来执行静态方法
                var tempManager = manager.NewManger();

                // 将所有静态变量的当前值添加到临时管理器中
                foreach (var (staticKey, staticValue) in StaticVariates)
                {
                    if (staticValue is not FuncLangValue) // 只添加变量，不添加方法
                    {
                        var currentVariableName = staticKey.IdName;
                        var currentValue = GetStaticVariableValue(currentVariableName, manager);
                        tempManager.Set(new LangId(currentVariableName), currentValue);
                    }
                }

                // 在临时管理器中执行静态方法
                var result = func.Run(tempManager, instance.Ids);

                // 从临时管理器中提取更新后的静态变量值，并保存到类中
                foreach (var (staticKey, staticValue) in StaticVariates)
                {
                    if (staticValue is not FuncLangValue) // 只处理变量，不处理方法
                    {
                        var variableName = staticKey.IdName;
                        var tempValue = tempManager.GetValue(new LangId(variableName));
                        if (tempValue is LangValueType updatedValue)
                        {
                            _staticVariableValues[variableName] = updatedValue;
                        }
                    }
                }

                return result;
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

    /// <summary>
    /// 尝试创建嵌套类实例或调用静态方法
    /// </summary>
    /// <param name="instance">实例表达式</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>创建的实例或方法调用结果</returns>
    private LangValueType TryCreateInstanceOrCallStaticMethod(Instance instance, VariateManager manager)
    {
        // 首先检查是否是嵌套类的实例化
        if (instance.Ids.Count == 0)  // 没有参数，可能是类实例化
        {
            // 查找嵌套类
            foreach (var (memberId, memberExpr) in Variates)
            {
                if (memberId.IdName == instance.Id.IdName && memberExpr is TypeTemplate)
                {
                    // 找到嵌套类，创建实例
                    var nestedTypeTemplate = (TypeTemplate)memberExpr.Run(manager);
                    return nestedTypeTemplate.CreateInstance(manager);
                }
            }

            // 也检查静态成员中的嵌套类
            foreach (var (memberId, memberExpr) in StaticVariates)
            {
                if (memberId.IdName == instance.Id.IdName && memberExpr is TypeTemplate)
                {
                    // 找到嵌套类，创建实例
                    var nestedTypeTemplate = (TypeTemplate)memberExpr.Run(manager);
                    return nestedTypeTemplate.CreateInstance(manager);
                }
            }
        }

        // 如果不是嵌套类实例化，尝试调用静态方法
        return CallStaticMethod(instance, manager);
    }
}

/// <summary>
/// 类方法重载列表，用于存储一个方法的所有重载版本
/// </summary>
public class MethodOverloadList : LangValueType
{
    /// <summary>
    /// 重载方法列表
    /// </summary>
    public List<FuncLangValue> Overloads { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="overloads">重载方法列表</param>
    public MethodOverloadList(List<FuncLangValue> overloads)
    {
        Overloads = overloads ?? new List<FuncLangValue>();
    }

    /// <summary>
    /// 添加新的重载
    /// </summary>
    /// <param name="overload">要添加的重载方法</param>
    public void AddOverload(FuncLangValue overload)
    {
        Overloads.Add(overload);
    }

    /// <summary>
    /// 根据参数解析最佳的重载
    /// </summary>
    /// <param name="args">参数表达式列表</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>匹配的重载方法，如果没有匹配则返回null</returns>
    public FuncLangValue? ResolveOverload(List<LangExpression> args, VariateManager manager)
    {
        var argCount = args.Count;

        // 首先查找参数数量完全匹配的重载
        var exactMatches = Overloads.Where(overload =>
            (overload.Ids?.Count ?? 0) == argCount).ToList();

        if (exactMatches.Count == 1)
        {
            return exactMatches[0];
        }

        if (exactMatches.Count > 1)
        {
            // 如果有多个精确匹配，进行类型匹配
            return ResolveByTypeMatching(exactMatches, args, manager);
        }

        // 如果没有精确匹配，查找可以处理这些参数的重载
        var compatibleMatches = Overloads.Where(overload =>
            CanHandleArguments(overload, args)).ToList();

        return compatibleMatches.FirstOrDefault();
    }

    /// <summary>
    /// 通过类型匹配选择最佳的重载
    /// </summary>
    private FuncLangValue? ResolveByTypeMatching(List<FuncLangValue> candidates, List<LangExpression> args, VariateManager manager)
    {
        var scoredCandidates = new List<(FuncLangValue candidate, int score)>();

        foreach (var candidate in candidates)
        {
            int score = 0;
            if (candidate.Ids != null)
            {
                for (int i = 0; i < args.Count && i < candidate.Ids.Count; i++)
                {
                    var paramType = candidate.Ids[i].AssumptionType;
                    if (string.IsNullOrEmpty(paramType))
                    {
                        score += 1;
                    }
                    else
                    {
                        var argValue = args[i].Run(manager);
                        if (argValue != null)
                        {
                            string actualTypeName = argValue.GetType().Name;
                            if (actualTypeName.Equals(paramType, StringComparison.OrdinalIgnoreCase))
                            {
                                score += 3;
                            }
                            else if (IsCompatibleType(actualTypeName, paramType))
                            {
                                score += 2;
                            }
                            else
                            {
                                score += 1;
                            }
                        }
                    }
                }
            }
            scoredCandidates.Add((candidate, score));
        }

        var bestMatch = scoredCandidates.OrderByDescending(x => x.score).FirstOrDefault();
        return bestMatch.score > 0 ? bestMatch.candidate : null;
    }

    /// <summary>
    /// 检查重载是否能处理给定的参数
    /// </summary>
    private bool CanHandleArguments(FuncLangValue overload, List<LangExpression> args)
    {
        var expectedParams = overload.Ids?.Count ?? 0;
        var actualParams = args.Count;

        if (actualParams > expectedParams)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 检查类型是否兼容
    /// </summary>
    private bool IsCompatibleType(string actualType, string expectedType)
    {
        return actualType.ToLowerInvariant().Contains(expectedType.ToLowerInvariant()) ||
               expectedType.ToLowerInvariant().Contains(actualType.ToLowerInvariant());
    }

    public override string ToString()
    {
        return $"MethodOverloadList[{Overloads.Count} overloads]";
    }
}