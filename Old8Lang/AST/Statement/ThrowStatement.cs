using Old8Lang.Error;
using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

public class ThrowStatement(LangExpression expression, SourcePosition position = default) : OldStatement(position)
{
    
    public override void Run(VariateManager manager)
    {
        var value = expression.Run(manager);
        
        // 如果值已经是ErrorLangValue，直接抛出其包含的原始异常
        if (value is ErrorLangValue errorValue)
        {
            throw errorValue.Exception;
        }
        
        // 否则，创建一个新的CustomError
        throw new CustomError(
            this,
            value.ToDisplayString());
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 编译模式下的实现
        expression.LoadIlValue(ilGenerator, local);

        // 获取表达式的类型
        var exprType = expression.OutputType(local);

        // 如果是值类型，需要装箱后才能调用ToString()
        if (exprType is { IsValueType: true })
        {
            ilGenerator.Emit(OpCodes.Box, exprType);
        }

        // 将抛出的值转换为字符串
        ilGenerator.Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString")!);
        ilGenerator.Emit(OpCodes.Newobj, typeof(Exception).GetConstructor([typeof(string)])!);
        ilGenerator.Emit(OpCodes.Throw);
    }

    public override OldStatement? this[int index] => null;

    public override int Count => 0;

    public Type OutputType(LocalManager local) => typeof(void);

    public override string ToString() => $"throw {expression}";
}