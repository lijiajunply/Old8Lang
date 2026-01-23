namespace Old8Lang.AST.Expression.Intermediates;


/// <summary>
/// 可以用来存到引用里的
/// </summary>
/// <param name="position"></param>
public abstract class ImportInfo(SourcePosition position = default) : LangValueType(position);