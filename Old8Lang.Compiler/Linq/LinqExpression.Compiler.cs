using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.Intermediates;
using System.Collections;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Linq;

/// <summary>
/// LinqExpression 的编译器支持部分
/// 使用纯IL实现，不依赖VariateManager
/// </summary>
internal static partial class LinqExpressionRuntimeExtensions
{
    /// <summary>
    /// 生成 Where 子句的 IL 代码
    /// </summary>
    internal static void GenerateWhereClauseIL(this LinqExpression node, WhereClause whereClause, ILGenerator ilGenerator, LocalManager local)
    {
        // 栈顶是 IEnumerable
        // 我们需要生成类似于 source.Where(x => condition) 的代码

        // 创建一个 List 来存储过滤后的结果
        var resultLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Stloc, resultLocal);

        // 统一转换成 IEnumerable 再取枚举器（上游 join/orderby 的静态栈类型不一定是接口）
        ConvertToEnumerableStatic(ilGenerator);
        var enumeratorLocal = ilGenerator.DeclareLocal(typeof(IEnumerator));
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerable).GetMethod("GetEnumerator")!);
        ilGenerator.Emit(OpCodes.Stloc, enumeratorLocal);

        // 预先声明循环内使用的局部变量
        var currentLocal = ilGenerator.DeclareLocal(typeof(object));
        var rangeVarLocal = node.GetOrCreateRangeVariable(ilGenerator, local, node.FromClause.RangeVariable);
        // 循环标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();

        // 开始循环
        ilGenerator.MarkLabel(loopStart);
        ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetMethod("MoveNext")!);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd);

        // 获取当前元素
        ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetProperty("Current")!.GetMethod!);

        // 保存当前元素到局部变量
        ilGenerator.Emit(OpCodes.Stloc, currentLocal);

        // 将当前元素转换为LangValueType并存储到范围变量
        node.EmitBindRow(ilGenerator, local, currentLocal, whereClause);

        // 计算条件表达式
        whereClause.Condition.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

        // 检查条件是否为真（需要按条件类型装箱：语言值比较返回的是原生 bool）
        var conditionType = whereClause.Condition.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
        node.GenerateIsTruthy(ilGenerator, conditionType);

        // 如果条件为假，跳过添加
        var skipAdd = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Brfalse, skipAdd);

        // 添加到结果列表
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
        ilGenerator.Emit(OpCodes.Ldloc, currentLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("Add")!);

        ilGenerator.MarkLabel(skipAdd);

        // 继续循环
        ilGenerator.Emit(OpCodes.Br, loopStart);

        // 循环结束
        ilGenerator.MarkLabel(loopEnd);

        // 加载结果列表到栈上
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
    }

    /// <summary>
    /// 生成 Select 子句的 IL 代码
    /// </summary>
    internal static void GenerateSelectClauseIL(this LinqExpression node, SelectClause selectClause, ILGenerator ilGenerator, LocalManager local)
    {
        // 栈顶是 IEnumerable
        // 我们需要生成类似于 source.Select(x => projection) 的代码

        // 创建一个 List 来存储投影后的结果
        var resultLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Stloc, resultLocal);

        // 获取枚举器
        var enumeratorLocal = ilGenerator.DeclareLocal(typeof(IEnumerator));
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerable).GetMethod("GetEnumerator")!);
        ilGenerator.Emit(OpCodes.Stloc, enumeratorLocal);

        // 预先声明循环内使用的局部变量
        var currentLocal = ilGenerator.DeclareLocal(typeof(object));
        var projectionLocal = ilGenerator.DeclareLocal(typeof(LangValueType));
        var rangeVarLocal = node.GetOrCreateRangeVariable(ilGenerator, local, node.FromClause.RangeVariable);

        // 循环标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();

        // 开始循环
        ilGenerator.MarkLabel(loopStart);
        ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetMethod("MoveNext")!);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd);

        // 获取当前元素
        ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetProperty("Current")!.GetMethod!);

        // 保存当前元素到局部变量
        ilGenerator.Emit(OpCodes.Stloc, currentLocal);

        // 将当前元素转换为LangValueType并存储到范围变量
        node.EmitBindRow(ilGenerator, local, currentLocal, null);

        // 计算投影表达式
        selectClause.Projection.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

        var projectionType = selectClause.Projection.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
        if (projectionType.IsValueType) ilGenerator.Emit(OpCodes.Box, projectionType);
        node.GenerateConvertToLangValue(ilGenerator);

        // 保存投影结果
        ilGenerator.Emit(OpCodes.Stloc, projectionLocal);

        // 添加到结果列表
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
        ilGenerator.Emit(OpCodes.Ldloc, projectionLocal);
        node.GenerateConvertFromLangValue(ilGenerator);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("Add")!);

        // 继续循环
        ilGenerator.Emit(OpCodes.Br, loopStart);

        // 循环结束
        ilGenerator.MarkLabel(loopEnd);

        // 加载结果列表到栈上
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
    }

    /// <summary>
    /// 生成 OrderBy 子句的 IL 代码
    /// </summary>
    internal static void GenerateOrderByClauseIL(this LinqExpression node, OrderByClause orderByClause, ILGenerator ilGenerator, LocalManager local)
    {
        var rows = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Stloc, rows);
        ConvertToEnumerableStatic(ilGenerator);
        var enumerator = ilGenerator.DeclareLocal(typeof(IEnumerator));
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerable).GetMethod("GetEnumerator")!);
        ilGenerator.Emit(OpCodes.Stloc, enumerator);
        var current = ilGenerator.DeclareLocal(typeof(object));
        var loop = ilGenerator.DefineLabel();
        var end = ilGenerator.DefineLabel();
        ilGenerator.MarkLabel(loop);
        ilGenerator.Emit(OpCodes.Ldloc, enumerator);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetMethod("MoveNext")!);
        ilGenerator.Emit(OpCodes.Brfalse, end);
        ilGenerator.Emit(OpCodes.Ldloc, enumerator);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetProperty("Current")!.GetMethod!);
        ilGenerator.Emit(OpCodes.Stloc, current);
        node.EmitBindRow(ilGenerator, local, current, orderByClause);

        ilGenerator.Emit(OpCodes.Ldloc, rows);
        ilGenerator.Emit(OpCodes.Ldloc, current);
        ilGenerator.Emit(OpCodes.Ldc_I4, orderByClause.Orderings.Count);
        ilGenerator.Emit(OpCodes.Newarr, typeof(object));
        for (var i = 0; i < orderByClause.Orderings.Count; i++)
        {
            var expression = orderByClause.Orderings[i].KeyExpression;
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Ldc_I4, i);
            expression.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            var type = expression.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
            if (type.IsValueType) ilGenerator.Emit(OpCodes.Box, type);
            ilGenerator.Emit(OpCodes.Stelem_Ref);
        }
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod(nameof(LinqCompilerHelper.AddSortRow))!);
        ilGenerator.Emit(OpCodes.Br, loop);
        ilGenerator.MarkLabel(end);
        ilGenerator.Emit(OpCodes.Ldloc, rows);
        ilGenerator.Emit(OpCodes.Ldc_I4, orderByClause.Orderings.Count);
        ilGenerator.Emit(OpCodes.Newarr, typeof(bool));
        for (var i = 0; i < orderByClause.Orderings.Count; i++)
        {
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Ldc_I4, i);
            ilGenerator.Emit(orderByClause.Orderings[i].IsAscending ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Stelem_I1);
        }
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod(nameof(LinqCompilerHelper.SortRows))!);
    }

    /// <summary>
    /// 生成 Let 子句的 IL 代码
    /// </summary>
    internal static void GenerateLetClauseIL(this LinqExpression node, LetClause letClause, ILGenerator ilGenerator, LocalManager local)
    {
        // Let 子句需要在遍历过程中计算表达式并存储到变量
        // 由于Let子句在循环内部执行，我们需要在循环外部声明变量

        // 栈顶是 IEnumerable
        // 创建一个 List 来存储带有let变量的结果
        var resultLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Stloc, resultLocal);

        // 获取枚举器
        var enumeratorLocal = ilGenerator.DeclareLocal(typeof(IEnumerator));
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerable).GetMethod("GetEnumerator")!);
        ilGenerator.Emit(OpCodes.Stloc, enumeratorLocal);

        // 预先声明循环内使用的局部变量
        var currentLocal = ilGenerator.DeclareLocal(typeof(object));
        var rangeVarLocal = node.GetOrCreateRangeVariable(ilGenerator, local, node.FromClause.RangeVariable);

        // 循环标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();

        // 开始循环
        ilGenerator.MarkLabel(loopStart);
        ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetMethod("MoveNext")!);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd);

        // 获取当前元素
        ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetProperty("Current")!.GetMethod!);

        // 保存当前元素到局部变量
        ilGenerator.Emit(OpCodes.Stloc, currentLocal);

        // 将当前元素转换为LangValueType并存储到范围变量
        node.EmitBindRow(ilGenerator, local, currentLocal, letClause);

        // 计算let表达式
        letClause.Expression.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        var letType = letClause.Expression.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
        if (letType.IsValueType) ilGenerator.Emit(OpCodes.Box, letType);
        node.GenerateConvertToLangValue(ilGenerator);
        var letVarLocal = BindLinqVariable(ilGenerator, local, letClause.Variable);
        ilGenerator.Emit(OpCodes.Stloc, letVarLocal);

        // 添加当前元素到结果列表
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
        ilGenerator.Emit(OpCodes.Ldloc, currentLocal);
        ilGenerator.Emit(OpCodes.Ldstr, node.FromClause.RangeVariable);
        ilGenerator.Emit(OpCodes.Ldstr, letClause.Variable);
        ilGenerator.Emit(OpCodes.Ldloc, letVarLocal);
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod(nameof(LinqCompilerHelper.AddRowBinding))!);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("Add")!);

        // 继续循环
        ilGenerator.Emit(OpCodes.Br, loopStart);

        // 循环结束
        ilGenerator.MarkLabel(loopEnd);

        // 加载结果列表到栈上
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
    }

    /// <summary>
    /// 生成 GroupBy 子句的 IL 代码
    /// </summary>
    internal static void GenerateGroupByClauseIL(this LinqExpression node, GroupByClause groupByClause, ILGenerator ilGenerator, LocalManager local)
    {
        // 栈顶是 IEnumerable
        // 创建分组字典
        var groupDictLocal = ilGenerator.DeclareLocal(typeof(Dictionary<object, List<object>>));
        var dictCtor = typeof(Dictionary<object, List<object>>).GetConstructor(Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Newobj, dictCtor);
        ilGenerator.Emit(OpCodes.Stloc, groupDictLocal);

        // 获取枚举器
        var enumeratorLocal = ilGenerator.DeclareLocal(typeof(IEnumerator));
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerable).GetMethod("GetEnumerator")!);
        ilGenerator.Emit(OpCodes.Stloc, enumeratorLocal);

        // 预先声明循环内使用的局部变量
        var currentLocal = ilGenerator.DeclareLocal(typeof(object));
        var keyLocal = ilGenerator.DeclareLocal(typeof(object));
        var elementLocal = ilGenerator.DeclareLocal(typeof(object));
        var rangeVarLocal = node.GetOrCreateRangeVariable(ilGenerator, local, node.FromClause.RangeVariable);

        // 循环标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();

        // 开始循环
        ilGenerator.MarkLabel(loopStart);
        ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetMethod("MoveNext")!);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd);

        // 获取当前元素
        ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetProperty("Current")!.GetMethod!);

        // 保存当前元素到局部变量
        ilGenerator.Emit(OpCodes.Stloc, currentLocal);

        // 将当前元素转换为LangValueType并存储到范围变量
        node.EmitBindRow(ilGenerator, local, currentLocal, null);

        // 计算分组键
        groupByClause.KeyExpression.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        var keyType = groupByClause.KeyExpression.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
        if (keyType.IsValueType) ilGenerator.Emit(OpCodes.Box, keyType);
        ilGenerator.Emit(OpCodes.Call, typeof(Old8Lang.InstanceMethods.Core.IlValueBridge)
            .GetMethod(nameof(Old8Lang.InstanceMethods.Core.IlValueBridge.UnwrapDynamic))!);
        ilGenerator.Emit(OpCodes.Stloc, keyLocal);

        // 计算分组元素
        groupByClause.ElementExpression.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        var elementType = groupByClause.ElementExpression.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
        if (elementType.IsValueType) ilGenerator.Emit(OpCodes.Box, elementType);
        ilGenerator.Emit(OpCodes.Call, typeof(Old8Lang.InstanceMethods.Core.IlValueBridge)
            .GetMethod(nameof(Old8Lang.InstanceMethods.Core.IlValueBridge.UnwrapDynamic))!);
        ilGenerator.Emit(OpCodes.Stloc, elementLocal);

        // 调用辅助方法添加到分组
        ilGenerator.Emit(OpCodes.Ldloc, groupDictLocal);
        ilGenerator.Emit(OpCodes.Ldloc, keyLocal);
        ilGenerator.Emit(OpCodes.Ldloc, elementLocal);
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("AddToGroup")!);

        // 继续循环
        ilGenerator.Emit(OpCodes.Br, loopStart);

        // 循环结束
        ilGenerator.MarkLabel(loopEnd);

        // 将分组字典转换为列表
        ilGenerator.Emit(OpCodes.Ldloc, groupDictLocal);
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("GroupDictToList")!);
    }

    /// <summary>
    /// 生成 Join 子句的 IL 代码
    /// 支持 Inner Join 和 Group Join
    /// </summary>
    internal static void GenerateJoinClauseIL(this LinqExpression node, JoinClause joinClause, ILGenerator ilGenerator, LocalManager local)
    {
        // 栈顶是外部数据源的 IEnumerable
        // 保存外部数据源
        var outerSourceLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Stloc, outerSourceLocal);

        // 将外部数据源复制到列表
        var outerEnumeratorLocal = ilGenerator.DeclareLocal(typeof(IEnumerator));
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerable).GetMethod("GetEnumerator")!);
        ilGenerator.Emit(OpCodes.Stloc, outerEnumeratorLocal);

        var copyOuterLoopStart = ilGenerator.DefineLabel();
        var copyOuterLoopEnd = ilGenerator.DefineLabel();

        ilGenerator.MarkLabel(copyOuterLoopStart);
        ilGenerator.Emit(OpCodes.Ldloc, outerEnumeratorLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetMethod("MoveNext")!);
        ilGenerator.Emit(OpCodes.Brfalse, copyOuterLoopEnd);

        ilGenerator.Emit(OpCodes.Ldloc, outerSourceLocal);
        ilGenerator.Emit(OpCodes.Ldloc, outerEnumeratorLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetProperty("Current")!.GetMethod!);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("Add")!);

        ilGenerator.Emit(OpCodes.Br, copyOuterLoopStart);
        ilGenerator.MarkLabel(copyOuterLoopEnd);

        // 创建结果列表
        var resultLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Stloc, resultLocal);

        // 获取或创建范围变量
        var outerRangeVarLocal = node.GetOrCreateRangeVariable(ilGenerator, local, node.FromClause.RangeVariable);

        // 预先声明循环内使用的局部变量（避免在循环内重复声明）
        var outerIndexLocal = ilGenerator.DeclareLocal(typeof(int));
        var outerItemLocal = ilGenerator.DeclareLocal(typeof(object));
        var outerKeyLocal = ilGenerator.DeclareLocal(typeof(object));
        var innerIndexLocal = ilGenerator.DeclareLocal(typeof(int));
        var innerItemLocal = ilGenerator.DeclareLocal(typeof(object));
        var innerKeyLocal = ilGenerator.DeclareLocal(typeof(object));

        // 外部循环
        var outerLoopStart = ilGenerator.DefineLabel();
        var outerLoopEnd = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Stloc, outerIndexLocal);

        ilGenerator.MarkLabel(outerLoopStart);
        // 检查外部索引是否小于外部列表长度
        ilGenerator.Emit(OpCodes.Ldloc, outerIndexLocal);
        ilGenerator.Emit(OpCodes.Ldloc, outerSourceLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(List<object>).GetProperty("Count")!.GetMethod!);
        ilGenerator.Emit(OpCodes.Bge, outerLoopEnd);

        // 获取外部元素
        ilGenerator.Emit(OpCodes.Ldloc, outerSourceLocal);
        ilGenerator.Emit(OpCodes.Ldloc, outerIndexLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("get_Item")!);
        ilGenerator.Emit(OpCodes.Stloc, outerItemLocal);

        // 设置外部范围变量
        node.EmitBindRow(ilGenerator, local, outerItemLocal, joinClause);

        // 加载内部数据源
        joinClause.InnerDataSource.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        ConvertToEnumerableStatic(ilGenerator);

        // 将内部数据源复制到列表
        var innerSourceLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Stloc, innerSourceLocal);

        var innerEnumeratorLocal = ilGenerator.DeclareLocal(typeof(IEnumerator));
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerable).GetMethod("GetEnumerator")!);
        ilGenerator.Emit(OpCodes.Stloc, innerEnumeratorLocal);

        var copyInnerLoopStart = ilGenerator.DefineLabel();
        var copyInnerLoopEnd = ilGenerator.DefineLabel();

        ilGenerator.MarkLabel(copyInnerLoopStart);
        ilGenerator.Emit(OpCodes.Ldloc, innerEnumeratorLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetMethod("MoveNext")!);
        ilGenerator.Emit(OpCodes.Brfalse, copyInnerLoopEnd);

        ilGenerator.Emit(OpCodes.Ldloc, innerSourceLocal);
        ilGenerator.Emit(OpCodes.Ldloc, innerEnumeratorLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetProperty("Current")!.GetMethod!);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("Add")!);

        ilGenerator.Emit(OpCodes.Br, copyInnerLoopStart);
        ilGenerator.MarkLabel(copyInnerLoopEnd);

        // 计算外部键
        joinClause.OuterKeyExpression.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        node.EmitUnwrapQueryExpression(joinClause.OuterKeyExpression, ilGenerator, local);
        ilGenerator.Emit(OpCodes.Stloc, outerKeyLocal);
        var innerRangeVarLocal = BindLinqVariable(ilGenerator, local, joinClause.RangeVariable);

        if (joinClause.IsGroupJoin && joinClause.GroupVariable != null)
        {
            // Group Join: 收集所有匹配的内部元素
            var matchingItemsLocal = ilGenerator.DeclareLocal(typeof(List<object>));
            ilGenerator.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(Type.EmptyTypes)!);
            ilGenerator.Emit(OpCodes.Stloc, matchingItemsLocal);

            // 内部循环
            var innerLoopStart = ilGenerator.DefineLabel();
            var innerLoopEnd = ilGenerator.DefineLabel();
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Stloc, innerIndexLocal);

            ilGenerator.MarkLabel(innerLoopStart);
            ilGenerator.Emit(OpCodes.Ldloc, innerIndexLocal);
            ilGenerator.Emit(OpCodes.Ldloc, innerSourceLocal);
            ilGenerator.Emit(OpCodes.Callvirt, typeof(List<object>).GetProperty("Count")!.GetMethod!);
            ilGenerator.Emit(OpCodes.Bge, innerLoopEnd);

            // 获取内部元素
            ilGenerator.Emit(OpCodes.Ldloc, innerSourceLocal);
            ilGenerator.Emit(OpCodes.Ldloc, innerIndexLocal);
            ilGenerator.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("get_Item")!);
            ilGenerator.Emit(OpCodes.Stloc, innerItemLocal);

            // 设置内部范围变量
            ilGenerator.Emit(OpCodes.Ldloc, innerItemLocal);
            node.GenerateConvertToLangValue(ilGenerator);
            ilGenerator.Emit(OpCodes.Stloc, innerRangeVarLocal);

            // 计算内部键
            joinClause.InnerKeyExpression.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            node.EmitUnwrapQueryExpression(joinClause.InnerKeyExpression, ilGenerator, local);
            ilGenerator.Emit(OpCodes.Stloc, innerKeyLocal);

            // 比较键
            ilGenerator.Emit(OpCodes.Ldloc, outerKeyLocal);
            ilGenerator.Emit(OpCodes.Ldloc, innerKeyLocal);
            ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("KeysEqual")!);
            var skipAddLabel = ilGenerator.DefineLabel();
            ilGenerator.Emit(OpCodes.Brfalse, skipAddLabel);

            // 键匹配，添加内部元素到匹配列表
            ilGenerator.Emit(OpCodes.Ldloc, matchingItemsLocal);
            ilGenerator.Emit(OpCodes.Ldloc, innerItemLocal);
            ilGenerator.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("Add")!);

            ilGenerator.MarkLabel(skipAddLabel);

            // 内部循环递增
            ilGenerator.Emit(OpCodes.Ldloc, innerIndexLocal);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Add);
            ilGenerator.Emit(OpCodes.Stloc, innerIndexLocal);
            ilGenerator.Emit(OpCodes.Br, innerLoopStart);

            ilGenerator.MarkLabel(innerLoopEnd);

            // 设置分组变量
            var groupVarLocal = BindLinqVariable(ilGenerator, local, joinClause.GroupVariable);
            ilGenerator.Emit(OpCodes.Ldloc, matchingItemsLocal);
            ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("ConvertToListLangValue")!);
            ilGenerator.Emit(OpCodes.Stloc, groupVarLocal);

            // 每一行保存自己的外部值和分组值，后续子句按当前行恢复。
            ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
            ilGenerator.Emit(OpCodes.Ldloc, outerItemLocal);
            ilGenerator.Emit(OpCodes.Ldstr, node.FromClause.RangeVariable);
            ilGenerator.Emit(OpCodes.Ldstr, joinClause.GroupVariable);
            ilGenerator.Emit(OpCodes.Ldloc, groupVarLocal);
            ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod(nameof(LinqCompilerHelper.AddRowBinding))!);
            ilGenerator.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("Add")!);
        }
        else
        {
            // Inner Join: 为每个匹配创建结果
            var innerLoopStart = ilGenerator.DefineLabel();
            var innerLoopEnd = ilGenerator.DefineLabel();
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Stloc, innerIndexLocal);

            ilGenerator.MarkLabel(innerLoopStart);
            ilGenerator.Emit(OpCodes.Ldloc, innerIndexLocal);
            ilGenerator.Emit(OpCodes.Ldloc, innerSourceLocal);
            ilGenerator.Emit(OpCodes.Callvirt, typeof(List<object>).GetProperty("Count")!.GetMethod!);
            ilGenerator.Emit(OpCodes.Bge, innerLoopEnd);

            // 获取内部元素
            ilGenerator.Emit(OpCodes.Ldloc, innerSourceLocal);
            ilGenerator.Emit(OpCodes.Ldloc, innerIndexLocal);
            ilGenerator.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("get_Item")!);
            ilGenerator.Emit(OpCodes.Stloc, innerItemLocal);

            // 设置内部范围变量
            ilGenerator.Emit(OpCodes.Ldloc, innerItemLocal);
            node.GenerateConvertToLangValue(ilGenerator);
            ilGenerator.Emit(OpCodes.Stloc, innerRangeVarLocal);

            // 计算内部键
            joinClause.InnerKeyExpression.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            node.EmitUnwrapQueryExpression(joinClause.InnerKeyExpression, ilGenerator, local);
            ilGenerator.Emit(OpCodes.Stloc, innerKeyLocal);

            // 比较键
            ilGenerator.Emit(OpCodes.Ldloc, outerKeyLocal);
            ilGenerator.Emit(OpCodes.Ldloc, innerKeyLocal);
            ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("KeysEqual")!);
            var skipAddLabel = ilGenerator.DefineLabel();
            ilGenerator.Emit(OpCodes.Brfalse, skipAddLabel);

            // 键匹配时保存外部行和当前内层值。
            ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
            ilGenerator.Emit(OpCodes.Ldloc, outerItemLocal);
            ilGenerator.Emit(OpCodes.Ldstr, node.FromClause.RangeVariable);
            ilGenerator.Emit(OpCodes.Ldstr, joinClause.RangeVariable);
            ilGenerator.Emit(OpCodes.Ldloc, innerRangeVarLocal);
            ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod(nameof(LinqCompilerHelper.AddRowBinding))!);
            ilGenerator.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("Add")!);

            ilGenerator.MarkLabel(skipAddLabel);

            // 内部循环递增
            ilGenerator.Emit(OpCodes.Ldloc, innerIndexLocal);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Add);
            ilGenerator.Emit(OpCodes.Stloc, innerIndexLocal);
            ilGenerator.Emit(OpCodes.Br, innerLoopStart);

            ilGenerator.MarkLabel(innerLoopEnd);
        }

        // 外部循环递增
        ilGenerator.Emit(OpCodes.Ldloc, outerIndexLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Add);
        ilGenerator.Emit(OpCodes.Stloc, outerIndexLocal);
        ilGenerator.Emit(OpCodes.Br, outerLoopStart);

        ilGenerator.MarkLabel(outerLoopEnd);

        // 加载结果列表到栈上
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
    }

    /// <summary>
    /// 生成查询延续（into 子句）的 IL 代码
    /// </summary>
    internal static void GenerateContinuationIL(this LinqExpression node, QueryContinuation continuation, ILGenerator ilGenerator, LocalManager local)
    {
        // 栈顶是前一阶段的结果。延续变量代表每个元素，而不是整个结果集合；
        // 用独立查询上下文复用逐行绑定逻辑。
        var continuationNode = new LinqExpression(
            new FromClause(continuation.Variable, node.FromClause.DataSource),
            continuation.BodyClauses, continuation.TerminationClause, continuation.Continuation);

        continuationNode.WithLinqScope(ilGenerator, local, () =>
        {
            foreach (var clause in continuation.BodyClauses)
                continuationNode.GenerateClauseIL(clause, ilGenerator, local);
            continuationNode.GenerateTerminationClauseIL(continuation.TerminationClause, ilGenerator, local);
            if (continuation.Continuation is not null)
                continuationNode.GenerateContinuationIL(continuation.Continuation, ilGenerator, local);
        });
    }

    /// <summary>
    /// 静态方法：将值转换为 IEnumerable（用于 Join 子句中的内部数据源）
    /// </summary>
    internal static void ConvertToEnumerableStatic(ILGenerator ilGenerator)
    {
        // 调用辅助方法进行转换
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("ConvertToEnumerable")!);
    }

    private static void EmitUnwrapQueryExpression(this LinqExpression node, LangExpression expression,
        ILGenerator il, LocalManager local)
    {
        var type = expression.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
        if (type.IsValueType) il.Emit(OpCodes.Box, type);
        il.Emit(OpCodes.Call, typeof(Old8Lang.InstanceMethods.Core.IlValueBridge)
            .GetMethod(nameof(Old8Lang.InstanceMethods.Core.IlValueBridge.UnwrapDynamic))!);
    }

    private static void EmitBindRow(this LinqExpression node, ILGenerator ilGenerator, LocalManager local,
        LocalBuilder row, LinqClause? stopBefore)
    {
        void Bind(string name, bool primary)
        {
            var slot = node.GetOrCreateRangeVariable(ilGenerator, local, name);
            ilGenerator.Emit(OpCodes.Ldloc, row);
            ilGenerator.Emit(OpCodes.Ldstr, name);
            ilGenerator.Emit(primary ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod(nameof(LinqCompilerHelper.GetRowBinding))!);
            ilGenerator.Emit(OpCodes.Stloc, slot);
        }

        Bind(node.FromClause.RangeVariable, true);
        foreach (var clause in node.BodyClauses)
        {
            if (ReferenceEquals(clause, stopBefore)) break;
            if (clause is JoinClause join)
                Bind(join.IsGroupJoin && join.GroupVariable != null ? join.GroupVariable : join.RangeVariable, false);
            else if (clause is LetClause let)
                Bind(let.Variable, false);
        }
    }

    /// <summary>
    /// 获取或创建范围变量的局部变量
    /// </summary>
    internal static LocalBuilder GetOrCreateRangeVariable(this LinqExpression node, ILGenerator ilGenerator, LocalManager local, string variableName)
    {
        // 检查是否已经存在
        var existingVar = local.GetLocalVar(variableName);
        if (existingVar != null)
        {
            return existingVar;
        }

        // 创建新的局部变量
        var newLocal = ilGenerator.DeclareLocal(typeof(LangValueType));
        local.LocalVar[variableName] = newLocal;
        return newLocal;
    }

    /// <summary>
    /// 将结果转换为 ListLangValue
    /// </summary>
    internal static void ConvertToListLangValue(this LinqExpression node, ILGenerator ilGenerator, LocalManager local)
    {
        // 栈顶是 List<object> 或 IEnumerable
        // 需要转换为 ListLangValue

        // 调用辅助方法
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("ConvertToListLangValue")!);
    }

    /// <summary>
    /// 生成将 object 转换为 LangValueType 的 IL 代码
    /// </summary>
    internal static void GenerateConvertToLangValue(this LinqExpression node, ILGenerator ilGenerator)
    {
        // 调用辅助方法
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("ConvertToLangValue")!);
    }

    /// <summary>
    /// 生成将 LangValueType 转换为 object 的 IL 代码
    /// </summary>
    internal static void GenerateConvertFromLangValue(this LinqExpression node, ILGenerator ilGenerator)
    {
        // 调用辅助方法
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("ConvertFromLangValue")!);
    }

    /// <summary>
    /// 生成判断 LangValueType 是否为真的 IL 代码
    /// </summary>
    internal static void GenerateIsTruthy(this LinqExpression node, ILGenerator ilGenerator, Type conditionType)
    {
        // 条件可能是 LangValueType（语言值比较）也可能是原生 bool（object 静态类型的比较），
        // 值类型必须先装箱；运行期助手统一接受 object?。
        if (conditionType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, conditionType);
        }

        ilGenerator.Emit(OpCodes.Call, typeof(LinqStreamingHelper).GetMethod(nameof(LinqStreamingHelper.IsTruthy))!);
    }
}

