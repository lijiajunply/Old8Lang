using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// 语句解析器 - 简单语句
/// </summary>
public partial class StatementParser
{
    public ReturnStatement ParseReturnStatement()
    {
        var returnToken = CurrentToken;
        var position = new SourcePosition(returnToken.Line, returnToken.Column, tokenValue: returnToken.Value);
        Expect(LangTokenType.Return);

        // 检查是否有返回值表达式
        // 如果下一个 token 是语句结束符（右大括号、换行符等），则没有返回值
        if (CurrentToken.Type == LangTokenType.RightBrace ||
            CurrentToken.Type == LangTokenType.EndOfFile ||
            CurrentIndex >= Tokens.Count)
        {
            // void 返回，使用 VoidLangValue
            return new ReturnStatement(new VoidLangValue(position), position);
        }

        var expression = expressionParser.ParseExpression();
        return new ReturnStatement(expression, position);
    }


    public BreakStatement ParseBreakStatement()
    {
        var breakToken = CurrentToken;
        var position = new SourcePosition(breakToken.Line, breakToken.Column, tokenValue: breakToken.Value);
        Expect(LangTokenType.Break);
        return new BreakStatement(position);
    }


    public ContinueStatement ParseContinueStatement()
    {
        var continueToken = CurrentToken;
        var position = new SourcePosition(continueToken.Line, continueToken.Column, tokenValue: continueToken.Value);
        Expect(LangTokenType.Continue);
        return new ContinueStatement(position);
    }

    /// <summary>
    /// 解析yield语句：yield expression
    /// </summary>

    public YieldStatement ParseYieldStatement()
    {
        var yieldToken = CurrentToken;
        var position = new SourcePosition(yieldToken.Line, yieldToken.Column, tokenValue: yieldToken.Value);
        Expect(LangTokenType.Yield);

        // 解析yield表达式
        var expression = expressionParser.ParseExpression();
        return new YieldStatement(expression, position);
    }


    public ThrowStatement ParseThrowStatement()
    {
        var throwToken = CurrentToken;
        var position = new SourcePosition(throwToken.Line, throwToken.Column, tokenValue: throwToken.Value);
        Expect(LangTokenType.Throw);
        var expression = expressionParser.ParseExpression();
        return new ThrowStatement(expression, position);
    }

    // lrBlock = "(" statement ")" ;

    public OldStatement ParseLrBlock()
    {
        Expect(LangTokenType.LeftParen);
        var statement = ParseStatement();
        Expect(LangTokenType.RightParen);
        return statement;
    }

    // declaration = identifier ":" type "<-" expression | identifier "<-" expression | memberAccess ":" type "<-" expression | memberAccess "<-" expression ;

    public OldStatement ParseIdentifierLeftParen()
    {
        // 先保存当前位置
        var savedIndex = CurrentIndex;

        try
        {
            // 检查是否是函数定义：
            // 1. 只有当标识符前面没有赋值符号时，才可能是函数定义
            // 2. 如果前面有赋值符号（<-），则是函数调用
            // 3. 只有当标识符后面跟着左括号，并且接下来有箭头或左大括号时，才是函数定义
            var isAfterAssignment = false;
            if (savedIndex > 0)
            {
                var prevToken = Tokens[savedIndex - 1];
                isAfterAssignment = prevToken.Type == LangTokenType.Assignment;
            }

            // 如果是函数调用，直接解析
            if (isAfterAssignment)
            {
                return ParseFuncRunStatement();
            }

            // 解析标识符和左括号
            functionParser.ParseIdentifier();
            Expect(LangTokenType.LeftParen);
            functionParser.ParseIdList();
            Expect(LangTokenType.RightParen);

            // 保存当前位置，用于回滚
            // var afterParamsIndex = CurrentIndex;

            // 检查是否是函数定义：
            // - 箭头函数：标识符( params ) -> block
            // - 常规函数：标识符( params ) block（block必须是左大括号开始）
            var isFuncDeclaration = false;
            if (CurrentToken.Type == LangTokenType.Arrow)
            {
                // 箭头函数定义
                isFuncDeclaration = true;
            }
            else if (CurrentToken.Type == LangTokenType.LeftBrace)
            {
                // 常规函数定义，带有左大括号
                isFuncDeclaration = true;
            }

            if (isFuncDeclaration)
            {
                // 回滚到函数开始位置，完整解析函数定义
                CurrentIndex = savedIndex;
                return functionParser.ParseFuncDeclaration();
            }

            // 否则是函数调用，回滚到开始位置，解析为函数调用
            CurrentIndex = savedIndex;
            return ParseFuncRunStatement();
        }
        catch
        {
            // 解析失败，回滚，尝试解析为函数调用
            CurrentIndex = savedIndex;
            return ParseFuncRunStatement();
        }
    }


    public FuncRunStatement ParseFuncRunStatement()
    {
        var funcName = CurrentToken.Value;
        var position = new SourcePosition(CurrentToken.Line, CurrentToken.Column, tokenValue: CurrentToken.Value);
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.LeftParen);
        functionParser.ParseArgList(out var positionalArgs, out var namedArgs);
        Expect(LangTokenType.RightParen);

        // 创建初始的函数调用表达式
        LangExpression expr = new Instance(new LangId(funcName), positionalArgs, namedArgs);

        // 处理链式调用：spawn(worker).Start() 或 func().method().property
        expr = expressionParser.ParseDotExpr(expr);

        return new FuncRunStatement(expr, position);
    }
}
