using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.TypeSystem;

/// <summary>
/// 约束验证结果
/// </summary>
public class ConstraintValidationResult
{
    /// <summary>
    /// 验证是否成功
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// 错误消息（如果验证失败）
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// 失败的约束（如果验证失败）
    /// </summary>
    public GenericConstraint? FailedConstraint { get; }

    private ConstraintValidationResult(bool isValid, string? errorMessage = null, GenericConstraint? failedConstraint = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
        FailedConstraint = failedConstraint;
    }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static ConstraintValidationResult Success() => new(true);

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static ConstraintValidationResult Failure(string errorMessage, GenericConstraint? failedConstraint = null)
        => new(false, errorMessage, failedConstraint);
}

/// <summary>
/// 泛型约束验证器
/// 用于验证类型参数是否满足泛型约束
/// </summary>
public static class GenericConstraintValidator
{
    /// <summary>
    /// Old8Lang 中的值类型列表
    /// </summary>
    private static readonly HashSet<string> ValueTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "int", "double", "bool", "char",
        "Int32", "Double", "Boolean", "Char",
        "IntLangValue", "DoubleLangValue", "BoolLangValue", "CharLangValue"
    };

    /// <summary>
    /// 验证泛型参数的所有约束
    /// </summary>
    /// <param name="genericParam">泛型参数定义</param>
    /// <param name="actualType">实际类型信息</param>
    /// <param name="typeArgumentMapping">类型参数映射（用于类型参数约束）</param>
    /// <param name="manager">变量管理器（用于查找类型定义）</param>
    /// <returns>验证结果</returns>
    public static ConstraintValidationResult Validate(
        GenericParameter genericParam,
        ITypeInfo actualType,
        Dictionary<string, ITypeInfo>? typeArgumentMapping,
        VariateManager? manager)
    {
        if (genericParam.StructuredConstraints is null || genericParam.StructuredConstraints.Count == 0)
        {
            return ConstraintValidationResult.Success();
        }

        foreach (var constraint in genericParam.StructuredConstraints)
        {
            var result = ValidateSingleConstraint(constraint, actualType, typeArgumentMapping, manager, genericParam.Name);
            if (!result.IsValid)
            {
                return result;
            }
        }

        return ConstraintValidationResult.Success();
    }

    /// <summary>
    /// 验证单个约束
    /// </summary>
    private static ConstraintValidationResult ValidateSingleConstraint(
        GenericConstraint constraint,
        ITypeInfo actualType,
        Dictionary<string, ITypeInfo>? typeArgumentMapping,
        VariateManager? manager,
        string genericParamName)
    {
        return constraint.Kind switch
        {
            GenericConstraintKind.New => ValidateNewConstraint(actualType, manager, genericParamName),
            GenericConstraintKind.Class => ValidateClassConstraint(actualType, genericParamName),
            GenericConstraintKind.Struct => ValidateStructConstraint(actualType, genericParamName),
            GenericConstraintKind.TypeName => ValidateTypeNameConstraint(constraint, actualType, manager, genericParamName),
            GenericConstraintKind.TypeParameter => ValidateTypeParameterConstraint(constraint, actualType, typeArgumentMapping, genericParamName),
            _ => ConstraintValidationResult.Success()
        };
    }

    /// <summary>
    /// 验证 new() 约束
    /// 检查类型是否有无参构造函数（init 方法）
    /// </summary>
    private static ConstraintValidationResult ValidateNewConstraint(
        ITypeInfo actualType,
        VariateManager? manager,
        string genericParamName)
    {
        var typeName = actualType.Name;

        // 值类型总是满足 new() 约束
        if (IsValueType(typeName))
        {
            return ConstraintValidationResult.Success();
        }

        // 检查是否是 Old8Lang 类
        if (manager != null)
        {
            var typeTemplate = FindTypeTemplate(typeName, manager);
            if (typeTemplate != null)
            {
                // 检查是否有无参 init 方法
                if (HasParameterlessInit(typeTemplate))
                {
                    return ConstraintValidationResult.Success();
                }

                return ConstraintValidationResult.Failure(
                    $"类型 '{typeName}' 不满足泛型参数 '{genericParamName}' 的 new() 约束：类型没有无参构造函数（init 方法）",
                    GenericConstraint.CreateNew());
            }
        }

        // 检查 .NET 类型
        var dotNetType = GetDotNetType(typeName);
        if (dotNetType != null)
        {
            // 检查是否有无参公共构造函数
            var hasParameterlessCtor = dotNetType.GetConstructor(Type.EmptyTypes) != null;
            if (hasParameterlessCtor)
            {
                return ConstraintValidationResult.Success();
            }

            return ConstraintValidationResult.Failure(
                $"类型 '{typeName}' 不满足泛型参数 '{genericParamName}' 的 new() 约束：类型没有无参构造函数",
                GenericConstraint.CreateNew());
        }

        // 未知类型，默认通过（可能是动态类型）
        return ConstraintValidationResult.Success();
    }

    /// <summary>
    /// 验证 class 约束
    /// 检查类型是否是引用类型
    /// </summary>
    private static ConstraintValidationResult ValidateClassConstraint(
        ITypeInfo actualType,
        string genericParamName)
    {
        var typeName = actualType.Name;

        // 值类型不满足 class 约束
        if (IsValueType(typeName))
        {
            return ConstraintValidationResult.Failure(
                $"类型 '{typeName}' 不满足泛型参数 '{genericParamName}' 的 class 约束：'{typeName}' 是值类型，不是引用类型",
                GenericConstraint.CreateClass());
        }

        return ConstraintValidationResult.Success();
    }

    /// <summary>
    /// 验证 struct 约束
    /// 检查类型是否是值类型
    /// </summary>
    private static ConstraintValidationResult ValidateStructConstraint(
        ITypeInfo actualType,
        string genericParamName)
    {
        var typeName = actualType.Name;

        // 只有值类型满足 struct 约束
        if (!IsValueType(typeName))
        {
            return ConstraintValidationResult.Failure(
                $"类型 '{typeName}' 不满足泛型参数 '{genericParamName}' 的 struct 约束：'{typeName}' 不是值类型",
                GenericConstraint.CreateStruct());
        }

        return ConstraintValidationResult.Success();
    }

    /// <summary>
    /// 验证类型名称约束（接口或基类）
    /// </summary>
    private static ConstraintValidationResult ValidateTypeNameConstraint(
        GenericConstraint constraint,
        ITypeInfo actualType,
        VariateManager? manager,
        string genericParamName)
    {
        var constraintTypeName = constraint.TypeName!;
        var actualTypeName = actualType.Name;

        // 检查是否是同一类型
        if (string.Equals(actualTypeName, constraintTypeName, StringComparison.OrdinalIgnoreCase))
        {
            return ConstraintValidationResult.Success();
        }

        // 检查 Old8Lang 类型系统
        if (manager != null)
        {
            var actualTypeTemplate = FindTypeTemplate(actualTypeName, manager);
            var constraintTypeTemplate = FindTypeTemplate(constraintTypeName, manager);

            if (actualTypeTemplate != null && constraintTypeTemplate != null)
            {
                // 检查是否实现了接口
                if (constraintTypeTemplate.IsInterface)
                {
                    if (actualTypeTemplate.ImplementsNames.Contains(constraintTypeName))
                    {
                        return ConstraintValidationResult.Success();
                    }
                }

                // 检查是否继承自基类
                if (IsSubclassOf(actualTypeTemplate, constraintTypeName, manager))
                {
                    return ConstraintValidationResult.Success();
                }
            }
        }

        // 检查 .NET 类型兼容性
        var actualDotNetType = GetDotNetType(actualTypeName);
        var constraintDotNetType = GetDotNetType(constraintTypeName);

        if (actualDotNetType != null && constraintDotNetType != null)
        {
            if (constraintDotNetType.IsAssignableFrom(actualDotNetType))
            {
                return ConstraintValidationResult.Success();
            }
        }

        // 使用 ITypeInfo 的兼容性检查
        var constraintTypeInfo = manager?.Interpreter?.TypeAnnotationManager?.GetTypeFamily().GetType(constraintTypeName);
        if (constraintTypeInfo != null && actualType.IsCompatibleWith(constraintTypeInfo))
        {
            return ConstraintValidationResult.Success();
        }

        return ConstraintValidationResult.Failure(
            $"类型 '{actualTypeName}' 不满足泛型参数 '{genericParamName}' 的约束 '{constraintTypeName}'：类型没有实现接口或继承自该基类",
            constraint);
    }

    /// <summary>
    /// 验证类型参数约束（T: U）
    /// </summary>
    private static ConstraintValidationResult ValidateTypeParameterConstraint(
        GenericConstraint constraint,
        ITypeInfo actualType,
        Dictionary<string, ITypeInfo>? typeArgumentMapping,
        string genericParamName)
    {
        var constraintTypeParamName = constraint.TypeName!;

        // 从类型参数映射中获取约束类型参数的实际类型
        if (typeArgumentMapping == null || !typeArgumentMapping.TryGetValue(constraintTypeParamName, out var constraintActualType))
        {
            // 如果找不到映射，可能是约束引用了未解析的类型参数
            return ConstraintValidationResult.Failure(
                $"无法验证泛型参数 '{genericParamName}' 的类型参数约束 '{constraintTypeParamName}'：找不到类型参数 '{constraintTypeParamName}' 的实际类型",
                constraint);
        }

        // 检查 actualType 是否兼容 constraintActualType
        if (actualType.IsCompatibleWith(constraintActualType))
        {
            return ConstraintValidationResult.Success();
        }

        return ConstraintValidationResult.Failure(
            $"类型 '{actualType.Name}' 不满足泛型参数 '{genericParamName}' 的类型参数约束 '{constraintTypeParamName}'：'{actualType.Name}' 不兼容 '{constraintActualType.Name}'",
            constraint);
    }

    /// <summary>
    /// 检查类型是否是值类型
    /// </summary>
    private static bool IsValueType(string typeName)
    {
        return ValueTypes.Contains(typeName);
    }

    /// <summary>
    /// 查找类型模板
    /// </summary>
    private static TypeTemplate? FindTypeTemplate(string typeName, VariateManager manager)
    {
        // 首先从全局类型注册表查找
        var template = TypeTemplate.FindType(typeName);
        if (template != null)
        {
            return template;
        }

        // 从变量管理器查找
        var value = manager.GetAny(new LangId(typeName));
        return value as TypeTemplate;
    }

    /// <summary>
    /// 检查类型是否有无参 init 方法
    /// </summary>
    private static bool HasParameterlessInit(TypeTemplate typeTemplate)
    {
        foreach (var (memberId, memberExpr) in typeTemplate.Variates)
        {
            if (memberId.IdName == "init" && memberExpr is FuncLangValue funcValue)
            {
                // 检查是否无参或所有参数都有默认值
                if (funcValue.Ids == null || funcValue.Ids.Count == 0)
                {
                    return true;
                }

                // 检查所有参数是否都有默认值
                if (funcValue.Ids.All(id => id.DefaultValue != null))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 检查类型是否继承自指定基类
    /// </summary>
    private static bool IsSubclassOf(TypeTemplate typeTemplate, string baseClassName, VariateManager manager)
    {
        var currentType = typeTemplate;

        while (currentType != null)
        {
            if (string.Equals(currentType.ClassName, baseClassName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (currentType.ParentClassName == null)
            {
                break;
            }

            currentType = FindTypeTemplate(currentType.ParentClassName, manager);
        }

        return false;
    }

    /// <summary>
    /// 获取 .NET 类型
    /// </summary>
    private static Type? GetDotNetType(string typeName)
    {
        return typeName.ToLowerInvariant() switch
        {
            "int" or "int32" => typeof(int),
            "double" => typeof(double),
            "bool" or "boolean" => typeof(bool),
            "char" => typeof(char),
            "string" => typeof(string),
            "object" => typeof(object),
            "list" => typeof(List<object>),
            "dictionary" or "dict" => typeof(Dictionary<object, object>),
            _ => Type.GetType(typeName)
        };
    }

    /// <summary>
    /// 验证泛型参数的所有约束（使用 .NET Type）
    /// </summary>
    public static ConstraintValidationResult ValidateWithDotNetType(
        GenericParameter genericParam,
        Type actualType,
        Dictionary<string, Type>? typeArgumentMapping)
    {
        if (genericParam.StructuredConstraints is null || genericParam.StructuredConstraints.Count == 0)
        {
            return ConstraintValidationResult.Success();
        }

        foreach (var constraint in genericParam.StructuredConstraints)
        {
            var result = ValidateSingleConstraintWithDotNetType(constraint, actualType, typeArgumentMapping, genericParam.Name);
            if (!result.IsValid)
            {
                return result;
            }
        }

        return ConstraintValidationResult.Success();
    }

    /// <summary>
    /// 验证单个约束（使用 .NET Type）
    /// </summary>
    private static ConstraintValidationResult ValidateSingleConstraintWithDotNetType(
        GenericConstraint constraint,
        Type actualType,
        Dictionary<string, Type>? typeArgumentMapping,
        string genericParamName)
    {
        return constraint.Kind switch
        {
            GenericConstraintKind.New => ValidateNewConstraintWithDotNetType(actualType, genericParamName),
            GenericConstraintKind.Class => ValidateClassConstraintWithDotNetType(actualType, genericParamName),
            GenericConstraintKind.Struct => ValidateStructConstraintWithDotNetType(actualType, genericParamName),
            GenericConstraintKind.TypeName => ValidateTypeNameConstraintWithDotNetType(constraint, actualType, genericParamName),
            GenericConstraintKind.TypeParameter => ValidateTypeParameterConstraintWithDotNetType(constraint, actualType, typeArgumentMapping, genericParamName),
            _ => ConstraintValidationResult.Success()
        };
    }

    private static ConstraintValidationResult ValidateNewConstraintWithDotNetType(Type actualType, string genericParamName)
    {
        // 值类型总是满足 new() 约束
        if (actualType.IsValueType)
        {
            return ConstraintValidationResult.Success();
        }

        // 检查是否有无参公共构造函数
        var hasParameterlessCtor = actualType.GetConstructor(Type.EmptyTypes) != null;
        if (hasParameterlessCtor)
        {
            return ConstraintValidationResult.Success();
        }

        return ConstraintValidationResult.Failure(
            $"类型 '{actualType.Name}' 不满足泛型参数 '{genericParamName}' 的 new() 约束：类型没有无参构造函数",
            GenericConstraint.CreateNew());
    }

    private static ConstraintValidationResult ValidateClassConstraintWithDotNetType(Type actualType, string genericParamName)
    {
        if (actualType.IsValueType)
        {
            return ConstraintValidationResult.Failure(
                $"类型 '{actualType.Name}' 不满足泛型参数 '{genericParamName}' 的 class 约束：'{actualType.Name}' 是值类型，不是引用类型",
                GenericConstraint.CreateClass());
        }

        return ConstraintValidationResult.Success();
    }

    private static ConstraintValidationResult ValidateStructConstraintWithDotNetType(Type actualType, string genericParamName)
    {
        if (!actualType.IsValueType || actualType == typeof(void))
        {
            return ConstraintValidationResult.Failure(
                $"类型 '{actualType.Name}' 不满足泛型参数 '{genericParamName}' 的 struct 约束：'{actualType.Name}' 不是值类型",
                GenericConstraint.CreateStruct());
        }

        return ConstraintValidationResult.Success();
    }

    private static ConstraintValidationResult ValidateTypeNameConstraintWithDotNetType(
        GenericConstraint constraint,
        Type actualType,
        string genericParamName)
    {
        var constraintTypeName = constraint.TypeName!;
        var constraintType = GetDotNetType(constraintTypeName);

        if (constraintType != null && constraintType.IsAssignableFrom(actualType))
        {
            return ConstraintValidationResult.Success();
        }

        return ConstraintValidationResult.Failure(
            $"类型 '{actualType.Name}' 不满足泛型参数 '{genericParamName}' 的约束 '{constraintTypeName}'",
            constraint);
    }

    private static ConstraintValidationResult ValidateTypeParameterConstraintWithDotNetType(
        GenericConstraint constraint,
        Type actualType,
        Dictionary<string, Type>? typeArgumentMapping,
        string genericParamName)
    {
        var constraintTypeParamName = constraint.TypeName!;

        if (typeArgumentMapping == null || !typeArgumentMapping.TryGetValue(constraintTypeParamName, out var constraintActualType))
        {
            return ConstraintValidationResult.Failure(
                $"无法验证泛型参数 '{genericParamName}' 的类型参数约束 '{constraintTypeParamName}'：找不到类型参数的实际类型",
                constraint);
        }

        if (constraintActualType.IsAssignableFrom(actualType))
        {
            return ConstraintValidationResult.Success();
        }

        return ConstraintValidationResult.Failure(
            $"类型 '{actualType.Name}' 不满足泛型参数 '{genericParamName}' 的类型参数约束 '{constraintTypeParamName}'",
            constraint);
    }
}
