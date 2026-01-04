using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

namespace Old8Lang.Compiler;

/// <summary>
/// 泛型方法特化器，用于为泛型函数创建具体的类型特化版本
/// </summary>
public static class GenericMethodSpecializer
{
    /// <summary>
    /// 为泛型函数创建特化版本
    /// </summary>
    /// <param name="funcValue">泛型函数值</param>
    /// <param name="typeArguments">类型参数映射</param>
    /// <param name="local">局部管理器</param>
    /// <returns>特化后的MethodInfo</returns>
    public static MethodInfo CreateSpecialization(
        FuncLangValue funcValue, 
        Dictionary<string, Type> typeArguments, 
        LocalManager local)
    {
        if (!funcValue.IsGeneric || funcValue.GenericParameters == null)
        {
            throw new ArgumentException("函数必须是泛型函数");
        }

        // 构建特化键
        var typeArgNames = funcValue.GenericParameters
            .Select(p => typeArguments.TryGetValue(p.Name, out var type) ? type.Name : "object")
            .ToArray();
        var specializationKey = $"{funcValue.Id?.IdName ?? "anonymous"}${string.Join("_", typeArgNames)}";

        // 检查是否已经存在特化
        if (local.GenericSpecializations.TryGetValue(specializationKey, out var existingMethod))
        {
            Console.WriteLine($"[DEBUG] Reusing existing specialization: {specializationKey}");
            return existingMethod;
        }

        // 创建类型解析器
        var resolver = new GenericTypeResolver(typeArguments, local, local.Interpreter);

        // 解析参数类型
        var parameterTypes = resolver.ResolveParameterTypes(funcValue.Ids!);

        // 解析返回类型
        var returnType = resolver.ResolveReturnType(funcValue.Id?.AssumptionType);

        // 创建特化方法
        var dynamicMethod = new DynamicMethod(
            $"{funcValue.Id?.IdName ?? "anonymous"}_{string.Join("_", typeArgNames)}",
            returnType,
            parameterTypes,
            true
        );

        // 创建方法的IL发射器
        var methodIl = dynamicMethod.GetILGenerator();

        // 创建专门的函数本地管理器
        var funcLocal = new LocalManager() 
        { 
            FilePath = local.FilePath, 
            Interpreter = local.Interpreter,
            CurrentGenericTypeResolver = resolver
        };

        // 复制全局信息
        foreach (var (key, value) in local.DelegateVar)
        {
            funcLocal.DelegateVar[key] = value;
        }
        foreach (var (key, value) in local.ClassVar)
        {
            funcLocal.ClassVar[key] = value;
        }
        foreach (var (key, value) in local.GlobalStaticClasses)
        {
            funcLocal.GlobalStaticClasses[key] = value;
        }
        foreach (var (key, value) in local.FuncParameters)
        {
            funcLocal.FuncParameters[key] = value;
        }

        // 处理参数：为特化版本创建具体的参数局部变量
        for (var i = 0; i < funcValue.Ids!.Count; i++)
        {
            var id = funcValue.Ids[i];
            var paramType = parameterTypes[i];
            var localVar = methodIl.DeclareLocal(paramType);
            funcLocal.AddLocalVar(id.IdName, localVar);
            funcLocal.LocalVarTypes[id.IdName] = paramType;
            
            // 加载参数并存储到局部变量
            methodIl.Emit(OpCodes.Ldarg, i);
            methodIl.Emit(OpCodes.Stloc, localVar);
        }

        // 创建函数结束标签
        var endLabel = methodIl.DefineLabel();
        funcLocal.ReturnLabel = endLabel;

        // 生成方法体的IL代码，使用特化的类型信息
        GenerateSpecializedMethodBody(funcValue, methodIl, funcLocal, resolver);

        // 检查是否需要默认返回值
        var lastStatement = funcValue.BlockStatement.Count > 0
            ? funcValue.BlockStatement[^1]
            : null;

        if (lastStatement is not AST.Statement.ReturnStatement && returnType != typeof(void))
        {
            // 提供默认返回值
            GenerateDefaultValue(methodIl, returnType);
        }

        methodIl.Emit(OpCodes.Ret);

        // 缓存特化方法
        local.GenericSpecializations[specializationKey] = dynamicMethod;
        
        return dynamicMethod;
    }

    /// <summary>
    /// 生成特化的方法体
    /// </summary>
    private static void GenerateSpecializedMethodBody(
        FuncLangValue funcValue,
        ILGenerator methodIl,
        LocalManager funcLocal,
        GenericTypeResolver resolver)
    {
        // 使用特化的类型信息生成方法体
        funcValue.BlockStatement.GenerateIl(methodIl, funcLocal);
    }

    /// <summary>
    /// 生成指定类型的默认值
    /// </summary>
    private static void GenerateDefaultValue(ILGenerator methodIl, Type type)
    {
        if (type == typeof(int))
        {
            methodIl.Emit(OpCodes.Ldc_I4_0);
        }
        else if (type == typeof(double))
        {
            methodIl.Emit(OpCodes.Ldc_R8, 0.0);
        }
        else if (type == typeof(bool))
        {
            methodIl.Emit(OpCodes.Ldc_I4_0);
        }
        else if (type == typeof(string))
        {
            methodIl.Emit(OpCodes.Ldnull);
        }
        else if (type.IsValueType)
        {
            // 对于其他值类型，初始化并加载默认值
            var defaultLocal = methodIl.DeclareLocal(type);
            methodIl.Emit(OpCodes.Initobj, type);
            methodIl.Emit(OpCodes.Ldloc, defaultLocal);
        }
        else
        {
            // 引用类型返回null
            methodIl.Emit(OpCodes.Ldnull);
        }
    }
}