using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser;
using Old8Lang.Syntax;

namespace Old8Lang.AsyncGeneration;

/// <summary>
/// 异步状态机表达式内 <c>await</c> 预降级器。
/// </summary>
/// <remarks>
/// CLR 在受保护区域挂起（<c>leave</c>）要求求值栈为空；当 <c>await</c> 位于复合表达式内部
/// （如 <c>1 + await f()</c>、<c>PrintLine(await f())</c>、集合/元组/字典/索引/切片/模板字面量等）时，
/// 求值栈上会残留中间值。本降级器在分析与 IL 生成前，按从左到右求值顺序把前序子表达式与
/// <c>AwaitExpression</c> 提升到语句前的临时变量赋值（<c>__async_await_tmp_N &lt;- ...</c>），
/// 并直接保留原始 <see cref="AwaitExpression"/> 实例引用，确保挂起点位于栈空位置且索引映射不变。
/// </remarks>
internal sealed class AsyncExpressionAwaitLowerer(bool isAsyncGenerator)
{
    private int _tempCounter;

    public BlockStatement LowerBlock(BlockStatement block)
    {
        if (!BlockNeedsLowering(block))
        {
            return block;
        }

        var statements = new List<IOldLangTree>(block.ImportStatements.Count + block.OtherStatements.Count);
        statements.AddRange(block.ImportStatements);

        foreach (var stmt in block.OtherStatements)
        {
            LowerStatementInto(stmt, statements);
        }

        return new BlockStatement(statements, block.Position, block.EndPosition);
    }

    private bool BlockNeedsLowering(BlockStatement block)
    {
        foreach (var stmt in block.OtherStatements)
        {
            if (StatementNeedsLowering(stmt))
            {
                return true;
            }
        }

        return false;
    }

    private bool StatementNeedsLowering(OldStatement? stmt)
    {
        switch (stmt)
        {
            case null:
            case DeferStatement:
            case FuncInit:
            case AsyncFuncInit:
            case ClassInit:
                return false;

            case BlockStatement block:
                return BlockNeedsLowering(block);

            case SetStatement setStmt:
                if (setStmt.LeftExpression is not null && ContainsAwait(setStmt.LeftExpression))
                {
                    return true;
                }

                return ExpressionNeedsLowering(setStmt.Value, allowTopLevelAwait: setStmt.LeftExpression is null);

            case FuncRunStatement funcRunStmt:
                return ExpressionNeedsLowering(funcRunStmt.Expression, allowTopLevelAwait: true);

            case ReturnStatement returnStmt:
                return ExpressionNeedsLowering(returnStmt.Expression, allowTopLevelAwait: true);

            case ThrowStatement throwStmt:
                return ExpressionNeedsLowering(throwStmt.Expression, allowTopLevelAwait: true);

            case YieldStatement yieldStmt:
                return isAsyncGenerator && ExpressionNeedsLowering(yieldStmt.YieldExpression, allowTopLevelAwait: true);

            case IfStatement ifStmt:
                foreach (var child in ifStmt.Children)
                {
                    if (ContainsAwait(child.Condition) || BlockNeedsLowering(child.Block))
                    {
                        return true;
                    }
                }

                return ifStmt.ElseBlock is not null && BlockNeedsLowering(ifStmt.ElseBlock);

            case WhileStatement whileStmt:
                return ContainsAwait(whileStmt.Condition) || StatementNeedsLowering(whileStmt.Block);

            case ForStatement forStmt:
                return StatementNeedsLowering(forStmt.Init) ||
                       ContainsAwait(forStmt.Condition) ||
                       StatementNeedsLowering(forStmt.Operation) ||
                       StatementNeedsLowering(forStmt.Block);

            case ForInStatement forInStmt:
                return ContainsAwait(forInStmt.Expression) || StatementNeedsLowering(forInStmt.Body);

            case AsyncForInStatement asyncForInStmt:
                return ExpressionNeedsLowering(asyncForInStmt.Expression, allowTopLevelAwait: true) ||
                       StatementNeedsLowering(asyncForInStmt.Body);

            case SwitchStatement switchStmt:
                if (ContainsAwait(switchStmt.SwitchExpression))
                {
                    return true;
                }

                foreach (var c in switchStmt.SwitchCaseList)
                {
                    if (StatementNeedsLowering(c.BlockStatement))
                    {
                        return true;
                    }
                }

                return switchStmt.DefaultBlockStatement is not null &&
                       BlockNeedsLowering(switchStmt.DefaultBlockStatement);

            case TryStatement tryStmt:
                if (BlockNeedsLowering(tryStmt.TryBlock))
                {
                    return true;
                }

                foreach (var (_, _, _, catchBlock) in tryStmt.CatchBlocks)
                {
                    if (BlockNeedsLowering(catchBlock))
                    {
                        return true;
                    }
                }

                return tryStmt.FinallyBlock is not null && BlockNeedsLowering(tryStmt.FinallyBlock);

            case UsingStatement usingStmt:
                return ExpressionNeedsLowering(usingStmt.ResourceExpression, allowTopLevelAwait: true) ||
                       BlockNeedsLowering(usingStmt.BlockStatement);

            default:
                return false;
        }
    }

