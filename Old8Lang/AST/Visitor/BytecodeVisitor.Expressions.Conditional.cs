using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Linq;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - 条件和模式匹配
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitTernaryExpression(TernaryExpression node)
    {
        // 三元运算符: condition ? trueExpr : falseExpr
        // 生成条件表达式代码
        node.Condition.Accept(this);

        // 如果条件为false，跳转到false分支
        int jumpIfFalseIndex = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1);

        // True分支
        node.TrueExpression.Accept(this);
        int jumpToEndIndex = GetCurrentPosition();
        Emit(OpCode.Jump, -1); // 跳转到结束

        // False分支
        PatchJump(jumpIfFalseIndex, GetCurrentPosition());
        node.FalseExpression.Accept(this);

        // 结束
        PatchJump(jumpToEndIndex, GetCurrentPosition());

        return null;
    }


    public Instruction? VisitMatchExpression(MatchExpression node)
    {
        // Match 表达式（模式匹配）
        // 实现完整的 match 支持：
        // 1. 值匹配
        // 2. 通配符匹配 (_)
        // 3. 变量绑定
        // 4. 类型匹配（带可选守卫条件）
        // 5. 元组解构匹配
        // 6. 范围匹配

        // 1. 计算被匹配的值并存储到局部变量
        node.MatchValue.Accept(this);
        int matchValueLocal = _compiler.AllocateLocal();
        Emit(OpCode.StoreLocal, matchValueLocal);

        // 2. 为每个 case 创建标签
        var caseLabels = new List<int>();
        var endLabel = -1; // 将在最后设置

        // 3. 生成每个 case 的匹配判断
        for (int i = 0; i < node.Cases.Count; i++)
        {
            var matchCase = node.Cases[i];

            // 检查是否是通配符或变量绑定（匹配所有）
            if (matchCase.IsWildcard || matchCase.IsVariableBinding)
            {
                // 通配符或变量绑定：直接跳转到对应的 case
                int jumpIndex = GetCurrentPosition();
                Emit(OpCode.Jump, -1); // 占位，稍后修补
                caseLabels.Add(jumpIndex);
                continue;
            }

            // 根据模式类型生成匹配代码
            switch (matchCase.Type)
            {
                case PatternType.Value:
                    // 值匹配：比较 match 值和 case 值
                    Emit(OpCode.LoadLocal, matchValueLocal);
                    matchCase.Pattern!.Accept(this);
                    Emit(OpCode.Equal);

                    // 如果匹配，跳转到对应的 case
                    int jumpIfTrueIndex = GetCurrentPosition();
                    Emit(OpCode.JumpIfTrue, -1); // 占位，稍后修补
                    caseLabels.Add(jumpIfTrueIndex);
                    break;

                case PatternType.TypeMatch:
                    // 类型匹配（带可选守卫条件）
                    GenerateTypeMatchCode(matchCase, matchValueLocal);
                    int typeMatchJumpIndex = GetCurrentPosition();
                    Emit(OpCode.JumpIfTrue, -1); // 占位，稍后修补
                    caseLabels.Add(typeMatchJumpIndex);
                    break;

                case PatternType.Range:
                    // 范围匹配
                    GenerateRangeMatchCode(matchCase, matchValueLocal);
                    int rangeMatchJumpIndex = GetCurrentPosition();
                    Emit(OpCode.JumpIfTrue, -1); // 占位，稍后修补
                    caseLabels.Add(rangeMatchJumpIndex);
                    break;

                case PatternType.Tuple:
                    // 元组解构匹配
                    GenerateTupleMatchCode(matchCase, matchValueLocal);
                    int tupleMatchJumpIndex = GetCurrentPosition();
                    Emit(OpCode.JumpIfTrue, -1); // 占位，稍后修补
                    caseLabels.Add(tupleMatchJumpIndex);
                    break;

                default:
                    throw new NotSupportedException($"虚拟机模式暂不支持模式类型: {matchCase.Type}");
            }
        }

        // 4. 所有 case 都不匹配，抛出异常
        Emit(OpCode.LoadConst, _compiler.AddConstant("Match 表达式没有匹配的分支"));
        Emit(OpCode.Throw);

        // 5. 生成各个 case 的执行代码
        var caseEndJumps = new List<int>();

        for (int i = 0; i < node.Cases.Count; i++)
        {
            var matchCase = node.Cases[i];

            // 修补跳转到此 case 的指令
            PatchJump(caseLabels[i], GetCurrentPosition());

            // 如果有变量绑定，将 match 值赋给局部变量
            if (matchCase.BindingVariable != null)
            {
                Emit(OpCode.LoadLocal, matchValueLocal);
                int bindingLocal = _compiler.AllocateLocal(matchCase.BindingVariable);
                Emit(OpCode.StoreLocal, bindingLocal);
            }

            // 执行 case 的结果表达式
            matchCase.ResultExpression.Accept(this);

            // 跳转到结束
            int jumpToEndIndex = GetCurrentPosition();
            Emit(OpCode.Jump, -1); // 占位，稍后修补
            caseEndJumps.Add(jumpToEndIndex);
        }

        // 6. 修补所有跳转到结束的指令
        int endPosition = GetCurrentPosition();
        foreach (var jumpIndex in caseEndJumps)
        {
            PatchJump(jumpIndex, endPosition);
        }

        // 7. 释放 match 值的局部变量
        _compiler.FreeLocal(matchValueLocal);

        return null;
    }

    /// <summary>
    /// 生成类型匹配代码（带可选守卫条件）
    /// </summary>
    private void GenerateTypeMatchCode(MatchCase matchCase, int matchValueLocal)
    {
        // 加载 match 值
        Emit(OpCode.LoadLocal, matchValueLocal);

        // 调用类型检查指令
        Emit(OpCode.IsType, matchCase.TypeAnnotation!);

        // 如果有守卫条件，需要额外检查
        if (matchCase.GuardCondition != null)
        {
            // 如果类型不匹配，跳过守卫条件检查
            int skipGuardJump = GetCurrentPosition();
            Emit(OpCode.JumpIfFalse, -1); // 占位

            // 类型匹配，绑定变量并评估守卫条件
            Emit(OpCode.LoadLocal, matchValueLocal);
            int guardBindingLocal = _compiler.AllocateLocal(matchCase.BindingVariable!);
            Emit(OpCode.StoreLocal, guardBindingLocal);

            // 评估守卫条件
            matchCase.GuardCondition.Accept(this);

            // 释放临时变量
            _compiler.FreeLocal(guardBindingLocal);

            // 修补跳过守卫的跳转
            PatchJump(skipGuardJump, GetCurrentPosition());
        }
    }

    /// <summary>
    /// 生成范围匹配代码
    /// </summary>
    private void GenerateRangeMatchCode(MatchCase matchCase, int matchValueLocal)
    {
        var rangePattern = matchCase.RangePattern!;

        // 加载 match 值
        Emit(OpCode.LoadLocal, matchValueLocal);

        // 计算范围起始值
        rangePattern.Start.Accept(this);

        // 计算范围结束值
        rangePattern.End.Accept(this);

        // 加载范围包含标志
        Emit(OpCode.LoadConst, _compiler.AddConstant(rangePattern.IncludeStart));
        Emit(OpCode.LoadConst, _compiler.AddConstant(rangePattern.IncludeEnd));

        // 调用原生函数进行范围检查
        // 参数顺序: value, start, end, includeStart, includeEnd
        Emit(OpCode.CallNative, new object[] { 5, "CheckRange" });
    }

    /// <summary>
    /// 生成元组解构匹配代码
    /// </summary>
    private void GenerateTupleMatchCode(MatchCase matchCase, int matchValueLocal)
    {
        var tuplePattern = matchCase.TuplePattern!;

        // 加载 match 值
        Emit(OpCode.LoadLocal, matchValueLocal);

        // 检查是否是元组类型
        Emit(OpCode.IsType, "tuple");

        // 如果不是元组，返回 false
        int notTupleJump = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1); // 占位

        // 展平元组并检查元素数量
        Emit(OpCode.LoadLocal, matchValueLocal);
        Emit(OpCode.CallNative, new object[] { 1, "FlattenTuple" });
        int flattenedLocal = _compiler.AllocateLocal();
        Emit(OpCode.StoreLocal, flattenedLocal);

        // 检查元素数量
        Emit(OpCode.LoadLocal, flattenedLocal);
        Emit(OpCode.CallNative, new object[] { 1, "GetCount" });
        Emit(OpCode.LoadConst, _compiler.AddConstant(tuplePattern.Elements.Count));
        Emit(OpCode.Equal);

        // 如果数量不匹配，返回 false
        int countMismatchJump = GetCurrentPosition();
        Emit(OpCode.JumpIfFalse, -1); // 占位

        // 逐个匹配元组元素
        bool allMatched = true;
        var elementMismatchJumps = new List<int>();

        for (int i = 0; i < tuplePattern.Elements.Count; i++)
        {
            var patternElement = tuplePattern.Elements[i];

            // 通配符跳过
            if (patternElement.IsWildcard)
                continue;

            // 获取元组元素
            Emit(OpCode.LoadLocal, flattenedLocal);
            Emit(OpCode.LoadConst, _compiler.AddConstant(i));
            Emit(OpCode.GetIndex);

            if (patternElement.Variable != null)
            {
                // 变量绑定：存储到局部变量
                int elementLocal = _compiler.AllocateLocal(patternElement.Variable);
                Emit(OpCode.StoreLocal, elementLocal);
            }
            else if (patternElement.Value != null)
            {
                // 值匹配：比较值
                patternElement.Value.Accept(this);
                Emit(OpCode.Equal);

                // 如果不匹配，跳转
                int mismatchJump = GetCurrentPosition();
                Emit(OpCode.JumpIfFalse, -1); // 占位
                elementMismatchJumps.Add(mismatchJump);
            }
        }

        // 所有元素都匹配，压入 true
        Emit(OpCode.LoadTrue);
        int successJump = GetCurrentPosition();
        Emit(OpCode.Jump, -1); // 占位

        // 修补不匹配的跳转（压入 false）
        PatchJump(notTupleJump, GetCurrentPosition());
        PatchJump(countMismatchJump, GetCurrentPosition());
        foreach (var jump in elementMismatchJumps)
        {
            PatchJump(jump, GetCurrentPosition());
        }
        Emit(OpCode.LoadFalse);

        // 修补成功跳转
        PatchJump(successJump, GetCurrentPosition());

        // 释放临时变量
        _compiler.FreeLocal(flattenedLocal);
    }


}
