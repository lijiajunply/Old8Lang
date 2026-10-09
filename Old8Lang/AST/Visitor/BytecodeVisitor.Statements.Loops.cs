using Old8Lang.AST.Expression;
using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - 循环和迭代
/// </summary>
public partial class BytecodeVisitor
{
    /// <summary>
    /// 为循环变量发出「每轮迭代重新绑定」指令
    /// </summary>
    /// <remarks>
    /// 解释器每轮迭代都为循环变量建立独立绑定，循环体内创建的闭包各自捕获当轮的值
    /// （<c>for i in [0~2] { fs.Add(() -&gt; i) }</c> 得到 0,1,2）。虚拟机的局部变量槽位是
    /// 复用的：一旦某个闭包把槽位装箱成共享单元，后续迭代若不换新单元，所有闭包就会
    /// 共享同一个变量、全都读到最终值（得到 2,2,2）。槽位未装箱时该指令不做事。
    /// </remarks>
    private void EmitLoopVariableRebind(string? variableName)
    {
        if (string.IsNullOrEmpty(variableName) || !_compiler.IsLocalVariable(variableName))
        {
            return;
        }

        Emit(OpCode.RefreshLocalBinding, _compiler.GetLocalIndex(variableName));
    }

    public Instruction? VisitForInStatement(ForInStatement node)
    {
        // For-in 循环：for item in collection { ... }
        // 也支持多标识符解构：for key, value in dict { ... }
        // 获取字段（主构造函数参数）
        var id = GetPrimaryConstructorParameter<LangId>(node, "id");
        var expression = GetPrimaryConstructorParameter<LangExpression>(node, "expression");
        var body = GetPrimaryConstructorParameter<OldStatement>(node, "body");

        if (id == null || expression == null || body == null)
        {
            return null;
        }

        var ids = node.Ids;
        bool isDestructuring = ids.Count > 1;

        // 创建循环标签
        var loopLabels = new LoopLabels();
        _loopLabels.Push(loopLabels);

        // 生成集合表达式的代码（栈上现在有集合）
        expression.Accept(this);

        // 解构（for k, v in ...）时把被迭代表达式留在栈上备用。
        // 迭代元素现在统一是「值或 (键, 值) 元组」，拆包不再需要回查原集合，
        // 但 __for_in_unpack 的调用点仍按三参数协议传它（见该原生函数的说明），
        // 因此这里继续保留集合本身。
        int collectionLocalIndex = -1;
        if (isDestructuring)
        {
            Emit(OpCode.Dup);
        }

        // 获取迭代器（栈上现在有迭代器）
        Emit(OpCode.GetIterator);

        // 将迭代器保存到一个临时局部变量
        // 使用唯一的名称避免嵌套循环中的冲突
        int iteratorLocalIndex = _compiler.AllocateLocal($"<iterator_{GetCurrentPosition()}>");
        Emit(OpCode.StoreLocal, iteratorLocalIndex);

        if (isDestructuring)
        {
            collectionLocalIndex = _compiler.AllocateLocal($"<iterable_{GetCurrentPosition()}>");
            Emit(OpCode.StoreLocal, collectionLocalIndex);
        }

        // 循环开始标签
        int loopStart = GetCurrentPosition();
        loopLabels.ContinueTarget = loopStart;

        // 加载迭代器到栈
        Emit(OpCode.LoadLocal, iteratorLocalIndex);

        // 调用 MoveNext（栈：迭代器 → 迭代器, hasNext）
        // 注意：IteratorMoveNext 使用 Peek，所以迭代器仍在栈上
        Emit(OpCode.IteratorMoveNext);

        // 如果 MoveNext 返回 false，跳出循环
        // JumpIfFalse 会弹出 hasNext，栈上还剩迭代器
        int jumpIfFalse = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1);

        // 此时栈上还有迭代器对象（因为 IteratorMoveNext 使用 Peek）
        // 不需要再次加载迭代器

        // 获取当前元素（栈：迭代器 → 迭代器, current）
        // 注意：IteratorCurrent 也使用 Peek，所以迭代器仍在栈上
        Emit(OpCode.IteratorCurrent);

