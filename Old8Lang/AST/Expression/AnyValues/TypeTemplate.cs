using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

namespace Old8Lang.AST.Expression.AnyValues;

/// <summary>
/// 类型模板类，用于存储类的定义信息
/// </summary>
public partial class TypeTemplate(
    string className,
    Dictionary<ClassMemberId, LangExpression> variates,
    Dictionary<ClassMemberId, LangExpression> staticVariates,
    string? parentClassName = null,
    bool isMixin = false,
    List<string>? mixinNames = null,
    List<string>? implementsNames = null,
    bool isInterface = false,
    bool isAbstract = false,
    List<GenericParameter>? genericParameters = null,
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
    /// 泛型参数列表
    /// 例如: class Box&lt;T, U> 中的 [T, U]
    /// </summary>
    public readonly List<GenericParameter>? GenericParameters = genericParameters;

    /// <summary>
    /// 是否为泛型类
    /// </summary>
    public bool IsGeneric => GenericParameters is { Count: > 0 };

    /// <summary>
    /// 当前实例的类型参数映射（用于泛型实例化）
    /// 例如: Box&lt;int, string> 时为 {"T": int, "U": string}
    /// </summary>
    public Dictionary<string, ITypeInfo>? TypeArgumentMapping { get; private set; }

    /// <summary>
    /// 文档注释内容
    /// 存储通过 /// 语法编写的类/接口文档注释（结构化）
    /// </summary>
    public DocCommentInfo? DocComment { get; set; }

    /// <summary>
    /// 存储运行时的静态变量值，支持在静态方法调用之间保持状态
    /// </summary>
    private readonly Dictionary<string, LangValueType> StaticVariableValues = [];

    /// <summary>
    /// 获取静态变量的当前值，如果已修改则返回存储的值，否则返回初始值
    /// </summary>
    /// <param name="variableName">变量名</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>静态变量的值</returns>
    private LangValueType GetStaticVariableValue(string variableName, VariateManager manager)
    {
        if (StaticVariableValues.TryGetValue(variableName, out var storedValue))
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
                StaticVariableValues[variableName] = initialValue;
                return initialValue;
            }
        }

        throw new NameError(this, variableName);
    }

    /// <summary>
    /// 设置静态成员的值
    /// </summary>
    /// <param name="memberName">成员名称</param>
    /// <param name="value">新值</param>
    /// <param name="manager">变量管理器</param>
    public void SetStaticMember(string memberName, LangValueType value, VariateManager manager)
    {
        // 检查静态成员是否存在
        bool memberExists = false;
        foreach (var (memberId, memberExpr) in StaticVariates)
        {
            if (memberId.IdName == memberName && memberExpr is not FuncLangValue)
            {
                memberExists = true;
                break;
            }
        }

        // 检查父类的静态成员
        if (!memberExists && ParentClassName is not null)
        {
            if (manager.GetAny(new LangId(ParentClassName)) is TypeTemplate parentType)
            {
                parentType.SetStaticMember(memberName, value, manager);
                return;
            }
        }

        if (!memberExists)
        {
            throw new NameError(this, memberName);
        }

        // 设置静态字段值到运行时缓存
        StaticVariableValues[memberName] = value;

        // 同时更新到元数据缓存中，确保其他访问能获取到最新值（如果元数据已构建）
        if (MetadataCache is not null)
        {
            MetadataCache.StaticMembers[memberName] = value;
        }

        // 同时更新到当前管理器中的局部变量（如果在静态方法执行上下文中）
        // 这样可以确保静态方法执行完成后，CallStaticMethod 能够获取到正确的更新值
        var existingValue = manager.GetValue(new LangId(memberName));
        if (existingValue is not null)
        {
            manager.Set(new LangId(memberName), value);
        }
    }

    public override string ToString()
    {
        var baseStr = IsInterface ? $"InterfaceTemplate({ClassName})" :
            IsMixin ? $"MixinTemplate({ClassName})" :
            IsAbstract ? $"AbstractTypeTemplate({ClassName})" : $"TypeTemplate({ClassName})";

        if (ParentClassName is not null)
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
        if (type.ParentClassName is not null)
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
    /// 创建类的实例（V1 兼容接口，内部使用 V2）
    /// </summary>
    /// <param name="manager">变量管理器，用于获取父类信息</param>
    /// <returns>类的实例（V2 版本）</returns>
    public AnyLangValue CreateInstance(VariateManager manager)
    {
        // 直接使用 V2 实现
        return CreateInstanceV2(manager);
    }

    public override LangValueType Run(VariateManager manager)
    {
        return this;
    }

    /// <summary>
    /// 接受 Visitor 访问
    /// </summary>
    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        // TypeTemplate 是类型定义的元数据，直接返回自身
        return (TResult)(object)this;
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
        if (actualMemberId is null && ParentClassName is not null)
        {
            if (manager.GetAny(new LangId(ParentClassName)) is TypeTemplate parentType)
            {
                return parentType.GetStaticMember(id, manager);
            }
        }

        // 3. 如果没找到，查找嵌套类
        if (actualMemberId is null)
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
        if (actualMemberId is null)
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
        if (actualMemberId is null || expr is null)
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

        // 6. 优先返回运行时存储的值，如果没有则执行初始化表达式
        return GetStaticVariableValue(id.IdName, manager);
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
                // 关键修复：使用全局 manager 而不是调用者的 manager，这样可以确保访问到全局注册的类型
                // 并且能够正确处理静态变量的赋值操作
                var tempManager = manager.Interpreter?.Manager?.NewManger() ?? manager.NewManger();

                // 将当前类添加到临时管理器中，这样静态方法内部就能访问到类（用于 ClassName.staticField 形式的赋值）
                tempManager.AddClassAndFunc(this);

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
                // 注意:如果静态方法内部通过 ClassName.staticField <- value 进行赋值,
                // 值已经通过 SetStaticMember 直接更新到 StaticVariableValues 了,
                // 这里我们只需要同步那些作为局部变量被修改的静态变量
                foreach (var (staticKey, staticValue) in StaticVariates)
                {
                    if (staticValue is not FuncLangValue) // 只处理变量，不处理方法
                    {
                        var variableName = staticKey.IdName;
                        // 尝试从tempManager中获取局部变量形式的更新值
                        var tempValue = tempManager.GetValue(new LangId(variableName));
                        if (tempValue is not null)
                        {
                            // 只有当tempManager中的值与StaticVariableValues中的值不同时才更新
                            // 这样可以避免覆盖通过 SetStaticMember 已经更新的值
                            var currentStoredValue = StaticVariableValues.GetValueOrDefault(variableName);
                            if (currentStoredValue is null || !ReferenceEquals(tempValue, currentStoredValue))
                            {
                                // 保存到 StaticVariableValues（TypeTemplate 的实例字段）
                                StaticVariableValues[variableName] = tempValue;
                                // 同时保存到元数据缓存（如果存在）
                                if (MetadataCache is not null && MetadataCache.StaticMembers.ContainsKey(variableName))
                                {
                                    MetadataCache.StaticMembers[variableName] = tempValue;
                                }
                            }
                        }
                    }
                }

                return result;
            }
        }

        // 查找父类的静态方法
        if (ParentClassName is not null)
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
        if (instance.Ids.Count == 0) // 没有参数，可能是类实例化
        {
            // 查找嵌套类
            foreach (var (memberId, memberExpr) in Variates)
            {
                if (memberId.IdName == instance.Id.IdName && memberExpr is TypeTemplate)
                {
                    // 找到嵌套类，创建实例（使用 V2）
                    var nestedTypeTemplate = (TypeTemplate)memberExpr.Run(manager);
                    var nestedInstance = nestedTypeTemplate.CreateInstanceV2(manager);
                    nestedInstance.Init(manager.Interpreter);
                    return nestedInstance;
                }
            }

            // 也检查静态成员中的嵌套类
            foreach (var (memberId, memberExpr) in StaticVariates)
            {
                if (memberId.IdName == instance.Id.IdName && memberExpr is TypeTemplate)
                {
                    // 找到嵌套类，创建实例（使用 V2）
                    var nestedTypeTemplate = (TypeTemplate)memberExpr.Run(manager);
                    var nestedInstance = nestedTypeTemplate.CreateInstanceV2(manager);
                    nestedInstance.Init(manager.Interpreter);
                    return nestedInstance;
                }
            }
        }

        // 如果不是嵌套类实例化，尝试调用静态方法
        return CallStaticMethod(instance, manager);
    }

    // ==================== V2 架构支持 ====================

    /// <summary>
    /// 类型元数据缓存（V2 架构）
    /// 只构建一次，所有实例共享
    /// </summary>
    private ClassMetadata? MetadataCache;

    /// <summary>
    /// 元数据属性（V2 架构）
    /// </summary>
    public ClassMetadata? Metadata => MetadataCache;

    /// <summary>
    /// 构建类型元数据（V2 架构）
    /// 只在第一次调用时构建，之后直接返回缓存
    /// </summary>
    public ClassMetadata BuildMetadata(VariateManager manager)
    {
        if (MetadataCache is not null)
            return MetadataCache;

        // 创建 ClassMetadata
        MetadataCache = new ClassMetadata(
            className: ClassName,
            parentClassName: ParentClassName,
            interfaceNames: ImplementsNames,
            mixinNames: MixinNames,
            isInterface: IsInterface,
            isAbstract: IsAbstract,
            isMixin: IsMixin
        );

        // 构建方法表和字段表
        BuildMethodTableAndFieldTable(manager, this, MetadataCache.MethodTable, MetadataCache.FieldTable);

        // 初始化静态成员
        foreach (var (memberId, expr) in StaticVariates)
        {
            if (expr is not FuncLangValue)
            {
                var value = expr.Run(manager);
                MetadataCache.StaticMembers[memberId.IdName] = value;
            }
        }

        return MetadataCache;
    }

    /// <summary>
    /// 递归构建方法表和字段表（包括继承的成员）
    /// </summary>
    private void BuildMethodTableAndFieldTable(
        VariateManager manager,
        TypeTemplate type,
        MethodTable methodTable,
        FieldDefinitionTable fieldTable)
    {
        // 1. 先递归处理父类
        if (type.ParentClassName is not null)
        {
            if (manager.GetAny(new LangId(type.ParentClassName)) is TypeTemplate parentType)
            {
                // 递归处理父类（会添加父类及其祖先的所有成员）
                BuildMethodTableAndFieldTable(manager, parentType, methodTable, fieldTable);
            }
        }

        // 2. 处理接口
        foreach (var interfaceName in type.ImplementsNames)
        {
            if (manager.GetAny(new LangId(interfaceName)) is TypeTemplate interfaceType)
            {
                BuildMethodTableAndFieldTable(manager, interfaceType, methodTable, fieldTable);
            }
        }

        // 3. 处理 Mixin
        foreach (var mixinName in type.MixinNames)
        {
            if (manager.GetAny(new LangId(mixinName)) is TypeTemplate mixinType)
            {
                BuildMethodTableAndFieldTable(manager, mixinType, methodTable, fieldTable);
            }
        }

        // 4. 最后添加当前类的成员（子类成员可以覆盖父类）
        AddMembersToTables(type, methodTable, fieldTable);
    }

    /// <summary>
    /// 将类型的成员添加到方法表和字段表
    /// </summary>
    private void AddMembersToTables(
        TypeTemplate type,
        MethodTable methodTable,
        FieldDefinitionTable fieldTable)
    {
        foreach (var (memberId, expr) in type.Variates)
        {
            if (expr is FuncLangValue funcValue)
            {
                // 方法
                var methodInfo = new LangMethodInfo(
                    methodName: memberId.IdName,
                    implementation: funcValue,
                    modifiers: memberId.Modifiers,
                    isStatic: memberId.HasModifier(AccessModifierType.Static),
                    isVirtual: true, // Old8Lang 中所有方法都可以被重写
                    isAbstract: memberId.HasModifier(AccessModifierType.Abstract),
                    originClassName: type.ClassName
                );

                methodTable.AddMethod(methodInfo);
            }
            else if (expr is MethodOverloadList overloadList)
            {
                // 重载方法列表
                bool isFirst = true;
                foreach (var overload in overloadList.Overloads)
                {
                    var methodInfo = new LangMethodInfo(
                        methodName: memberId.IdName,
                        implementation: overload,
                        modifiers: memberId.Modifiers,
                        isStatic: memberId.HasModifier(AccessModifierType.Static),
                        isVirtual: true,
                        isAbstract: memberId.HasModifier(AccessModifierType.Abstract),
                        originClassName: type.ClassName
                    );

                    // 第一个重载使用默认的 allowOverride (true)，后续重载使用 false
                    methodTable.AddMethod(methodInfo, isFirst);
                    isFirst = false;
                }
            }
            else
            {
                // 字段
                var fieldDef = new FieldDefinition(
                    fieldName: memberId.IdName,
                    initialValueExpression: expr,
                    modifiers: memberId.Modifiers,
                    isStatic: memberId.HasModifier(AccessModifierType.Static),
                    originClassName: type.ClassName
                );

                fieldTable.AddField(fieldDef);
            }
        }
    }

    /// <summary>
    /// 实例化泛型类
    /// </summary>
    public TypeTemplate InstantiateGeneric(
        Dictionary<string, TypeSystem.ITypeInfo> typeArguments,
        TypeAnnotationManager typeAnnotationManager)
    {
        if (!IsGeneric)
        {
            throw new InvalidOperationException($"类型 {ClassName} 不是泛型类");
        }

        // 验证类型参数数量
        if (typeArguments.Count != GenericParameters!.Count)
        {
            throw new ArgumentException(
                $"类型参数数量不匹配：期望 {GenericParameters.Count} 个，实际 {typeArguments.Count} 个");
        }

        // 验证约束（如果有）
        foreach (var genericParam in GenericParameters)
        {
            if (genericParam.HasConstraints && typeArguments.TryGetValue(genericParam.Name, out var actualType))
            {
                foreach (var constraintName in genericParam.Constraints!)
                {
                    var constraintType = typeAnnotationManager.GetTypeFamily().GetType(constraintName);
                    if (constraintType is not null && !actualType.IsCompatibleWith(constraintType))
                    {
                        throw new ArgumentException(
                            $"类型 {actualType.Name} 不满足约束 {constraintName}");
                    }
                }
            }
        }

        // 创建实例化的TypeTemplate（复制所有字段）
        var instantiated = new TypeTemplate(
            className: ClassName,
            variates: Variates,
            staticVariates: StaticVariates,
            parentClassName: ParentClassName,
            isMixin: IsMixin,
            mixinNames: MixinNames,
            implementsNames: ImplementsNames,
            isInterface: IsInterface,
            isAbstract: IsAbstract,
            genericParameters: GenericParameters,
            position: Position
        )
        {
            // 设置类型参数映射
            TypeArgumentMapping = typeArguments
        };

        return instantiated;
    }

    /// <summary>
    /// 创建类的实例（V2 架构）
    /// </summary>
    public AnyLangValue CreateInstanceV2(VariateManager manager)
    {
        // 构建或获取缓存的元数据
        var metadata = BuildMetadata(manager);

        // 创建实例
        var instance = new AnyLangValue(
            classId: new LangId(ClassName),
            metadata: metadata,
            position: Position
        );

        // 如果是泛型实例，传递类型参数映射
        if (TypeArgumentMapping is not null)
        {
            instance.TypeArgumentMapping = TypeArgumentMapping;
        }

        // 初始化字段
        instance.InitializeFields(manager);

        return instance;
    }
}

