using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 字符串插值
/// </summary>
/// <param name="list"></param>
/// <param name="position"></param>
public class StringTreeList(List<OldExpr> list, SourcePosition position = default) : LangValueType(position)
{
    public readonly List<OldExpr> List = list;

    public override LangValueType Run(LangParser.VariateManager manager)
    {
        // 这个方法被调用时，list 中的元素已经是解析后的表达式，
        // 而不是原始的字符串模板。这是因为 ParseStringTree 方法已经将
        // 字符串模板中的占位符分解为了 LangId 对象。
        // 
        // 所以我们只需要直接执行每个表达式，然后将结果拼接起来即可。
        var result = List.Select(item => item.Run(manager)).Aggregate(string.Empty,
            (current, exprResult) => current + exprResult.ToDisplayString());

        return new StringLangValue(result);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 初始化结果字符串为空字符串
        ilGenerator.Emit(OpCodes.Ldstr, "");

        // 遍历所有字符串片段和表达式
        foreach (var item in List)
        {
            // 将当前结果字符串留在栈上
            // 加载当前项的值
            item.LoadIlValue(ilGenerator, local);

            // 确保当前项的值是字符串类型
            var itemType = item.OutputType(local);
            if (itemType != typeof(string))
            {
                // 调用ToString()方法将值转换为字符串
                var toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes)!;
                ilGenerator.Emit(OpCodes.Call, toStringMethod);
            }

            // 调用string.Concat(string, string)方法将当前项的值附加到结果字符串
            var concatMethod = typeof(string).GetMethod("Concat", [typeof(string), typeof(string)])!;
            ilGenerator.Emit(OpCodes.Call, concatMethod);
        }
    }

    public override Type OutputType(LocalManager local)
    {
        return typeof(string);
    }
}