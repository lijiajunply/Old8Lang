using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.TypeSystem;

using Old8Lang.AST.Visitor;

namespace Old8Lang.Compiler.Specialization;

/// <summary>
/// 泛型类特化器，用于为泛型类创建具体的类型特化版本
/// </summary>
public static class GenericClassSpecializer
{
    /// <summary>
    /// 为泛型类创建特化版本
    /// </summary>
    /// <param name="typeTemplate">泛型类模板</param>
    /// <param name="typeArguments">类型参数映射（泛型参数名 -> 具体类型）</param>
    /// <param name="local">局部管理器</param>
    /// <returns>特化后的Type</returns>
    public static Type CreateSpecialization(
        TypeTemplate typeTemplate,
        Dictionary<string, Type> typeArguments,
        LocalManager local)
    {
        if (!typeTemplate.IsGeneric || typeTemplate.GenericParameters is null)
        {
            throw new ArgumentException("类必须是泛型类");
        }

        // 构建特化键
        var typeArgNames = typeTemplate.GenericParameters
            .Select(p => typeArguments.TryGetValue(p.Name, out var type) ? type.Name : "Object")
            .ToArray();
        var specializationKey = $"{typeTemplate.ClassName}${string.Join("_", typeArgNames)}";

        // 检查是否已经存在特化
        if (local.GenericClassSpecializations.TryGetValue(specializationKey, out var existingType))
        {
            return existingType;
        }

        // 确保有动态模块
        if (local.DynamicModule is null)
        {
            var assemblyName = new AssemblyName("Old8LangDynamicAssembly");
            local.DynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            local.DynamicModule = local.DynamicAssembly.DefineDynamicModule("Old8LangDynamicModule");
        }

        // 创建类型解析器
        var resolver = new GenericTypeResolver(typeArguments, local, local.Interpreter);

        // 验证泛型约束（使用新的约束验证器）
        if (typeTemplate.GenericParameters != null)
        {
            foreach (var param in typeTemplate.GenericParameters)
            {
                if (typeArguments.TryGetValue(param.Name, out var actualType))
                {
                    var validationResult = GenericConstraintValidator.ValidateWithDotNetType(
                        param,
                        actualType,
                        typeArguments);

                    if (!validationResult.IsValid)
                    {
                        throw new ArgumentException(validationResult.ErrorMessage);
                    }
                }
            }
        }

        // 解析父类
        Type parentType = typeof(object);
        if (!string.IsNullOrEmpty(typeTemplate.ParentClassName))
        {
            var parentName = typeTemplate.ParentClassName;

            // 1. 查找父类 TypeTemplate
            TypeTemplate? parentTemplate = null;
            if (local.GenericClasses.TryGetValue(parentName, out var pTemp))
            {
                parentTemplate = pTemp;
            }
            // 2. 如果是泛型类模板，需要特化
            if (parentTemplate is { IsGeneric: true })
            {
                // 构建父类的类型参数映射
                var parentTypeArgs = new Dictionary<string, Type>();

                if (typeTemplate.ParentGenericTypeParameters != null &&
                    typeTemplate.ParentGenericTypeParameters.Count == parentTemplate.GenericParameters!.Count)
                {
                    for (int i = 0; i < typeTemplate.ParentGenericTypeParameters.Count; i++)
                    {
                        var argName = typeTemplate.ParentGenericTypeParameters[i];
                        var paramName = parentTemplate.GenericParameters[i].Name;

                        // 解析 argName
                        // 优先从当前类的 typeArguments 查找（T -> int）
                        var resolvedType = typeArguments.TryGetValue(argName, out var t) ? t :
                            // 尝试作为具体类型解析
                            resolver.ResolveType(argName);

                        parentTypeArgs[paramName] = resolvedType;
                    }

                    // 递归特化父类
                    parentType = CreateSpecialization(parentTemplate, parentTypeArgs, local);
                }
                else
                {
                    // 泛型参数不匹配，回退到 object (或者应该报错)
                    parentType = typeof(object);
                }
            }
            // 3. 检查是否是已编译的类
            else if (local.ClassVar.TryGetValue(parentName, out var pType))
            {
                parentType = pType;
            }
        }

        // 创建特化的类类型
        var typeBuilder = local.DynamicModule.DefineType(
            $"{typeTemplate.ClassName}_{string.Join("_", typeArgNames)}",
            TypeAttributes.Public | TypeAttributes.Class,
            parentType);

        // 缓存特化类型（提前缓存以支持递归引用）
        local.GenericClassSpecializations[specializationKey] = typeBuilder;

        try
        {
            // 生成类的字段和方法
            GenerateSpecializedClassMembers(typeTemplate, typeBuilder, resolver, local, typeArguments);

            // 创建类型并将缓存更新为最终类型。
            var specializedType = typeBuilder.CreateType();
            local.GenericClassSpecializations[specializationKey] = specializedType;
            return specializedType;
        }
        catch
        {
            // 失败的 TypeBuilder 不应作为有效特化留在当前编译上下文中。
            local.GenericClassSpecializations.Remove(specializationKey);
            throw;
        }
        finally
        {
            local.GenericClassConstructors.Remove(typeBuilder);
            local.GenericClassInitializers.Remove(typeBuilder);
        }
    }

