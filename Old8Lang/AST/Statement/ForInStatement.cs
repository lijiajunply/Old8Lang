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
        // var variableName = id.IdName;
        // LocalBuilder arrayLocal = ilGenerator.DeclareLocal(typeof(int[])); 
        // LocalBuilder indexLocal = ilGenerator.DeclareLocal(typeof(int));   
        // LocalBuilder elementLocal = ilGenerator.DeclareLocal(typeof(int), variableName); // 使用指定变量名
        //
        // // 存储输入数组
        // ilGenerator.Emit(OpCodes.Ldarg_0);
        // ilGenerator.Emit(OpCodes.Stloc, arrayLocal.LocalIndex);
        //
        // // 初始化索引
        // ilGenerator.Emit(OpCodes.Ldc_I4_0);
        // ilGenerator.Emit(OpCodes.Stloc, indexLocal.LocalIndex);
        //
        // // 循环开始标签
        // Label loopStart = ilGenerator.DefineLabel();
        // Label loopEnd = ilGenerator.DefineLabel();
        //
        // ilGenerator.MarkLabel(loopStart);
        //
        // // 检查索引是否小于数组长度
        // ilGenerator.Emit(OpCodes.Ldloc, indexLocal.LocalIndex);
        // ilGenerator.Emit(OpCodes.Ldloc, arrayLocal.LocalIndex);
        // ilGenerator.Emit(OpCodes.Ldlen);
        // ilGenerator.Emit(OpCodes.Conv_I4);
        // ilGenerator.Emit(OpCodes.Bge, loopEnd);
        //
        // // 获取当前元素
        // ilGenerator.Emit(OpCodes.Ldloc, arrayLocal.LocalIndex);
        // ilGenerator.Emit(OpCodes.Ldloc, indexLocal.LocalIndex);
        // ilGenerator.Emit(OpCodes.Ldelem_I4);
        // ilGenerator.Emit(OpCodes.Stloc, elementLocal.LocalIndex);
        //
        // // 处理元素（支持自定义处理逻辑）
        // body.GenerateIL(ilGenerator, local);
        //
        // // 增加索引
        // ilGenerator.Emit(OpCodes.Ldloc, indexLocal.LocalIndex);
        // ilGenerator.Emit(OpCodes.Ldc_I4_1);
        // ilGenerator.Emit(OpCodes.Add);
        // ilGenerator.Emit(OpCodes.Stloc, indexLocal.LocalIndex);
        //
        // // 跳回循环开始
        // ilGenerator.Emit(OpCodes.Br, loopStart);
        //
        // // 循环结束
        // ilGenerator.MarkLabel(loopEnd);
        // ilGenerator.Emit(OpCodes.Ret);
    }
}