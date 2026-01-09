using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;
using System.Reflection.Emit;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 泛型实例化表达式
/// 例如: Box&lt;int>(), map&lt;string>(arr, func)
/// </summary>
public class GenericInstanceExpression : LangExpression
{
    /// <summary>
    /// 基础表达式（通常是标识符）
    /// 例如: Box, map
    /// </summary>
    public LangExpression BaseExpression { get; }

    /// <summary>
    /// 类型参数列表（类型名称字符串）
    /// 例如: ["int"], ["string", "Person"]
    /// </summary>
    public List<string> TypeArguments { get; }

    /// <summary>
    /// 调用参数（如果是函数调用）
    /// 例如: Box&lt;int>() 时为空列表，map&lt;string>(arr, func) 时为 [arr, func]
    /// </summary>
    public List<LangExpression>? CallArguments { get; }

    /// <summary>
    /// 构造函数（泛型类实例化）
    /// </summary>
    public GenericInstanceExpression(
        LangExpression baseExpression,
        List<string> typeArguments,
        SourcePosition position = default)
    {
        BaseExpression = baseExpression;
        TypeArguments = typeArguments;
        CallArguments = null;
        Position = position;
    }

    /// <summary>
    /// 构造函数（泛型函数调用）
    /// </summary>
    public GenericInstanceExpression(
        LangExpression baseExpression,
        List<string> typeArguments,
        List<LangExpression> callArguments,
        SourcePosition position = default)
    {
        BaseExpression = baseExpression;
        TypeArguments = typeArguments;
        CallArguments = callArguments;
        Position = position;
    }

    /// <summary>
    /// 是否为函数调用
    /// </summary>
    public bool IsFunctionCall => CallArguments is not null;

    public override LangValueType Run(VariateManager manager)
    {
        // 获取基础类型或函数
        var baseValue = BaseExpression.Run(manager);

        // 从解释器获取类型注解管理器
        if (manager.Interpreter is null)
        {
            throw new InvalidOperationError(this, "无法获取 TypeAnnotationManager：解释器未初始化");
        }

        var typeAnnotationManager = manager.Interpreter.TypeAnnotationManager;

        var resolvedTypeArgs = new Dictionary<string, ITypeInfo>();

        // 处理泛型类
        if (baseValue is TypeTemplate typeTemplate)
        {
            if (typeTemplate.GenericParameters is null || typeTemplate.GenericParameters.Count == 0)
            {
                throw new InvalidOperationError(this, $"类型 {typeTemplate.ClassName} 不是泛型类");
            }

            // 验证类型参数数量
            if (TypeArguments.Count != typeTemplate.GenericParameters.Count)
            {
                throw new InvalidOperationError(this,
                    $"类型参数数量不匹配：期望 {typeTemplate.GenericParameters.Count} 个，实际 {TypeArguments.Count} 个");
            }

            // 解析类型参数
            for (int i = 0; i < TypeArguments.Count; i++)
            {
                var paramName = typeTemplate.GenericParameters[i].Name;
                var typeArgName = TypeArguments[i];

                var typeInfo = typeAnnotationManager.GetTypeFamily().GetType(typeArgName);
                if (typeInfo is null)
                {
                    throw new InvalidOperationError(this, $"未知的类型: {typeArgName}");
                }

                resolvedTypeArgs[paramName] = typeInfo;
            }

            // 实例化泛型类
            var instantiatedTemplate = typeTemplate.InstantiateGeneric(resolvedTypeArgs, typeAnnotationManager);

            // 如果后面跟着调用参数，创建实例
            if (IsFunctionCall)
            {
                var instance = instantiatedTemplate.CreateInstanceV2(manager);
                instance.Init(manager.Interpreter);

                // 调用用户定义的 init 构造函数
                instance.CallInit(CallArguments!, manager);

                return instance;
            }

            // 否则返回实例化的类模板
            return instantiatedTemplate;
        }

        // 处理泛型函数
        if (baseValue is FuncLangValue funcValue)
        {
            if (funcValue.GenericParameters is null || funcValue.GenericParameters.Count == 0)
            {
                throw new InvalidOperationError(this, $"函数 {funcValue.Id?.IdName} 不是泛型函数");
            }

            // 验证类型参数数量
            if (TypeArguments.Count != funcValue.GenericParameters.Count)
            {
                throw new InvalidOperationError(this,
                    $"类型参数数量不匹配：期望 {funcValue.GenericParameters.Count} 个，实际 {TypeArguments.Count} 个");
            }

            // 解析类型参数
            for (int i = 0; i < TypeArguments.Count; i++)
            {
                var paramName = funcValue.GenericParameters[i].Name;
                var typeArgName = TypeArguments[i];

                var typeInfo = typeAnnotationManager.GetTypeFamily().GetType(typeArgName);
                if (typeInfo is null)
                {
                    throw new InvalidOperationError(this, $"未知的类型: {typeArgName}");
                }

                resolvedTypeArgs[paramName] = typeInfo;
            }

            // 实例化泛型函数
            var instantiatedFunc = funcValue.InstantiateGeneric(resolvedTypeArgs, typeAnnotationManager);

            // 调用实例化后的函数
            if (IsFunctionCall)
            {
                return instantiatedFunc.Run(manager, CallArguments!);
            }

            // 返回实例化的函数（作为一等公民）
            return instantiatedFunc;
        }

        throw new InvalidOperationError(this, $"表达式 {BaseExpression} 不是泛型类型或泛型函数");
    }

