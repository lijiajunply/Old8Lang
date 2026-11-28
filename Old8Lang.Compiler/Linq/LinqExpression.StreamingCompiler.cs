using System.Collections;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Linq;

internal static partial class LinqExpressionRuntimeExtensions
{
    // 顺序执行同一行的子句，让 let 值只属于当前范围变量。
    internal static void GenerateStreamingQuery(this LinqExpression node, ILGenerator il, LocalManager local)
    {
        var select = (SelectClause)node.TerminationClause;
        var orderBy = node.BodyClauses.LastOrDefault() as OrderByClause;
        var names = node.BodyClauses.OfType<LetClause>().Select(clause => clause.Variable)
            .Prepend(node.FromClause.RangeVariable).Distinct().ToArray();
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
            node.FromClause.DataSource.Accept(RuntimeVisitors.CreateCompiler(il, local));
            var sourceType = node.FromClause.DataSource.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
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
            LocalBuilder? rows = null;
            if (orderBy is not null)
            {
                rows = il.DeclareLocal(typeof(List<LinqOrderedRow>));
                il.Emit(OpCodes.Newobj, typeof(List<LinqOrderedRow>).GetConstructor(Type.EmptyTypes)!);
                il.Emit(OpCodes.Stloc, rows);
            }
            var range = il.DeclareLocal(typeof(object));
            Bind(node.FromClause.RangeVariable, range);

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

            foreach (var clause in node.BodyClauses)
            {
                if (clause is LetClause let)
                {
                    let.Expression.Accept(RuntimeVisitors.CreateCompiler(il, local));
                    var type = let.Expression.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
                    var variable = il.DeclareLocal(type);
                    il.Emit(OpCodes.Stloc, variable);
                    Bind(let.Variable, variable);
                }
                else if (clause is WhereClause where)
                {
                    where.Condition.Accept(RuntimeVisitors.CreateCompiler(il, local));
                    var type = where.Condition.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
                    if (type.IsValueType)
                    {
                        il.Emit(OpCodes.Box, type);
                    }
                    il.Emit(OpCodes.Call, typeof(LinqStreamingHelper).GetMethod(nameof(LinqStreamingHelper.IsTruthy))!);
                    il.Emit(OpCodes.Brfalse, next);
                }
            }

            void EmitProjection()
            {
                il.Emit(OpCodes.Ldloc, result);
                select.Projection.Accept(RuntimeVisitors.CreateCompiler(il, local));
                var projectionType = select.Projection.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
                if (projectionType.IsValueType)
                {
                    il.Emit(OpCodes.Box, projectionType);
                }
                il.Emit(OpCodes.Call, typeof(LinqStreamingHelper).GetMethod(nameof(LinqStreamingHelper.Unwrap))!);
                il.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.Add))!);
            }

            LocalBuilder[]? captured = null;
            if (orderBy is null)
            {
                EmitProjection();
            }
            else
            {
                captured = names.Select(name => local.GetLocalVar(name)!).ToArray();
                il.Emit(OpCodes.Ldloc, rows!);
                EmitLocalArray(il, captured);
                il.Emit(OpCodes.Ldc_I4, orderBy.Orderings.Count);
                il.Emit(OpCodes.Newarr, typeof(object));
                for (var i = 0; i < orderBy.Orderings.Count; i++)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4, i);
                    var expression = orderBy.Orderings[i].KeyExpression;
                    expression.Accept(RuntimeVisitors.CreateCompiler(il, local));
                    var type = expression.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
                    if (type.IsValueType)
                    {
                        il.Emit(OpCodes.Box, type);
                    }
                    il.Emit(OpCodes.Call, typeof(LinqStreamingHelper).GetMethod(nameof(LinqStreamingHelper.Unwrap))!);
                    il.Emit(OpCodes.Stelem_Ref);
                }
                il.Emit(OpCodes.Newobj, typeof(LinqOrderedRow).GetConstructor([typeof(object[]), typeof(object[])])!);
                il.Emit(OpCodes.Callvirt, typeof(List<LinqOrderedRow>).GetMethod(nameof(List<LinqOrderedRow>.Add))!);
            }
            il.MarkLabel(next);
            il.Emit(OpCodes.Br, loop);
            il.MarkLabel(done);
            if (orderBy is not null)
            {
                il.Emit(OpCodes.Ldloc, rows!);
                il.Emit(OpCodes.Ldc_I4, orderBy.Orderings.Count);
                il.Emit(OpCodes.Newarr, typeof(bool));
                for (var i = 0; i < orderBy.Orderings.Count; i++)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(orderBy.Orderings[i].IsAscending ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Stelem_I1);
                }
                il.Emit(OpCodes.Call, typeof(LinqStreamingHelper).GetMethod(nameof(LinqStreamingHelper.SortRows))!);
                il.Emit(OpCodes.Stloc, rows!);

                var index = il.DeclareLocal(typeof(int));
                var row = il.DeclareLocal(typeof(LinqOrderedRow));
                var sortedLoop = il.DefineLabel();
                var sortedDone = il.DefineLabel();
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc, index);
                il.MarkLabel(sortedLoop);
                il.Emit(OpCodes.Ldloc, index);
                il.Emit(OpCodes.Ldloc, rows!);
                il.Emit(OpCodes.Callvirt, typeof(List<LinqOrderedRow>).GetProperty(nameof(List<LinqOrderedRow>.Count))!.GetMethod!);
                il.Emit(OpCodes.Bge, sortedDone);
                il.Emit(OpCodes.Ldloc, rows!);
                il.Emit(OpCodes.Ldloc, index);
                il.Emit(OpCodes.Callvirt, typeof(List<LinqOrderedRow>).GetProperty("Item")!.GetMethod!);
                il.Emit(OpCodes.Stloc, row);
                for (var i = 0; i < captured!.Length; i++)
                {
                    il.Emit(OpCodes.Ldloc, row);
                    il.Emit(OpCodes.Callvirt, typeof(LinqOrderedRow).GetProperty(nameof(LinqOrderedRow.Values))!.GetMethod!);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);
                    var type = captured[i].LocalType;
                    if (type.IsValueType)
                    {
                        il.Emit(OpCodes.Unbox_Any, type);
                    }
                    else if (type != typeof(object))
                    {
                        il.Emit(OpCodes.Castclass, type);
                    }
                    il.Emit(OpCodes.Stloc, captured[i]);
                }
                EmitProjection();
                il.Emit(OpCodes.Ldloc, index);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, index);
                il.Emit(OpCodes.Br, sortedLoop);
                il.MarkLabel(sortedDone);
            }
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

    internal static void EmitLocalArray(ILGenerator il, LocalBuilder[] locals)
    {
        il.Emit(OpCodes.Ldc_I4, locals.Length);
        il.Emit(OpCodes.Newarr, typeof(object));
        for (var i = 0; i < locals.Length; i++)
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldloc, locals[i]);
            if (locals[i].LocalType.IsValueType)
            {
                il.Emit(OpCodes.Box, locals[i].LocalType);
            }
            il.Emit(OpCodes.Stelem_Ref);
        }
    }
}