/// <summary>
/// LINQ 编译器辅助类
/// 提供在IL中难以直接实现的辅助方法
/// </summary>
public static class LinqCompilerHelper
{
    private static readonly object NullGroupKey = new();
    private sealed class QueryRow
    {
        public readonly Dictionary<string, LangValueType> Bindings = new(StringComparer.Ordinal);
    }

    private sealed record SortRow(object? Value, object?[] Keys, int Index);

    /// <summary>记录排序键，保持每行的原始范围变量。</summary>
    public static void AddSortRow(List<object> rows, object? value, object?[] keys)
    {
        for (var i = 0; i < keys.Length; i++)
            if (keys[i] is LangValueType langValue) keys[i] = ConvertFromLangValue(langValue);
        rows.Add(new SortRow(value, keys, rows.Count));
    }

    /// <summary>按全部排序键稳定排序，并返回原查询行。</summary>
    public static List<object> SortRows(List<object> rows, bool[] ascending)
    {
        var sorted = rows.Cast<SortRow>().ToList();
        sorted.Sort((left, right) =>
        {
            for (var i = 0; i < ascending.Length; i++)
            {
                var comparison = CompareObjects(left.Keys[i], right.Keys[i]);
                if (comparison != 0) return ascending[i] ? comparison : -comparison;
            }
            return left.Index.CompareTo(right.Index);
        });
        return sorted.Select(row => row.Value!).ToList();
    }

