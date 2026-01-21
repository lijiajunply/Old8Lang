using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.TypeSystem;

namespace Old8Lang.Bytecode;

/// <summary>
/// BytecodeVisitor - 泛型支持
/// </summary>
public partial class BytecodeVisitor
{
    /// <summary>
    /// 生成泛型类的特化版本
    /// </summary>
    private void GenerateSpecializedClass(TypeTemplate typeTemplate, List<string> typeArguments, string specializedClassName)
    {
        // 创建类型参数映射
        var typeMapping = new Dictionary<string, string>();
        if (typeTemplate.GenericParameters != null)
        {
            for (int i = 0; i < Math.Min(typeArguments.Count, typeTemplate.GenericParameters.Count); i++)
            {
                var genericParamName = typeTemplate.GenericParameters[i].Name;
                var typeArgName = typeArguments[i];
                typeMapping[genericParamName] = typeArgName;
            }

            // 验证泛型约束
            ValidateGenericConstraints(typeTemplate.GenericParameters, typeMapping);
        }

        // 收集字段名称、类型和初始值（从实例成员中提取）
        var fields = new List<(string fieldName, string fieldType, LangExpression? initialValue)>();
        foreach (var (memberId, memberExpr) in typeTemplate.Variates)
        {
            if (memberExpr is not FuncLangValue)
            {
                // 这是一个字段，保存字段名、类型和初始值
                // 替换泛型类型参数
                var fieldType = memberId.AssumptionType ?? "";
                if (!string.IsNullOrEmpty(fieldType) && typeMapping.TryGetValue(fieldType, out var mappedType))
                {
                    fieldType = mappedType;
                }
                fields.Add((memberId.IdName, fieldType, memberExpr));
            }
        }

        // 收集方法（从实例成员和静态成员中提取）
        var methods = new List<(string methodName, FuncLangValue funcValue, bool isStatic, AccessModifier accessModifier)>();

        // 实例方法
        foreach (var (memberId, memberExpr) in typeTemplate.Variates)
        {
            if (memberExpr is FuncLangValue funcValue)
            {
                // 创建方法的特化版本（替换类型参数）
                var specializedMethod = CreateSpecializedMethod(funcValue, typeMapping);
                var accessModifier = GetAccessModifier(memberId.Modifiers);
                methods.Add((memberId.IdName, specializedMethod, false, accessModifier));
            }
        }

        // 静态方法
        foreach (var (memberId, memberExpr) in typeTemplate.StaticVariates)
        {
            if (memberExpr is FuncLangValue funcValue)
            {
                var specializedMethod = CreateSpecializedMethod(funcValue, typeMapping);
                var accessModifier = GetAccessModifier(memberId.Modifiers);
                methods.Add((memberId.IdName, specializedMethod, true, accessModifier));
            }
        }

        // 注册特化类
        _compiler.DeclareClass(specializedClassName, fields, [], methods, null);
    }

    /// <summary>
    /// 生成泛型函数的特化版本
    /// </summary>
    private void GenerateSpecializedFunction(FuncLangValue genericFunc, List<string> typeArguments, string specializedFuncName)
    {
        // 创建类型参数映射
        var typeMapping = new Dictionary<string, string>();
        if (genericFunc.GenericParameters != null)
        {
            for (int i = 0; i < Math.Min(typeArguments.Count, genericFunc.GenericParameters.Count); i++)
            {
                var genericParamName = genericFunc.GenericParameters[i].Name;
                var typeArgName = typeArguments[i];
                typeMapping[genericParamName] = typeArgName;
            }

            // 验证泛型约束
            ValidateGenericConstraints(genericFunc.GenericParameters, typeMapping);
        }

        // 创建特化函数
        var specializedFunc = CreateSpecializedMethod(genericFunc, typeMapping);

        // 提取参数名称
        var paramNames = specializedFunc.Ids?.Select(id => id.IdName).ToList() ?? [];
        var paramTypes = specializedFunc.Ids?.Select(id => id.AssumptionType ?? "").ToList() ?? [];

        // 提取默认参数值
        var defaultValues = new List<object?>();
        if (specializedFunc.Ids != null)
        {
            foreach (var param in specializedFunc.Ids)
            {
                if (param.DefaultValue != null)
                {
                    var defaultValue = EvaluateConstantExpression(param.DefaultValue);
                    defaultValues.Add(defaultValue);
                }
                else
                {
                    defaultValues.Add(null);
                }
            }
        }

        // 编译特化函数
        _compiler.CompileFunction(
            specializedFuncName,
            paramNames,
            paramTypes,
            defaultValues,
            specializedFunc.BlockStatement
        );
    }

    /// <summary>
    /// 验证泛型约束
    /// </summary>
    /// <param name="genericParameters">泛型参数列表</param>
    /// <param name="typeMapping">类型参数映射</param>
    private void ValidateGenericConstraints(List<GenericParameter> genericParameters, Dictionary<string, string> typeMapping)
    {
        foreach (var param in genericParameters)
        {
            if (!param.HasConstraints || param.StructuredConstraints == null)
                continue;

            if (!typeMapping.TryGetValue(param.Name, out var actualTypeName))
                continue;

            foreach (var constraint in param.StructuredConstraints)
            {
                ValidateSingleConstraint(constraint, actualTypeName, param.Name, typeMapping);
            }
        }
    }