    /// <summary>
    /// 生成特化类的成员（字段和方法）
    /// </summary>
    private static void GenerateSpecializedClassMembers(
        TypeTemplate typeTemplate,
        TypeBuilder typeBuilder,
        GenericTypeResolver resolver,
        LocalManager local,
        Dictionary<string, Type> typeArguments)
    {
        // 1. 生成实例字段与静态字段
        var fieldMap = new Dictionary<string, FieldBuilder>();
        foreach (var (memberId, memberExpr, isStatic) in typeTemplate.Variates
                     .Select(pair => (pair.Key, pair.Value, false))
                     .Concat(typeTemplate.StaticVariates.Select(pair => (pair.Key, pair.Value, true))))
        {
            // 跳过方法（FuncLangValue）
            if (memberExpr is FuncLangValue)
                continue;

            // 解析字段类型
            var fieldType = ResolveFieldType(memberId, resolver);

            // 创建字段
            var fieldAttributes = memberId.HasModifier(AccessModifierType.Private)
                ? FieldAttributes.Private
                : FieldAttributes.Public;
            if (isStatic)
            {
                fieldAttributes |= FieldAttributes.Static;
            }
            var fieldBuilder = typeBuilder.DefineField(
                memberId.IdName,
                fieldType,
                fieldAttributes);

            fieldMap[memberId.IdName] = fieldBuilder;
        }

        GenerateStaticFieldInitializer(typeTemplate, typeBuilder, resolver, local, fieldMap);

        // 2. 生成构造函数
        local.GenericClassConstructors[typeBuilder] = GenerateConstructor(typeBuilder, fieldMap);

        // 3. 生成实例方法与静态方法
        GenerateInstanceMethods(typeTemplate, typeBuilder, resolver, local, typeArguments, fieldMap);
        GenerateStaticMethods(typeTemplate, typeBuilder, resolver, local, typeArguments, fieldMap);
    }

    /// <summary>
    /// 解析字段类型
    /// </summary>
    private static Type ResolveFieldType(ClassMemberId memberId, GenericTypeResolver resolver)
    {
        if (!string.IsNullOrEmpty(memberId.AssumptionType))
        {
            return resolver.ResolveType(memberId.AssumptionType);
        }
        return typeof(object);
    }

