using System.Collections;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using ClassMetadata = Old8Lang.Bytecode.Metadata.ClassMetadata;

namespace Old8Lang.Bytecode.VM;

/// <summary>
/// VirtualMachine - 切片操作
/// </summary>
public partial class VirtualMachine
{
    private object SliceArray(Array array, int start, int end, int step)
    {
        var length = array.Length;

        // 处理负索引
        if (start < 0) start = length + start;
        if (end < 0) end = length + end;

        // 边界检查
        start = Math.Max(0, Math.Min(start, length));
        end = Math.Min(length, end);

        var result = new List<object?>();

        if (step > 0)
        {
            for (int i = start; i < end; i += step)
            {
                result.Add(array.GetValue(i));
            }
        }
        else if (step < 0)
        {
            for (int i = start; i > end; i += step)
            {
                result.Add(array.GetValue(i));
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// 对列表执行切片操作
    /// </summary>

    private object? SliceList(IList list, int start, int end, int step)
    {
        var length = list.Count;

        // 处理负索引
        if (start < 0) start = length + start;
        if (end < 0) end = length + end;

        // 边界检查
        start = Math.Max(0, Math.Min(start, length));
        end = Math.Min(length, end);

        var result = new List<object?>();

        if (step > 0)
        {
            for (int i = start; i < end; i += step)
            {
                result.Add(list[i]);
            }
        }
        else if (step < 0)
        {
            for (int i = start; i > end; i += step)
            {
                result.Add(list[i]);
            }
        }

        return result;
    }

    /// <summary>
    /// 对字符串执行切片操作
    /// </summary>

    private string SliceString(string str, int start, int end, int step)
    {
        var length = str.Length;

        // 处理负索引
        if (start < 0) start = length + start;
        if (end < 0) end = length + end;

        // 边界检查
        start = Math.Max(0, Math.Min(start, length));
        end = Math.Min(length, end);

        var result = new System.Text.StringBuilder();

        if (step > 0)
        {
            for (int i = start; i < end; i += step)
            {
                result.Append(str[i]);
            }
        }
        else if (step < 0)
        {
            for (int i = start; i > end; i += step)
            {
                result.Append(str[i]);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// 重新排列参数以匹配函数参数定义(支持命名参数)
    /// </summary>
    /// <param name="function">函数元数据</param>
    /// <param name="positionalArgs">位置参数</param>
    /// <param name="namedArgNames">命名参数名称数组</param>
    /// <param name="namedArgValues">命名参数值数组</param>
    /// <returns>按函数参数定义顺序排列的参数数组</returns>

}
