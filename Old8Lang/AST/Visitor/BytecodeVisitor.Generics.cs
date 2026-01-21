using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;

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
