using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Runtime;

namespace Old8Lang.GlobalFunctions.Implementations;

/// <summary>IL 模式打印值的辅助方法。</summary>
public static class IlPrintHelper
{
    internal static void Emit(ILGenerator il, LocalManager local, List<LangExpression> parameters, bool newline)
    {
        il.Emit(OpCodes.Ldc_I4, parameters.Count);
        il.Emit(OpCodes.Newarr, typeof(object));
        for (var i = 0; i < parameters.Count; i++)
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4, i);
            var expression = parameters[i];
            expression.LoadIlValue(il, local);
            var type = expression.OutputType(local) ?? typeof(object);
            if (type == typeof(void))
            {
                throw new CompilerException("打印参数不能是无返回值的表达式", expression.Position);
            }
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }
            il.Emit(OpCodes.Stelem_Ref);
        }
        il.Emit(newline ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Call, typeof(IlPrintHelper).GetMethod(nameof(Write))!);
    }

    /// <summary>按语言显示规则连接值并写入控制台。</summary>
    /// <param name="values">已经求值的参数。</param>
    /// <param name="newline">是否输出换行。</param>
    public static void Write(object?[] values, bool newline)
    {
        var output = string.Concat(values.Select(VMValueFormatter.ToDisplayString));
        if (newline)
        {
            Console.WriteLine(output);
        }
        else
        {
            Console.Write(output);
        }
    }
}
