using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.AST;

public abstract class LangExpression : IOldLangTree
{
    /// <inheritdoc />
    public SourcePosition Position { get; set; }

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
        var valueType = OutputType(local) ?? typeof(int); // 默认类型为int

        // 检查变量是否已经在LocalVarTypes中有类型注解
        if (local.LocalVarTypes.TryGetValue(idName, out var existingType))
        {
            // 验证新值的类型与现有类型注解匹配
            local.ValidateType(existingType, valueType, Position);
        }
        else
        {
            // 如果变量还没有类型注解，保存新值的类型到LocalVarTypes
            local.LocalVarTypes[idName] = valueType;
        }

        // 先声明变量，确保在使用前已经存在
        var localVar = local.GetLocalVar(idName);
        if (localVar != null)
        {
            if (localVar.LocalType != valueType)
            {
                // 类型不匹配，重新声明变量
                local.RemoveLocalVar(idName);
                localVar = ilGenerator.DeclareLocal(valueType);
                local.AddLocalVar(idName, localVar);
            }
        }
        else
        {
            // 首次声明变量
            localVar = ilGenerator.DeclareLocal(valueType);
            local.AddLocalVar(idName, localVar);
        }

        // 然后加载值
        LoadIlValue(ilGenerator, local);

        // 最后存储到变量
        ilGenerator.Emit(OpCodes.Stloc, localVar.LocalIndex);
    }

    public virtual Type? OutputType(LocalManager local)
    {
        throw new InvalidOperationError(this, "表达式未实现OutputType方法", "请在子类中实现OutputType方法");
    }
}