/// <summary>查询排序时保存当前行的变量和排序键。</summary>
/// <param name="values">当前行的变量值。</param>
/// <param name="keys">当前行的排序键。</param>
public sealed class LinqOrderedRow(object?[] values, object?[] keys)
{
    /// <summary>当前行的范围变量和 let 变量。</summary>
    public object?[] Values { get; } = values;
    /// <summary>当前行的排序键。</summary>
    public object?[] Keys { get; } = keys;
}

/// <summary>IL 查询使用的原生集合和值转换。</summary>
public static class LinqStreamingHelper
{
    /// <summary>按各排序键稳定排序查询行。</summary>
    /// <param name="rows">待排序的查询行。</param>
    /// <param name="ascending">每个排序键的升序标记。</param>
    /// <returns>按排序键排列的查询行。</returns>
    public static List<LinqOrderedRow> SortRows(List<LinqOrderedRow> rows, bool[] ascending)
    {
        var comparer = Comparer<object?>.Create(LinqCompilerHelper.CompareObjects);
        IOrderedEnumerable<LinqOrderedRow>? sorted = null;
        for (var i = 0; i < ascending.Length; i++)
        {
            var key = i;
            sorted = sorted is null
                ? ascending[i] ? rows.OrderBy(row => row.Keys[key], comparer) : rows.OrderByDescending(row => row.Keys[key], comparer)
                : ascending[i] ? sorted.ThenBy(row => row.Keys[key], comparer) : sorted.ThenByDescending(row => row.Keys[key], comparer);
        }
        return sorted?.ToList() ?? rows;
    }
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
