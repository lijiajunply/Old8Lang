using Old8Lang.LangParser;
using System.Collections;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class ForInStatement(LangId id, OldExpr expr, OldStatement body, SourcePosition position = default) : OldStatement(position)
{
    public override void Run(VariateManager manager)
    {
        manager.AddChildren();

        var value = expr.Run(manager);
        if (value is not ILangList oldList)
            throw new TypeError(this, "IOldList", value.GetType().Name);

        foreach (var idValue in oldList.GetItems())
        {
            manager.Set(id, idValue);
            try
            {
                body.Run(manager);
            }
            catch (BreakException)
            {
                // 处理break
                break;
            }
            catch (ContinueException)
            {
                // 处理continue，直接进入下一轮循环
                continue;
            }
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
        var continueLabel = ilGenerator.DefineLabel();

        // 保存当前的break和continue标签，以便嵌套循环使用
        var oldBreakLabel = local.BreakLabel;
        var oldContinueLabel = local.ContinueLabel;
        
        // 设置当前循环的break和continue标签
        local.BreakLabel = loopListEnd;
        local.ContinueLabel = continueLabel;

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

        // Continue label
        ilGenerator.MarkLabel(continueLabel);
        // Loop back
        ilGenerator.Emit(OpCodes.Br, loopListStart);

        // End of loop
        ilGenerator.MarkLabel(loopListEnd);
        
        // 恢复之前的break和continue标签
        local.BreakLabel = oldBreakLabel;
        local.ContinueLabel = oldContinueLabel;
    }

    public override OldStatement this[int index] => body[index]!;

    public override int Count => body.Count;
}