/// <summary>
/// 类方法重载列表，用于存储一个方法的所有重载版本
/// </summary>
public partial class MethodOverloadList : LangValueType
{
    /// <summary>
    /// 重载方法列表
    /// </summary>
    public List<FuncLangValue> Overloads { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="overloads">重载方法列表</param>
    public MethodOverloadList(List<FuncLangValue>? overloads)
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

        if (compatibleMatches.Count > 0)
        {
            // 在兼容的方法中，选择参数数量最接近的
            return compatibleMatches
                .OrderBy(overload => Math.Abs((overload.Ids?.Count ?? 0) - argCount))
                .ThenBy(overload => overload.Ids?.Count ?? 0)
                .First();
        }

        return null;
    }

    /// <summary>
    /// 通过类型匹配选择最佳的重载
    /// </summary>
    private FuncLangValue? ResolveByTypeMatching(List<FuncLangValue> candidates, List<LangExpression> args,
        VariateManager manager)
    {
        var scoredCandidates = new List<(FuncLangValue candidate, int score)>();

        foreach (var candidate in candidates)
        {
            int score = 0;
            if (candidate.Ids is not null)
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

        // 如果实际参数数量小于期望参数数量，检查缺失的参数是否都有默认值
        if (actualParams < expectedParams && overload.Ids is not null)
        {
            for (int i = actualParams; i < expectedParams; i++)
            {
                var parameter = overload.Ids[i];
                if (parameter.DefaultValue is null)
                {
                    return false; // 缺失的参数没有默认值
                }
            }
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