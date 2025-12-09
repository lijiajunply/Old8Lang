using Old8Lang.LangParser;
using System.Collections;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class ForInStatement(
    LangId id,
    OldExpr expr,
    OldStatement body,
    SourcePosition position = default,
    List<LangId>? additionalIds = null) : OldStatement(position)
{
    // 获取所有标识符，包括主标识符和附加标识符
    private List<LangId> AllIds
    {
        get => [id, .. field];
    } = additionalIds ?? [];

    public override void Run(VariateManager manager)
    {
        manager.AddChildren();

        var value = expr.Run(manager);
        if (value is not ILangList oldList)
            throw new TypeError(this, "IOldList", value.GetType().Name);

        foreach (var idValue in oldList.GetItems())
        {
            if (AllIds.Count == 1)
            {
                // 单个标识符的情况，保持原有行为
                manager.Set(id, idValue);
            }
            else
            {
                // 多个标识符的情况，处理键值对
                if (idValue is TupleLangValue tupleValue)
                {
                    // 运行元组，获取实际值
                    tupleValue.Run(manager);

                    // 字典键值对，赋值给多个标识符
                    var values = new List<LangValueType> { tupleValue.Value.Item1, tupleValue.Value.Item2 };

                    for (int i = 0; i < AllIds.Count && i < values.Count; i++)
                    {
                        manager.Set(AllIds[i], values[i]);
                    }
                }
                else
                {
                    // 不是键值对，只赋值给第一个标识符
                    manager.Set(id, idValue);
                }
            }

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
                // continue;
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
        ilGenerator.Emit(OpCodes.Stloc, current);

        if (AllIds.Count == 1)
        {
            // 单个标识符的情况，保持原有行为
            local.AddLocalVar(id.IdName, current);
        }
        else if (ty == typeof(Dictionary<object, object>))
        {
            // 多个标识符且是字典的情况，分解KeyValuePair
            var keyValuePairType = typeof(KeyValuePair<object, object>);
            var keyProperty = keyValuePairType.GetProperty("Key")!;
            var valueProperty = keyValuePairType.GetProperty("Value")!;
            var keyGetMethod = keyProperty.GetGetMethod()!;
            var valueGetMethod = valueProperty.GetGetMethod()!;

            // 处理第一个标识符（键）
            ilGenerator.Emit(OpCodes.Ldloc, current);
            ilGenerator.Emit(OpCodes.Call, keyGetMethod); // 使用Call而不是Callvirt，因为KeyValuePair是值类型
            var keyLocal = ilGenerator.DeclareLocal(typeof(object));
            ilGenerator.Emit(OpCodes.Stloc, keyLocal);
            local.AddLocalVar(AllIds[0].IdName, keyLocal);

            // 处理第二个标识符（值）
            if (AllIds.Count > 1)
            {
                ilGenerator.Emit(OpCodes.Ldloc, current);
                ilGenerator.Emit(OpCodes.Call, valueGetMethod); // 使用Call而不是Callvirt，因为KeyValuePair是值类型
                var valueLocal = ilGenerator.DeclareLocal(typeof(object));
                ilGenerator.Emit(OpCodes.Stloc, valueLocal);
                local.AddLocalVar(AllIds[1].IdName, valueLocal);
            }
        }
        else
        {
            // 多个标识符但不是字典的情况，只赋值给第一个标识符
            local.AddLocalVar(AllIds[0].IdName, current);
        }

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