        if (isDestructuring)
        {
            // 当前元素是键/元组，交由原生函数拆成一组值再逐个绑定
            int itemLocalIndex = _compiler.AllocateLocal($"<loop_item_{GetCurrentPosition()}>");
            Emit(OpCode.StoreLocal, itemLocalIndex);

            // 参数顺序：[collection, item, arity]
            // 注意：LoadConst 的操作数是常量池下标，必须先入池再引用
            Emit(OpCode.LoadLocal, collectionLocalIndex);
            Emit(OpCode.LoadLocal, itemLocalIndex);
            Emit(OpCode.LoadConst, _compiler.AddConstant(ids.Count));
            Emit(OpCode.CallNative, new object[] { 3, "__for_in_unpack" });

            int unpackedLocalIndex = _compiler.AllocateLocal($"<loop_unpacked_{GetCurrentPosition()}>");
            Emit(OpCode.StoreLocal, unpackedLocalIndex);

            for (var i = 0; i < ids.Count; i++)
            {
                var targetName = ids[i].IdName;

                Emit(OpCode.LoadLocal, unpackedLocalIndex);
                Emit(OpCode.LoadConst, _compiler.AddConstant(i));
                Emit(OpCode.GetIndex);

                // 先换新共享单元，再写入本轮的值：此后新建的闭包捕获的是本轮绑定。
                // 下标要在声明之后再取，首轮此处会把变量声明成局部变量（槽位尚未装箱，
                // 该指令此时是空操作）。
                int targetLocalIndex = _compiler.IsLocalVariable(targetName)
                    ? _compiler.GetLocalIndex(targetName)
                    : _compiler.DeclareLocalVariable(targetName);
                Emit(OpCode.RefreshLocalBinding, targetLocalIndex);
                Emit(OpCode.StoreLocal, targetLocalIndex);
            }

            _compiler.FreeLocal(itemLocalIndex);
            _compiler.FreeLocal(unpackedLocalIndex);
        }
        else
        {
            // 将当前元素存储到循环变量（弹出 current）
            string varName = id.IdName;
            // 先换新共享单元，再写入本轮的元素：此后新建的闭包捕获的是本轮绑定。
            // 下标要在声明之后再取，首轮此处会把变量声明成局部变量（槽位尚未装箱，
            // 该指令此时是空操作）。
            int loopVarLocalIndex = _compiler.IsLocalVariable(varName)
                ? _compiler.GetLocalIndex(varName)
                : _compiler.DeclareLocalVariable(varName);
            Emit(OpCode.RefreshLocalBinding, loopVarLocalIndex);
            Emit(OpCode.StoreLocal, loopVarLocalIndex);
        }

        // 此时栈上还有迭代器对象，需要弹出
        Emit(OpCode.Pop);

        // 执行循环体
        body.Accept(this);

        // 跳回循环开始
        Emit(OpCode.Jump, loopStart);

        // 修补跳出循环的跳转
        int loopEnd = GetCurrentPosition();
        PatchJump(jumpIfFalse, loopEnd);

        // 跳出循环时（通过 JumpIfFalse），栈上还有迭代器对象，需要弹出
        Emit(OpCode.Pop);

        // 修补所有break跳转
        // break 跳转到这里时，栈上没有迭代器对象（已经在循环体中被弹出了）
        int breakTarget = GetCurrentPosition();
        foreach (var breakJump in loopLabels.BreakJumps)
        {
            PatchJump(breakJump, breakTarget);
        }

        // 修补所有continue跳转
        foreach (var continueJump in loopLabels.ContinueJumps)
        {
            PatchJump(continueJump, loopLabels.ContinueTarget);
        }

        _loopLabels.Pop();

        // 释放迭代器局部变量
        _compiler.FreeLocal(iteratorLocalIndex);

        if (collectionLocalIndex >= 0)
        {
            _compiler.FreeLocal(collectionLocalIndex);
        }

