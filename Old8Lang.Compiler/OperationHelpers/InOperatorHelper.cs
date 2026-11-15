using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;

using Old8Lang.AST.Visitor;

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
    internal static void SwapStackOrder(ILGenerator ilGenerator, Type collectionType, Type valueType)
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
    /// <remarks>
    /// 左侧值在主入口统一装箱成了 object，而 <c>List&lt;T&gt;.Contains(T)</c> 的形参是 T。
    /// 直接传 object 会生成无效 IL（JIT 报 <c>InvalidProgramException</c>），
    /// 因此必须按元素类型拆箱、引用类型则 castclass。
    /// </remarks>
    internal static void GenerateListContains(ILGenerator ilGenerator, Type listType, Type? valueType)
    {
        // 交换栈顺序：[value, list] -> [list, value]
        SwapStackOrder(ilGenerator, listType, typeof(object));

        var elementType = listType.GetGenericArguments()[0];
        EmitValueConversion(ilGenerator, elementType, valueType);

        // 调用 List<T>.Contains(T) 方法
        var containsMethod = listType.GetMethod("Contains", [elementType])!;
        ilGenerator.Emit(OpCodes.Callvirt, containsMethod);
    }

    /// <summary>
    /// 把栈顶装箱值转换成目标元素/键类型。
    /// </summary>
    private static void EmitValueConversion(ILGenerator ilGenerator, Type targetType, Type? sourceType)
    {
        if (targetType == typeof(object)) return;

        if (targetType.IsValueType)
        {
            // 数值宽度不一致时先按实际装箱类型拆箱，再转成目标类型。
            var unboxType = sourceType is { IsValueType: true } && sourceType != targetType
                ? sourceType
                : targetType;

            if (unboxType != targetType && IsNumericType(unboxType) && IsNumericType(targetType))
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, unboxType);
                EmitNumericConversion(ilGenerator, targetType);
                return;
            }

            ilGenerator.Emit(OpCodes.Unbox_Any, targetType);
            return;
        }

        ilGenerator.Emit(OpCodes.Castclass, targetType);
    }

    private static bool IsNumericType(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
        type == typeof(sbyte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong) ||
        type == typeof(float) || type == typeof(double) || type == typeof(decimal);

    private static void EmitNumericConversion(ILGenerator ilGenerator, Type targetType)
    {
        if (targetType == typeof(double)) ilGenerator.Emit(OpCodes.Conv_R8);
        else if (targetType == typeof(float)) ilGenerator.Emit(OpCodes.Conv_R4);
        else if (targetType == typeof(long)) ilGenerator.Emit(OpCodes.Conv_I8);
        else if (targetType == typeof(short)) ilGenerator.Emit(OpCodes.Conv_I2);
        else if (targetType == typeof(byte)) ilGenerator.Emit(OpCodes.Conv_U1);
        else if (targetType == typeof(sbyte)) ilGenerator.Emit(OpCodes.Conv_I1);
        else if (targetType == typeof(ushort)) ilGenerator.Emit(OpCodes.Conv_U2);
        else if (targetType == typeof(uint)) ilGenerator.Emit(OpCodes.Conv_U4);
        else if (targetType == typeof(ulong)) ilGenerator.Emit(OpCodes.Conv_U8);
        else ilGenerator.Emit(OpCodes.Conv_I4);
    }

    /// <summary>
    /// 生成数组的 IndexOf 检查
    /// </summary>
    internal static void GenerateArrayIndexOf(ILGenerator ilGenerator, Type arrayType)
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
    internal static void GenerateDictionaryContainsKey(ILGenerator ilGenerator, Type dictType, Type? valueType)
    {
        // 交换栈顺序：[value, dict] -> [dict, value]
        SwapStackOrder(ilGenerator, dictType, typeof(object));

        var keyType = dictType.GetGenericArguments()[0];
        EmitValueConversion(ilGenerator, keyType, valueType);

        // 调用 Dictionary<TKey, TValue>.ContainsKey(TKey) 方法
        var containsMethod = dictType.GetMethod("ContainsKey", [keyType])!;
        ilGenerator.Emit(OpCodes.Callvirt, containsMethod);
    }

    /// <summary>
    /// 生成字符串的 Contains 检查
    /// </summary>
    internal static void GenerateStringContains(ILGenerator ilGenerator)
    {
        // 交换栈顺序：[value, string] -> [string, value]
        SwapStackOrder(ilGenerator, typeof(string), typeof(object));

        // 字符串的 in 既支持字符（'a' in "abc"），也支持子串（"bc" in "abc"）。
        // 之前固定调用 string.Contains(string)，左操作数若是装箱的 char 会得到 false，
        // 与解释器不一致。
        ilGenerator.Emit(OpCodes.Call, typeof(InOperatorHelper).GetMethod(nameof(ContainsText))!);
    }

    /// <summary>
    /// 字符串 <c>in</c> 的运行期实现，供 IL 模式调用。
    /// </summary>
    /// <remarks>
    /// 语义与解释器一致：字符按单字符包含判断，字符串按子串（序数）判断，
    /// 其它类型按显示文本判断。
    /// </remarks>
    public static bool ContainsText(string text, object? value)
    {
        return value switch
        {
            char character => text.Contains(character),
            string substring => text.Contains(substring, StringComparison.Ordinal),
            null => false,
            _ => text.Contains(value.ToString() ?? string.Empty, StringComparison.Ordinal)
        };
    }

    /// <summary>
    /// 运行期 <c>in</c> 回退：按语言的相等语义枚举集合。
    /// </summary>
    /// <remarks>
    /// 栈上顺序是 [左侧值(object), 右侧集合(object)]，因此形参顺序为 value、collection。
    /// </remarks>
    public static bool RuntimeContains(object? value, object? collection)
    {
        if (collection is null) return false;
        if (collection is string text) return ContainsText(text, value);

        if (collection is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (IlValueBridge.Wrap(item).Equal(IlValueBridge.Wrap(value))) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 尝试生成通用的 Contains 方法调用
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="collectionType">集合类型</param>
    /// <param name="leftType">左侧值类型</param>
    /// <param name="operation">操作节点（用于错误报告）</param>
    /// <returns>如果成功生成返回 true，否则返回 false</returns>
    internal static bool TryGenerateGenericContains(
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
        left.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        var leftInType = left.Accept(new TypeInferenceVisitor(local));

        // 确保左侧值是 object 类型（装箱值类型）
        if (leftInType is { IsValueType: true })
        {
            ilGenerator.Emit(OpCodes.Box, leftInType);
        }

        // 加载右侧集合
        right.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

        // 根据集合类型生成不同的检查代码
        if (rightType is null)
        {
            throw new InvalidOperationError(operation, "in 操作符右侧类型不能为空");
        }

        // List<T>
        if (rightType.IsGenericType && rightType.GetGenericTypeDefinition() == typeof(List<>))
        {
            GenerateListContains(ilGenerator, rightType, leftInType);
        }
        // Array
        else if (rightType.IsArray)
        {
            GenerateArrayIndexOf(ilGenerator, rightType);
        }
        // Dictionary<TKey, TValue>
        else if (rightType.IsGenericType && rightType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            GenerateDictionaryContainsKey(ilGenerator, rightType, leftInType);
        }
        // String：必须先于通用 Contains 回退处理。string.Contains(char) 存在，
        // 通用回退会按左操作数类型（char）选中它，但栈上的值其实是装箱的 object，
        // 传参类型不符，运行期得到 false（'a' in "abc" 因此恒为 false）。
        else if (rightType == typeof(string))
        {
            GenerateStringContains(ilGenerator);
        }
        // 其他类型：尝试调用 Contains 方法；找不到重载时退回运行期枚举，
        // 不能「什么都不发射却声明返回 bool」——那会留下未消费的栈值，生成无效 IL。
        else if (!TryGenerateGenericContains(ilGenerator, rightType, leftInType, operation))
        {
            ilGenerator.Emit(OpCodes.Call,
                typeof(InOperatorHelper).GetMethod(nameof(RuntimeContains))!);
        }

        return typeof(bool);
    }
}