    private static bool ExpressionNeedsLowering(LangExpression? expr, bool allowTopLevelAwait)
    {
        if (expr is null)
        {
            return false;
        }

        if (expr is AwaitExpression awaitExpr)
        {
            return !allowTopLevelAwait || ContainsAwait(awaitExpr.Expression);
        }

        return ContainsAwait(expr);
    }

    internal static bool ContainsAwait(LangExpression? expr)
    {
        switch (expr)
        {
            case null:
                return false;
            case AwaitExpression:
                return true;
            case Operation op:
                return ContainsAwait(op.Left) || ContainsAwait(op.Right);
            case CallExpression call:
                return call.Arguments.Any(ContainsAwait) ||
                       (call.NamedArguments?.Any(na => ContainsAwait(na.Value)) ?? false);
            case Instance inst:
                return inst.Ids.Any(ContainsAwait) ||
                       (inst.NamedArgs?.Any(na => ContainsAwait(na.Value)) ?? false);
            case FunctionCallExpression funcCall:
                return ContainsAwait(funcCall.FunctionExpression) ||
                       funcCall.Arguments.Any(ContainsAwait) ||
                       funcCall.NamedArguments.Any(na => ContainsAwait(na.Value));
            case TernaryExpression ternary:
                return ContainsAwait(ternary.Condition) ||
                       ContainsAwait(ternary.TrueExpression) ||
                       ContainsAwait(ternary.FalseExpression);
            case StringTemplateValue template:
                return template.ExpressionList.Any(ContainsAwait);
            case ListLiteralExpression list:
                return list.Elements.Any(ContainsAwait);
            case ArrayLiteralExpression arr:
                return arr.Elements.Any(ContainsAwait);
            case TupleLiteralExpression tuple:
                return tuple.Elements.Any(ContainsAwait);
            case DictionaryLiteralExpression dict:
                return dict.Entries.Any(e => ContainsAwait(e.Key) || ContainsAwait(e.Value));
            case RangeLiteralExpression range:
                return ContainsAwait(range.Start) || ContainsAwait(range.End);
            case SliceExpression slice:
                return ContainsAwait(slice.Start) || ContainsAwait(slice.End) || ContainsAwait(slice.Step);
            case IndexExpression idx:
                return ContainsAwait(idx.Key);
            case NestedIndexExpression nestedIdx:
                return ContainsAwait(nestedIdx.Target) || ContainsAwait(nestedIdx.Index);
            case NestedSliceExpression nestedSlice:
                return ContainsAwait(nestedSlice.Target) || ContainsAwait(nestedSlice.Start) ||
                       ContainsAwait(nestedSlice.End) || ContainsAwait(nestedSlice.Step);
            case ListLangValue listVal:
                return listVal.Values.Any(ContainsAwait);
            case ArrayLangValue arrVal:
                return arrVal.Values.Any(ContainsAwait);
            default:
                return false;
        }
    }

