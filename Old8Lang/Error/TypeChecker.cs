using System.Linq;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Error;

/// <summary>
/// 全局类型检查器，提供统一的类型验证和转换功能
/// </summary>
public static class TypeChecker
{
    /// <summary>
    /// 验证函数调用时的参数类型匹配
    /// </summary>
    /// <param name="argumentExpressions">传入的参数表达式列表</param>
    /// <param name="argumentValues">计算后的参数值列表</param>
    /// <param name="parameters">函数参数定义列表</param>
    /// <exception cref="TypeError">当参数类型不匹配时抛出</exception>
    public static void ValidateParameterTypes(
        List<IOldLangTree> argumentExpressions,
        List<LangValueType> argumentValues,
        List<LangId> parameters)
    {
        for (int i = 0; i < Math.Min(argumentValues.Count, parameters.Count); i++)
        {
            var parameter = parameters[i];
            var argumentValue = argumentValues[i];

            // 只对有类型注解的参数进行类型检查
            if (!string.IsNullOrEmpty(parameter.AssumptionType))
            {
                var expectedType = parameter.AssumptionType;
                var actualType = GetLangValueType(argumentValue);

                if (!IsTypeCompatible(expectedType, actualType))
                {
                    throw new TypeError(
                        argumentExpressions[i],
                        expectedType,
                        actualType,
                        $"参数 '{parameter.IdName}' 类型不匹配"
                    );
                }
            }
        }
    }

    /// <summary>
    /// 获取 Old8Lang 值的类型名称
    /// </summary>
    /// <param name="value">Old8Lang 值</param>
    /// <returns>类型名称</returns>
    private static string GetLangValueType(LangValueType value)
    {
        return value switch
        {
            IntLangValue => "int",
            DoubleLangValue => "double",
            StringLangValue => "string",
            BoolLangValue => "bool",
            CharLangValue => "char",
            ArrayLangValue => "array",
            ListLangValue => "list",
            DictionaryLangValue => "dict",
            FuncLangValue => "function",
            AsyncFuncLangValue => "async_func",
            GeneratorLangValue => "generator",
            AsyncGeneratorLangValue => "async_generator",
            AnyLangValue any => any.Id.IdName,
            NullLangValue => "null",
            TupleLangValue => "tuple",
            VoidLangValue => "void",
            TaskLangValue => "task",
            TaskClassLangValue => "task_static",
            ThreadLangValue => "thread",
            TypeTemplate => "class",
            TypeLangValue => "type",
            ListComprehension => "list",
            StringTemplateValue => "string",
            ErrorLangValue => "error",
            _ => "object"
        };
    }

    /// <summary>
    /// 检查类型是否兼容
    /// </summary>
    /// <param name="expectedType">期望类型</param>
    /// <param name="actualType">实际类型</param>
    /// <returns>是否兼容</returns>
    public static bool IsTypeCompatible(string expectedType, string actualType)
    {
        // const 是只读修饰符，不是类型，任何类型都可以与 const 兼容
        if (expectedType.Equals("const", StringComparison.OrdinalIgnoreCase)) return true;

        // 完全匹配
        if (expectedType == actualType) return true;

        // any 类型可以匹配任何类型
        if (expectedType == "any") return true;

        // null 可以赋值给任何引用类型
        if (actualType == "null" && expectedType != "int" && expectedType != "double" && expectedType != "bool" &&
            expectedType != "char") return true;

        // 数值类型的兼容性（int 可以隐式转换为 double，但反之不行）
        if (expectedType == "double" && actualType == "int") return true; // int 可以隐式转换为 double

        // 严格类型检查：不允许字符串到数字的自动转换
        // 这是测试期望的行为

        // TODO: 可以添加更多的类型兼容性规则，比如子类型关系等

        return false;
    }

    /// <summary>
    /// 验证变量赋值的类型兼容性
    /// </summary>
    /// <param name="expectedType">期望的类型注解（可以为空）</param>
    /// <param name="actualValue">实际的值</param>
    /// <param name="node">AST节点，用于错误报告</param>
    /// <param name="variableName">变量名</param>
    /// <exception cref="TypeError">当类型不匹配时抛出</exception>
    public static void ValidateVariableAssignment(
        string? expectedType,
        LangValueType actualValue,
        IOldLangTree node,
        string variableName)
    {
        if (string.IsNullOrEmpty(expectedType)) return; // 没有类型注解，跳过检查

        var actualType = GetLangValueType(actualValue);

        if (!IsTypeCompatible(expectedType, actualType))
        {
            throw new TypeError(
                node,
                expectedType,
                actualType,
                $"变量 '{variableName}' 赋值类型不匹配"
            );
        }
    }

    /// <summary>
    /// 验证返回值的类型兼容性
    /// </summary>
    /// <param name="expectedReturnType">期望的返回类型（可以为空）</param>
    /// <param name="actualReturnValue">实际的返回值</param>
    /// <param name="returnStatement">return 语句节点，用于错误报告</param>
    /// <param name="functionName">函数名</param>
    /// <exception cref="TypeError">当类型不匹配时抛出</exception>
    public static void ValidateReturnType(
        string? expectedReturnType,
        LangValueType actualReturnValue,
        IOldLangTree returnStatement,
        string functionName)
    {
        if (string.IsNullOrEmpty(expectedReturnType)) return; // 没有返回类型注解，跳过检查

        var actualType = GetLangValueType(actualReturnValue);

        if (!IsTypeCompatible(expectedReturnType, actualType))
        {
            throw new TypeError(
                returnStatement,
                expectedReturnType,
                actualType,
                $"函数 '{functionName}' 返回值类型不匹配"
            );
        }
    }
}