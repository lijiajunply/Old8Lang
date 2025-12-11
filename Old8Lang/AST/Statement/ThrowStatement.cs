using Old8Lang.Error;
using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.Compiler;

namespace Old8Lang.AST.Statement;

public class ThrowStatement(LangExpression expression, SourcePosition position = default) : OldStatement(position)
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    
    public override void Run(VariateManager manager)
{
    var value = expression.Run(manager);
    // 使用CustomError抛出异常
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