    private void LowerStatementInto(OldStatement stmt, List<IOldLangTree> output)
    {
        switch (stmt)
        {
            case BlockStatement block:
                output.Add(LowerBlock(block));
                return;

            case SetStatement setStmt:
            {
                if (setStmt.LeftExpression is not null)
                {
                    var loweredLeft = LowerExpression(setStmt.LeftExpression, output, hoistDirectAwait: false);
                    var loweredValue = LowerExpression(setStmt.Value, output, hoistDirectAwait: true);
                    output.Add(new SetStatement(loweredLeft, loweredValue, setStmt.Position));
                }
                else if (setStmt.Value is AwaitExpression directAwait && !ContainsAwait(directAwait.Expression))
                {
                    output.Add(setStmt);
                }
                else
                {
                    var loweredValue = LowerExpression(setStmt.Value, output, hoistDirectAwait: false);
                    output.Add(new SetStatement(setStmt.Id!, loweredValue, setStmt.Position));
                }

                return;
            }

            case FuncRunStatement { Expression: not null } funcRunStmt:
            {
                if (funcRunStmt.Expression is AwaitExpression directAwait && !ContainsAwait(directAwait.Expression))
                {
                    output.Add(funcRunStmt);
                }
                else
                {
                    var loweredExpr = LowerExpression(funcRunStmt.Expression, output, hoistDirectAwait: false);
                    output.Add(new FuncRunStatement(loweredExpr, funcRunStmt.Position));
                }

                return;
            }

            case ReturnStatement returnStmt:
            {
                if (returnStmt.Expression is null ||
                    (returnStmt.Expression is AwaitExpression directAwait && !ContainsAwait(directAwait.Expression)))
                {
                    output.Add(returnStmt);
                }
                else
                {
                    var loweredExpr = LowerExpression(returnStmt.Expression, output, hoistDirectAwait: false);
                    output.Add(new ReturnStatement(loweredExpr, returnStmt.Position));
                }

                return;
            }

            case ThrowStatement throwStmt:
            {
                if (throwStmt.Expression is AwaitExpression directAwait && !ContainsAwait(directAwait.Expression))
                {
                    output.Add(throwStmt);
                }
                else
                {
                    var loweredExpr = LowerExpression(throwStmt.Expression, output, hoistDirectAwait: false);
                    output.Add(new ThrowStatement(loweredExpr, throwStmt.Position));
                }

                return;
            }

            case YieldStatement yieldStmt when isAsyncGenerator:
            {
                if (yieldStmt.YieldExpression is AwaitExpression directAwait && !ContainsAwait(directAwait.Expression))
                {
                    output.Add(yieldStmt);
                }
                else
                {
                    var loweredExpr = LowerExpression(yieldStmt.YieldExpression, output, hoistDirectAwait: false);
                    output.Add(new YieldStatement(loweredExpr, yieldStmt.Position));
                }

                return;
            }

            case IfStatement ifStmt:
                LowerIfStatementInto(ifStmt, output);
                return;

            case WhileStatement whileStmt:
                LowerWhileStatementInto(whileStmt, output);
                return;

            case ForStatement forStmt:
            {
                if (!StatementNeedsLowering(forStmt))
                {
                    output.Add(forStmt);
                    return;
                }

                // 仅当体/初始化/增量可降级；条件含 await 时展开为 while 语义块
                if (ContainsAwait(forStmt.Condition) || StatementNeedsLowering(forStmt.Operation))
                {
                    var outerStmts = new List<IOldLangTree>();
                    LowerStatementInto(forStmt.Init, outerStmts);
                    var loopBody = new List<IOldLangTree>();
                    var loweredCond = LowerExpression(forStmt.Condition, loopBody, hoistDirectAwait: true);
                    var breakBlock = new BlockStatement([new BreakStatement(forStmt.Position)], forStmt.Position);
                    var negated = new Operation(null, LangTokenType.Exclamation, loweredCond, forStmt.Position);
                    loopBody.Add(new IfStatement(new IfChild(negated, breakBlock, forStmt.Position), [], null, forStmt.Position));
                    LowerStatementInto(forStmt.Block, loopBody);
                    LowerStatementInto(forStmt.Operation, loopBody);
                    var trueLiteral = new ScalarLiteralExpression(LiteralSyntax.Bool(true, forStmt.Position));
                    outerStmts.Add(new WhileStatement(trueLiteral, new BlockStatement(loopBody, forStmt.Position), forStmt.Position));
                    output.Add(new BlockStatement(outerStmts, forStmt.Position));
                }
                else
                {
                    var loweredBody = LowerBlock(forStmt.Block);
                    output.Add(new ForStatement(forStmt.Init, forStmt.Condition, forStmt.Operation, loweredBody, forStmt.Position));
                }

                return;
            }

            case ForInStatement forInStmt:
            {
                var loweredExpr = LowerExpression(forInStmt.Expression, output, hoistDirectAwait: true);
                var loweredBody = LowerSingleStatement(forInStmt.Body);
                var additionalIds = forInStmt.Ids.Count > 1 ? forInStmt.Ids.Skip(1).ToList() : null;
                output.Add(new ForInStatement(forInStmt.Id, loweredExpr, loweredBody, forInStmt.Position, additionalIds));
                return;
            }

            case AsyncForInStatement asyncForInStmt:
            {
                var loweredExpr = LowerExpression(asyncForInStmt.Expression, output, hoistDirectAwait: false);
                var loweredBody = LowerSingleStatement(asyncForInStmt.Body);
                var additionalIds = asyncForInStmt.Ids.Count > 1 ? asyncForInStmt.Ids.Skip(1).ToList() : null;
                output.Add(new AsyncForInStatement(asyncForInStmt.Id, loweredExpr, loweredBody, asyncForInStmt.Position, additionalIds));
                return;
            }

            case SwitchStatement switchStmt:
            {
                var loweredExpr = LowerExpression(switchStmt.SwitchExpression, output, hoistDirectAwait: true);
                var loweredCases = switchStmt.SwitchCaseList
                    .Select(c => new CaseStatement(
                        c.Expression,
                        LowerBlock(c.BlockStatement),
                        c.Position))
                    .ToList();
                var loweredDefault = switchStmt.DefaultBlockStatement is not null
                    ? LowerBlock(switchStmt.DefaultBlockStatement)
                    : null;
                output.Add(new SwitchStatement(loweredExpr, loweredCases, loweredDefault, switchStmt.Position));
                return;
            }

            case TryStatement tryStmt:
            {
                var loweredTry = LowerBlock(tryStmt.TryBlock);
                var loweredCatches = tryStmt.CatchBlocks
                    .Select(cb => (cb.exceptionType, cb.exceptionVar, cb.filter, LowerBlock(cb.catchBlock)))
                    .ToList();
                var loweredFinally = tryStmt.FinallyBlock is not null
                    ? LowerBlock(tryStmt.FinallyBlock)
                    : null;
                output.Add(new TryStatement(loweredTry, loweredCatches, loweredFinally, tryStmt.Position));
                return;
            }

            case UsingStatement usingStmt:
            {
                var loweredResource = LowerExpression(usingStmt.ResourceExpression, output, hoistDirectAwait: false);
                var loweredBody = LowerBlock(usingStmt.BlockStatement);
                output.Add(new UsingStatement(usingStmt.VariableName, loweredResource, loweredBody,
                    usingStmt.Position, usingStmt.VariablePosition));
                return;
            }

            default:
                output.Add(stmt);
                return;
        }
    }

