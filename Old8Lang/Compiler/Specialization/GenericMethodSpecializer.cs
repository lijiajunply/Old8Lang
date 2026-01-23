using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.TypeSystem;

namespace Old8Lang.Compiler.Specialization;

/// <summary>
/// 泛型方法特化器，用于为泛型函数创建具体的类型特化版本
/// </summary>
public static class GenericMethodSpecializer
{
    /// <summary>
    /// 从调用参数类型推断泛型类型参数
    /// </summary>
    public static Dictionary<string, Type>? InferTypeArguments(
        FuncLangValue funcValue,
        List<Type> argTypes,
        LocalManager local)
    {
        if (!funcValue.IsGeneric || funcValue.GenericParameters is null)
        {
            return null;
        }

        var typeArgMapping = new Dictionary<string, Type>();
        var funcIds = funcValue.Ids ?? [];

        // 检查参数数量（暂时只处理位置参数，忽略默认参数的复杂情况）
        // 如果实际参数少于函数参数，可能是用了默认参数，这会增加推断难度
        // 简单起见，我们只使用提供的参数进行推断
        
        for (int i = 0; i < argTypes.Count && i < funcIds.Count; i++)
        {
            var paramId = funcIds[i];
            var paramTypeAnnotation = paramId.AssumptionType;
            if (string.IsNullOrEmpty(paramTypeAnnotation)) continue;

            var argType = argTypes[i];
            MatchTypePattern(paramTypeAnnotation, argType, typeArgMapping);
        }

        // 检查是否所有泛型参数都已推断
        foreach (var genericParam in funcValue.GenericParameters)
        {
            if (!typeArgMapping.ContainsKey(genericParam.Name))
            {
                // 无法推断所有类型参数
                return null;
            }
        }

        return typeArgMapping;
    }

    private static bool MatchTypePattern(string pattern, Type actualType, Dictionary<string, Type> mapping)
    {
        pattern = pattern.Trim();
        
        // 1. 单个泛型参数 "T"
        if (IsGenericParamName(pattern))
        {
            if (mapping.TryGetValue(pattern, out var existingType))
            {
                return existingType == actualType;
            }
            mapping[pattern] = actualType;
            return true;
        }

        // 2. 泛型类型 "List<T>"
        var patternGenericStart = pattern.IndexOf('<');
        if (patternGenericStart != -1 && actualType.IsGenericType)
        {
            var patternBaseName = pattern.Substring(0, patternGenericStart).Trim();
            
            // 检查基础类型名称是否匹配 (例如 "List" 匹配 "List`1")
            // System.Type 的 Name 通常是 "List`1"
            var actualTypeName = actualType.Name;
            var backtickIndex = actualTypeName.IndexOf('`');
            var actualBaseName = backtickIndex != -1 ? actualTypeName.Substring(0, backtickIndex) : actualTypeName;

            if (patternBaseName != actualBaseName)
            {
                return false;
            }

            // 提取泛型参数
            var patternGenericEnd = pattern.LastIndexOf('>');
            if (patternGenericEnd == -1) return false;
            
            var patternGenericArgsStr = pattern.Substring(patternGenericStart + 1, patternGenericEnd - patternGenericStart - 1);
            var patternArgs = SplitGenericArguments(patternGenericArgsStr);
            
            var actualArgs = actualType.GetGenericArguments();

            if (patternArgs.Count != actualArgs.Length) return false;

            for (int i = 0; i < patternArgs.Count; i++)
            {
                if (!MatchTypePattern(patternArgs[i], actualArgs[i], mapping))
                {
                    return false;
                }
            }

            return true;
        }

        // 3. 数组 "T[]" 或 "int[]"
        if (pattern.EndsWith("[]") && actualType.IsArray)
        {
            var elementPattern = pattern.Substring(0, pattern.Length - 2);
            var actualElementType = actualType.GetElementType();
            return actualElementType != null && MatchTypePattern(elementPattern, actualElementType, mapping);
        }

        return false;
    }

    private static bool IsGenericParamName(string name)
    {
        // 简单判断：大写字母开头，不包含特殊字符
        return !string.IsNullOrEmpty(name) && 
               char.IsUpper(name[0]) && 
               !name.Contains('<') && 
               !name.Contains('[') &&
               name != "List" && name != "Dictionary"; // 排除常见类型名
    }

    private static List<string> SplitGenericArguments(string args)
    {
        var result = new List<string>();
        var current = "";
        var depth = 0;

        foreach (var ch in args)
        {
            if (ch == '<')
            {
                depth++;
                current += ch;
            }
            else if (ch == '>')
            {
                depth--;
                current += ch;
            }
            else if (ch == ',' && depth == 0)
            {
                result.Add(current.Trim());
                current = "";
            }
            else
            {
                current += ch;
            }
        }

        if (current.Length > 0)
        {
            result.Add(current.Trim());
        }

        return result;
    }

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
        if (!funcValue.IsGeneric || funcValue.GenericParameters is null)
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

        // 验证泛型约束（使用新的约束验证器）
        if (funcValue.GenericParameters != null)
        {
            foreach (var param in funcValue.GenericParameters)
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
        foreach (var (key, value) in local.GenericFunctions)
        {
            funcLocal.GenericFunctions[key] = value;
        }
        foreach (var (key, value) in local.GenericClasses)
        {
            funcLocal.GenericClasses[key] = value;
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

        // 声明返回值局部变量（如果需要）
        if (returnType != typeof(void))
        {
            funcLocal.ReturnValueLocal = methodIl.DeclareLocal(returnType);
        }

        // 开始 try-finally 块以支持 defer
        methodIl.BeginExceptionBlock();

        // 生成方法体的IL代码，使用特化的类型信息
        GenerateSpecializedMethodBody(funcValue, methodIl, funcLocal, resolver);

        // 检查是否需要默认返回值
        var lastStatement = funcValue.BlockStatement.Count > 0
            ? funcValue.BlockStatement[^1]
            : null;

        if (lastStatement is not AST.Statement.ReturnStatement)
        {
            if (returnType != typeof(void))
            {
                // 提供默认返回值
                GenerateDefaultValue(methodIl, returnType);
                methodIl.Emit(OpCodes.Stloc, funcLocal.ReturnValueLocal!);
            }
            
            // 显式离开 try 块，触发 finally 块执行
            methodIl.Emit(OpCodes.Leave, endLabel);
        }

        // Finally 块：执行 defer 语句
        methodIl.BeginFinallyBlock();
        funcLocal.IsInFinallyBlock = true;
        funcLocal.GenerateDeferIL(methodIl);
        funcLocal.IsInFinallyBlock = false;
        methodIl.EndExceptionBlock();

        // 标记函数结束位置
        methodIl.MarkLabel(endLabel);

        // 加载返回值并返回
        if (returnType != typeof(void))
        {
            methodIl.Emit(OpCodes.Ldloc, funcLocal.ReturnValueLocal!);
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