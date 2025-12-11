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
    LangExpression expression,
    OldStatement body,
    SourcePosition position = default,
    List<LangId>? additionalIds = null) : OldStatement(position)
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    
    // 获取所有标识符，包括主标识符和附加标识符
    private List<LangId> AllIds
    {
        get => [id, .. field];
    } = additionalIds ?? [];

    public override void Run(VariateManager manager)
    {
        manager.AddChildren();

        var value = expression.Run(manager);
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
        var ty = expression.OutputType(local) ?? typeof(object);
        
        // 对于字典类型，使用特殊处理
        if (ty == typeof(Dictionary<object, object>))
        {
            GenerateDictionaryIl(ilGenerator, local);
            return;
        }
        
        // 非字典类型，使用普通的IEnumerator处理
        var enumerator = ilGenerator.DeclareLocal(typeof(IEnumerator));
        var current = ilGenerator.DeclareLocal(typeof(object));
        
        // 获取枚举器
        var getEnumeratorMethod = typeof(IEnumerable).GetMethod("GetEnumerator")!;
        expression.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Callvirt, getEnumeratorMethod);
        ilGenerator.Emit(OpCodes.Stloc, enumerator);
        
        // 定义循环标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();
        var continueLabel = ilGenerator.DefineLabel();
        
        // 保存当前的break和continue标签
        var oldBreakLabel = local.BreakLabel;
        var oldContinueLabel = local.ContinueLabel;
        
        // 设置当前循环的break和continue标签
        local.BreakLabel = loopEnd;
        local.ContinueLabel = continueLabel;
        
        // 循环开始
        ilGenerator.MarkLabel(loopStart);
        
        // 调用MoveNext
        var moveNextMethod = typeof(IEnumerator).GetMethod("MoveNext")!;
        ilGenerator.Emit(OpCodes.Ldloc, enumerator);
        ilGenerator.Emit(OpCodes.Callvirt, moveNextMethod);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd);
        
        // 获取当前元素
        var currentProperty = typeof(IEnumerator).GetProperty("Current")!;
        var getCurrentMethod = currentProperty.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Ldloc, enumerator);
        ilGenerator.Emit(OpCodes.Callvirt, getCurrentMethod);
        ilGenerator.Emit(OpCodes.Stloc, current);
        
        // 处理标识符赋值
        if (AllIds.Count == 1)
        {
            // 单个标识符，直接赋值
            local.AddLocalVar(AllIds[0].IdName, current);
        }
        else
        {
            // 多个标识符，只赋值给第一个
            local.AddLocalVar(AllIds[0].IdName, current);
        }
        
        // 生成循环体
        body.GenerateIl(ilGenerator, local);
        
        // 继续标签
        ilGenerator.MarkLabel(continueLabel);
        
        // 跳回循环开始
        ilGenerator.Emit(OpCodes.Br, loopStart);
        
        // 循环结束
        ilGenerator.MarkLabel(loopEnd);
        
        // 恢复之前的break和continue标签
        local.BreakLabel = oldBreakLabel;
        local.ContinueLabel = oldContinueLabel;
    }
    
    /// <summary>
    /// 生成字典类型的IL代码，使用更简单可靠的方式
    /// </summary>
    private void GenerateDictionaryIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 保存字典到局部变量
        expression.LoadIlValue(ilGenerator, local);
        var dictLocal = ilGenerator.DeclareLocal(typeof(Dictionary<object, object>));
        ilGenerator.Emit(OpCodes.Stloc, dictLocal);
        
        // 获取字典的Keys集合
        var keysProperty = typeof(Dictionary<object, object>).GetProperty("Keys")!;
        var keysGetMethod = keysProperty.GetGetMethod()!;
        
        // 获取Keys集合的IEnumerable接口
        var enumerableType = typeof(IEnumerable);
        ilGenerator.Emit(OpCodes.Ldloc, dictLocal);
        ilGenerator.Emit(OpCodes.Callvirt, keysGetMethod);
        
        // 获取Keys集合的枚举器
        var keysEnumerator = ilGenerator.DeclareLocal(typeof(IEnumerator));
        var keysGetEnumeratorMethod = enumerableType.GetMethod("GetEnumerator")!;
        ilGenerator.Emit(OpCodes.Callvirt, keysGetEnumeratorMethod);
        ilGenerator.Emit(OpCodes.Stloc, keysEnumerator);
        
        // 定义循环标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();
        var continueLabel = ilGenerator.DefineLabel();
        
        // 保存当前的break和continue标签
        var oldBreakLabel = local.BreakLabel;
        var oldContinueLabel = local.ContinueLabel;
        
        // 设置当前循环的break和continue标签
        local.BreakLabel = loopEnd;
        local.ContinueLabel = continueLabel;
        
        // 循环开始
        ilGenerator.MarkLabel(loopStart);
        
        // 调用MoveNext
        var moveNextMethod = typeof(IEnumerator).GetMethod("MoveNext")!;
        ilGenerator.Emit(OpCodes.Ldloc, keysEnumerator);
        ilGenerator.Emit(OpCodes.Callvirt, moveNextMethod);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd);
        
        // 获取当前键
        var currentProperty = typeof(IEnumerator).GetProperty("Current")!;
        var getCurrentMethod = currentProperty.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Ldloc, keysEnumerator);
        ilGenerator.Emit(OpCodes.Callvirt, getCurrentMethod);
        var keyLocal = ilGenerator.DeclareLocal(typeof(object));
        ilGenerator.Emit(OpCodes.Stloc, keyLocal);
        
        // 将键添加到局部变量管理器
        local.AddLocalVar(AllIds[0].IdName, keyLocal);
        
        // 生成循环体
        body.GenerateIl(ilGenerator, local);
        
        // 继续标签
        ilGenerator.MarkLabel(continueLabel);
        
        // 跳回循环开始
        ilGenerator.Emit(OpCodes.Br, loopStart);
        
        // 循环结束
        ilGenerator.MarkLabel(loopEnd);
        
        // 恢复之前的break和continue标签
        local.BreakLabel = oldBreakLabel;
        local.ContinueLabel = oldContinueLabel;
    }

    public override OldStatement this[int index] => body[index]!;

    public override int Count => body.Count;
}