    private OldStatement LowerSingleStatement(OldStatement stmt)
    {
        if (stmt is BlockStatement block)
        {
            return LowerBlock(block);
        }

        var stmts = new List<IOldLangTree>();
        LowerStatementInto(stmt, stmts);
        if (stmts.Count == 1 && stmts[0] is OldStatement single)
        {
            return single;
        }

        return new BlockStatement(stmts, stmt.Position);
    }

    private void LowerIfStatementInto(IfStatement ifStmt, List<IOldLangTree> output)
    {
        var children = ifStmt.Children.ToList();
        var loweredElse = ifStmt.ElseBlock is not null ? LowerBlock(ifStmt.ElseBlock) : null;

        // 若任意 elif 条件含 await，从右往左折叠为 nested else { <hoisted>; if ... }，保证短路求值语义。
        var hasElifAwait = children.Skip(1).Any(c => ContainsAwait(c.Condition));
        if (hasElifAwait)
        {
            BlockStatement? currentElse = loweredElse;
            for (var i = children.Count - 1; i >= 1; i--)
            {
                var branch = children[i];
                var branchStmts = new List<IOldLangTree>();
                var cond = LowerExpression(branch.Condition, branchStmts, hoistDirectAwait: true);
                branchStmts.Add(new IfStatement(
                    new IfChild(cond, LowerBlock(branch.Block), branch.Position),
                    [],
                    currentElse,
                    branch.Position));
                currentElse = new BlockStatement(branchStmts, branch.Position);
            }

            var first = children[0];
            var firstCond = LowerExpression(first.Condition, output, hoistDirectAwait: true);
            output.Add(new IfStatement(
                new IfChild(firstCond, LowerBlock(first.Block), first.Position),
                [],
                currentElse,
                ifStmt.Position));
            return;
        }

        var head = children[0];
        var loweredHeadCond = LowerExpression(head.Condition, output, hoistDirectAwait: true);
        var loweredHead = new IfChild(loweredHeadCond, LowerBlock(head.Block), head.Position);
        var loweredElifs = children.Skip(1)
            .Select(c => (IfChild?)new IfChild(c.Condition, LowerBlock(c.Block), c.Position))
            .ToList();
        output.Add(new IfStatement(loweredHead, loweredElifs, loweredElse, ifStmt.Position));
    }