    /// <summary>复制查询行并添加一个连接范围变量。</summary>
    public static object AddRowBinding(object? source, string primaryName, string name, LangValueType value)
    {
        var row = new QueryRow();
        if (source is QueryRow existing)
        {
            foreach (var (key, item) in existing.Bindings) row.Bindings[key] = item;
        }
        else
        {
            row.Bindings[primaryName] = ConvertToLangValue(source);
        }
        row.Bindings[name] = value;
        return row;
    }

    /// <summary>获取当前查询行的范围变量，尚未连接的源值作为主范围变量。</summary>
    public static LangValueType GetRowBinding(object? source, string name, bool primary)
    {
        if (source is QueryRow row && row.Bindings.TryGetValue(name, out var value)) return value;
        return primary ? ConvertToLangValue(source) : new NullLangValue();
    }

    /// <summary>
    /// 将 object 转换为 LangValueType
    /// </summary>
    public static LangValueType ConvertToLangValue(object? obj)
    {
        return obj switch
        {
            null => new NullLangValue(),
            LangValueType langValue => langValue,
            int intVal => new IntLangValue(intVal),
            double doubleVal => new DoubleLangValue(doubleVal),
            string strVal => new StringLangValue(strVal),
            bool boolVal => new BoolLangValue(boolVal),
            char charVal => new CharLangValue(charVal),
            _ => Old8Lang.InstanceMethods.Core.IlValueBridge.Wrap(obj)
        };
    }

