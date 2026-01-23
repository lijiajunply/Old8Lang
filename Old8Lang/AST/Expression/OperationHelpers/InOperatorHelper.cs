using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.OperationHelpers;

/// <summary>
/// In 操作符助手类
/// 处理 in 操作符的 IL 代码生成，支持 List、Array、Dictionary、String 等集合类型
/// </summary>
internal static class InOperatorHelper
{
    /// <summary>
    /// 交换栈上两个值的顺序
    /// 将栈上的 [value, collection] 交换为 [collection, value]
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="collectionType">集合类型</param>
    /// <param name="valueType">值类型</param>
    private static void SwapStackOrder(ILGenerator ilGenerator, Type collectionType, Type valueType)
    {
        var collectionLocal = ilGenerator.DeclareLocal(collectionType);
        var valueLocal = ilGenerator.DeclareLocal(valueType);

        // 保存 collection 和 value 到局部变量（栈是后进先出）
        ilGenerator.Emit(OpCodes.Stloc, collectionLocal); // 保存 collection
        ilGenerator.Emit(OpCodes.Stloc, valueLocal);      // 保存 value

        // 按正确顺序重新加载：collection, value
        ilGenerator.Emit(OpCodes.Ldloc, collectionLocal);
        ilGenerator.Emit(OpCodes.Ldloc, valueLocal);
    }

    /// <summary>
    /// 生成 List&lt;T&gt; 的 Contains 检查
    /// </summary>
    private static void GenerateListContains(ILGenerator ilGenerator, Type listType)
    {
        // 交换栈顺序：[value, list] -> [list, value]
        SwapStackOrder(ilGenerator, listType, typeof(object));

        // 调用 List<T>.Contains(T) 方法
        var containsMethod = listType.GetMethod("Contains", [listType.GetGenericArguments()[0]])!;
        ilGenerator.Emit(OpCodes.Callvirt, containsMethod);
    }

    /// <summary>
    /// 生成数组的 IndexOf 检查
    /// </summary>
    private static void GenerateArrayIndexOf(ILGenerator ilGenerator, Type arrayType)
    {
        // 交换栈顺序：[value, array] -> [array, value]
        SwapStackOrder(ilGenerator, arrayType, typeof(object));

        // 调用 Array.IndexOf(array, value) 方法
        var indexOfMethod = typeof(Array).GetMethod("IndexOf", [typeof(Array), typeof(object)])!;
        ilGenerator.Emit(OpCodes.Call, indexOfMethod);

        // 检查结果是否 >= 0
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Clt);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Xor);
    }

    /// <summary>
    /// 生成 Dictionary&lt;TKey, TValue&gt; 的 ContainsKey 检查
    /// </summary>
    private static void GenerateDictionaryContainsKey(ILGenerator ilGenerator, Type dictType)
    {
        // 交换栈顺序：[value, dict] -> [dict, value]
        SwapStackOrder(ilGenerator, dictType, typeof(object));

        // 调用 Dictionary<TKey, TValue>.ContainsKey(TKey) 方法
        var containsMethod = dictType.GetMethod("ContainsKey", [dictType.GetGenericArguments()[0]])!;
        ilGenerator.Emit(OpCodes.Callvirt, containsMethod);
    }

    /// <summary>
    /// 生成字符串的 Contains 检查
    /// </summary>
    private static void GenerateStringContains(ILGenerator ilGenerator)
    {
        // 交换栈顺序：[value, string] -> [string, value]
        SwapStackOrder(ilGenerator, typeof(string), typeof(object));

        // 调用 string.Contains(string) 方法
        var stringContainsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;
        ilGenerator.Emit(OpCodes.Callvirt, stringContainsMethod);
    }

    /// <summary>
    /// 尝试生成通用的 Contains 方法调用
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="collectionType">集合类型</param>
    /// <param name="leftType">左侧值类型</param>
    /// <param name="operation">操作节点（用于错误报告）</param>
    /// <returns>如果成功生成返回 true，否则返回 false</returns>
    private static bool TryGenerateGenericContains(
        ILGenerator ilGenerator,
        Type collectionType,
        Type? leftType,
        Operation operation)
    {
        // 尝试调用 Contains 方法，参数类型为 object
        var containsMethod = collectionType.GetMethod("Contains", [typeof(object)]);

        if (containsMethod is not null)
        {
            // 非静态方法需要交换栈顺序
            if (!containsMethod.IsStatic)
            {
                SwapStackOrder(ilGenerator, collectionType, typeof(object));
            }

            ilGenerator.Emit(containsMethod.IsStatic ? OpCodes.Call : OpCodes.Callvirt, containsMethod);
            return true;
        }

        // 尝试调用 Contains 方法，参数类型为左侧值的类型
        if (leftType is not null)
        {
            containsMethod = collectionType.GetMethod("Contains", [leftType]);
            if (containsMethod is not null)
            {
                // 非静态方法需要交换栈顺序
                if (!containsMethod.IsStatic)
                {
                    SwapStackOrder(ilGenerator, collectionType, leftType);
                }

                ilGenerator.Emit(containsMethod.IsStatic ? OpCodes.Call : OpCodes.Callvirt, containsMethod);
                return true;
            }
        }

        // 对于字符串类型，特殊处理
        if (collectionType == typeof(string))
        {
            GenerateStringContains(ilGenerator);
            return true;
        }

        // 没有找到合适的 Contains 方法
        throw new InvalidOperationError(operation, $"类型 {collectionType.Name} 不支持 in 操作符");
    }

    /// <summary>
    /// 生成 in 操作符的 IL 代码
    /// </summary>
    /// <param name="left">左操作数（要查找的值）</param>
    /// <param name="right">右操作数（集合）</param>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <param name="rightType">右操作数类型</param>
    /// <param name="operation">操作节点（用于错误报告）</param>
    /// <returns>返回 bool 类型</returns>
    public static Type GenerateInOperator(
        LangExpression left,
        LangExpression right,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? rightType,
        Operation operation)
    {
        // 加载左侧值
        left.LoadIlValue(ilGenerator, local);
        var leftInType = left.OutputType(local);

        // 确保左侧值是 object 类型（装箱值类型）
        if (leftInType is { IsValueType: true })
        {
            ilGenerator.Emit(OpCodes.Box, leftInType);
        }

        // 加载右侧集合
        right.LoadIlValue(ilGenerator, local);

        // 根据集合类型生成不同的检查代码
        if (rightType is null)
        {
            throw new InvalidOperationError(operation, "in 操作符右侧类型不能为空");
        }

        // List<T>
        if (rightType.IsGenericType && rightType.GetGenericTypeDefinition() == typeof(List<>))
        {
            GenerateListContains(ilGenerator, rightType);
        }
        // Array
        else if (rightType.IsArray)
        {
            GenerateArrayIndexOf(ilGenerator, rightType);
        }
        // Dictionary<TKey, TValue>
        else if (rightType.IsGenericType && rightType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            GenerateDictionaryContainsKey(ilGenerator, rightType);
        }
        // 其他类型：尝试调用 Contains 方法
        else
        {
            TryGenerateGenericContains(ilGenerator, rightType, leftInType, operation);
        }

        return typeof(bool);
    }
}
