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
public partial class GenericInstanceExpression : LangExpression
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
                    // 尝试从 manager 中获取用户定义的类
                    var value = manager.GetAny(new LangId(typeArgName));
                    if (value is TypeTemplate tt)
                    {
                        // 创建 ClassTypeInfo 代理
                        typeInfo = new ClassTypeInfo(
                            tt.ClassName,
                            baseType: null, // 暂时为 null，因为需要递归解析父类
                            interfaceNames: tt.ImplementsNames
                        );
                    }
                }

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

        // 获取基础表达式名称
        if (BaseExpression is not LangId identifier)
        {
            return typeof(object);
        }

        var name = identifier.IdName;

        // 判断是泛型类还是泛型函数
        if (local.GenericClasses.ContainsKey(name))
        {
            // 泛型类实例化：返回特化后的类型
            var typeTemplate = local.GenericClasses[name];
            var typeMapping = CreateTypeMapping(typeTemplate.GenericParameters, local);
            var specializedType = GenericClassSpecializer.CreateSpecialization(typeTemplate, typeMapping, local);
            return specializedType;
        }

        if (local.GenericFunctions.ContainsKey(name))
        {
            // 泛型函数调用：返回函数的返回类型
            var genericFunc = local.GenericFunctions[name];
            var typeMapping = CreateTypeMapping(genericFunc.GenericParameters, local);
            var resolver = new GenericTypeResolver(typeMapping, local, local.Interpreter);
            return resolver.ResolveReturnType(genericFunc.Id?.AssumptionType);
        }

        // 默认返回 object 类型
        return typeof(object);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 编译器模式下的IL生成
        // 处理泛型类实例化和泛型函数调用

        // 获取基础表达式名称
        if (BaseExpression is not LangId identifier)
        {
            throw new InvalidOperationError(this, "编译器模式下泛型表达式必须使用简单的标识符");
        }

        var name = identifier.IdName;

        // 尝试判断是泛型类还是泛型函数
        bool isGenericClass = local.GenericClasses.ContainsKey(name);
        bool isGenericFunction = local.GenericFunctions.ContainsKey(name);

        if (isGenericClass)
        {
            // 处理泛型类实例化
            HandleGenericClassInstantiation(ilGenerator, local, name);
        }
        else if (isGenericFunction)
        {
            // 处理泛型函数调用
            HandleGenericFunctionCall(ilGenerator, local, name);
        }
        else
        {
            throw new InvalidOperationError(this, $"找不到泛型类或泛型函数定义：{name}");
        }
    }

    /// <summary>
    /// 处理泛型类实例化
    /// </summary>
    private void HandleGenericClassInstantiation(ILGenerator ilGenerator, LocalManager local, string className)
    {
        // 获取泛型类模板
        var typeTemplate = local.GenericClasses[className];

        // 创建类型参数映射
        var typeMapping = CreateTypeMapping(typeTemplate.GenericParameters, local);

        // 创建或获取特化类型
        var specializedType = GenericClassSpecializer.CreateSpecialization(typeTemplate, typeMapping, local);

        // 创建实例
        var constructor = specializedType.GetConstructor(Type.EmptyTypes);
        if (constructor is null)
        {
            throw new InvalidOperationError(this, $"找不到类 {className} 的无参构造函数");
        }

        ilGenerator.Emit(OpCodes.Newobj, constructor);

        // 如果有调用参数，调用 init 方法
        if (IsFunctionCall && CallArguments!.Count > 0)
        {
            // 复制实例引用，用于调用 init 方法
            ilGenerator.Emit(OpCodes.Dup);

            // 加载参数
            var argTypes = new Type[CallArguments.Count];
            for (int i = 0; i < CallArguments.Count; i++)
            {
                CallArguments[i].LoadIlValue(ilGenerator, local);
                argTypes[i] = CallArguments[i].OutputType(local) ?? typeof(object);
            }

            // 查找 init 方法
            var initMethod = specializedType.GetMethod("init", argTypes);
            if (initMethod is null)
            {
                // 尝试查找任何名为 init 的方法（忽略参数类型精确匹配，依赖运行时/CLR检查或后续改进）
                // 这是一个简单的回退策略，更严谨的做法是实现完整的重载解析
                initMethod = specializedType.GetMethod("init");
            }

            if (initMethod is null)
            {
                 throw new InvalidOperationError(this, $"找不到类 {className} 的匹配 init 方法");
            }

            ilGenerator.Emit(OpCodes.Callvirt, initMethod);
            
            // 如果 init 方法有返回值（虽然通常是 void），需要弹出
            if (initMethod.ReturnType != typeof(void))
            {
                ilGenerator.Emit(OpCodes.Pop);
            }
        }
    }

    /// <summary>
    /// 处理泛型函数调用
    /// </summary>
    private void HandleGenericFunctionCall(ILGenerator ilGenerator, LocalManager local, string funcName)
    {
        // 获取泛型函数定义
        var genericFunc = local.GenericFunctions[funcName];

        // 创建类型参数映射
        var typeMapping = CreateTypeMapping(genericFunc.GenericParameters, local);

        // 构建特化键
        // 使用 GenericTypeResolver 来解析类型名称，获取正确的 Type.Name
        var resolver = new GenericTypeResolver(typeMapping, local, local.Interpreter);
        var typeArgNames = TypeArguments.Select(t => resolver.ResolveType(t)?.Name ?? "Object").ToArray();
        var specializationKey = $"{funcName}${string.Join("_", typeArgNames)}";

        // 检查是否已经存在特化方法
        if (!local.GenericSpecializations.TryGetValue(specializationKey, out var specializedMethod))
        {
            // 创建新的特化方法
            specializedMethod = GenericMethodSpecializer.CreateSpecialization(genericFunc, typeMapping, local);
        }

        // 生成所有调用参数的IL代码
        if (CallArguments is not null)
        {
            foreach (var arg in CallArguments)
            {
                arg.LoadIlValue(ilGenerator, local);
            }
        }

        // 调用特化方法
        ilGenerator.Emit(OpCodes.Call, specializedMethod);
    }

    /// <summary>
    /// 创建类型参数映射
    /// </summary>
    private Dictionary<string, Type> CreateTypeMapping(List<GenericParameter>? genericParameters, LocalManager local)
    {
        var typeMapping = new Dictionary<string, Type>();
        var resolver = new GenericTypeResolver(new Dictionary<string, Type>(), local, local.Interpreter);

        if (genericParameters is not null)
        {
            for (int i = 0; i < Math.Min(TypeArguments.Count, genericParameters.Count); i++)
            {
                var genericParamName = genericParameters[i].Name;
                var typeArgumentName = TypeArguments[i];
                var type = resolver.ResolveType(typeArgumentName) ?? typeof(object);
                typeMapping[genericParamName] = type;
            }
        }
        else
        {
            // 如果没有泛型参数信息，使用默认映射
            for (int i = 0; i < TypeArguments.Count; i++)
            {
                var typeArgumentName = TypeArguments[i];
                var type = resolver.ResolveType(typeArgumentName) ?? typeof(object);
                var genericParamName = $"T{i + 1}";
                typeMapping[genericParamName] = type;
            }
        }

        return typeMapping;
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