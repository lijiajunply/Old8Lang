using System.Reflection.Emit;
using System.Text;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 类
/// </summary>
public class AnyLangValue : LangValueType
{
    public readonly Dictionary<ClassMemberId, LangExpression> Variates;
    public readonly Dictionary<string, LangValueType> Result = [];
    public readonly LangId Id;

    public readonly VariateManager Manager;

    /// <summary>
    /// 函数查找缓存，用于提高类方法查找效率
    /// </summary>
    private readonly Dictionary<string, (ClassMemberId memberId, LangValueType value)> FunctionLookupCache = [];

    /// <summary>
    /// 获取所有可用的成员（包括继承的成员）
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <returns>包含所有成员的字典</returns>
    private Dictionary<ClassMemberId, LangExpression> GetAllMembers(VariateManager manager)
    {
        var allMembers = new Dictionary<ClassMemberId, LangExpression>(Variates);

        // 获取当前类的类型模板
        if (Manager.GetAny(new LangId(Id.IdName)) is TypeTemplate currentTypeTemplate)
        {
            // 获取所有父类成员
            GetAllParentMembers(manager, currentTypeTemplate, allMembers);
        }

        return allMembers;
    }

    /// <summary>
    /// 递归获取所有父类成员
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="type">类型模板</param>
    /// <param name="allMembers">存储所有成员的字典</param>
    private void GetAllParentMembers(VariateManager manager, TypeTemplate type,
        Dictionary<ClassMemberId, LangExpression> allMembers)
    {
        // 如果有父类，递归获取父类的所有成员
        if (type.ParentClassName != null)
        {
            if (manager.GetAny(new LangId(type.ParentClassName)) is TypeTemplate parentType)
            {
                // 先递归获取祖父类的成员
                GetAllParentMembers(manager, parentType, allMembers);

                // 然后添加直接父类的成员，允许子类方法覆盖父类方法
                foreach (var parentMember in parentType.Variates)
                {
                    // 如果还没有这个成员，添加父类的成员
                    if (!allMembers.ContainsKey(parentMember.Key))
                    {
                        allMembers[parentMember.Key] = parentMember.Value;
                    }
                }
            }
        }

        // 处理所有实现的接口
        foreach (var interfaceName in type.ImplementsNames)
        {
            if (manager.GetAny(new LangId(interfaceName)) is TypeTemplate interfaceType)
            {
                // 递归获取接口的成员（接口可以继承其他接口）
                GetAllParentMembers(manager, interfaceType, allMembers);

                // 添加当前接口的成员
                foreach (var interfaceMember in interfaceType.Variates.Where(interfaceMember =>
                             !allMembers.ContainsKey(interfaceMember.Key)))
                {
                    allMembers[interfaceMember.Key] = interfaceMember.Value;
                }
            }
        }

        // 处理所有mixin类
        foreach (var mixinName in type.MixinNames)
        {
            if (manager.GetAny(new LangId(mixinName)) is TypeTemplate mixinType)
            {
                // 添加mixin的成员
                foreach (var mixinMember in mixinType.Variates.Where(mixinMember =>
                             !allMembers.ContainsKey(mixinMember.Key)))
                {
                    allMembers[mixinMember.Key] = mixinMember.Value;
                }
            }
        }
    }

    /// <summary>
    /// 检查调用上下文是否在类内部
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <returns>如果是在类内部访问返回true</returns>
    private bool CheckInternalAccess(VariateManager manager)
    {
        // 首先检查自己的 Manager 中是否有 this（在类方法内部访问时）
        try
        {
            var thisInManager = Manager.GetValue(new LangId("this"));
            if (ReferenceEquals(thisInManager, this))
            {
                return true;
            }
        }
        catch
        {
            // Manager 中没有 this
        }

        // 如果还没有确认是内部访问，再检查 ExternalManager
        if (ExternalManager != null)
        {
            try
            {
                var thisInfo = ExternalManager.GetValue(new LangId("this"));
                // GetValue 返回 LangValueType，AnyLangValue 继承自 LangValueType
                // 所以可以直接比较引用
                if (ReferenceEquals(thisInfo, this))
                {
                    return true;
                }
            }
            catch
            {
                // ExternalManager 中没有 this，说明不在类内部
            }
        }

        // 检查传入的manager中是否有this
        try
        {
            var thisInManager = manager.GetValue(new LangId("this"));
            if (ReferenceEquals(thisInManager, this))
            {
                return true;
            }
        }
        catch
        {
            // manager 中没有 this
        }

        return false;
    }