    /// <summary>
    /// 验证单个约束
    /// </summary>
    private void ValidateSingleConstraint(
        GenericConstraint constraint,
        string actualTypeName,
        string genericParamName,
        Dictionary<string, string> typeMapping)
    {
        // 如果 actualTypeName 是另一个类型参数，尝试解析它
        var resolvedTypeName = actualTypeName;
        while (typeMapping.TryGetValue(resolvedTypeName, out var mappedType) && mappedType != resolvedTypeName)
        {
            resolvedTypeName = mappedType;
        }

        // 如果解析后仍然是类型参数（未映射），跳过验证
        // 这种情况发生在嵌套泛型中，内部类使用外部类的类型参数
        if (typeMapping.ContainsKey(resolvedTypeName) && typeMapping[resolvedTypeName] == resolvedTypeName)
        {
            // 类型参数未被映射到具体类型，跳过验证
            return;
        }

        switch (constraint.Kind)
        {
            case GenericConstraintKind.New:
                // new() 约束：检查类型是否有无参构造函数
                // 在虚拟机模式下，值类型总是满足 new() 约束
                if (!IsValueType(resolvedTypeName))
                {
                    // 对于引用类型，需要在运行时检查
                    // 这里只做基本检查，实际验证在实例化时进行
                }
                break;

            case GenericConstraintKind.Class:
                // class 约束：检查类型是否是引用类型
                if (IsValueType(resolvedTypeName))
                {
                    throw new ArgumentException(
                        $"类型 '{resolvedTypeName}' 不满足泛型参数 '{genericParamName}' 的 class 约束：'{resolvedTypeName}' 是值类型，不是引用类型");
                }
                break;

            case GenericConstraintKind.Struct:
                // struct 约束：检查类型是否是值类型
                if (!IsValueType(resolvedTypeName))
                {
                    throw new ArgumentException(
                        $"类型 '{resolvedTypeName}' 不满足泛型参数 '{genericParamName}' 的 struct 约束：'{resolvedTypeName}' 不是值类型");
                }
                break;

            case GenericConstraintKind.TypeName:
                // 类型名称约束：检查是否实现接口或继承基类
                // 在虚拟机模式下，这需要在运行时检查
                break;

            case GenericConstraintKind.TypeParameter:
                // 类型参数约束：检查 T 是否兼容 U
                var constraintTypeParamName = constraint.TypeName!;
                if (typeMapping.TryGetValue(constraintTypeParamName, out var constraintActualTypeName))
                {
                    // 简单检查：类型名称是否相同
                    // 更复杂的兼容性检查需要在运行时进行
                    if (!string.Equals(resolvedTypeName, constraintActualTypeName, StringComparison.OrdinalIgnoreCase))
                    {
                        // 这里只是警告，实际兼容性检查在运行时进行
                    }
                }
                break;
        }
    }

    /// <summary>
    /// 检查类型是否是值类型
    /// </summary>
    private bool IsValueType(string typeName)
    {
        var lowerTypeName = typeName.ToLowerInvariant();
        return lowerTypeName is "int" or "double" or "bool" or "char" or
            "int32" or "boolean" or "single" or "float" or "long" or "int64" or
            "short" or "int16" or "byte" or "sbyte" or "uint" or "uint32" or
            "ulong" or "uint64" or "ushort" or "uint16" or "decimal";
    }

    /// <summary>
    /// 创建方法的特化版本（替换类型参数）
    /// </summary>
    private FuncLangValue CreateSpecializedMethod(FuncLangValue originalMethod, Dictionary<string, string> typeMapping)
    {
        // 如果没有类型映射，直接返回原始方法
        if (typeMapping.Count == 0)
            return originalMethod;

        // 创建新的参数列表，替换类型参数
        List<LangId>? newIds = null;
        if (originalMethod.Ids != null)
        {
            newIds = new List<LangId>();
            foreach (var param in originalMethod.Ids)
            {
                var paramType = param.AssumptionType ?? "";
                // 替换泛型类型参数
                if (!string.IsNullOrEmpty(paramType) && typeMapping.TryGetValue(paramType, out var mappedType))
                {
                    paramType = mappedType;
                }
                // 创建新的参数，使用替换后的类型
                var newParam = new LangId(param.IdName, paramType, param.DefaultValue, param.IsParams, param.Position);
                newIds.Add(newParam);
            }
        }

        // 替换返回类型
        var returnType = originalMethod.Id?.AssumptionType ?? "";
        if (!string.IsNullOrEmpty(returnType) && typeMapping.TryGetValue(returnType, out var mappedReturnType))
        {
            returnType = mappedReturnType;
        }

        // 创建新的函数值，使用替换后的参数和返回类型
        var newId = originalMethod.Id != null
            ? new LangId(originalMethod.Id.IdName, returnType, originalMethod.Id.DefaultValue, originalMethod.Id.IsParams, originalMethod.Id.Position)
            : null;

        return new FuncLangValue(newId, newIds, originalMethod.BlockStatement);
    }

    /// <summary>
    /// 解析简单类型名称
    /// </summary>
    private string ResolveSimpleTypeName(string typeName)
    {
        // 将Old8Lang类型名称转换为字节码表示
        return typeName.ToLower() switch
        {
            "int" => "int",
            "string" => "string",
            "double" => "double",
            "bool" => "bool",
            "char" => "char",
            "void" => "void",
            "object" => "object",
            _ => typeName
        };
    }

}
