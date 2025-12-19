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

        // 初始化Result字典，运行变量表达式
        // 注意：不要在这里运行表达式，因为此时Interpreter还没有被设置
        // 而是在Instance.Run方法中，当调用init方法前设置Interpreter
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
                // 查找字段的ClassMemberId，获取访问修饰符
                ClassMemberId? memberId = null;
                foreach (var (key, _) in Variates)
                {
                    if (key.IdName == id.IdName)
                    {
                        memberId = key;
                        break;
                    }
                }

                // 检查访问权限
                bool isPrivate = memberId?.HasModifier(AccessModifierType.Private) ?? false;
                bool isProtected = memberId?.HasModifier(AccessModifierType.Protected) ?? false;
                bool isStatic = memberId?.HasModifier(AccessModifierType.Static) ?? false;

                // 检查调用上下文是否在类内部
                // 关键：需要检查外部管理器（ExternalManager）中的 this，而不是当前实例的 Manager
                bool isInternalAccess = false;

                // 首先检查自己的 Manager 中是否有 this（在类方法内部访问时）
                try
                {
                    var thisInManager = Manager.GetValue(new LangId("this"));
                    if (ReferenceEquals(thisInManager, this))
                    {
                        isInternalAccess = true;
                    }
                }
                catch
                {
                    // Manager 中没有 this
                }

                // 如果还没有确认是内部访问，再检查 ExternalManager
                if (!isInternalAccess && ExternalManager != null)
                {
                    try
                    {
                        var thisInfo = ExternalManager.GetValue(new LangId("this"));
                        // GetValue 返回 LangValueType，AnyLangValue 继承自 LangValueType
                        // 所以可以直接比较引用
                        if (ReferenceEquals(thisInfo, this))
                        {
                            isInternalAccess = true;
                        }
                    }
                    catch
                    {
                        // ExternalManager 中没有 this，说明不在类内部
                    }
                }

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

                // 首先检查Result字典中是否有该属性
                if (Result.TryGetValue(id.IdName, out var value))
                {
                    // 检查value是否是函数类型
                    if (value is FuncLangValue funcValue)
                    {
                        // 在调用类方法时，将当前实例作为"this"变量添加到变量储存器中
                        Manager.Set(new LangId("this"), this);
                        var funcResult = funcValue.Run(Manager);
                        return funcResult;
                    }

                    return value;
                }

                // 检查Variates字典中是否有该属性（成员变量）
                // 使用类名创建ClassMemberId进行查找
                var classMemberId = new ClassMemberId(id);
                if (Variates.TryGetValue(classMemberId, out var variate))
                {
                    // 如果有，运行它并返回结果
                    return variate.Run(Manager);
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

                // 检查Result字典中是否有该方法
                if (Result.TryGetValue(methodName, out var value))
                {
                    if (value is FuncLangValue funcValue)
                    {
                        // 处理方法参数，先运行参数表达式，这样可以访问外部变量
                        List<LangExpression> methodArgs = [];
                        methodArgs.AddRange(instance.Ids);

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
                            // 调用方法时使用当前管理器，这样可以访问外部变量
                            var funcResult = funcValue.Run(currentManager, methodArgs);

                            // 方法调用完成后，将管理器中修改的变量同步回实例的 Result 字典
                            // 对于直接修改的变量（如logs <- logs + "text"），需要从manager同步回实例
                            // 但对于通过this.x <- value修改的变量，SetStatement已经直接修改了实例的Result字典
                            // 我们需要比较并只同步真正需要同步的变量
                            foreach (var member in Result.Keys.ToList())
                            {
                                var updatedValue = currentManager.GetValue(new LangId(member));
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

                            return funcResult;
                        }
                        finally
                        {
                            // 恢复原始的 ExternalManager
                            ExternalManager = originalExternalManager;
                        }
                    }
                }

                // 检查Variates字典中是否有该方法
                if (Variates.TryGetValue(new ClassMemberId(instance.Id), out var variate))
                {
                    // 在调用类方法时，将当前实例添加到变量储存器中，以便this关键字访问
                    Manager.Set(new LangId("this"), this);

                    // 将实例的所有成员变量添加到Manager中，以便方法内部直接访问
                    foreach (var member in Result)
                    {
                        Manager.Set(new LangId(member.Key), member.Value);
                    }

                    // 使用外部管理器或内部管理器作为回退
                    var currentManager = ExternalManager ?? Manager;

                    var methodValue = variate.Run(currentManager);
                    if (methodValue is FuncLangValue funcValue)
                    {
                        // 处理方法参数，先运行参数表达式，这样可以访问外部变量
                        List<LangExpression> methodArgs = [];
                        methodArgs.AddRange(instance.Ids);

                        // 在调用类方法时，将当前实例添加到变量储存器中，以便this关键字访问
                        currentManager.Set(new LangId("this"), this);

                        // 将实例的所有成员变量添加到Manager中，以便方法内部直接访问
                        foreach (var member in Result)
                        {
                            currentManager.Set(new LangId(member.Key), member.Value);
                        }

                        // 调用方法时使用当前管理器，这样可以访问外部变量
                        var funcResult = funcValue.Run(currentManager, methodArgs);

                        // 方法调用完成后，将管理器中修改的变量同步回实例的 Result 字典
                        // 对于直接修改的变量（如logs <- logs + "text"），需要从manager同步回实例
                        // 但对于通过this.x <- value修改的变量，SetStatement已经直接修改了实例的Result字典
                        foreach (var member in Result.Keys.ToList())
                        {
                            var updatedValue = currentManager.GetValue(new LangId(member));
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

                        return funcResult;
                    }
                }

                // 如果没有找到，检查是否是嵌套类访问
                // 首先检查当前实例的Variates中是否有嵌套类
                foreach (var (memberId, memberExpr) in Variates)
                {
                    if (memberId.IdName == methodName && memberExpr is TypeTemplate)
                    {
                        // 找到嵌套类，如果是无参数的Instance调用，创建实例
                        if (instance.Ids.Count == 0)
                        {
                            var nestedTypeTemplate = (TypeTemplate)memberExpr.Run(Manager);
                            return nestedTypeTemplate.CreateInstance(Manager);
                        }
                    }
                }

                // 如果当前实例的Variates中没有，尝试从Manager获取类型模板
                var typeTemplate = Manager.GetAny(new LangId(Id.IdName)) as TypeTemplate;

                // 如果直接通过Id找不到，尝试查找所有注册的类型
                if (typeTemplate == null)
                {
                    // 查找所有ImportInfos中匹配的类型模板
                    foreach (var importInfo in Manager.ImportInfos)
                    {
                        if (importInfo is TypeTemplate tt && tt.ClassName == Id.IdName)
                        {
                            typeTemplate = tt;
                            break;
                        }
                    }
                }
                if (typeTemplate != null)
                {
                    // 检查该类型是否有嵌套类
                    foreach (var (memberId, memberExpr) in typeTemplate.Variates)
                    {
                        if (memberId.IdName == methodName && memberExpr is TypeTemplate)
                        {
                            // 找到嵌套类，如果是无参数的Instance调用，创建实例
                            if (instance.Ids.Count == 0)
                            {
                                var nestedTypeTemplate = (TypeTemplate)memberExpr.Run(Manager);
                                return nestedTypeTemplate.CreateInstance(Manager);
                            }
                        }
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