    /// <summary>
    /// 为泛型特化类型发射静态字段初始化器。CLR 对 object 静态字段只给 null，
    /// 不能丢失模板中诸如 <c>count &lt;- 0</c> 的显式初始值。
    /// </summary>
    private static void GenerateStaticFieldInitializer(
        TypeTemplate typeTemplate,
        TypeBuilder typeBuilder,
        GenericTypeResolver resolver,
        LocalManager local,
        Dictionary<string, FieldBuilder> fieldMap)
    {
        var initializers = typeTemplate.StaticVariates
            .Where(pair => pair.Value is not FuncLangValue && fieldMap.TryGetValue(pair.Key.IdName, out var field) && field.IsStatic)
            .ToList();
        if (initializers.Count == 0)
        {
            return;
        }

        var initializer = typeBuilder.DefineTypeInitializer();
        var ilGenerator = initializer.GetILGenerator();
        var initializerLocal = CreateMethodLocalManager(local, resolver, typeBuilder, fieldMap);

        foreach (var (memberId, expression) in initializers)
        {
            var field = fieldMap[memberId.IdName];
            expression.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, initializerLocal));
            var valueType = expression.Accept(new TypeInferenceVisitor(initializerLocal)) ?? typeof(object);
            if (field.FieldType == typeof(object) && valueType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, valueType);
            }
            else if (field.FieldType.IsValueType && valueType == typeof(object))
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, field.FieldType);
            }

            ilGenerator.Emit(OpCodes.Stsfld, field);
        }

        ilGenerator.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// 生成构造函数
    /// </summary>
    private static ConstructorBuilder GenerateConstructor(TypeBuilder typeBuilder, Dictionary<string, FieldBuilder> fieldMap)
    {
        var constructor = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            Type.EmptyTypes);

        var ctorIL = constructor.GetILGenerator();

        // 调用基类构造函数
        ctorIL.Emit(OpCodes.Ldarg_0);
        
        var parentCtor = typeBuilder.BaseType?.GetConstructor(Type.EmptyTypes) 
                         ?? typeof(object).GetConstructor(Type.EmptyTypes)!;
        ctorIL.Emit(OpCodes.Call, parentCtor);

        // 初始化所有字段为默认值
        foreach (var (fieldName, fieldBuilder) in fieldMap)
        {
            ctorIL.Emit(OpCodes.Ldarg_0);
            GenerateDefaultValue(ctorIL, fieldBuilder.FieldType);
            ctorIL.Emit(OpCodes.Stfld, fieldBuilder);
        }

        ctorIL.Emit(OpCodes.Ret);
        return constructor;
    }

    /// <summary>
    /// 生成实例方法（两遍：先声明并预登记所有 MethodBuilder，再生成方法体）
    /// </summary>
    private static void GenerateInstanceMethods(
        TypeTemplate typeTemplate,
        TypeBuilder typeBuilder,
        GenericTypeResolver resolver,
        LocalManager local,
        Dictionary<string, Type> typeArguments,
        Dictionary<string, FieldBuilder> fieldMap)
    {
        var declared = new List<(FuncLangValue FuncValue, MethodBuilder MethodBuilder, Type[] ParamTypes, Type ReturnType)>();

        // Pass 1：声明全部实例方法并预登记到 BuildingClassMethods
        foreach (var (memberId, memberExpr) in typeTemplate.Variates.OrderBy(pair => pair.Key.IdName == "init" ? 0 : 1))
        {
            if (memberExpr is not FuncLangValue funcValue)
                continue;

            var parameterTypes = resolver.ResolveParameterTypes(funcValue.Ids!);
            var parameterNames = funcValue.Ids?.Select(id => id.IdName).ToArray() ?? [];
            var returnType = AST.Statement.IlReturnTypeHelper.Resolve(
                funcValue.Id,
                resolver.ResolveReturnType(funcValue.Id?.AssumptionType));

            var methodBuilder = typeBuilder.DefineMethod(
                memberId.IdName,
                MethodAttributes.Public,
                returnType,
                parameterTypes);

            for (var i = 0; i < parameterNames.Length; i++)
            {
                methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, parameterNames[i]);
            }

            local.RegisterBuildingClassMethod(typeBuilder, new PendingClassMethod(
                methodBuilder,
                memberId.IdName,
                parameterTypes,
                parameterNames,
                returnType,
                IsStatic: false));

            if (memberId.IdName == "init")
            {
                local.GenericClassInitializers[typeBuilder] = methodBuilder;
            }

            declared.Add((funcValue, methodBuilder, parameterTypes, returnType));
        }

        // Pass 2：生成方法体
        foreach (var (funcValue, methodBuilder, parameterTypes, returnType) in declared)
        {
            var methodIL = methodBuilder.GetILGenerator();
            var methodLocal = CreateMethodLocalManager(local, resolver, typeBuilder, fieldMap);
            GenerateMethodBody(funcValue, methodIL, methodLocal, parameterTypes, returnType);
        }
    }

    /// <summary>
    /// 生成泛型特化类型的静态方法。泛型模板把这类成员保存在 StaticVariates，
    /// 必须与普通类的静态成员一样生成真正的 CLR static 方法。
    /// </summary>
    private static void GenerateStaticMethods(
        TypeTemplate typeTemplate,
        TypeBuilder typeBuilder,
        GenericTypeResolver resolver,
        LocalManager local,
        Dictionary<string, Type> typeArguments,
        Dictionary<string, FieldBuilder> fieldMap)
    {
        var declared = new List<(FuncLangValue FuncValue, MethodBuilder MethodBuilder, Type[] ParamTypes, Type ReturnType)>();

        foreach (var (memberId, memberExpr) in typeTemplate.StaticVariates)
        {
            if (memberExpr is not FuncLangValue funcValue)
            {
                continue;
            }

            var parameterTypes = resolver.ResolveParameterTypes(funcValue.Ids!);
            var parameterNames = funcValue.Ids?.Select(id => id.IdName).ToArray() ?? [];
            var returnType = AST.Statement.IlReturnTypeHelper.Resolve(
                funcValue.Id,
                resolver.ResolveReturnType(funcValue.Id?.AssumptionType));
            var methodBuilder = typeBuilder.DefineMethod(
                memberId.IdName,
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                returnType,
                parameterTypes);

            for (var i = 0; i < parameterNames.Length; i++)
            {
                methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, parameterNames[i]);
            }

            local.RegisterBuildingClassMethod(typeBuilder, new PendingClassMethod(
                methodBuilder,
                memberId.IdName,
                parameterTypes,
                parameterNames,
                returnType,
                IsStatic: true));

            declared.Add((funcValue, methodBuilder, parameterTypes, returnType));
        }

        foreach (var (funcValue, methodBuilder, parameterTypes, returnType) in declared)
        {
            var methodLocal = CreateMethodLocalManager(local, resolver, typeBuilder, fieldMap);
            GenerateMethodBody(funcValue, methodBuilder.GetILGenerator(), methodLocal,
                parameterTypes, returnType, isStatic: true);
        }
    }

    /// <summary>
    /// 创建方法的局部管理器
    /// </summary>
    private static LocalManager CreateMethodLocalManager(
        LocalManager local,
        GenericTypeResolver resolver,
        TypeBuilder typeBuilder,
        Dictionary<string, FieldBuilder> fieldMap)
    {
        var methodLocal = new LocalManager
        {
            FilePath = local.FilePath,
            Interpreter = local.Interpreter,
            DynamicAssembly = local.DynamicAssembly,
            DynamicModule = local.DynamicModule,
            GenericClassSpecializations = local.GenericClassSpecializations,
            GenericClassConstructors = local.GenericClassConstructors,
            GenericClassInitializers = local.GenericClassInitializers,
            BuildingClassMethods = local.BuildingClassMethods,
            CurrentGenericTypeResolver = resolver,
            InClassEnv = typeBuilder,
            Globals = local.Globals
        };

        // 复制全局信息
        foreach (var (key, value) in local.DelegateVar)
            methodLocal.DelegateVar[key] = value;
        foreach (var (key, value) in local.ClassVar)
            methodLocal.ClassVar[key] = value;
        foreach (var (key, value) in local.GlobalStaticClasses)
            methodLocal.GlobalStaticClasses[key] = value;
        foreach (var (key, value) in local.GenericFunctions)
            methodLocal.GenericFunctions[key] = value;
        foreach (var (key, value) in local.GenericClasses)
            methodLocal.GenericClasses[key] = value;

        // 添加字段信息
        foreach (var (fieldName, fieldBuilder) in fieldMap)
            methodLocal.FieldVar[fieldName] = fieldBuilder;

        // 获取父类字段
        if (typeBuilder.BaseType != null && typeBuilder.BaseType != typeof(object))
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var parentFields = typeBuilder.BaseType.GetFields(flags);
            foreach (var field in parentFields)
            {
                methodLocal.FieldVar.TryAdd(field.Name, field);
            }
        }

        return methodLocal;
    }

    /// <summary>
    /// 生成方法体
    /// </summary>
    private static void GenerateMethodBody(
        FuncLangValue funcValue,
        ILGenerator methodIL,
        LocalManager methodLocal,
        Type[] parameterTypes,
        Type returnType,
        bool isStatic = false)
    {
        methodLocal.ReturnTypeAnnotation = methodLocal.CurrentGenericTypeResolver is not null
            ? methodLocal.CurrentGenericTypeResolver.ResolveReturnTypeAnnotation(funcValue.Id?.AssumptionType)
            : funcValue.Id?.AssumptionType?.Trim();
        if (returnType != typeof(void))
        {
            methodLocal.ReturnValueLocal = methodIL.DeclareLocal(returnType);
        }

        // 处理参数：为方法创建参数局部变量
        // 实例方法的参数索引从 1 开始（0 是 this）；静态方法从 0 开始。
        int paramIndex = isStatic ? 0 : 1;
        if (funcValue.Ids is not null)
        {
            for (var i = 0; i < funcValue.Ids.Count; i++)
            {
                var id = funcValue.Ids[i];
                var paramType = parameterTypes[i];
                var localVar = methodIL.DeclareLocal(paramType);

                // 加载参数并存储到局部变量
                methodIL.Emit(OpCodes.Ldarg, paramIndex);
                methodIL.Emit(OpCodes.Stloc, localVar);

                // 将参数添加到LocalManager中
                methodLocal.AddLocalVar(id.IdName, localVar);

                paramIndex++;
            }
        }

        // 生成方法体的IL代码
        funcValue.BlockStatement.Accept(RuntimeVisitors.CreateCompiler(methodIL, methodLocal));

        // 检查方法体的最后一个语句是否是 ReturnStatement
        var lastStatement = funcValue.BlockStatement.Count > 0
            ? funcValue.BlockStatement[^1]
            : null;

        // 只有当最后一个语句不是 ReturnStatement 时，才添加 Ret 指令
        if (lastStatement is not AST.Statement.ReturnStatement)
        {
            methodIL.Emit(OpCodes.Ret);
        }
    }

    /// <summary>
    /// 生成指定类型的默认值
    /// </summary>
    private static void GenerateDefaultValue(ILGenerator il, Type type)
    {
        if (type == typeof(int))
        {
            il.Emit(OpCodes.Ldc_I4_0);
        }
        else if (type == typeof(double))
        {
            il.Emit(OpCodes.Ldc_R8, 0.0);
        }
        else if (type == typeof(bool))
        {
            il.Emit(OpCodes.Ldc_I4_0);
        }
        else if (type == typeof(string))
        {
            il.Emit(OpCodes.Ldnull);
        }
        else if (type.IsValueType)
        {
            // 对于其他值类型，创建默认值
            var defaultLocal = il.DeclareLocal(type);
            il.Emit(OpCodes.Ldloca_S, defaultLocal);
            il.Emit(OpCodes.Initobj, type);
            il.Emit(OpCodes.Ldloc, defaultLocal);
        }
        else
        {
            // 引用类型返回null
            il.Emit(OpCodes.Ldnull);
        }
    }
}
