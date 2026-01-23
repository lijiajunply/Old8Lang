using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;
using System.Collections;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.AST.Expression.Linq;

/// <summary>
/// LinqExpression 的编译器支持部分
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
        var currentLocal = ilGenerator.DeclareLocal(typeof(object));
        ilGenerator.Emit(OpCodes.Stloc, currentLocal);

        // 设置范围变量
        // local.Interpreter.Manager.Set(new LangId(rangeVariable), ConvertToLangValue(current))
        LoadVariateManager(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Ldstr, FromClause.RangeVariable);
        ilGenerator.Emit(OpCodes.Newobj, typeof(LangId).GetConstructor([typeof(string)])!);

        ilGenerator.Emit(OpCodes.Ldloc, currentLocal);
        GenerateConvertToLangValue(ilGenerator);

        ilGenerator.Emit(OpCodes.Callvirt, typeof(VariateManager).GetMethod("Set", [typeof(LangId), typeof(LangValueType)])!);

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
        var currentLocal = ilGenerator.DeclareLocal(typeof(object));
        ilGenerator.Emit(OpCodes.Stloc, currentLocal);

        // 设置范围变量
        LoadVariateManager(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Ldstr, FromClause.RangeVariable);
        ilGenerator.Emit(OpCodes.Newobj, typeof(LangId).GetConstructor([typeof(string)])!);

        ilGenerator.Emit(OpCodes.Ldloc, currentLocal);
        GenerateConvertToLangValue(ilGenerator);

        ilGenerator.Emit(OpCodes.Callvirt, typeof(VariateManager).GetMethod("Set", [typeof(LangId), typeof(LangValueType)])!);

        // 计算投影表达式
        selectClause.Projection.LoadIlValue(ilGenerator, local);

        // 保存投影结果
        var projectionLocal = ilGenerator.DeclareLocal(typeof(LangValueType));
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
        // OrderBy 实现较复杂，暂时使用简化版本
        throw new NotSupportedException("OrderBy 子句在编译器模式下暂不支持");
    }

    /// <summary>
    /// 生成 Let 子句的 IL 代码
    /// </summary>
    private void GenerateLetClauseIL(LetClause letClause, ILGenerator ilGenerator, LocalManager local)
    {
        // Let 子句需要在作用域中设置新变量
        throw new NotSupportedException("Let 子句在编译器模式下暂不支持");
    }

    /// <summary>
    /// 生成 GroupBy 子句的 IL 代码
    /// </summary>
    private void GenerateGroupByClauseIL(GroupByClause groupByClause, ILGenerator ilGenerator, LocalManager local)
    {
        // GroupBy 实现较复杂
        throw new NotSupportedException("GroupBy 子句在编译器模式下暂不支持");
    }

    /// <summary>
    /// 将结果转换为 ListLangValue
    /// </summary>
    private void ConvertToListLangValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 栈顶是 List<object>
        // 需要转换为 ListLangValue

        // 创建 List<LangValueType>
        var langListLocal = ilGenerator.DeclareLocal(typeof(List<LangValueType>));
        ilGenerator.Emit(OpCodes.Newobj, typeof(List<LangValueType>).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Stloc, langListLocal);

        // 保存原始列表
        var objectListLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Stloc, objectListLocal);

        // 获取枚举器
        var enumeratorLocal = ilGenerator.DeclareLocal(typeof(IEnumerator));
        ilGenerator.Emit(OpCodes.Ldloc, objectListLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerable).GetMethod("GetEnumerator")!);
        ilGenerator.Emit(OpCodes.Stloc, enumeratorLocal);

        // 循环标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();

        // 开始循环
        ilGenerator.MarkLabel(loopStart);
        ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetMethod("MoveNext")!);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd);

        // 获取当前元素并转换为 LangValueType
        ilGenerator.Emit(OpCodes.Ldloc, langListLocal);
        ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetProperty("Current")!.GetMethod!);
        GenerateConvertToLangValue(ilGenerator);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(List<LangValueType>).GetMethod("Add")!);

        // 继续循环
        ilGenerator.Emit(OpCodes.Br, loopStart);

        // 循环结束
        ilGenerator.MarkLabel(loopEnd);

        // 创建 ListLangValue
        ilGenerator.Emit(OpCodes.Ldloc, langListLocal);
        ilGenerator.Emit(OpCodes.Newobj, typeof(ListLangValue).GetConstructor([typeof(List<LangValueType>)])!);
    }

    /// <summary>
    /// 生成将 object 转换为 LangValueType 的 IL 代码
    /// </summary>
    private void GenerateConvertToLangValue(ILGenerator ilGenerator)
    {
        // 栈顶是 object
        // 需要根据类型转换为对应的 LangValueType

        var objLocal = ilGenerator.DeclareLocal(typeof(object));
        ilGenerator.Emit(OpCodes.Stloc, objLocal);

        // 检查是否为 null
        var notNullLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Ldloc, objLocal);
        ilGenerator.Emit(OpCodes.Brtrue, notNullLabel);
        
        // 如果是 null，返回 NullLangValue
        ilGenerator.Emit(OpCodes.Newobj, typeof(NullLangValue).GetConstructor(Type.EmptyTypes)!);
        var endLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Br, endLabel);

        ilGenerator.MarkLabel(notNullLabel);

        // 检查是否已经是 LangValueType
        var notLangValueLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Ldloc, objLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(LangValueType));
        ilGenerator.Emit(OpCodes.Brfalse, notLangValueLabel);
        ilGenerator.Emit(OpCodes.Ldloc, objLocal);
        ilGenerator.Emit(OpCodes.Castclass, typeof(LangValueType));
        ilGenerator.Emit(OpCodes.Br, endLabel);

        ilGenerator.MarkLabel(notLangValueLabel);

        // 检查是否是 int
        var notIntLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Ldloc, objLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(int));
        ilGenerator.Emit(OpCodes.Brfalse, notIntLabel);
        ilGenerator.Emit(OpCodes.Ldloc, objLocal);
        ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
        ilGenerator.Emit(OpCodes.Newobj, typeof(IntLangValue).GetConstructor([typeof(int)])!);
        ilGenerator.Emit(OpCodes.Br, endLabel);

        ilGenerator.MarkLabel(notIntLabel);

        // 检查是否是 double
        var notDoubleLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Ldloc, objLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(double));
        ilGenerator.Emit(OpCodes.Brfalse, notDoubleLabel);
        ilGenerator.Emit(OpCodes.Ldloc, objLocal);
        ilGenerator.Emit(OpCodes.Unbox_Any, typeof(double));
        ilGenerator.Emit(OpCodes.Newobj, typeof(DoubleLangValue).GetConstructor([typeof(double)])!);
        ilGenerator.Emit(OpCodes.Br, endLabel);

        ilGenerator.MarkLabel(notDoubleLabel);

        // 检查是否是 string
        var notStringLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Ldloc, objLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(string));
        ilGenerator.Emit(OpCodes.Brfalse, notStringLabel);
        ilGenerator.Emit(OpCodes.Ldloc, objLocal);
        ilGenerator.Emit(OpCodes.Castclass, typeof(string));
        ilGenerator.Emit(OpCodes.Newobj, typeof(StringLangValue).GetConstructor([typeof(string)])!);
        ilGenerator.Emit(OpCodes.Br, endLabel);

        ilGenerator.MarkLabel(notStringLabel);

        // 检查是否是 bool
        var notBoolLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Ldloc, objLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(bool));
        ilGenerator.Emit(OpCodes.Brfalse, notBoolLabel);
        ilGenerator.Emit(OpCodes.Ldloc, objLocal);
        ilGenerator.Emit(OpCodes.Unbox_Any, typeof(bool));
        ilGenerator.Emit(OpCodes.Newobj, typeof(BoolLangValue).GetConstructor([typeof(bool)])!);
        ilGenerator.Emit(OpCodes.Br, endLabel);

        ilGenerator.MarkLabel(notBoolLabel);

        // 默认返回 IntLangValue(0)
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Newobj, typeof(IntLangValue).GetConstructor([typeof(int)])!);

        ilGenerator.MarkLabel(endLabel);
    }

    /// <summary>
    /// 生成将 LangValueType 转换为 object 的 IL 代码
    /// </summary>
    private void GenerateConvertFromLangValue(ILGenerator ilGenerator)
    {
        // 栈顶是 LangValueType
        // 需要根据类型转换为对应的 object

        var langValueLocal = ilGenerator.DeclareLocal(typeof(LangValueType));
        ilGenerator.Emit(OpCodes.Stloc, langValueLocal);

        // 检查是否是 IntLangValue
        var notIntLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Ldloc, langValueLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(IntLangValue));
        ilGenerator.Emit(OpCodes.Brfalse, notIntLabel);
        ilGenerator.Emit(OpCodes.Ldloc, langValueLocal);
        ilGenerator.Emit(OpCodes.Castclass, typeof(IntLangValue));
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IntLangValue).GetProperty("Value")!.GetMethod!);
        ilGenerator.Emit(OpCodes.Box, typeof(int));
        var endLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Br, endLabel);

        ilGenerator.MarkLabel(notIntLabel);

        // 检查是否是 DoubleLangValue
        var notDoubleLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Ldloc, langValueLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(DoubleLangValue));
        ilGenerator.Emit(OpCodes.Brfalse, notDoubleLabel);
        ilGenerator.Emit(OpCodes.Ldloc, langValueLocal);
        ilGenerator.Emit(OpCodes.Castclass, typeof(DoubleLangValue));
        ilGenerator.Emit(OpCodes.Callvirt, typeof(DoubleLangValue).GetProperty("Value")!.GetMethod!);
        ilGenerator.Emit(OpCodes.Box, typeof(double));
        ilGenerator.Emit(OpCodes.Br, endLabel);

        ilGenerator.MarkLabel(notDoubleLabel);

        // 检查是否是 StringLangValue
        var notStringLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Ldloc, langValueLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(StringLangValue));
        ilGenerator.Emit(OpCodes.Brfalse, notStringLabel);
        ilGenerator.Emit(OpCodes.Ldloc, langValueLocal);
        ilGenerator.Emit(OpCodes.Castclass, typeof(StringLangValue));
        ilGenerator.Emit(OpCodes.Callvirt, typeof(StringLangValue).GetProperty("Value")!.GetMethod!);
        ilGenerator.Emit(OpCodes.Br, endLabel);

        ilGenerator.MarkLabel(notStringLabel);

        // 检查是否是 BoolLangValue
        var notBoolLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Ldloc, langValueLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(BoolLangValue));
        ilGenerator.Emit(OpCodes.Brfalse, notBoolLabel);
        ilGenerator.Emit(OpCodes.Ldloc, langValueLocal);
        ilGenerator.Emit(OpCodes.Castclass, typeof(BoolLangValue));
        ilGenerator.Emit(OpCodes.Callvirt, typeof(BoolLangValue).GetProperty("Value")!.GetMethod!);
        ilGenerator.Emit(OpCodes.Box, typeof(bool));
        ilGenerator.Emit(OpCodes.Br, endLabel);

        ilGenerator.MarkLabel(notBoolLabel);

        // 检查是否是 NullLangValue
        var notNullLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Ldloc, langValueLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(NullLangValue));
        ilGenerator.Emit(OpCodes.Brfalse, notNullLabel);
        ilGenerator.Emit(OpCodes.Ldnull);
        ilGenerator.Emit(OpCodes.Br, endLabel);

        ilGenerator.MarkLabel(notNullLabel);

        // 默认返回 LangValueType 本身
        ilGenerator.Emit(OpCodes.Ldloc, langValueLocal);

        ilGenerator.MarkLabel(endLabel);
    }

    /// <summary>
    /// 生成判断 LangValueType 是否为真的 IL 代码
    /// </summary>
    private void GenerateIsTruthy(ILGenerator ilGenerator)
    {
        // 栈顶是 LangValueType
        // 需要判断是否为真值

        var langValueLocal = ilGenerator.DeclareLocal(typeof(LangValueType));
        ilGenerator.Emit(OpCodes.Stloc, langValueLocal);

        // 检查是否是 BoolLangValue
        var notBoolLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Ldloc, langValueLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(BoolLangValue));
        ilGenerator.Emit(OpCodes.Brfalse, notBoolLabel);
        ilGenerator.Emit(OpCodes.Ldloc, langValueLocal);
        ilGenerator.Emit(OpCodes.Castclass, typeof(BoolLangValue));
        ilGenerator.Emit(OpCodes.Callvirt, typeof(BoolLangValue).GetProperty("Value")!.GetMethod!);
        var endLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Br, endLabel);

        ilGenerator.MarkLabel(notBoolLabel);

        // 检查是否是 NullLangValue
        var notNullLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Ldloc, langValueLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(NullLangValue));
        ilGenerator.Emit(OpCodes.Brfalse, notNullLabel);
        ilGenerator.Emit(OpCodes.Ldc_I4_0); // false
        ilGenerator.Emit(OpCodes.Br, endLabel);

        ilGenerator.MarkLabel(notNullLabel);

        // 检查是否是 IntLangValue
        var notIntLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Ldloc, langValueLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(IntLangValue));
        ilGenerator.Emit(OpCodes.Brfalse, notIntLabel);
        ilGenerator.Emit(OpCodes.Ldloc, langValueLocal);
        ilGenerator.Emit(OpCodes.Castclass, typeof(IntLangValue));
        ilGenerator.Emit(OpCodes.Callvirt, typeof(IntLangValue).GetProperty("Value")!.GetMethod!);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Ceq);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Ceq); // != 0
        ilGenerator.Emit(OpCodes.Br, endLabel);

        ilGenerator.MarkLabel(notIntLabel);

        // 默认返回 true
        ilGenerator.Emit(OpCodes.Ldc_I4_1);

        ilGenerator.MarkLabel(endLabel);
    }

    /// <summary>
    /// 加载 VariateManager 到栈上
    /// </summary>
    private void LoadVariateManager(ILGenerator ilGenerator, LocalManager local)
    {
        // 在编译器模式下，由于生成的方法是无参数的 Action，
        // 我们无法直接访问 VariateManager 对象。
        // 完整的 LINQ 编译器支持需要重构编译器架构，将 VariateManager 作为参数传递。

        // 当前方案：抛出友好的异常，引导用户使用解释器模式
        throw new NotSupportedException(
            "LINQ 查询在编译器模式下暂不支持。\n" +
            "原因：编译器模式下无法访问运行时的 VariateManager 对象。\n" +
            "解决方案：请使用解释器模式 (-f) 运行包含 LINQ 的代码。\n" +
            "示例：dotnet run --project Old8Lang.App -- -f your_file.old8");
    }
}
