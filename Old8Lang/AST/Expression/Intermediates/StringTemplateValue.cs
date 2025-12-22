using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 字符串插值
/// </summary>
/// <param name="list"></param>
/// <param name="position"></param>
public partial class StringTemplateValue(List<LangExpression> list, SourcePosition position = default) : LangValueType(position)
{

    public override LangValueType Run(VariateManager manager)
    {
        var result = list.Select(item => item.Run(manager)).Aggregate(string.Empty,
            (current, exprResult) => current + exprResult.ToDisplayString());

        return new StringLangValue(result);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 对于字符串模板，我们将所有部分转换为字符串，然后使用string.Concat方法连接
        // 我们将使用string.Concat(object[])方法，这样可以处理不同类型的参数

        // 首先创建一个数组，大小为List的长度
        ilGenerator.Emit(OpCodes.Ldc_I4, list.Count);
        ilGenerator.Emit(OpCodes.Newarr, typeof(object));

        // 遍历所有字符串片段和表达式，将它们添加到数组中
        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];

            // 复制数组引用到栈顶
            ilGenerator.Emit(OpCodes.Dup);

            // 加载索引i
            ilGenerator.Emit(OpCodes.Ldc_I4, i);

            // 加载当前项的值
            item.LoadIlValue(ilGenerator, local);

            // 将值转换为object类型（如果需要）
            var itemType = item.OutputType(local);
            if (itemType is { IsValueType: true })
            {
                // 值类型需要装箱
                ilGenerator.Emit(OpCodes.Box, itemType);
            }
            // 引用类型不需要转换，直接可以作为object

            // 将值存储到数组的第i个位置
            ilGenerator.Emit(OpCodes.Stelem_Ref);
        }

        // 调用string.Concat(object[])方法将所有部分连接起来
        var concatMethod = typeof(string).GetMethod("Concat", [typeof(object[])])!;
        ilGenerator.Emit(OpCodes.Call, concatMethod);
    }

    public override Type OutputType(LocalManager local)
    {
        return typeof(string);
    }
}