    private void LowerWhileStatementInto(WhileStatement whileStmt, List<IOldLangTree> output)
    {
        if (!ContainsAwait(whileStmt.Condition))
        {
            output.Add(new WhileStatement(whileStmt.Condition, LowerSingleStatement(whileStmt.Block), whileStmt.Position));
            return;
        }

        var loopStmts = new List<IOldLangTree>();
        var loweredCond = LowerExpression(whileStmt.Condition, loopStmts, hoistDirectAwait: true);
        var breakBlock = new BlockStatement([new BreakStatement(whileStmt.Position)], whileStmt.Position);
        var negated = new Operation(null, LangTokenType.Exclamation, loweredCond, whileStmt.Position);
        loopStmts.Add(new IfStatement(new IfChild(negated, breakBlock, whileStmt.Position), [], null, whileStmt.Position));
        LowerStatementInto(whileStmt.Block, loopStmts);

        var trueLiteral = new ScalarLiteralExpression(LiteralSyntax.Bool(true, whileStmt.Position));
        output.Add(new WhileStatement(trueLiteral, new BlockStatement(loopStmts, whileStmt.Position), whileStmt.Position));
    }

    /// <summary>
    /// 将表达式中的 await 提升到 <paramref name="prefix"/> 语句序列，并返回不含栈非空 await 的等价表达式。
    /// </summary>
    private LangExpression LowerExpression(LangExpression expr, List<IOldLangTree> prefix, bool hoistDirectAwait)
    {
        if (!ContainsAwait(expr))
        {
            return expr;
        }

        switch (expr)
        {
            case AwaitExpression awaitExpr:
            {
                AwaitExpression targetAwait = awaitExpr;
                if (ContainsAwait(awaitExpr.Expression))
                {
                    var loweredInner = LowerExpression(awaitExpr.Expression, prefix, hoistDirectAwait: true);
                    targetAwait = new AwaitExpression(loweredInner, awaitExpr.Position);
                }

                if (!hoistDirectAwait)
                {
                    return targetAwait;
                }

                return HoistToTemp(targetAwait, prefix);
            }

            case Operation op:
                return LowerOperation(op, prefix);

            case CallExpression call:
            {
                var loweredArgs = LowerSequence(call.Arguments, prefix, forceHoistAllAwaits: true);
                List<NamedArgument>? loweredNamed = null;
                if (call.NamedArguments is { Count: > 0 })
                {
                    var namedValues = LowerSequence(
                        call.NamedArguments.Select(n => n.Value).ToList(),
                        prefix,
                        forceHoistAllAwaits: true);
                    loweredNamed = [];
                    for (var i = 0; i < call.NamedArguments.Count; i++)
                    {
                        var orig = call.NamedArguments[i];
                        loweredNamed.Add(new NamedArgument(orig.Name, namedValues[i], orig.Position));
                    }
                }

                return new CallExpression(call.Id, loweredArgs, loweredNamed, call.Position);
            }

            case Instance inst:
            {
                var loweredArgs = LowerSequence(inst.Ids, prefix, forceHoistAllAwaits: true);
                List<NamedArgument>? loweredNamed = null;
                if (inst.NamedArgs is { Count: > 0 })
                {
                    var namedValues = LowerSequence(
                        inst.NamedArgs.Select(n => n.Value).ToList(),
                        prefix,
                        forceHoistAllAwaits: true);
                    loweredNamed = [];
                    for (var i = 0; i < inst.NamedArgs.Count; i++)
                    {
                        var orig = inst.NamedArgs[i];
                        loweredNamed.Add(new NamedArgument(orig.Name, namedValues[i], orig.Position));
                    }
                }

                return new Instance(inst.Id, loweredArgs, loweredNamed ?? [], inst.Position);
            }

            case FunctionCallExpression funcCall:
            {
                var argsHaveAwait = funcCall.Arguments.Any(ContainsAwait) ||
                                    funcCall.NamedArguments.Any(n => ContainsAwait(n.Value));
                var loweredTarget = LowerExpression(funcCall.FunctionExpression, prefix, hoistDirectAwait: true);
                if (argsHaveAwait && !IsPureIdentifier(loweredTarget))
                {
                    loweredTarget = HoistToTemp(loweredTarget, prefix);
                }

                var loweredArgs = LowerSequence(funcCall.Arguments, prefix, forceHoistAllAwaits: true);
                var loweredNamed = new List<NamedArgument>();
                if (funcCall.NamedArguments.Count > 0)
                {
                    var namedValues = LowerSequence(
                        funcCall.NamedArguments.Select(n => n.Value).ToList(),
                        prefix,
                        forceHoistAllAwaits: true);
                    for (var i = 0; i < funcCall.NamedArguments.Count; i++)
                    {
                        var orig = funcCall.NamedArguments[i];
                        loweredNamed.Add(new NamedArgument(orig.Name, namedValues[i], orig.Position));
                    }
                }

                return new FunctionCallExpression(loweredTarget, loweredArgs, loweredNamed, funcCall.Position);
            }

            case TernaryExpression ternary:
            {
                var cond = LowerExpression(ternary.Condition, prefix, hoistDirectAwait: true);
                if (!ContainsAwait(ternary.TrueExpression) && !ContainsAwait(ternary.FalseExpression))
                {
                    return new TernaryExpression(cond, ternary.TrueExpression, ternary.FalseExpression, ternary.Position);
                }

                var resultId = CreateTempId(ternary.Position);
                var trueStmts = new List<IOldLangTree>();
                var loweredTrue = LowerExpression(ternary.TrueExpression, trueStmts, hoistDirectAwait: false);
                trueStmts.Add(new SetStatement(resultId, loweredTrue, ternary.TrueExpression.Position));

                var falseStmts = new List<IOldLangTree>();
                var loweredFalse = LowerExpression(ternary.FalseExpression, falseStmts, hoistDirectAwait: false);
                falseStmts.Add(new SetStatement(resultId, loweredFalse, ternary.FalseExpression.Position));

                prefix.Add(new IfStatement(
                    new IfChild(cond, new BlockStatement(trueStmts, ternary.Position), ternary.Position),
                    [],
                    new BlockStatement(falseStmts, ternary.Position),
                    ternary.Position));
                return resultId;
            }

            case StringTemplateValue template:
            {
                var parts = LowerSequence(template.ExpressionList, prefix, forceHoistAllAwaits: true);
                return new StringTemplateValue(parts, template.Position);
            }

            case ListLiteralExpression list:
            {
                var items = LowerSequence(list.Elements, prefix, forceHoistAllAwaits: true);
                return new ListLiteralExpression(items, list.Position);
            }

            case ArrayLiteralExpression arr:
            {
                var items = LowerSequence(arr.Elements, prefix, forceHoistAllAwaits: true);
                return new ArrayLiteralExpression(items, arr.Position);
            }

            case TupleLiteralExpression tuple:
            {
                var items = LowerSequence(tuple.Elements, prefix, forceHoistAllAwaits: true);
                return new TupleLiteralExpression(items, tuple.FieldNames, tuple.Position);
            }

            case DictionaryLiteralExpression dict:
            {
                var flat = new List<LangExpression>(dict.Entries.Count * 2);
                foreach (var entry in dict.Entries)
                {
                    flat.Add(entry.Key);
                    flat.Add(entry.Value);
                }

                var loweredFlat = LowerSequence(flat, prefix, forceHoistAllAwaits: true);
                var entries = new List<KeyValuePair<LangExpression, LangExpression>>(dict.Entries.Count);
                for (var i = 0; i < dict.Entries.Count; i++)
                {
                    entries.Add(new KeyValuePair<LangExpression, LangExpression>(loweredFlat[i * 2], loweredFlat[i * 2 + 1]));
                }

                return new DictionaryLiteralExpression(entries, dict.Position);
            }

            case RangeLiteralExpression range:
            {
                var items = new List<LangExpression>();
                if (range.Start is not null) items.Add(range.Start);
                if (range.End is not null) items.Add(range.End);
                var lowered = LowerSequence(items, prefix, forceHoistAllAwaits: true);
                var idx = 0;
                var start = range.Start is not null ? lowered[idx++] : null;
                var end = range.End is not null ? lowered[idx] : null;
                return new RangeLiteralExpression(start, end, range.Position, range.IncludeStart, range.IncludeEnd);
            }

            case SliceExpression slice:
            {
                var items = new List<LangExpression>();
                if (slice.Start is not null) items.Add(slice.Start);
                if (slice.End is not null) items.Add(slice.End);
                if (slice.Step is not null) items.Add(slice.Step);
                var lowered = LowerSequence(items, prefix, forceHoistAllAwaits: true);
                var idx = 0;
                var start = slice.Start is not null ? lowered[idx++] : null;
                var end = slice.End is not null ? lowered[idx++] : null;
                var step = slice.Step is not null ? lowered[idx] : null;
                return new SliceExpression(slice.Id, start, end, step);
            }

            case IndexExpression indexExpr:
            {
                var key = LowerExpression(indexExpr.Key, prefix, hoistDirectAwait: true);
                return new IndexExpression(indexExpr.Target, key, indexExpr.Position);
            }

            case NestedIndexExpression nestedIdx:
            {
                var parts = LowerSequence([nestedIdx.Target, nestedIdx.Index], prefix, forceHoistAllAwaits: true);
                return new NestedIndexExpression(parts[0], parts[1], nestedIdx.Position);
            }

            case NestedSliceExpression nestedSlice:
            {
                var items = new List<LangExpression> { nestedSlice.Target, nestedSlice.Start };
                if (nestedSlice.End is not null) items.Add(nestedSlice.End);
                if (nestedSlice.Step is not null) items.Add(nestedSlice.Step);
                var lowered = LowerSequence(items, prefix, forceHoistAllAwaits: true);
                var idx = 2;
                var end = nestedSlice.End is not null ? lowered[idx++] : null;
                var step = nestedSlice.Step is not null ? lowered[idx] : null;
                return new NestedSliceExpression(lowered[0], lowered[1], end, step, nestedSlice.Position);
            }

            case ListLangValue listVal:
            {
                var items = LowerSequence(listVal.Values, prefix, forceHoistAllAwaits: true);
                return new ListLangValue(items, position: listVal.Position);
            }

            case ArrayLangValue arrVal:
            {
                var items = LowerSequence(arrVal.Values, prefix, forceHoistAllAwaits: true);
                return new ArrayLangValue(items, position: arrVal.Position);
            }

            default:
                return expr;
        }
    }