    /// <summary>
    /// 查找类成员（支持继承和缓存）
    /// </summary>
    /// <param name="memberName">成员名称</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>找到的成员信息，如果未找到则返回null</returns>
    private (ClassMemberId? memberId, LangValueType? value)? FindMember(string memberName, VariateManager manager)
    {
        // 1. 首先检查缓存
        if (FunctionLookupCache.TryGetValue(memberName, out var cached))
        {
            return (cached.memberId, cached.value);
        }

        // 2. 在Result字典中查找已初始化的成员
        if (Result.TryGetValue(memberName, out var resultValue))
        {
            var cacheEntry = (new ClassMemberId(memberName), resultValue);
            FunctionLookupCache[memberName] = cacheEntry;
            return (cacheEntry.Item1, cacheEntry.Item2);
        }

        // 3. 在所有成员（包括继承的成员）中查找
        var allMembers = GetAllMembers(manager);
        ClassMemberId? foundMemberId = null;
        LangExpression? foundExpression = null;

        foreach (var (memberId, expression) in allMembers)
        {
            if (memberId.IdName != memberName) continue;
            foundMemberId = memberId;
            foundExpression = expression;
            break;
        }

        // 4. 如果找到了成员表达式，运行它并缓存结果
        if (foundExpression != null)
        {
            var value = foundExpression as LangValueType ?? foundExpression.Run(manager);
            var cacheEntry = (foundMemberId!, value);
            FunctionLookupCache[memberName] = cacheEntry;
            return (foundMemberId, value);
        }

        // 5. 如果没有找到，返回null
        return null;
    }

    public AnyLangValue(LangId id, Dictionary<ClassMemberId, LangExpression> variates,
        SourcePosition position = default) :
        base(position)
    {
        Variates = variates;
        Id = id;
        Manager = new VariateManager
        {
            IsClass = true
        };
    }

    /// <summary>
    /// 初始化实例，设置Interpreter
    /// </summary>
    /// <param name="interpreter">解释器实例</param>
    public void Init(LangInterpreter interpreter)
    {
        Manager.Interpreter = interpreter;

        // 现在可以运行变量表达式了
        foreach (var variable in Variates)
        {
            // 运行变量表达式，获取结果
            // 如果是函数，则直接存储函数本身，不执行
            var value = variable.Value as FuncLangValue ?? variable.Value.Run(Manager);
            Result[variable.Key.IdName] = value;
        }

        // 不要调用Manager.Init(Result)，否则会导致变量重复定义
        // 而是直接将Result字典中的值存储到实例中
    }

    public AnyLangValue(Dictionary<ClassMemberId, LangExpression> variates, SourcePosition position = default) :
        base(position)
    {
        Variates = variates;
        Id = new LangId("JsonNative");
        Manager = new VariateManager();
        foreach (var variate in variates)
        {
            if (variate.Value is LangValueType valueType) Result.Add(variate.Key.IdName, valueType);
        }

        Manager.Init(Result);
        Manager.IsClass = true;
    }

    /// <summary>
    /// 外部管理器引用，用于在Dot方法中访问外部变量
    /// </summary>
    internal VariateManager? ExternalManager;

