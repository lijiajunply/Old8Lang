using Old8Lang.AST.Expression.Intermediates;

namespace Old8Lang.AST.Expression.Value;

/// <summary> 
/// 类型模板类，用于存储类的定义信息 
/// </summary> 
public class TypeTemplate(string className, Dictionary<LangId, OldExpr> variates, SourcePosition position = default)
    : ImportInfo(position)
{
    public readonly string ClassName = className;
    public readonly Dictionary<LangId, OldExpr> Variates = variates;

    public override string ToString() => $"TypeTemplate({ClassName})";

    /// <summary>
    /// 创建类的实例
    /// </summary>
    /// <returns>类的实例</returns>
    public AnyLangValue CreateInstance()
    {
        // 创建一个新的空AnyLangValue实例，传递类的所有成员变量和方法
        var instance = new AnyLangValue(new LangId(ClassName), new Dictionary<LangId, OldExpr>(Variates), Position);

        return instance;
    }

    public override LangValueType Run(LangParser.VariateManager manager)
    {
        return this;
    }
}