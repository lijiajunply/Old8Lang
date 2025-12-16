using Old8Lang.LangParser;
using System.Collections;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 异步 for-in 语句
/// 支持语法：async for item in asyncStream { ... }
/// 类似于 C# 的 await foreach
/// </summary>
public class AsyncForInStatement(
    LangId id,
    LangExpression expression,
    OldStatement body,
    SourcePosition position = default,
    List<LangId>? additionalIds = null) : OldStatement(position)
{
    // 获取所有标识符，包括主标识符和附加标识符
    private List<LangId> AllIds { get; } = [id, .. (additionalIds ?? [])];

    /// <summary>
    /// 执行异步 for-in 循环
    /// </summary>
    /// <param name="manager">变量管理器</param>
    public override void Run(VariateManager manager)
    {
        manager.AddChildren();
        // 压入新的控制流状态
        manager.ControlFlowManager.PushState();

        try
        {
            var value = expression.Run(manager);

            // 处理异步生成器对象
            if (value is AsyncGeneratorLangValue asyncGenerator)
            {
                // 异步生成器迭代逻辑
                while (true)
                {
                    // 在每次循环迭代开始时重置控制流标志
                    manager.ControlFlowManager.ResetCurrentState();

                    // 异步运行生成器，获取下一个值的 Task
                    var nextValueTask = asyncGenerator.RunAsync(manager);

                    // 等待 Task 完成并获取值
                    var nextValue = nextValueTask.Await();

                    // 检查生成器是否已完成
                    if (asyncGenerator.State == AsyncGeneratorLangValue.AsyncGeneratorState.Completed)
                    {
                        break;
                    }

                    // 检查生成器是否处于Suspended状态，表示有值生成
                    if (asyncGenerator.State == AsyncGeneratorLangValue.AsyncGeneratorState.Suspended)
                    {
                        // 使用asyncGenerator.NextValue作为当前值
                        var currentValue = asyncGenerator.NextValue;

                        if (currentValue != null && !(currentValue is VoidLangValue))
                        {
                            // 赋值给标识符
                            if (AllIds.Count == 1)
                            {
                                manager.Set(id, currentValue);
                            }
                            else
                            {
                                // 多个标识符的情况，处理键值对
                                if (currentValue is TupleLangValue tupleValue)
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
                                    manager.Set(id, currentValue);
                                }
                            }

                            // 执行循环体
                            body.Run(manager);

                            // 处理break
                            if (manager.ControlFlowManager.BreakFlag)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            // 处理普通生成器对象（向后兼容）
            else if (value is GeneratorLangValue generator)
            {
                // 使用同步的 for-in 逻辑
                while (true)
                {
                    manager.ControlFlowManager.ResetCurrentState();

                    var nextValue = generator.Run(manager);

                    if (generator.State == GeneratorLangValue.GeneratorState.Completed)
                    {
                        break;
                    }

                    if (generator.State == GeneratorLangValue.GeneratorState.Suspended)
                    {
                        var currentValue = generator.NextValue;

                        if (currentValue != null && !(currentValue is VoidLangValue))
                        {
                            if (AllIds.Count == 1)
                            {
                                manager.Set(id, currentValue);
                            }
                            else
                            {
                                if (currentValue is TupleLangValue tupleValue)
                                {
                                    tupleValue.Run(manager);
                                    var values = new List<LangValueType> { tupleValue.Value.Item1, tupleValue.Value.Item2 };

                                    for (int i = 0; i < AllIds.Count && i < values.Count; i++)
                                    {
                                        manager.Set(AllIds[i], values[i]);
                                    }
                                }
                                else
                                {
                                    manager.Set(id, currentValue);
                                }
                            }

                            body.Run(manager);

                            if (manager.ControlFlowManager.BreakFlag)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            // 处理普通列表对象
            else if (value is ILangList oldList)
            {
                foreach (var idValue in oldList.GetItems())
                {
                    manager.ControlFlowManager.ResetCurrentState();

                    if (AllIds.Count == 1)
                    {
                        manager.Set(id, idValue);
                    }
                    else
                    {
                        if (idValue is TupleLangValue tupleValue)
                        {
                            tupleValue.Run(manager);
                            var values = new List<LangValueType> { tupleValue.Value.Item1, tupleValue.Value.Item2 };

                            for (int i = 0; i < AllIds.Count && i < values.Count; i++)
                            {
                                manager.Set(AllIds[i], values[i]);
                            }
                        }
                        else
                        {
                            manager.Set(id, idValue);
                        }
                    }

                    body.Run(manager);

                    if (manager.ControlFlowManager.BreakFlag)
                    {
                        break;
                    }
                }
            }
            else
            {
                throw new TypeError(this, "ILangList、GeneratorLangValue 或 AsyncGeneratorLangValue", value.GetType().Name);
            }
        }
        finally
        {
            // 弹出当前控制流状态
            manager.ControlFlowManager.PopState();
            manager.RemoveChildren();
        }
    }

    /// <summary>
    /// 生成 IL 代码（编译器模式）
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="local">局部变量管理器</param>
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
    /// 生成字典类型的IL代码
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

        // 如果有多个标识符（键值对遍历），获取值
        if (AllIds.Count > 1)
        {
            // 获取字典的索引器方法
            var itemProperty = typeof(Dictionary<object, object>).GetProperty("Item")!;
            var getItemMethod = itemProperty.GetGetMethod()!;

            // 加载字典和键，调用索引器获取值
            ilGenerator.Emit(OpCodes.Ldloc, dictLocal);
            ilGenerator.Emit(OpCodes.Ldloc, keyLocal);
            ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);

            // 保存值到局部变量
            var valueLocal = ilGenerator.DeclareLocal(typeof(object));
            ilGenerator.Emit(OpCodes.Stloc, valueLocal);

            // 将值添加到局部变量管理器
            local.AddLocalVar(AllIds[1].IdName, valueLocal);
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

    public override OldStatement this[int index] => body[index]!;

    public override int Count => body.Count;
}
