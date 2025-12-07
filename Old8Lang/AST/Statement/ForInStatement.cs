using Old8Lang.LangParser;
using System.Collections;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang;

namespace Old8Lang.AST.Statement;

public class ForInStatement(OldId id, OldExpr expr, OldStatement body, SourcePosition position = default) : OldStatement(position)
{
    public override void Run(VariateManager manager)
    {
        manager.AddChildren();

        var value = expr.Run(manager);
        if (value is not IOldList oldList)
            throw new TypeError(this, "IOldList", value.GetType().Name);

        foreach (var idValue in oldList.GetItems())
        {
            manager.Set(id, idValue);
            body.Run(manager);
        }

        manager.RemoveChildren();
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
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
        expr.LoadIlValue(ilGenerator, local);
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

        body.GenerateIl(ilGenerator, local);

        // Loop back
        ilGenerator.Emit(OpCodes.Br, loopListStart);

        // End of loop
        ilGenerator.MarkLabel(loopListEnd);
    }

    public override OldStatement this[int index] => body[index]!;

    public override int Count => body.Count;
}