    public sealed override LangValueType Run(VariateManager manager)
    {
        // 保存外部管理器的引用
        ExternalManager = manager;

        Manager.AddImportInfoRange(manager.ImportInfos.Where(x => x is not FuncLangValue).ToList());

        foreach (var variable in Variates.Keys)
        {
            // 运行变量表达式，并将结果添加到Result字典中
            var value = Variates[variable].Run(manager);
            Result.Add(variable.IdName, value);
        }

        return this;
    }

    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        switch (dotExpression)
        {
            case LangId id:
            {
                // 使用新的查找机制
                var memberInfo = FindMember(id.IdName, manager);
                if (memberInfo.HasValue)
                {
                    var (memberId, value) = memberInfo.Value;

                    // 检查访问权限
                    bool isPrivate = memberId?.HasModifier(AccessModifierType.Private) ?? false;
                    bool isProtected = memberId?.HasModifier(AccessModifierType.Protected) ?? false;

                    // 检查调用上下文是否在类内部
                    bool isInternalAccess = CheckInternalAccess(manager);

                    // 外部无法访问私有字段
                    if (isPrivate && !isInternalAccess)
                    {
                        throw new AttributeError(this, id.IdName, Id.IdName);
                    }

                    // 外部无法访问保护字段（protected 的语义与 private 在 Old8Lang 中相同，因为没有继承）
                    if (isProtected && !isInternalAccess)
                    {
                        throw new AttributeError(this, id.IdName, Id.IdName);
                    }

                    // 如果找到的是函数类型
                    if (value is FuncLangValue funcValue)
                    {
                        // 在调用类方法时，将当前实例作为"this"变量添加到变量储存器中
                        var currentManager = ExternalManager ?? Manager;
                        currentManager.Set(new LangId("this"), this);

                        // 将实例的所有成员变量添加到Manager中，以便方法内部直接访问
                        foreach (var member in Result)
                        {
                            currentManager.Set(new LangId(member.Key), member.Value);
                        }

                        var funcResult = funcValue.Run(currentManager);
                        return funcResult;
                    }

                    return value ?? throw new AttributeError(this, id.IdName, Id.IdName);
                }

                // 如果没有找到，抛出AttributeError异常
                throw new AttributeError(this, id.IdName, Id.IdName);
            }
            case FuncLangValue func:
            {
                if (func.Id?.IdName == "GetType")
                    return new TypeLangValue(TypeToString());
                // 在调用类方法时，将当前实例添加到AnyInfo中，以便this关键字访问
                Manager.Set(new LangId("this"), this);
                var funcResult = func.Run(Manager);
                return funcResult;
            }
            case Instance instance:
            {
                // 处理方法调用，如: alice.getName()
                // 首先获取方法名
                var methodName = instance.Id.IdName;

                // 使用新的查找机制
                var memberInfo = FindMember(methodName, manager);
                if (memberInfo.HasValue)
                {
                    var (memberId, value) = memberInfo.Value;

                    // 检查访问权限
                    bool isPrivate = memberId?.HasModifier(AccessModifierType.Private) ?? false;
                    bool isProtected = memberId?.HasModifier(AccessModifierType.Protected) ?? false;

                    // 检查调用上下文是否在类内部
                    bool isInternalAccess = CheckInternalAccess(manager);

                    // 外部无法访问私有字段
                    if (isPrivate && !isInternalAccess)
                    {
                        throw new AttributeError(this, methodName, Id.IdName);
                    }

                    // 外部无法访问保护字段
                    if (isProtected && !isInternalAccess)
                    {
                        throw new AttributeError(this, methodName, Id.IdName);
                    }

                    // 如果找到的是函数类型
                    if (value is FuncLangValue funcValue)
                    {
                        // 使用外部管理器或内部管理器作为回退
                        var currentManager = ExternalManager ?? Manager;

                        // 在调用类方法时，将当前实例添加到变量储存器中，以便this关键字访问
                        currentManager.Set(new LangId("this"), this);

                        // 将实例的所有成员变量添加到Manager中，以便方法内部直接访问
                        foreach (var member in Result)
                        {
                            currentManager.Set(new LangId(member.Key), member.Value);
                        }

                        // 临时设置 ExternalManager，使得在方法内部访问字段时能正确判断是内部访问
                        var originalExternalManager = ExternalManager;
                        ExternalManager = currentManager;

                        try
                        {
                            // 处理方法参数，先运行参数表达式，这样可以访问外部变量
                            List<LangExpression> methodArgs = [];
                            methodArgs.AddRange(instance.Ids);

                            // 调用方法时使用当前管理器，这样可以访问外部变量
                            var funcResult = funcValue.Run(currentManager, methodArgs);

                            // 方法调用完成后，将管理器中修改的变量同步回实例的 Result 字典
                            // 对于直接修改的变量（如logs <- logs + "text"），需要从manager同步回实例
                            SyncVariablesFromManager(currentManager);

                            return funcResult;
                        }
                        finally
                        {
                            // 恢复原始的 ExternalManager
                            ExternalManager = originalExternalManager;
                        }
                    }

                    // 不是函数类型，直接返回值
                    return value ?? throw new AttributeError(this, methodName, Id.IdName);
                }

                // 如果没有找到方法，检查是否是嵌套类访问
                var nestedClassInfo = FindNestedClass(methodName, manager);
                if (nestedClassInfo != null)
                {
                    // 如果是无参数的Instance调用，创建实例
                    if (instance.Ids.Count == 0)
                    {
                        return nestedClassInfo.CreateInstance(Manager);
                    }
                }

                // 如果没有找到，抛出AttributeError异常
                throw new AttributeError(this, methodName, Id.IdName);
            }
            default:
                // 其他情况，直接运行表达式
                // 在调用类方法时，将当前实例添加到AnyInfo中，以便this关键字访问
                Manager.Set(new LangId("this"), this);
                var defaultResult = dotExpression.Run(Manager);
                return defaultResult;
        }
    }

    public void Set(LangId id, LangValueType langValueType)
    {
        Result.TryAdd(id.IdName, langValueType);
    }

    /// <summary>
    /// 获取对象的属性值，用于编译模式下的对象解构赋值
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    /// <returns>属性值</returns>
    public LangValueType GetPropertyValue(string propertyName)
    {
        if (Result.TryGetValue(propertyName, out var value))
        {
            return value;
        }

        throw new AttributeError(this, propertyName, Id.IdName);
    }

    /// <summary>
    /// 同步管理器中的变量回实例的Result字典
    /// </summary>
    /// <param name="manager">变量管理器</param>
    private void SyncVariablesFromManager(VariateManager manager)
    {
        // 方法调用完成后，将管理器中修改的变量同步回实例的 Result 字典
        // 对于直接修改的变量（如logs <- logs + "text"），需要从manager同步回实例
        // 但对于通过this.x <- value修改的变量，SetStatement已经直接修改了实例的Result字典
        foreach (var member in Result.Keys.ToList())
        {
            var updatedValue = manager.GetValue(new LangId(member));
            if (updatedValue != null)
            {
                // 比较字符串表示，如果不同则需要同步
                var currentValueStr = Result[member].ToString();
                var updatedValueStr = updatedValue.ToString();

                if (currentValueStr != updatedValueStr)
                {
                    // 如果新值包含内容而原值不包含，很可能是直接变量赋值需要同步
                    // 特别是对于字符串变量，任何修改都应该同步
                    if (currentValueStr != updatedValueStr && !currentValueStr.Contains(updatedValueStr))
                    {
                        Result[member] = updatedValue;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 查找嵌套类
    /// </summary>
    /// <param name="className">类名</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>找到的类型模板，如果未找到则返回null</returns>
    private TypeTemplate? FindNestedClass(string className, VariateManager manager)
    {
        // 首先检查当前实例的Variates中是否有嵌套类
        foreach (var (memberId, memberExpr) in Variates)
        {
            if (memberId.IdName == className && memberExpr is TypeTemplate typeTemplate)
            {
                return typeTemplate;
            }
        }

        // 如果当前实例的Variates中没有，尝试从Manager获取类型模板
        if (Manager.GetAny(new LangId(Id.IdName)) is TypeTemplate currentTypeTemplate)
        {
            // 检查该类型是否有嵌套类
            foreach (var (memberId, memberExpr) in currentTypeTemplate.Variates)
            {
                if (memberId.IdName == className && memberExpr is TypeTemplate nestedTypeTemplate)
                {
                    return nestedTypeTemplate;
                }
            }
        }

        // 如果直接通过Id找不到，尝试查找所有注册的类型
        foreach (var importInfo in Manager.ImportInfos)
        {
            if (importInfo is TypeTemplate tt && tt.ClassName == Id.IdName)
            {
                foreach (var (memberId, memberExpr) in tt.Variates)
                {
                    if (memberId.IdName == className && memberExpr is TypeTemplate nestedTypeTemplate)
                    {
                        return nestedTypeTemplate;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 获取父类实例（super关键字对应的值）
    /// </summary>
    /// <returns>父类实例，如果没有父类则返回null</returns>
    public AnyLangValue? GetSuperInstance()
    {
        try
        {
            // 获取当前实例的类型模板
            var currentTypeTemplate = Manager.GetAny(new LangId(Id.IdName)) as TypeTemplate;
            if (currentTypeTemplate?.ParentClassName == null)
            {
                return null; // 没有父类
            }

            // 获取父类类型模板
            var parentTypeTemplate = Manager.GetAny(new LangId(currentTypeTemplate.ParentClassName)) as TypeTemplate;
            if (parentTypeTemplate == null)
            {
                return null; // 父类类型模板未找到
            }

            // 创建父类实例
            var superInstance = parentTypeTemplate.CreateInstance(Manager);

            // 将当前实例的字段值复制到父类实例中（只复制父类拥有的字段）
            foreach (var member in superInstance.Result)
            {
                if (Result.TryGetValue(member.Key, out var currentValue))
                {
                    superInstance.Set(new LangId(member.Key), currentValue);
                }
            }

            return superInstance;
        }
        catch
        {
            return null; // 出现任何错误都返回null
        }
    }

    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
    {
        if (otherLangValueType is not TypeLangValue type)
            throw new TypeError(this, "Type", otherLangValueType.GetType().Name);
        var info = manager.GetAny(new LangId(type.Value ?? ""));
        if (info is not TypeTemplate typeTemplate)
        {
            throw new TypeError(this, "Type", otherLangValueType.GetType().Name);
        }

        var typeAny = typeTemplate.CreateInstance(manager);

        foreach (var a in Result)
        {
            typeAny.Set(new LangId(a.Key), a.Value);
        }

        return typeAny;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append('{');
        for (var i = 0; i < Variates.Count; i++)
        {
            var variable = Variates.ElementAt(i);
            builder.Append($"{(i == 0 ? "" : ",")}\"{variable.Key}\":{variable.Value}");
        }

        builder.Append('}');
        return builder.ToString();
    }

    public override string ToDisplayString()
    {
        return $"Class {Id}";
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 创建一个字典来存储AnyValue的属性
        var dictType = typeof(Dictionary<string, object>);
        var constructor = dictType.GetConstructor(Type.EmptyTypes)!;

        // 实例化字典
        ilGenerator.Emit(OpCodes.Newobj, constructor);

        // 遍历所有属性，将它们添加到字典中
        foreach (var variate in Variates)
        {
            // 复制字典引用到堆栈上
            ilGenerator.Emit(OpCodes.Dup);

            // 加载属性名
            ilGenerator.Emit(OpCodes.Ldstr, variate.Key.IdName);

            // 加载属性值
            variate.Value.LoadIlValue(ilGenerator, local);

            // 确保值是对象类型，如果是值类型则装箱
            var valueType = variate.Value.OutputType(local);
            if (valueType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, valueType);
            }

            // 调用字典的Add方法
            var addMethod = dictType.GetMethod("Add", [typeof(string), typeof(object)])!;
            ilGenerator.Emit(OpCodes.Callvirt, addMethod);
        }
    }

    public override Type? OutputType(LocalManager local)
    {
        return local.ClassVar.GetValueOrDefault(Id.IdName);
    }
}