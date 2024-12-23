using System.Collections;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class OtherVariateChanging(OldID id, OldExpr sumId, OldExpr expr) : OldStatement
{
    public override void Run(VariateManager Manager)
    {
        var a = Manager.GetValue(id);
        if (a is AnyValue any)
        {
            if (sumId is not OldID sum) throw new TypeError(this, this);
            var result = expr.Run(Manager);
            any.Set(sum, result);
        }

        if (a is ArrayValue array)
        {
            var s = sumId.Run(Manager);
            if (s is not IntValue sum) throw new TypeError(this, this);
            var result = expr.Run(Manager);
            array.Set(sum, result);
        }

        if (a is DictionaryValue dictionary)
        {
            var s = sumId.Run(Manager);
            var result = expr.Run(Manager);
            dictionary.Update(s, result);
        }
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        if (local.InClassEnv != null && id.IdName == "this")
        {
            ilGenerator.Emit(OpCodes.Ldarg_0);
            if (sumId is OldID sum1)
            {
                expr.LoadILValue(ilGenerator, local);
                var field = local.InClassEnv.GetField(sum1.IdName);
                ilGenerator.Emit(OpCodes.Stfld, field!);
                return;
            }

            if (sumId is StringValue stringValue1)
            {
                expr.LoadILValue(ilGenerator, local);
                var field = local.InClassEnv.GetField(stringValue1.Value);
                ilGenerator.Emit(OpCodes.Stfld, field!);
            }
            return;
        }
        id.LoadILValue(ilGenerator, local);
        var leftType = id.OutputType(local);
        
        if (leftType.IsAssignableTo(typeof(IEnumerable)))
        {
            sumId.LoadILValue(ilGenerator, local);
            expr.LoadILValue(ilGenerator, local);
            ilGenerator.Emit(OpCodes.Stelem_I4);
            return;
        }

        if (sumId is OldID sum)
        {
            expr.LoadILValue(ilGenerator, local);
            var field = leftType.GetField(sum.IdName);
            ilGenerator.Emit(OpCodes.Stfld, field!);
            return;
        }

        if (sumId is StringValue stringValue)
        {
            expr.LoadILValue(ilGenerator, local);
            var field = leftType.GetField(stringValue.Value);
            ilGenerator.Emit(OpCodes.Stfld, field!);
        }
    }

    public override OldStatement? this[int index] => null;

    public override int Count => 0;

    public override string ToString() => $"{id}.{sumId} = {expr}";
}