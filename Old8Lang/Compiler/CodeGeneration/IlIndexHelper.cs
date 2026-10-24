using System.Collections;
using System.Runtime.CompilerServices;
using Old8Lang.Error;

namespace Old8Lang.Compiler.CodeGeneration;

/// <summary>
/// IL 模式索引访问的运行期辅助方法。
/// </summary>
/// <remarks>
/// 解释器（<c>ListLangValue</c> / <c>ArrayLangValue</c> / <c>StringLangValue</c> /
/// <c>TupleLangValue</c>）与虚拟机都支持负索引（<c>-1</c> 表示最后一个元素），
/// 而 .NET 的 <see cref="IList"/> 索引器、<see cref="Array.GetValue(int)"/>、
/// <see cref="string.Chars"/> 和 <see cref="ITuple"/> 索引器只接受非负下标。
/// IL 生成前无法得知集合长度，因此把下标归一化与越界检查放到这些静态方法里做，
/// 保证三种模式行为一致，并且越界时抛语言层的 <see cref="IndexError"/>。
/// </remarks>
public static class IlIndexHelper
{
    /// <summary>
    /// 把负索引归一化为从 0 开始的下标；非负索引原样返回。
    /// </summary>
    public static int Normalize(int index, int length)
    {
        return index < 0 ? index + length : index;
    }

    /// <summary>
    /// 归一化并检查范围，越界时抛 <see cref="IndexError"/>。
    /// </summary>
    private static int NormalizeChecked(int index, int length)
    {
        var normalized = Normalize(index, length);
        if (normalized < 0 || normalized >= length)
        {
            throw new IndexError(default(SourcePosition), index, length);
        }

        return normalized;
    }

    /// <summary>
    /// 读取列表元素（支持任意实现 <see cref="IList"/> 的集合，包括 <c>List&lt;T&gt;</c>）。
    /// </summary>
    public static object? GetListItem(IList list, int index)
    {
        return list[NormalizeChecked(index, list.Count)];
    }

    /// <summary>
    /// 读取数组元素。
    /// </summary>
    public static object? GetArrayItem(Array array, int index)
    {
        return array.GetValue(NormalizeChecked(index, array.Length));
    }

    /// <summary>
    /// 读取字符串字符。
    /// </summary>
    public static char GetStringChar(string text, int index)
    {
        return text[NormalizeChecked(index, text.Length)];
    }

    /// <summary>
    /// 读取元组元素。
    /// </summary>
    public static object? GetTupleItem(ITuple tuple, int index)
    {
        return tuple[NormalizeChecked(index, tuple.Length)];
    }

    /// <summary>
    /// 静态类型为 <c>object</c> 时的索引访问：按数组 → 列表 → 元组 → 字符串的顺序尝试。
    /// </summary>
    public static object? GetObjectItem(object? collection, int index)
    {
        return collection switch
        {
            // Array 也实现 IList，必须放在前面
            Array array => GetArrayItem(array, index),
            IList list => GetListItem(list, index),
            ITuple tuple => GetTupleItem(tuple, index),
            string text => GetStringChar(text, index),
            null => throw new IndexError(default(SourcePosition), $"索引访问的目标为 null（索引 {index}）"),
            _ => throw new InvalidOperationException(
                $"类型 '{collection.GetType().Name}' 不支持索引访问")
        };
    }
}