        return null;
    }


    public Instruction? VisitSwitchStatement(SwitchStatement node)
    {
        // 获取字段（主构造函数参数）
        var switchExpression = GetPrimaryConstructorParameter<LangExpression>(node, "switchExpression");
        var switchCaseList = GetPrimaryConstructorParameter<List<CaseStatement>>(node, "switchCaseList");
        var defaultBlockStatement = GetPrimaryConstructorParameter<BlockStatement>(node, "defaultBlockStatement");

        if (switchExpression == null || switchCaseList == null)
        {
            return null;
        }

        // 生成 switch 表达式的代码（栈上有 switch 值）
        switchExpression.Accept(this);

        var caseEndLabels = new List<int>();

        // 为每个 case 生成代码
        for (int i = 0; i < switchCaseList.Count; i++)
        {
            var caseStmt = switchCaseList[i];

            // 复制 switch 值用于比较（栈上现在有 2 个 switch 值）
            Emit(OpCode.Dup);

            // 生成 case 表达式的代码（栈上现在有 2 个 switch 值 + case 值）
            // 直接访问 expression 属性而不是调用 Accept
            caseStmt.Expression.Accept(this);

            // 比较是否相等（弹出 2 个值，栈上现在有 1 个 switch 值 + 比较结果）
            Emit(OpCode.Equal);

            // 如果不相等，跳转到下一个 case（弹出比较结果，栈上还有 1 个 switch 值）
            int jumpIfFalse = GetCurrentPosition();
            Emit(OpCode.JumpIfFalse, -1);

            // 匹配成功：弹出 switch 值（栈为空）
            Emit(OpCode.Pop);

            // 执行 case 块：直接访问 BlockStatement 属性
            caseStmt.BlockStatement.Accept(this);

            // 跳转到 switch 结束
            int jumpEnd = GetCurrentPosition();
            Emit(OpCode.Jump, -1);
            caseEndLabels.Add(jumpEnd);

            // 修补"不匹配"的跳转：跳到这里时栈上还有 switch 值
            PatchJump(jumpIfFalse, GetCurrentPosition());
        }

        // 所有 case 都不匹配，执行 default 块
        if (defaultBlockStatement != null)
        {
            // 弹出 switch 值
            Emit(OpCode.Pop);

            // 执行 default 块
            defaultBlockStatement.Accept(this);
        }
        else
        {
            // 没有 default，弹出 switch 值
            Emit(OpCode.Pop);
        }

        // 修补所有"匹配成功后跳转到结束"的指令
        int endPosition = GetCurrentPosition();
        foreach (var label in caseEndLabels)
        {
            PatchJump(label, endPosition);
        }

        return null;
    }


    public Instruction? VisitCaseStatement(CaseStatement node)
    {
        // CaseStatement 在 SwitchStatement 中已完整处理
        // 这个方法不应该被直接调用
        return null;
    }


    public Instruction? VisitAsyncForInStatement(AsyncForInStatement node)
    {
        // 异步 for-in 循环：async for item in asyncGenerator { ... }
        // 获取字段（主构造函数参数）
        var id = GetPrimaryConstructorParameter<LangId>(node, "id");
        var expression = GetPrimaryConstructorParameter<LangExpression>(node, "expression");
        var body = GetPrimaryConstructorParameter<OldStatement>(node, "body");

        if (id == null || expression == null || body == null)
        {
            return null;
        }

        string varName = id.IdName;

        // 创建循环标签
        var loopLabels = new LoopLabels();
        _loopLabels.Push(loopLabels);

        // 生成异步生成器表达式的代码（栈上现在有异步生成器）
        expression.Accept(this);

        // 将异步生成器保存到一个临时局部变量
        int asyncGenLocalIndex = _compiler.AllocateLocal("<async_generator>");
        Emit(OpCode.StoreLocal, asyncGenLocalIndex);

        // 循环开始标签
        int loopStart = GetCurrentPosition();
        loopLabels.ContinueTarget = loopStart;

        // 加载异步生成器到栈
        Emit(OpCode.LoadLocal, asyncGenLocalIndex);

        // 调用异步生成器的 MoveNextAsync（这会返回一个 Task）
        // 注意：这里需要虚拟机支持异步迭代器的 MoveNext 操作
        // 简化实现：使用同步的 MoveNext
        Emit(OpCode.IteratorMoveNext);

        // 如果 MoveNext 返回 false，跳出循环
        int jumpIfFalse = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1);

        // 加载异步生成器到栈
        Emit(OpCode.LoadLocal, asyncGenLocalIndex);

        // 获取当前元素
        Emit(OpCode.IteratorCurrent);

        // 将当前元素存储到循环变量
        if (_compiler.IsLocalVariable(varName))
        {
            int localIndex = _compiler.GetLocalIndex(varName);
            Emit(OpCode.StoreLocal, localIndex);
        }
        else
        {
            // 声明为局部变量
            int localIndex = _compiler.DeclareLocalVariable(varName);
            Emit(OpCode.StoreLocal, localIndex);
        }

        // 执行循环体
        body.Accept(this);

        // 跳回循环开始
        Emit(OpCode.Jump, loopStart);

        // 修补跳出循环的跳转
        int loopEnd = GetCurrentPosition();
        PatchJump(jumpIfFalse, loopEnd);

        // 修补所有break跳转
        foreach (var breakJump in loopLabels.BreakJumps)
        {
            PatchJump(breakJump, loopEnd);
        }

        // 修补所有continue跳转
        foreach (var continueJump in loopLabels.ContinueJumps)
        {
            PatchJump(continueJump, loopStart);
        }

        _loopLabels.Pop();

        return null;
    }


}