    private LangExpression LowerOperation(Operation op, List<IOldLangTree> prefix)
    {
        // 成员访问 / 方法调用：右侧是成员名或方法调用节点，不能把右侧整体提升为独立变量
        if (op.Opera == LangTokenType.Dot)
        {
            var rightHasAwait = ContainsAwait(op.Right);
            var loweredLeft = op.Left is not null
                ? LowerExpression(op.Left, prefix, hoistDirectAwait: true)
                : null;
            if (rightHasAwait && loweredLeft is not null && !IsPureIdentifier(loweredLeft))
            {
                loweredLeft = HoistToTemp(loweredLeft, prefix);
            }

            var loweredRight = op.Right is not null
                ? LowerExpression(op.Right, prefix, hoistDirectAwait: false)
                : null;
            return new Operation(loweredLeft, op.Opera, loweredRight, op.Position);
        }

        // 短路逻辑运算符：右侧含 await 时展开为条件语句，避免误求值右侧
        if ((op.Opera == LangTokenType.And || op.Opera == LangTokenType.Or) &&
            op.Left is not null && op.Right is not null && ContainsAwait(op.Right))
        {
            var resultId = CreateTempId(op.Position);
            var loweredLeft = LowerExpression(op.Left, prefix, hoistDirectAwait: true);
            prefix.Add(new SetStatement(resultId, loweredLeft, op.Left.Position));

            var rightStmts = new List<IOldLangTree>();
            var loweredRight = LowerExpression(op.Right, rightStmts, hoistDirectAwait: false);
            rightStmts.Add(new SetStatement(resultId, loweredRight, op.Right.Position));

            LangExpression cond = op.Opera == LangTokenType.And
                ? resultId
                : new Operation(null, LangTokenType.Exclamation, resultId, op.Position);
            prefix.Add(new IfStatement(
                new IfChild(cond, new BlockStatement(rightStmts, op.Position), op.Position),
                [],
                null,
                op.Position));
            return resultId;
        }

        // 一元运算符（Left 为 null）
        if (op.Left is null)
        {
            var loweredRight = op.Right is not null
                ? LowerExpression(op.Right, prefix, hoistDirectAwait: true)
                : null;
            return new Operation(null, op.Opera, loweredRight, op.Position);
        }

        // 二元运算符：若右侧含 await，则先把左侧求值结果保存到临时变量，再求值右侧 await
        var rightContainsAwait = ContainsAwait(op.Right);
        var leftLowered = LowerExpression(op.Left, prefix, hoistDirectAwait: true);
        if (rightContainsAwait && !IsPureIdentifier(leftLowered))
        {
            leftLowered = HoistToTemp(leftLowered, prefix);
        }

        var rightLowered = op.Right is not null
            ? LowerExpression(op.Right, prefix, hoistDirectAwait: true)
            : null;
        return new Operation(leftLowered, op.Opera, rightLowered, op.Position);
    }

