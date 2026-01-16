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

        // 收集字段名称（从实例成员中提取）
        var fields = new List<string>();
        foreach (var (memberId, memberExpr) in typeTemplate.Variates)
        {
            if (memberExpr is not FuncLangValue)
            {
                // 这是一个字段
                fields.Add(memberId.IdName);
            }
        }

        // 收集方法（从实例成员和静态成员中提取）
        var methods = new List<(string methodName, FuncLangValue funcValue, bool isStatic)>();

        // 实例方法
        foreach (var (memberId, memberExpr) in typeTemplate.Variates)
        {
            if (memberExpr is FuncLangValue funcValue)
            {
                // 创建方法的特化版本（替换类型参数）
                var specializedMethod = CreateSpecializedMethod(funcValue, typeMapping);
                methods.Add((memberId.IdName, specializedMethod, false));
            }
        }

        // 静态方法
        foreach (var (memberId, memberExpr) in typeTemplate.StaticVariates)
        {
            if (memberExpr is FuncLangValue funcValue)
            {
                var specializedMethod = CreateSpecializedMethod(funcValue, typeMapping);
                methods.Add((memberId.IdName, specializedMethod, true));
            }
        }

        // 注册特化类
        _compiler.DeclareClass(specializedClassName, fields, methods, null);
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
        var paramNames = specializedFunc.Ids?.Select(id => id.IdName).ToList() ?? new List<string>();

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
            defaultValues,
            specializedFunc.BlockStatement
        );
    }

    /// <summary>
    /// 创建方法的特化版本（替换类型参数）
    /// </summary>
    private FuncLangValue CreateSpecializedMethod(FuncLangValue originalMethod, Dictionary<string, string> typeMapping)
    {
        // 注意：这里我们创建一个浅拷贝，因为字节码模式不需要深度替换类型参数
        // 类型参数的替换主要在运行时通过动态类型处理
        return originalMethod;
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
