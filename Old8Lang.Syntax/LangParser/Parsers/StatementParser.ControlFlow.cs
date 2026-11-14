using Old8Lang.AST.Expression;
using Old8Lang.AST.Statement;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// 语句解析器 - 控制流语句
/// </summary>
public partial class StatementParser
{
    public IfStatement ParseIfStatement()
    {
        var ifToken = CurrentToken;
        Expect(LangTokenType.If);
        var condition = expressionParser.ParseExpression();
        var ifBlock = ParseBlock();
        var oldIfs = new List<IfChild?>();
        while (CurrentToken.Type == LangTokenType.Elif)
        {
            var elifToken = CurrentToken;
            Expect(LangTokenType.Elif);
            var elifCondition = expressionParser.ParseExpression();
            var elifBlock = ParseBlock();
            var elifPosition = new SourcePosition(elifToken.Line, elifToken.Column);
            oldIfs.Add(new IfChild(elifCondition, elifBlock, elifPosition));
        }

        BlockStatement? elseBlock = null;
        if (CurrentToken.Type == LangTokenType.Else)
        {
            Expect(LangTokenType.Else);
            elseBlock = ParseBlock();
        }

        var ifPosition = new SourcePosition(ifToken.Line, ifToken.Column);
        return new IfStatement(new IfChild(condition, ifBlock, ifPosition), oldIfs, elseBlock, ifPosition);
    }

    // forStatement = "for" set "," expression "," statement block ;

    public ForStatement ParseForStatement()
    {
        var forToken = CurrentToken;
        Expect(LangTokenType.For);
        var set = ParseSet();
        Expect(LangTokenType.Comma);
        var condition = expressionParser.ParseExpression();
        Expect(LangTokenType.Comma);
        var statement = ParseStatement();
        var block = ParseBlock();
        var position = new SourcePosition(forToken.Line, forToken.Column);
        return new ForStatement(set, condition, statement, block, position);
    }

    // forInStatement = "for" identifier ( "," identifier )* "in" expression block ;

    public ForInStatement ParseForInStatement()
    {
        var forToken = CurrentToken;
        Expect(LangTokenType.For);

        // 解析多个标识符，支持 key, value 格式
        var identifiers = new List<LangId>();
        while (true)
        {
            var identifier = CurrentToken.Value;
            Expect(LangTokenType.Identifier);
            identifiers.Add(new LangId(identifier));

            if (CurrentToken.Type != LangTokenType.Comma)
                break;

            Expect(LangTokenType.Comma);
        }

        Expect(LangTokenType.In);
        var expression = expressionParser.ParseExpression();
        var block = ParseBlock();

        var position = new SourcePosition(forToken.Line, forToken.Column);

        // 如果只有一个标识符，直接使用；否则使用多个标识符
        if (identifiers.Count == 1)
        {
            return new ForInStatement(identifiers[0], expression, block, position);
        }

        // 创建一个复合标识符，将所有标识符存储起来
        return new ForInStatement(identifiers[0], expression, block, position, identifiers.Skip(1).ToList());
    }

    // asyncForInStatement = "async" "for" identifier ( "," identifier )* "in" expression block ;

    public AsyncForInStatement ParseAsyncForInStatement()
    {
        var asyncForToken = CurrentToken;
        Expect(LangTokenType.For);

        // 解析多个标识符，支持 key, value 格式
        var identifiers = new List<LangId>();
        while (true)
        {
            var identifier = CurrentToken.Value;
            Expect(LangTokenType.Identifier);
            identifiers.Add(new LangId(identifier));

            if (CurrentToken.Type != LangTokenType.Comma)
                break;

            Expect(LangTokenType.Comma);
        }

        Expect(LangTokenType.In);
        var expression = expressionParser.ParseExpression();
        var block = ParseBlock();

        var position = new SourcePosition(asyncForToken.Line, asyncForToken.Column);

        // 如果只有一个标识符，直接使用；否则使用多个标识符
        if (identifiers.Count == 1)
        {
            return new AsyncForInStatement(identifiers[0], expression, block, position);
        }

        // 创建一个复合标识符，将所有标识符存储起来
        return new AsyncForInStatement(identifiers[0], expression, block, position, identifiers.Skip(1).ToList());
    }

    // whileStatement = "while" expression block ;

    public WhileStatement ParseWhileStatement()
    {
        var whileToken = CurrentToken;
        Expect(LangTokenType.While);
        var condition = expressionParser.ParseExpression();
        var block = ParseBlock();
        var position = new SourcePosition(whileToken.Line, whileToken.Column);
        return new WhileStatement(condition, block, position);
    }

    // switchStatement = "switch" expression "{" caseBlock* ( "default" block )? "}" ;

    public SwitchStatement ParseSwitchStatement()
    {
        Expect(LangTokenType.Switch);
        var expression = expressionParser.ParseExpression();
        Expect(LangTokenType.LeftBrace);
        var cases = new List<CaseStatement>();
        while (CurrentToken.Type == LangTokenType.Case)
        {
            cases.Add(ParseCaseBlock());
        }

        BlockStatement? defaultBlock = null;
        if (CurrentToken.Type == LangTokenType.Default)
        {
            Expect(LangTokenType.Default);
            defaultBlock = ParseBlock();
        }

        Expect(LangTokenType.RightBrace);
        return new SwitchStatement(expression, cases, defaultBlock);
    }

    // caseBlock = "case" expression block ;

    public CaseStatement ParseCaseBlock()
    {
        var caseToken = CurrentToken;
        var position = new SourcePosition(caseToken.Line, caseToken.Column, tokenValue: caseToken.Value);
        Expect(LangTokenType.Case);
        var expression = expressionParser.ParseExpression();
        var block = ParseBlock();
        return new CaseStatement(expression, block, position);
    }
}