    private List<LangExpression> LowerSequence(
        IReadOnlyList<LangExpression> items,
        List<IOldLangTree> prefix,
        bool forceHoistAllAwaits)
    {
        var lastAwaitIndex = -1;
        for (var i = items.Count - 1; i >= 0; i--)
        {
            if (ContainsAwait(items[i]))
            {
                lastAwaitIndex = i;
                break;
            }
        }

        var result = new List<LangExpression>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (i <= lastAwaitIndex)
            {
                var lowered = LowerExpression(item, prefix, hoistDirectAwait: forceHoistAllAwaits || i < lastAwaitIndex);
                // 若后续项仍含 await 且当前项不是纯变量/字面量，提前求值保存到临时变量以保持从左到右求值顺序
                if (i < lastAwaitIndex && !IsPureIdentifier(lowered))
                {
                    lowered = HoistToTemp(lowered, prefix);
                }

                result.Add(lowered);
            }
            else
            {
                result.Add(item);
            }
        }

        return result;
    }

    private LangId HoistToTemp(LangExpression expression, List<IOldLangTree> prefix)
    {
        var tempId = CreateTempId(expression.Position);
        prefix.Add(new SetStatement(tempId, expression, expression.Position));
        return tempId;
    }

    private LangId CreateTempId(SourcePosition position)
    {
        return new LangId($"<>__async_tmp_{_tempCounter++}", position: position);
    }

    private static bool IsPureIdentifier(LangExpression expr)
    {
        return expr is LangId or ThisExpression or SuperExpression or ScalarLiteralExpression;
    }
}
