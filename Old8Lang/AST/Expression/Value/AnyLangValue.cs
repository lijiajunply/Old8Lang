using Old8Lang.LangParser;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 类
/// </summary>
public class AnyLangValue : LangValueType
{
    public readonly Dictionary<ClassMemberId, LangExpression> Variates;
    public readonly Dictionary<string, LangValueType> Result = [];
    public readonly LangId Id;
    
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public readonly VariateManager Manager;

    public AnyLangValue(LangId id, Dictionary<ClassMemberId, LangExpression> variates, SourcePosition position = default) :
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
    public void Init(IMiniInterpreter interpreter)
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

    public AnyLangValue(Dictionary<ClassMemberId, LangExpression> variates, SourcePosition position = default) : base(position)
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

        Manager.ImportInfos.AddRange(manager.ImportInfos.Where(x => x is not FuncLangValue).ToList());

        foreach (var variable in Variates.Keys)
        {
            // 运行变量表达式，并将结果添加到Result字典中
            var value = Variates[variable].Run(manager);
            Result.Add(variable.IdName, value);
        }

        return this;
    }

    public override LangValueType Dot(LangExpression dotExpression)
    {
        switch (dotExpression)
        {
            case LangId id:
            {
                // 首先检查Result字典中是否有该属性
                if (Result.TryGetValue(id.IdName, out var value))
                {
                    // 检查value是否是函数类型
                    if (value is FuncLangValue funcValue)
                    {
                        // 在调用类方法时，将当前实例作为"this"变量添加到变量储存器中
                        Manager.Set(new LangId("this"), this);
                        var funcResult = funcValue.Run(Manager);
                        // 方法执行完毕后，移除"this"变量
                        // 注意：这里不需要手动移除，因为FuncLangValue.Run方法会调用RemoveChildren()
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

                        // 调用方法时使用当前管理器，这样可以访问外部变量
                        var funcResult = funcValue.Run(currentManager, methodArgs);
                        return funcResult;
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
                        return funcResult;
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