    public override Type? OutputType(LocalManager local)
    {
        // 编译器模式下的类型推断
        // 对于泛型实例，需要根据基础表达式和类型参数推断最终类型
        
        // 获取基础表达式的类型
        var baseType = BaseExpression.OutputType(local);
        
        // 如果是函数调用，返回函数的返回类型
        if (IsFunctionCall)
        {
            // 简化处理：暂时返回 object 类型
            // 实际实现需要分析泛型函数的返回类型和类型参数
            return typeof(object);
        }
        
        // 否则返回实例化后的类型
        // 简化处理：暂时返回 object 类型
        // 实际实现需要根据类型参数确定具体类型
        return typeof(object);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 编译器模式下的IL生成
        // 处理泛型函数调用：func<T>(args)
        
        if (!IsFunctionCall)
        {
            throw new InvalidOperationError(this, "编译器模式下暂时不支持泛型类实例化，仅支持泛型函数调用");
        }
        
        // 获取基础函数名称
        if (BaseExpression is not LangId functionId)
        {
            throw new InvalidOperationError(this, "编译器模式下泛型函数调用必须使用简单的函数标识符");
        }
        
        var funcName = functionId.IdName;
        
        // 首先尝试从泛型函数定义中查找
        if (!local.GenericFunctions.TryGetValue(funcName, out var genericFunc))
        {
            // 如果找不到，尝试从普通函数定义中查找（可能是基础版本）
            if (local.DelegateVar.TryGetValue(funcName, out var baseMethod))
            {
                // 创建一个临时的泛型函数定义用于特化
                // 这里需要根据实际情况来创建
                // 暂时跳过泛型特化，使用基础方法
                var baseMethodInfo = baseMethod;
                
                // 生成所有调用参数的IL代码
                foreach (var arg in CallArguments!)
                {
                    arg.LoadIlValue(ilGenerator, local);
                }
                
                // 调用基础方法
                ilGenerator.Emit(OpCodes.Call, baseMethodInfo);
                return;
            }
            
            throw new InvalidOperationError(this, $"找不到泛型函数定义：{funcName}");
        }
        
        // 创建类型参数映射
        var typeMapping = new Dictionary<string, Type>();
        
        // 解析类型参数并映射到泛型参数名
        if (genericFunc.GenericParameters is not null)
        {
            for (int i = 0; i < Math.Min(TypeArguments.Count, genericFunc.GenericParameters.Count); i++)
            {
                var genericParamName = genericFunc.GenericParameters[i].Name;
                var typeArgumentName = TypeArguments[i];
                var type = ResolveSimpleType(typeArgumentName);
                typeMapping[genericParamName] = type;
            }
        }
        else
        {
            // 如果没有泛型参数信息，使用默认映射
            for (int i = 0; i < TypeArguments.Count; i++)
            {
                var typeArgumentName = TypeArguments[i];
                var type = ResolveSimpleType(typeArgumentName);
                var genericParamName = $"T{i + 1}";
                typeMapping[genericParamName] = type;
            }
        }
        
        // 构建特化键
        var typeArgNames = TypeArguments.Select(t => ResolveSimpleType(t).Name).ToArray();
        var specializationKey = $"{funcName}${string.Join("_", typeArgNames)}";
        
        // 检查是否已经存在特化方法
        if (local.GenericSpecializations.TryGetValue(specializationKey, out var specializedMethod))
        {
            // 使用现有的特化方法
        }
        else
        {
            // 创建新的特化方法
            specializedMethod = GenericMethodSpecializer.CreateSpecialization(genericFunc, typeMapping, local);
        }
        
        // 生成所有调用参数的IL代码
        foreach (var arg in CallArguments!)
        {
            arg.LoadIlValue(ilGenerator, local);
        }
        
        // 调用特化方法
        ilGenerator.Emit(OpCodes.Call, specializedMethod);
    }
    
    /// <summary>
    /// 解析简单类型名称为System.Type
    /// </summary>
    private static Type ResolveSimpleType(string typeName)
    {
        return typeName.ToLower() switch
        {
            "int" => typeof(int),
            "string" => typeof(string),
            "double" => typeof(double),
            "bool" => typeof(bool),
            "char" => typeof(char),
            "void" => typeof(void),
            "object" => typeof(object),
            _ => typeof(object)
        };
    }

    public override TResult Accept<TResult>(Visitor.IVisitor<TResult> visitor)
    {
        // TODO: Visitor 模式支持
        throw new NotSupportedException("GenericInstanceExpression 暂不支持 Visitor 模式");
    }

    public override string ToString()
    {
        var typeArgsStr = string.Join(", ", TypeArguments);
        if (IsFunctionCall)
        {
            var argsStr = string.Join(", ", CallArguments!.Select(a => a.ToString()));
            return $"{BaseExpression}<{typeArgsStr}>({argsStr})";
        }

        return $"{BaseExpression}<{typeArgsStr}>";
    }
}