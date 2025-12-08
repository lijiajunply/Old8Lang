using Old8Lang.LangParser;
using System.Collections;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;

using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class OtherVariateChanging(LangId id, OldExpr sumId, OldExpr expr, SourcePosition position = default) : OldStatement(position)
{
    public override void Run(VariateManager manager)
    {
        var a = manager.GetValue(id);
        if (a is AnyLangValue any)
        {
            if (sumId is not LangId sum) throw new TypeError(this, "OldId", sumId.GetType().Name);
            var result = expr.Run(manager);
            any.Set(sum, result);
        }

        if (a is ArrayLangValue array)
        {
            var s = sumId.Run(manager);
            if (s is not IntLangValue sum) throw new TypeError(this, "IntValue", s.GetType().Name);
            var result = expr.Run(manager);
            array.Set(sum, result);
        }

        if (a is DictionaryLangValue dictionary)
        {
            var s = sumId.Run(manager);
            var result = expr.Run(manager);
            dictionary.Update(s, result);
        }
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        if (local.InClassEnv != null && id.IdName == "this")
        {
            ilGenerator.Emit(OpCodes.Ldarg_0);
            if (sumId is LangId sum1)
            {
                expr.LoadIlValue(ilGenerator, local);
                var field = local.InClassEnv.GetField(sum1.IdName);
                ilGenerator.Emit(OpCodes.Stfld, field!);
                return;
            }

            if (sumId is StringLangValue stringValue1)
            {
                expr.LoadIlValue(ilGenerator, local);
                var field = local.InClassEnv.GetField(stringValue1.Value);
                ilGenerator.Emit(OpCodes.Stfld, field!);
            }
            return;
        }
        id.LoadIlValue(ilGenerator, local);
        var leftType = id.OutputType(local);
        
        if (leftType.IsAssignableTo(typeof(IEnumerable)))
        {
            sumId.LoadIlValue(ilGenerator, local);
            expr.LoadIlValue(ilGenerator, local);
            ilGenerator.Emit(OpCodes.Stelem_I4);
            return;
        }

        if (sumId is LangId sum)
        {
            expr.LoadIlValue(ilGenerator, local);
            var field = leftType.GetField(sum.IdName);
            ilGenerator.Emit(OpCodes.Stfld, field!);
            return;
        }

        if (sumId is StringLangValue stringValue)
        {
            expr.LoadIlValue(ilGenerator, local);
            var field = leftType.GetField(stringValue.Value);
            ilGenerator.Emit(OpCodes.Stfld, field!);
        }
    }

    public override OldStatement? this[int index] => null;

    public override int Count => 0;

    public override string ToString() => $"{id}.{sumId} <- {expr}";
}