    /// <summary>
    /// 将 LangValueType 转换为 object
    /// </summary>
    public static object? ConvertFromLangValue(LangValueType? langValue)
    {
        return langValue switch
        {
            null => null,
            NullLangValue => null,
            IntLangValue intVal => intVal.Value,
            DoubleLangValue doubleVal => doubleVal.Value,
            StringLangValue strVal => strVal.Value,
            BoolLangValue boolVal => boolVal.Value,
            CharLangValue charVal => charVal.Value,
            _ => langValue
        };
    }

    /// <summary>
    /// 判断 LangValueType 是否为真
    /// </summary>
    public static bool IsTruthy(LangValueType? langValue)
    {
        return langValue switch
        {
            null => false,
            NullLangValue => false,
            BoolLangValue boolVal => boolVal.Value,
            IntLangValue intVal => intVal.Value != 0,
            DoubleLangValue doubleVal => doubleVal.Value != 0,
            StringLangValue strVal => !string.IsNullOrEmpty(strVal.Value),
            _ => true
        };
    }

    /// <summary>
    /// 对列表进行排序
    /// </summary>
    public static List<object> SortList(List<object> list, bool isAscending)
    {
        var result = new List<object>(list);
        result.Sort((a, b) =>
        {
            int comparison = CompareObjects(a, b);
            return isAscending ? comparison : -comparison;
        });
        return result;
    }

