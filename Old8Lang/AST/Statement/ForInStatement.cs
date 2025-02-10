using System.Collections;
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
        var ty = expr.OutputType(local);
        var enumerator = ilGenerator.DeclareLocal(typeof(IEnumerator));
        var current = ilGenerator.DeclareLocal(ty == typeof(Dictionary<object, object>)
            ? typeof(KeyValuePair<object, object>)
            : typeof(object));

        // Get the GetEnumerator method
        var getEnumeratorMethod = typeof(IEnumerable).GetMethod("GetEnumerator")!;
        var moveNextMethod = typeof(IEnumerator).GetMethod("MoveNext")!;
        var getCurrentMethod = typeof(IEnumerator).GetProperty("Current")!.GetGetMethod()!;
        expr.LoadILValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Callvirt, getEnumeratorMethod);
        ilGenerator.Emit(OpCodes.Stloc, enumerator);

        // Define labels for loop
        var loopListStart = ilGenerator.DefineLabel();
        var loopListEnd = ilGenerator.DefineLabel();

        // Start of loop
        ilGenerator.MarkLabel(loopListStart);
        ilGenerator.Emit(OpCodes.Ldloc, enumerator);
        ilGenerator.Emit(OpCodes.Callvirt, moveNextMethod);
        ilGenerator.Emit(OpCodes.Brfalse, loopListEnd);

        // Get current element
        ilGenerator.Emit(OpCodes.Ldloc, enumerator);
        ilGenerator.Emit(OpCodes.Callvirt, getCurrentMethod);
        //ilGenerator.Emit(OpCodes.Box, typeof(int));
        ilGenerator.Emit(OpCodes.Stloc, current);
        local.AddLocalVar(id.IdName, current);

        body.GenerateIL(ilGenerator, local);

        // Loop back
        ilGenerator.Emit(OpCodes.Br, loopListStart);

        // End of loop
        ilGenerator.MarkLabel(loopListEnd);
    }

    public override OldStatement this[int index] => body[index]!;

    public override int Count => body.Count;
}