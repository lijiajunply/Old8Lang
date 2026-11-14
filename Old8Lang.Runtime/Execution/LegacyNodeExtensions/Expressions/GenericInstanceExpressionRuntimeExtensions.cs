using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Specialization;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>
/// GenericInstanceExpression 的执行/编译辅助实现（阶段 2 从语法节点迁出）。
/// </summary>
internal static class GenericInstanceExpressionRuntimeExtensions
{
    /// <summary>
    /// 将 ParsedTypeAnnotation 转换为 ITypeInfo
    /// </summary>
    internal static ITypeInfo? ConvertParsedTypeToTypeInfo(this GenericInstanceExpression node, ParsedTypeAnnotation parsedType, TypeAnnotationManager typeAnnotationManager, VariateManager manager)
    {
        // 对于可空类型、联合类型、交叉类型，我们直接使用完整的类型名称字符串
        // 因为 TypeSystem 目前没有专门的 NullableTypeInfo、UnionTypeInfo、IntersectionTypeInfo 类
        if (parsedType.IsNullable || parsedType.IsUnion || parsedType.IsIntersection)
        {
            // 使用完整的类型名称（包括可空标记、联合、交叉等）
            var fullTypeName = parsedType.GetFullName();
            // 尝试从 TypeFamily 获取
            var existingType = typeAnnotationManager.GetTypeFamily().GetType(fullTypeName);
            if (existingType != null)
            {
                return existingType;
            }
            // 如果不存在，创建一个 PrimitiveTypeInfo 作为占位符
            // 这样可以保留类型名称信息，供后续使用
            return new PrimitiveTypeInfo(fullTypeName);
        }

        // 处理泛型类型
        if (parsedType is { IsGeneric: true, GenericArguments: not null })
        {
            var baseType = typeAnnotationManager.GetTypeFamily().GetType(parsedType.BaseType);
            if (baseType is GenericTypeInfo genericType)
            {
                var typeArgs = new Dictionary<string, ITypeInfo>();
                for (int i = 0; i < parsedType.GenericArguments.Count && i < genericType.TypeParameters.Count; i++)
                {
                    var paramName = genericType.TypeParameters[i];
                    var argType = node.ConvertParsedTypeToTypeInfo(parsedType.GenericArguments[i], typeAnnotationManager, manager);
                    if (argType != null)
                    {
                        typeArgs[paramName] = argType;
                    }
                }
                return genericType.Instantiate(typeArgs);
            }
        }

        // 处理基本类型
        var typeInfo = typeAnnotationManager.GetTypeFamily().GetType(parsedType.BaseType);
        if (typeInfo != null)
        {
            return typeInfo;
        }

        // 尝试从 manager 中获取用户定义的类
        var value = manager.GetAny(new LangId(parsedType.BaseType));
        if (value is TypeTemplate tt)
        {
            // 创建 ClassTypeInfo 代理
            return new ClassTypeInfo(
                tt.ClassName,
                baseType: null, // 暂时为 null，因为需要递归解析父类
                interfaceNames: tt.ImplementsNames
            );
        }

        return null;
    }
    /// <summary>
    /// 创建类型参数映射
    /// </summary>
    internal static Dictionary<string, Type> CreateTypeMapping(this GenericInstanceExpression node, List<GenericParameter>? genericParameters, LocalManager local)
    {
        var typeMapping = new Dictionary<string, Type>();
        var resolver = local.CurrentGenericTypeResolver ??
                       new GenericTypeResolver(new Dictionary<string, Type>(), local, local.Interpreter);

        if (genericParameters is not null)
        {
            for (int i = 0; i < Math.Min(node.TypeArguments.Count, genericParameters.Count); i++)
            {
                var genericParamName = genericParameters[i].Name;
                var typeArgumentName = node.TypeArguments[i];
                var type = resolver.ResolveType(typeArgumentName) ?? typeof(object);
                typeMapping[genericParamName] = type;
            }
        }
        else
        {
            // 如果没有泛型参数信息，使用默认映射
            for (int i = 0; i < node.TypeArguments.Count; i++)
            {
                var typeArgumentName = node.TypeArguments[i];
                var type = resolver.ResolveType(typeArgumentName) ?? typeof(object);
                var genericParamName = $"T{i + 1}";
                typeMapping[genericParamName] = type;
            }
        }

        return typeMapping;
    }
    /// <summary>
    /// 处理泛型函数调用
    /// </summary>
    internal static void HandleGenericFunctionCall(this GenericInstanceExpression node, ILGenerator ilGenerator, LocalManager local, string funcName)
    {
        // 获取泛型函数定义
        var genericFunc = local.GenericFunctions[funcName];

        // 创建类型参数映射
        var typeMapping = node.CreateTypeMapping(genericFunc.GenericParameters, local);

        // 构建特化键
        // 使用 GenericTypeResolver 来解析类型名称，获取正确的 Type.Name
        var resolver = new GenericTypeResolver(typeMapping, local, local.Interpreter);
        var typeArgNames = node.TypeArguments.Select(t => resolver.ResolveType(t)?.Name ?? "Object").ToArray();
        var specializationKey = $"{funcName}${string.Join("_", typeArgNames)}";

        // 检查是否已经存在特化方法
        if (!local.GenericSpecializations.TryGetValue(specializationKey, out var specializedMethod))
        {
            // 创建新的特化方法
            specializedMethod = GenericMethodSpecializer.CreateSpecialization(genericFunc, typeMapping, local);
        }

        // 生成所有调用参数的IL代码
        if (node.CallArguments is not null)
        {
            foreach (var arg in node.CallArguments)
            {
                arg.Accept(new CompilerVisitor(ilGenerator, local));
            }
        }

        // 调用特化方法
        ilGenerator.Emit(OpCodes.Call, specializedMethod);
    }
    /// <summary>
    /// 处理泛型类实例化
    /// </summary>
    internal static void HandleGenericClassInstantiation(this GenericInstanceExpression node, ILGenerator ilGenerator, LocalManager local, string className)
    {
        // 获取泛型类模板
        var typeTemplate = local.GenericClasses[className];

        // 创建类型参数映射
        var typeMapping = node.CreateTypeMapping(typeTemplate.GenericParameters, local);

        // 创建或获取特化类型
        var specializedType = GenericClassSpecializer.CreateSpecialization(typeTemplate, typeMapping, local);

        // 创建实例
        var constructor = specializedType is TypeBuilder
            ? local.GenericClassConstructors.GetValueOrDefault(specializedType)
            : specializedType.GetConstructor(Type.EmptyTypes);
        if (constructor is null)
        {
            throw new InvalidOperationError(node, $"找不到类 {className} 的无参构造函数");
        }

        ilGenerator.Emit(OpCodes.Newobj, constructor);

        // 如果使用了调用语法（带括号），调用 init 方法（包括无参 init）
        if (node.IsFunctionCall)
        {
            // 复制实例引用，用于调用 init 方法
            ilGenerator.Emit(OpCodes.Dup);

            // 加载参数
            var argCount = node.CallArguments?.Count ?? 0;
            var argTypes = new Type[argCount];
            for (int i = 0; i < argCount; i++)
            {
                node.CallArguments![i].Accept(new CompilerVisitor(ilGenerator, local));
                argTypes[i] = node.CallArguments[i].Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
            }

            // 查找 init 方法
            // 尝试查找任何名为 init 的方法（忽略参数类型精确匹配，依赖运行时/CLR检查或后续改进）
            // 这是一个简单的回退策略，更严谨的做法是实现完整的重载解析
            var initMethod = specializedType is TypeBuilder
                ? local.GenericClassInitializers.GetValueOrDefault(specializedType)
                : specializedType.GetMethod("init", argTypes) ?? specializedType.GetMethod("init");

            if (initMethod is null)
            {
                throw new InvalidOperationError(node, $"找不到类 {className} 的匹配 init 方法");
            }

            ilGenerator.Emit(OpCodes.Callvirt, initMethod);

            // 如果 init 方法有返回值（虽然通常是 void），需要弹出
            if (initMethod.ReturnType != typeof(void))
            {
                ilGenerator.Emit(OpCodes.Pop);
            }
        }
    }
}
