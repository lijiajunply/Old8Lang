using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class ForInStatement(OldID id, OldExpr expr, OldStatement body) : OldStatement
{
    public override void Run(VariateManager Manager)
    {
        Manager.AddChildren();

        var value = expr.Run(Manager);
        if (value is not IOldList oldList)
            throw new Exception("ForInStatement: Expr is not IOldList");

        foreach (var idValue in oldList.GetItems())
        {
            Manager.Set(id, idValue);
            body.Run(Manager);
        }

        Manager.RemoveChildren();
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        expr.LoadILValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Ldlen); // 获取数组长度
        ilGenerator.Emit(OpCodes.Conv_I4); // 转换为 int
        var len = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Stloc, len.LocalIndex); // 存储到 length
        local.AddLocalVar("len", len);

        // 初始化 index
        ilGenerator.Emit(OpCodes.Ldc_I4_0); // 加载 0
        var index = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Stloc, index.LocalIndex); // 存储到 length
        local.AddLocalVar("index", index);

        // 创建循环开始标签
        Label loopStart = ilGenerator.DefineLabel();
        Label loopEnd = ilGenerator.DefineLabel();

        // 循环开始
        ilGenerator.MarkLabel(loopStart);

        // 检查 index 是否小于 length
        ilGenerator.Emit(OpCodes.Ldloc, index); // 加载 index
        ilGenerator.Emit(OpCodes.Ldloc, len); // 加载 length
        ilGenerator.Emit(OpCodes.Bge, loopEnd); // 如果 index >= length，跳转到 loopEnd

        // 获取当前元素
        expr.LoadILValue(ilGenerator, local); // 加载 enumerator
        ilGenerator.Emit(OpCodes.Ldloc,index); // 加载 index
        ilGenerator.Emit(OpCodes.Ldelem_I4); // 获取元素
        var item = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Stloc, item.LocalIndex); // 存储到 length
        local.AddLocalVar(id.IdName, item);

        // 打印当前元素
        body.GenerateIL(ilGenerator, local);

        // index +1
        ilGenerator.Emit(OpCodes.Ldloc, index); // 加载 index
        ilGenerator.Emit(OpCodes.Ldc_I4_1); // 加载 1
        ilGenerator.Emit(OpCodes.Add); // index++
        ilGenerator.Emit(OpCodes.Stloc, index); // 存储回 index

        // 跳回循环开始
        ilGenerator.Emit(OpCodes.Br, loopStart);

        // 循环结束标签
        ilGenerator.MarkLabel(loopEnd);
    }
}