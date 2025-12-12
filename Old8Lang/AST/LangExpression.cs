using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.AST;

public abstract class LangExpression : IOldLangTree
{
    /// <inheritdoc />
    public SourcePosition Position { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">源代码位置信息</param>
    protected LangExpression(SourcePosition position = default)
    {
        Position = position;
    }

    public virtual LangValueType Run(VariateManager manager) => throw new InvalidOperationError(this, "表达式未实现Run方法");

    public virtual void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        throw new InvalidOperationError(this, "表达式未实现LoadIlValue方法", "请在子类中实现LoadIlValue方法");
    }

    public virtual void SetValueToIl(ILGenerator ilGenerator, LocalManager local, string idName)
    {
        // 先获取值的类型
        var type = OutputType(local);
        if (type == null) type = typeof(int); // 默认类型为int
        
        // 先声明变量，确保在使用前已经存在
        var b = local.GetLocalVar(idName);
        if (b != null)
        {
            if (b.LocalType != type)
            {
                local.RemoveLocalVar(idName);
                b = ilGenerator.DeclareLocal(type);
                local.AddLocalVar(idName, b);
            }
        }
        else
        {
            b = ilGenerator.DeclareLocal(type);
            local.AddLocalVar(idName, b);
        }
        
        // 然后加载值
        LoadIlValue(ilGenerator, local);
        
        // 最后存储到变量
        ilGenerator.Emit(OpCodes.Stloc, b.LocalIndex);
    }

    public virtual Type? OutputType(LocalManager local)
    {
        throw new InvalidOperationError(this, "表达式未实现OutputType方法", "请在子类中实现OutputType方法");
    }
}