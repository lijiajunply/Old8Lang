using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.AST.Statement;

/// <summary>
/// SetStatement 的编译期辅助实现（阶段 2 从语法节点迁出）。
/// </summary>
internal static class SetStatementRuntimeExtensions
{
    /// <summary>
    /// 计算全局槽位赋值右侧表达式的静态类型。
    /// </summary>
    internal static Type ComputeGlobalValueType(this SetStatement node, LocalManager local)
    {
        if (node.Id is not null && !string.IsNullOrEmpty(node.Id.AssumptionType))
        {
            var annotatedType = node.Id.Accept(new TypeInferenceVisitor(local));
            if (annotatedType is not null && annotatedType != typeof(object) && annotatedType != typeof(void))
            {
                return annotatedType;
            }
        }

        var valueType = node.Value.Accept(new TypeInferenceVisitor(local));
        return valueType is null || valueType == typeof(void) ? typeof(object) : valueType;
    }

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

    /// <summary>
    /// 如果赋值目标是运行期全局槽位，生成槽位写入并返回 true。
    /// </summary>
    /// <remarks>
    /// 顶层管理器只对被提升的变量写槽位；函数体管理器对「已经是槽位」或
    /// 「在顶层声明过但尚未提升」的变量写槽位，后者由分析阶段兜底提升。
    /// 参数、局部变量、闭包单元和字段始终优先，不能被全局槽位遮蔽。
    /// </remarks>
    internal static bool TryEmitGlobalStore(this SetStatement node, ILGenerator ilGenerator, LocalManager local)
    {
        if (node.Id is null || string.IsNullOrEmpty(node.Id.IdName))
        {
            return false;
        }

        var name = node.Id.IdName;
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
        if ((node.Value is ListLiteralExpression { Elements.Count: 0 } or ListLangValue { Value.Count: 0, Values.Count: 0 }) &&
            node.Id.AssumptionType is not null &&
            (node.Id.AssumptionType.Equals("dict", StringComparison.OrdinalIgnoreCase) ||
             node.Id.AssumptionType.Equals("dictionary", StringComparison.OrdinalIgnoreCase)))
        {
            new DictionaryLangValue(position: node.Value.Position).Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            valueType = typeof(Dictionary<object, object>);
        }
        else
        {
            node.Value.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            valueType = node.ComputeGlobalValueType(local);
        }

        var slotType = globals.GetType(name);
        if (slotType == typeof(object) || slotType == typeof(void))
        {
            slotType = valueType;
            globals.Types[name] = slotType;
        }
        else if (slotType != valueType && !slotType.IsAssignableFrom(valueType))
        {
            // 同名变量在不同路径上类型不一致（如函数内 double、顶层 Join 结果是语言值）：
            // 槽位退化为 object，避免把值强转成槽位类型。
            slotType = typeof(object);
            globals.Types[name] = slotType;
        }

        local.LocalVarTypes[name] = slotType;
        RuntimeGlobals.EmitStore(ilGenerator, name, slotType, valueType);
        return true;
    }
}
