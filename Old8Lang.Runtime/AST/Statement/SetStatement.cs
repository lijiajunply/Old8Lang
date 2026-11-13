using Old8Lang.LangParser;
using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.OperationHelpers;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 赋值语句类，用于处理Old8Lang中的赋值操作
/// 支持普通变量赋值、成员访问赋值和索引访问赋值
/// </summary>
public partial class SetStatement : OldStatement
{
    /// <summary>
    /// 变量标识符（用于普通变量赋值）
    /// </summary>
    public readonly LangId? Id;

    /// <summary>
    /// 左侧表达式（用于成员访问或索引访问赋值）
    /// </summary>
    internal readonly LangExpression? _leftExpression;

    /// <summary>
    /// 获取左侧表达式（用于成员访问或索引访问赋值）
    /// </summary>
    public LangExpression? LeftExpression => _leftExpression;

    /// <summary>
    /// 赋值表达式
    /// </summary>
    public readonly LangExpression Value;

    /// <summary>
    /// 构造函数：创建普通变量赋值语句
    /// </summary>
    /// <param name="id">变量标识符</param>
    /// <param name="value">赋值表达式</param>
    /// <param name="position">源代码位置信息，用于错误报告</param>
    public SetStatement(LangId id, LangExpression value, SourcePosition position = default) : base(position)
    {
        Id = id;
        _leftExpression = null;
        Value = value;
    }

    /// <summary>
    /// 构造函数：创建成员访问或索引访问赋值语句
    /// </summary>
    /// <param name="leftExpression">左侧表达式（成员访问或索引访问）</param>
    /// <param name="value">赋值表达式</param>
    /// <param name="position">源代码位置信息，用于错误报告</param>
    public SetStatement(LangExpression leftExpression, LangExpression value, SourcePosition position = default) :
        base(position)
    {
        Id = null;
        _leftExpression = leftExpression;
        Value = value;
    }

    /// <summary>
    /// 如果赋值目标是运行期全局槽位，生成槽位写入并返回 true。
    /// </summary>
    /// <remarks>
    /// 顶层管理器只对被提升的变量写槽位；函数体管理器对「已经是槽位」或
    /// 「在顶层声明过但尚未提升」的变量写槽位，后者由分析阶段兜底提升。
    /// 参数、局部变量、闭包单元和字段始终优先，不能被全局槽位遮蔽。
    /// </remarks>
    internal bool TryEmitGlobalStore(ILGenerator ilGenerator, LocalManager local)
    {
        if (Id is null || string.IsNullOrEmpty(Id.IdName))
        {
            return false;
        }

        var name = Id.IdName;
        var globals = local.Globals;
        if (globals is null)
        {
            return false;
        }

        if (local.LocalVar.ContainsKey(name) ||
            local.ArgumentIndices.ContainsKey(name) ||
            local.CapturedLocals.ContainsKey(name) ||
            local.FieldVar.ContainsKey(name))
        {
            return false;
        }

        if (!globals.IsSlot(name))
        {
            // 函数体给顶层变量赋值：分析阶段通常已经提升过；这里兜底处理遗漏。
            if (local.IsTopLevelScope || !globals.IsGlobal(name))
            {
                return false;
            }

            globals.Promote(name);
        }

        // 先求值：部分节点（如一元运算）只有在 LoadIlValue 之后才会缓存真实类型。
        Type valueType;

        // 空花括号在语法层默认是列表；显式 dict 注解仍需生成真正的空字典。
        if (Value is ListLangValue { Value.Count: 0, Values.Count: 0 } &&
            Id.AssumptionType is not null &&
            (Id.AssumptionType.Equals("dict", StringComparison.OrdinalIgnoreCase) ||
             Id.AssumptionType.Equals("dictionary", StringComparison.OrdinalIgnoreCase)))
        {
            new DictionaryLangValue(position: Value.Position).Accept(new CompilerVisitor(ilGenerator, local));
            valueType = typeof(Dictionary<object, object>);
        }
        else
        {
            Value.Accept(new CompilerVisitor(ilGenerator, local));
            valueType = ComputeGlobalValueType(local);
        }

        var slotType = globals.GetType(name);
        if (slotType == typeof(object) || slotType == typeof(void))
        {
            slotType = valueType;
            globals.Types[name] = slotType;
        }

        local.LocalVarTypes[name] = slotType;
        RuntimeGlobals.EmitStore(ilGenerator, name, slotType, valueType);
        return true;
    }

    /// <summary>
    /// 计算全局槽位赋值右侧表达式的静态类型。
    /// </summary>
    internal static bool IsNumericType(Type type) => type == typeof(int) || type == typeof(double) ||
                                                     type == typeof(float) || type == typeof(long) ||
                                                     type == typeof(short) || type == typeof(byte);

    internal static void EmitNumericConversion(ILGenerator ilGenerator, Type targetType)
    {
        ilGenerator.Emit(targetType == typeof(double) ? OpCodes.Conv_R8 :
            targetType == typeof(float) ? OpCodes.Conv_R4 :
            targetType == typeof(long) ? OpCodes.Conv_I8 :
            targetType == typeof(short) ? OpCodes.Conv_I2 :
            targetType == typeof(byte) ? OpCodes.Conv_U1 : OpCodes.Conv_I4);
    }

    internal Type ComputeGlobalValueType(LocalManager local)
    {
        if (Id is not null && !string.IsNullOrEmpty(Id.AssumptionType))
        {
            var annotatedType = Id.Accept(new TypeInferenceVisitor(local));
            if (annotatedType is not null && annotatedType != typeof(object) && annotatedType != typeof(void))
            {
                return annotatedType;
            }
        }

        var valueType = Value.Accept(new TypeInferenceVisitor(local));
        return valueType is null || valueType == typeof(void) ? typeof(object) : valueType;
    }

    /// <summary>
    /// 获取指定索引处的语句（实现OldStatement接口）
    /// </summary>
    /// <param name="index">语句索引</param>
    /// <returns>返回当前语句本身，因为SetStatement是单个语句</returns>
    public override OldStatement this[int index] => this;

    /// <summary>
    /// 获取语句数量（实现OldStatement接口）
    /// </summary>
    /// <returns>返回0，因为SetStatement是单个语句</returns>
    public override int Count => 0;

    /// <summary>
    /// 将赋值语句转换为字符串表示
    /// </summary>
    /// <returns>赋值语句的字符串表示</returns>
    public override string ToString()
    {
        if (_leftExpression is not null)
        {
            return $"{_leftExpression} <- {Value}";
        }

        // 如果 Id 为空或 IdName 为空，只显示右值（用于块表达式的返回值）
        if (Id is null || string.IsNullOrEmpty(Id.IdName))
        {
            return $" <- {Value}";
        }

        return $"{Id} <- {Value}";
    }
}
