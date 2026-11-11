using System.Collections;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Linq;

public partial class LinqExpression
{
    private bool CanGenerateStreamingQuery() =>
        Continuation is null && TerminationClause is SelectClause &&
        BodyClauses.All(clause => clause is LetClause or WhereClause);

    // 顺序执行同一行的子句，让 let 值只属于当前范围变量。
    private void GenerateStreamingQuery(ILGenerator il, LocalManager local)
    {
        var select = (SelectClause)TerminationClause;
        var names = BodyClauses.OfType<LetClause>().Select(clause => clause.Variable)
            .Prepend(FromClause.RangeVariable).Distinct().ToArray();
        var previous = names.ToDictionary(name => name, name => local.GetLocalVar(name));
        var previousFields = names.Where(local.FieldVar.ContainsKey)
            .ToDictionary(name => name, name => local.FieldVar[name]);
        var previousStatics = names.Where(local.GlobalStaticClasses.ContainsKey)
            .ToDictionary(name => name, name => local.GlobalStaticClasses[name]);

        void Bind(string name, LocalBuilder variable)
        {
            local.FieldVar.Remove(name);
            local.GlobalStaticClasses.Remove(name);
            local.AddLocalVar(name, variable);
        }

        try
        {
            FromClause.DataSource.LoadIlValue(il, local);
            var sourceType = FromClause.DataSource.OutputType(local) ?? typeof(object);
            if (sourceType.IsValueType)
            {
                il.Emit(OpCodes.Box, sourceType);
            }
            il.Emit(OpCodes.Call, typeof(LinqStreamingHelper).GetMethod(nameof(LinqStreamingHelper.GetEnumerable))!);
            il.Emit(OpCodes.Callvirt, typeof(IEnumerable).GetMethod(nameof(IEnumerable.GetEnumerator))!);
            var enumerator = il.DeclareLocal(typeof(IEnumerator));
            il.Emit(OpCodes.Stloc, enumerator);

            var result = il.DeclareLocal(typeof(List<object>));
            il.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(Type.EmptyTypes)!);
            il.Emit(OpCodes.Stloc, result);
            var range = il.DeclareLocal(typeof(object));
            Bind(FromClause.RangeVariable, range);

            var loop = il.DefineLabel();
            var done = il.DefineLabel();
            var next = il.DefineLabel();
            il.MarkLabel(loop);
            il.Emit(OpCodes.Ldloc, enumerator);
            il.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext))!);
            il.Emit(OpCodes.Brfalse, done);
            il.Emit(OpCodes.Ldloc, enumerator);
            il.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetProperty(nameof(IEnumerator.Current))!.GetMethod!);
            il.Emit(OpCodes.Call, typeof(LinqStreamingHelper).GetMethod(nameof(LinqStreamingHelper.Unwrap))!);
            il.Emit(OpCodes.Stloc, range);

            foreach (var clause in BodyClauses)
            {
                if (clause is LetClause let)
                {
                    let.Expression.LoadIlValue(il, local);
                    var type = let.Expression.OutputType(local) ?? typeof(object);
                    var variable = il.DeclareLocal(type);
                    il.Emit(OpCodes.Stloc, variable);
                    Bind(let.Variable, variable);
                }
                else if (clause is WhereClause where)
                {
                    where.Condition.LoadIlValue(il, local);
                    var type = where.Condition.OutputType(local) ?? typeof(object);
                    if (type.IsValueType)
                    {
                        il.Emit(OpCodes.Box, type);
                    }
                    il.Emit(OpCodes.Call, typeof(LinqStreamingHelper).GetMethod(nameof(LinqStreamingHelper.IsTruthy))!);
                    il.Emit(OpCodes.Brfalse, next);
                }
            }

            il.Emit(OpCodes.Ldloc, result);
            select.Projection.LoadIlValue(il, local);
            var projectionType = select.Projection.OutputType(local) ?? typeof(object);
            if (projectionType.IsValueType)
            {
                il.Emit(OpCodes.Box, projectionType);
            }
            il.Emit(OpCodes.Call, typeof(LinqStreamingHelper).GetMethod(nameof(LinqStreamingHelper.Unwrap))!);
            il.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.Add))!);
            il.MarkLabel(next);
            il.Emit(OpCodes.Br, loop);
            il.MarkLabel(done);
            il.Emit(OpCodes.Ldloc, result);
        }
        finally
        {
            foreach (var (name, oldLocal) in previous)
            {
                if (oldLocal is null)
                {
                    local.RemoveLocalVar(name);
                }
                else
                {
                    local.AddLocalVar(name, oldLocal);
                }
            }
            foreach (var (name, field) in previousFields)
            {
                local.FieldVar[name] = field;
            }
            foreach (var (name, value) in previousStatics)
            {
                local.GlobalStaticClasses[name] = value;
            }
        }
    }
}

/// <summary>IL 查询使用的原生集合和值转换。</summary>
public static class LinqStreamingHelper
{
    /// <summary>获取查询数据源。</summary>
    public static IEnumerable GetEnumerable(object? source) => source switch
    {
        ILangList list => list.GetItems(),
        IEnumerable enumerable => enumerable,
        _ => throw new InvalidOperationError(default(SourcePosition), "LINQ 数据源必须是可枚举集合")
    };

    /// <summary>将解释器值转换为 IL 使用的原生值。</summary>
    public static object? Unwrap(object? value) => value is LangValueType langValue ? langValue.GetValue() : value;

    /// <summary>将查询条件转换为布尔值。</summary>
    public static bool IsTruthy(object? value) => Unwrap(value) switch
    {
        null => false,
        bool boolean => boolean,
        int number => number != 0,
        double number => number != 0,
        string text => text.Length != 0,
        _ => true
    };
}