    /// <summary>
    /// 比较两个对象
    /// </summary>
    internal static int CompareObjects(object? a, object? b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        if (a is string leftString && b is string rightString)
            return string.CompareOrdinal(leftString, rightString);
        if (a is int or double && b is int or double)
            return Convert.ToDouble(a).CompareTo(Convert.ToDouble(b));

        // 尝试使用 IComparable
        if (a is IComparable comparableA)
        {
            try
            {
                return comparableA.CompareTo(b);
            }
            catch
            {
                // 如果类型不兼容，使用字符串比较
            }
        }

        // 回退到字符串比较
        return string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// 添加元素到分组字典
    /// </summary>
    public static void AddToGroup(Dictionary<object, List<object>> groupDict, object? key, object element)
    {
        var actualKey = key ?? NullGroupKey;
        if (!groupDict.ContainsKey(actualKey))
        {
            groupDict[actualKey] = new List<object>();
        }
        groupDict[actualKey].Add(element);
    }

    /// <summary>
    /// 将分组字典转换为列表
    /// </summary>
    public static List<object> GroupDictToList(Dictionary<object, List<object>> groupDict)
    {
        var result = new List<object>();
        foreach (var kvp in groupDict)
        {
            var group = new Dictionary<string, object?>
            {
                ["Key"] = ReferenceEquals(kvp.Key, NullGroupKey) ? null : kvp.Key,
                ["Values"] = kvp.Value
            };
            result.Add(group);
        }
        return result;
    }

    /// <summary>
    /// 将结果转换为 ListLangValue
    /// </summary>
    public static ListLangValue ConvertToListLangValue(object source)
    {
        var langList = new List<LangValueType>();

        if (source is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                langList.Add(ConvertToLangValue(item));
            }
        }

        return new ListLangValue(langList);
    }

    /// <summary>
    /// 比较两个键是否相等
    /// </summary>
    public static bool KeysEqual(object? key1, object? key2)
    {
        if (key1 is null && key2 is null)
            return true;
        if (key1 is null || key2 is null)
            return false;

        // 处理 LangValueType 的比较
        if (key1 is LangValueType lv1 && key2 is LangValueType lv2)
            return lv1.Equal(lv2);

        // 处理基本类型的比较
        return key1.Equals(key2);
    }

    /// <summary>
    /// 将 LangValueType 转换为 IEnumerable
    /// </summary>
    public static IEnumerable ConvertToEnumerable(object? value)
    {
        if (value is ILangList langList)
        {
            return langList.GetItems();
        }

        if (value is ArrayLangValue arrayValue)
        {
            return arrayValue.Values;
        }

        if (value is IEnumerable enumerable)
        {
            return enumerable;
        }

        throw new InvalidOperationException($"无法将类型 {value?.GetType().Name ?? "null"} 转换为 IEnumerable");
    }
}
