using System.Collections;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// AsyncForInStatement 的执行/编译辅助实现（阶段 2 从语法节点迁出）。
/// </summary>
internal static partial class AsyncForInStatementRuntimeExtensions
{
    /// <summary>
    /// 生成字典类型的IL代码
    /// </summary>
    internal static void GenerateDictionaryIl(this AsyncForInStatement node, ILGenerator ilGenerator, LocalManager local)
    {
        // 保存字典到局部变量
        node.Expression.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
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
        local.AddLocalVar(node.Ids[0].IdName, keyLocal);

        // 如果有多个标识符（键值对遍历），获取值
        if (node.Ids.Count > 1)
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
            local.AddLocalVar(node.Ids[1].IdName, valueLocal);
        }

        // 生成循环体
        node.Body.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

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
    /// 生成异步枚举类型的IL代码
    /// </summary>
    internal static void GenerateAsyncEnumerableIl(this AsyncForInStatement node, ILGenerator ilGenerator, LocalManager local, Type asyncEnumerableType)
    {
        // 获取元素类型（IAsyncEnumerable<T>中的T）
        var elementType = asyncEnumerableType.GetGenericArguments()[0];
        
        // 声明局部变量
        var asyncEnumerator = ilGenerator.DeclareLocal(typeof(IAsyncEnumerator<>).MakeGenericType(elementType));
        var current = ilGenerator.DeclareLocal(elementType);
        
        // 获取异步枚举器
        var getAsyncEnumeratorMethod = asyncEnumerableType.GetMethod("GetAsyncEnumerator", [typeof(CancellationToken)])!;
        node.Expression.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        ilGenerator.Emit(OpCodes.Ldc_I4_0); // 默认的CancellationToken
        ilGenerator.Emit(OpCodes.Newobj, typeof(CancellationToken).GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Callvirt, getAsyncEnumeratorMethod);
        ilGenerator.Emit(OpCodes.Stloc, asyncEnumerator);
        
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
        
        // 调用MoveNextAsync并等待结果
        var moveNextAsyncMethod = typeof(IAsyncEnumerator<>).MakeGenericType(elementType).GetMethod("MoveNextAsync")!;
        ilGenerator.Emit(OpCodes.Ldloc, asyncEnumerator);
        ilGenerator.Emit(OpCodes.Callvirt, moveNextAsyncMethod);
        
        // 获取ValueTask<bool>的Result属性
        var resultProperty = typeof(ValueTask<bool>).GetProperty("Result")!;
        var getResultMethod = resultProperty.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Callvirt, getResultMethod);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd);
        
        // 获取当前元素
        var currentProperty = typeof(IAsyncEnumerator<>).MakeGenericType(elementType).GetProperty("Current")!;
        var getCurrentMethod = currentProperty.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Ldloc, asyncEnumerator);
        ilGenerator.Emit(OpCodes.Callvirt, getCurrentMethod);
        ilGenerator.Emit(OpCodes.Stloc, current);
        
        // 处理标识符赋值
        if (node.Ids.Count == 1)
        {
            // 单个标识符，直接赋值
            // 在异步状态机中，需要定义变量并存储
            if (local.AsyncStateMachineGenerator != null)
            {
                local.DefineVariable(ilGenerator, node.Ids[0].IdName, typeof(object));
                ilGenerator.Emit(OpCodes.Ldloc, current);
                local.StoreVariable(ilGenerator, node.Ids[0].IdName, node.Position);
            }
            else
            {
                local.AddLocalVar(node.Ids[0].IdName, current);
            }
        }
        else
        {
            // 多个标识符，只赋值给第一个
            if (local.AsyncStateMachineGenerator != null)
            {
                local.DefineVariable(ilGenerator, node.Ids[0].IdName, typeof(object));
                ilGenerator.Emit(OpCodes.Ldloc, current);
                local.StoreVariable(ilGenerator, node.Ids[0].IdName, node.Position);
            }
            else
            {
                local.AddLocalVar(node.Ids[0].IdName, current);
            }
        }
        
        // 生成循环体
        node.Body.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        
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
}
