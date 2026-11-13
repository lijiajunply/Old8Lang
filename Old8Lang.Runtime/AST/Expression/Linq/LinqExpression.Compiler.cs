using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.Intermediates;
using System.Collections;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.AST.Expression.Linq;

/// <summary>
/// LinqExpression 的编译器支持部分
/// 使用纯IL实现，不依赖VariateManager
/// </summary>
public partial class LinqExpression
{
    /// <summary>
    /// 生成 Where 子句的 IL 代码
    /// </summary>
    private void GenerateWhereClauseIL(WhereClause whereClause, ILGenerator ilGenerator, LocalManager local)
    {
        // 栈顶是 IEnumerable
        // 我们需要生成类似于 source.Where(x => condition) 的代码

        // 创建一个 List 来存储过滤后的结果
        var resultLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Stloc, resultLocal);

        // 获取枚举器
        var enumeratorLocal = ilGenerator.DeclareLocal(typeof(IEnumerator));
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerable).GetMethod("GetEnumerator")!);
        ilGenerator.Emit(OpCodes.Stloc, enumeratorLocal);

        // 预先声明循环内使用的局部变量
        var currentLocal = ilGenerator.DeclareLocal(typeof(object));
        var rangeVarLocal = GetOrCreateRangeVariable(ilGenerator, local, FromClause.RangeVariable);

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
        ilGenerator.Emit(OpCodes.Ldloc, currentLocal);
        GenerateConvertToLangValue(ilGenerator);
        ilGenerator.Emit(OpCodes.Stloc, rangeVarLocal);

        // 计算条件表达式
        whereClause.Condition.LoadIlValue(ilGenerator, local);

        // 检查条件是否为真
        GenerateIsTruthy(ilGenerator);

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
    private void GenerateSelectClauseIL(SelectClause selectClause, ILGenerator ilGenerator, LocalManager local)
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
        var rangeVarLocal = GetOrCreateRangeVariable(ilGenerator, local, FromClause.RangeVariable);

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
        ilGenerator.Emit(OpCodes.Ldloc, currentLocal);
        GenerateConvertToLangValue(ilGenerator);
        ilGenerator.Emit(OpCodes.Stloc, rangeVarLocal);

        // 计算投影表达式
        selectClause.Projection.LoadIlValue(ilGenerator, local);

        // 保存投影结果
        ilGenerator.Emit(OpCodes.Stloc, projectionLocal);

        // 添加到结果列表
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
        ilGenerator.Emit(OpCodes.Ldloc, projectionLocal);
        GenerateConvertFromLangValue(ilGenerator);
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
    private void GenerateOrderByClauseIL(OrderByClause orderByClause, ILGenerator ilGenerator, LocalManager local)
    {
        // 栈顶是 IEnumerable
        // 首先将其转换为 List<object>
        var sourceLocal = ilGenerator.DeclareLocal(typeof(List<object>));

        // 创建新列表
        ilGenerator.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Stloc, sourceLocal);

        // 获取枚举器
        var enumeratorLocal = ilGenerator.DeclareLocal(typeof(IEnumerator));
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerable).GetMethod("GetEnumerator")!);
        ilGenerator.Emit(OpCodes.Stloc, enumeratorLocal);

        // 复制所有元素到列表
        var copyLoopStart = ilGenerator.DefineLabel();
        var copyLoopEnd = ilGenerator.DefineLabel();

        ilGenerator.MarkLabel(copyLoopStart);
        ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetMethod("MoveNext")!);
        ilGenerator.Emit(OpCodes.Brfalse, copyLoopEnd);

        ilGenerator.Emit(OpCodes.Ldloc, sourceLocal);
        ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetProperty("Current")!.GetMethod!);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("Add")!);

        ilGenerator.Emit(OpCodes.Br, copyLoopStart);
        ilGenerator.MarkLabel(copyLoopEnd);

        // 调用排序辅助方法
        // 由于IL中实现复杂的排序逻辑很困难，我们调用一个静态辅助方法
        ilGenerator.Emit(OpCodes.Ldloc, sourceLocal);

        // 加载排序方向（第一个排序键）
        var firstOrdering = orderByClause.Orderings.FirstOrDefault();
        ilGenerator.Emit(firstOrdering?.IsAscending ?? true ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);

        // 调用 LinqCompilerHelper.SortList
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("SortList")!);
    }

    /// <summary>
    /// 生成 Let 子句的 IL 代码
    /// </summary>
    private void GenerateLetClauseIL(LetClause letClause, ILGenerator ilGenerator, LocalManager local)
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
        var letVarLocal = GetOrCreateRangeVariable(ilGenerator, local, letClause.Variable);
        var rangeVarLocal = GetOrCreateRangeVariable(ilGenerator, local, FromClause.RangeVariable);

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
        ilGenerator.Emit(OpCodes.Ldloc, currentLocal);
        GenerateConvertToLangValue(ilGenerator);
        ilGenerator.Emit(OpCodes.Stloc, rangeVarLocal);

        // 计算let表达式
        letClause.Expression.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Stloc, letVarLocal);

        // 添加当前元素到结果列表
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
        ilGenerator.Emit(OpCodes.Ldloc, currentLocal);
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
    private void GenerateGroupByClauseIL(GroupByClause groupByClause, ILGenerator ilGenerator, LocalManager local)
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
        var rangeVarLocal = GetOrCreateRangeVariable(ilGenerator, local, FromClause.RangeVariable);

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
        ilGenerator.Emit(OpCodes.Ldloc, currentLocal);
        GenerateConvertToLangValue(ilGenerator);
        ilGenerator.Emit(OpCodes.Stloc, rangeVarLocal);

        // 计算分组键
        groupByClause.KeyExpression.LoadIlValue(ilGenerator, local);
        GenerateConvertFromLangValue(ilGenerator);
        ilGenerator.Emit(OpCodes.Stloc, keyLocal);

        // 计算分组元素
        groupByClause.ElementExpression.LoadIlValue(ilGenerator, local);
        GenerateConvertFromLangValue(ilGenerator);
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
    private void GenerateJoinClauseIL(JoinClause joinClause, ILGenerator ilGenerator, LocalManager local)
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

        // 加载内部数据源
        joinClause.InnerDataSource.LoadIlValue(ilGenerator, local);
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

        // 创建结果列表
        var resultLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Stloc, resultLocal);

        // 获取或创建范围变量
        var outerRangeVarLocal = GetOrCreateRangeVariable(ilGenerator, local, FromClause.RangeVariable);
        var innerRangeVarLocal = GetOrCreateRangeVariable(ilGenerator, local, joinClause.RangeVariable);

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
        ilGenerator.Emit(OpCodes.Ldloc, outerItemLocal);
        GenerateConvertToLangValue(ilGenerator);
        ilGenerator.Emit(OpCodes.Stloc, outerRangeVarLocal);

        // 计算外部键
        joinClause.OuterKeyExpression.LoadIlValue(ilGenerator, local);
        GenerateConvertFromLangValue(ilGenerator);
        ilGenerator.Emit(OpCodes.Stloc, outerKeyLocal);

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
            GenerateConvertToLangValue(ilGenerator);
            ilGenerator.Emit(OpCodes.Stloc, innerRangeVarLocal);

            // 计算内部键
            joinClause.InnerKeyExpression.LoadIlValue(ilGenerator, local);
            GenerateConvertFromLangValue(ilGenerator);
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
            var groupVarLocal = GetOrCreateRangeVariable(ilGenerator, local, joinClause.GroupVariable);
            ilGenerator.Emit(OpCodes.Ldloc, matchingItemsLocal);
            ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("ConvertToListLangValue")!);
            ilGenerator.Emit(OpCodes.Stloc, groupVarLocal);

            // 添加外部元素到结果（分组变量已设置）
            ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
            ilGenerator.Emit(OpCodes.Ldloc, outerItemLocal);
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
            GenerateConvertToLangValue(ilGenerator);
            ilGenerator.Emit(OpCodes.Stloc, innerRangeVarLocal);

            // 计算内部键
            joinClause.InnerKeyExpression.LoadIlValue(ilGenerator, local);
            GenerateConvertFromLangValue(ilGenerator);
            ilGenerator.Emit(OpCodes.Stloc, innerKeyLocal);

            // 比较键
            ilGenerator.Emit(OpCodes.Ldloc, outerKeyLocal);
            ilGenerator.Emit(OpCodes.Ldloc, innerKeyLocal);
            ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("KeysEqual")!);
            var skipAddLabel = ilGenerator.DefineLabel();
            ilGenerator.Emit(OpCodes.Brfalse, skipAddLabel);

            // 键匹配，添加外部元素到结果
            // 注意：内部范围变量已设置，后续子句可以访问
            ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
            ilGenerator.Emit(OpCodes.Ldloc, outerItemLocal);
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
    private void GenerateContinuationIL(QueryContinuation continuation, ILGenerator ilGenerator, LocalManager local)
    {
        // 栈顶是前一个查询的结果（List<object>）
        // 将结果转换为 ListLangValue 并存储到延续变量
        var continuationVarLocal = GetOrCreateRangeVariable(ilGenerator, local, continuation.Variable);

        // 复制栈顶的结果
        ilGenerator.Emit(OpCodes.Dup);

        // 转换为 ListLangValue 并存储到延续变量
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("ConvertToListLangValue")!);
        ilGenerator.Emit(OpCodes.Stloc, continuationVarLocal);

        // 处理延续后的查询体子句
        foreach (var clause in continuation.BodyClauses)
        {
            GenerateClauseIL(clause, ilGenerator, local);
        }

        // 处理延续后的终止子句
        GenerateTerminationClauseIL(continuation.TerminationClause, ilGenerator, local);
    }

    /// <summary>
    /// 静态方法：将值转换为 IEnumerable（用于 Join 子句中的内部数据源）
    /// </summary>
    private static void ConvertToEnumerableStatic(ILGenerator ilGenerator)
    {
        // 调用辅助方法进行转换
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("ConvertToEnumerable")!);
    }

    /// <summary>
    /// 获取或创建范围变量的局部变量
    /// </summary>
    private LocalBuilder GetOrCreateRangeVariable(ILGenerator ilGenerator, LocalManager local, string variableName)
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
    private void ConvertToListLangValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 栈顶是 List<object> 或 IEnumerable
        // 需要转换为 ListLangValue

        // 调用辅助方法
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("ConvertToListLangValue")!);
    }

    /// <summary>
    /// 生成将 object 转换为 LangValueType 的 IL 代码
    /// </summary>
    private void GenerateConvertToLangValue(ILGenerator ilGenerator)
    {
        // 调用辅助方法
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("ConvertToLangValue")!);
    }

    /// <summary>
    /// 生成将 LangValueType 转换为 object 的 IL 代码
    /// </summary>
    private void GenerateConvertFromLangValue(ILGenerator ilGenerator)
    {
        // 调用辅助方法
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("ConvertFromLangValue")!);
    }

    /// <summary>
    /// 生成判断 LangValueType 是否为真的 IL 代码
    /// </summary>
    private void GenerateIsTruthy(ILGenerator ilGenerator)
    {
        // 调用辅助方法
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("IsTruthy")!);
    }
}

/// <summary>
/// LINQ 编译器辅助类
/// 提供在IL中难以直接实现的辅助方法
/// </summary>
public static class LinqCompilerHelper
{
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
            _ => new IntLangValue(0) // 默认值
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
    private static int CompareObjects(object? a, object? b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

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
    public static void AddToGroup(Dictionary<object, List<object>> groupDict, object key, object element)
    {
        if (!groupDict.ContainsKey(key))
        {
            groupDict[key] = new List<object>();
        }
        groupDict[key].Add(element);
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
                ["Key"] = kvp.Key,
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
    public static IEnumerable ConvertToEnumerable(LangValueType value)
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

        throw new InvalidOperationException($"无法将类型 {value.GetType().Name} 转换为 IEnumerable");
    }
}
