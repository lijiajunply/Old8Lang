using System.Collections;

namespace Old8Lang.Runtime;

/// <summary>
/// 集合运算的共享运行期实现（IL 模式调用）。
/// </summary>
/// <remarks>
/// <c>+</c> 对数组/列表按顺序拼接：解释器与虚拟机通过
/// <c>ArrayLangValue.Plus</c> / <c>ListLangValue.Plus</c> 执行，
/// IL 模式的原生数组与 <c>List&lt;object&gt;</c> 则由这里承接，
/// 保证三种模式结果一致。
/// </remarks>
public static class LangCollectionOperations
{
    /// <summary>数组按顺序拼接。</summary>
    public static object[] ConcatArrays(Array left, Array right)
    {
        var result = new object[left.Length + right.Length];
        Array.Copy(left, 0, result, 0, left.Length);
        Array.Copy(right, 0, result, left.Length, right.Length);
        return result;
    }

    /// <summary>列表按顺序拼接（原生 <see cref="List{T}"/> / 任意 <see cref="IEnumerable"/>）。</summary>
    public static List<object?> ConcatLists(IEnumerable left, IEnumerable right)
    {
        var result = new List<object?>();
        foreach (var item in left) result.Add(item);
        foreach (var item in right) result.Add(item);
        return result;
    }
}
