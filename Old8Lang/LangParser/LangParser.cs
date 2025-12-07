using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.LangParser;

public class LangParser(List<LangToken> tokens)
{
    #region 基础操作

    private int CurrentIndex;

    private LangToken CurrentToken => CurrentIndex >= tokens.Count
        ? new LangToken("", LangTokenType.EndOfFile, CurrentIndex)
        : tokens[CurrentIndex];

    private void Expect(LangTokenType type)
    {
        if (CurrentToken.Type == type)
        {
            CurrentIndex++;
        }
        else
            throw new Old8Lang.Error.SyntaxError(
                CurrentToken.Value,
                CurrentToken.Line,
                CurrentToken.Column,
                $"语法错误：期望 {type}，但得到了 {CurrentToken.Type}");
    }

    private LangToken Peek(int offset = 1)
    {
        if (CurrentIndex + offset >= tokens.Count)
        {
            return new LangToken("", LangTokenType.EndOfFile, CurrentIndex + offset);
        }

        return tokens[CurrentIndex + offset];
    }

    #endregion

    #region Root

    // root = statement* ;
    public BlockStatement ParseProgram()
    {
        var statements = new List<IOldLangTree>();
        while (CurrentIndex < tokens.Count)
        {
            statements.Add(ParseStatement());
        }

        return new BlockStatement(statements);
    }

    #endregion

    #region Statement

    // statement = lrBlock
    //           | declaration
    //           | assignment
    //           | expressionStatement
    //           | ifStatement
    //           | forStatement
    //           | whileStatement
    //           | forInStatement
    //           | switchStatement
    //           | funcDeclaration
    //           | classDeclaration
    //           | funcRunStatement
    //           | classFuncRunStatement
    //           | importStatement
    //           | nativeStatement
    //           | nativeStatic
    //           | nativeClass
    //           | plusPlus
    //           | minusMinus ;
    private OldStatement ParseStatement()
    {
        return CurrentToken.Type switch
        {
            LangTokenType.LeftParen => ParseLrBlock(),
            LangTokenType.If => ParseIfStatement(),
            LangTokenType.For when Peek().Type == LangTokenType.Identifier &&
                                   Peek(2).Type == LangTokenType.In => ParseForInStatement(),
            LangTokenType.For when Peek().Type == LangTokenType.Identifier => ParseForStatement(),
            LangTokenType.While => ParseWhileStatement(),
            LangTokenType.Switch => ParseSwitchStatement(),
            LangTokenType.Func when Peek().Type == LangTokenType.Identifier &&
                                    Peek(2).Type == LangTokenType.LeftParen => ParseFuncDeclaration(),
            LangTokenType.Return => ParseReturnStatement(),
            // 只有当标识符后面跟着 Assignment 标记时，才是声明语句
            LangTokenType.Identifier when Peek().Type == LangTokenType.Assignment => ParseSet(),
            // Lambda
            LangTokenType.Identifier when Peek().Type == LangTokenType.Arrow => ParseFuncDeclaration(),
            LangTokenType.Identifier when Peek().Type == LangTokenType.Colon && Peek(2).Type == LangTokenType.Identifier
                => ParseFuncDeclaration(),
            // 类型实例调用属性/方法
            LangTokenType.Identifier when Peek().Type == LangTokenType.Dot => ParseClassFuncRunStatement(),
            // 先尝试解析为函数定义，再解析为函数调用
            LangTokenType.Identifier when Peek().Type == LangTokenType.LeftParen => ParseIdentifierLeftParen(),
            LangTokenType.Identifier when Peek().Type == LangTokenType.PlusPlus => ParsePlusPlus(),
            LangTokenType.Identifier when Peek().Type == LangTokenType.MinusMinus => ParseMinusMinus(),
            LangTokenType.Class => ParseClassDeclaration(),
            LangTokenType.Import => ParseImportStatement(),
            // 先处理更具体的 nativeStatic 和 nativeClass，再处理更通用的 nativeStatement
            LangTokenType.LeftBracket when Peek().Type == LangTokenType.Import &&
                                           Peek(2).Type == LangTokenType.String &&
                                           Peek(3).Type == LangTokenType.Identifier &&
                                           Peek(4).Type == LangTokenType.RightBracket &&
                                           Peek(5).Type == LangTokenType.Arrow && Peek(6).Type == LangTokenType.String
                => ParseNativeStatic(),
            LangTokenType.LeftBracket when Peek().Type == LangTokenType.Import &&
                                           Peek(2).Type == LangTokenType.String &&
                                           Peek(3).Type == LangTokenType.Identifier &&
                                           Peek(4).Type == LangTokenType.RightBracket => ParseNativeClass(),
            LangTokenType.LeftBracket when Peek().Type == LangTokenType.Import => ParseNativeStatement(),
            _ => throw new Exception($"语法有误。在解析到ParseStatement时出现问题。在{CurrentToken.Line}:{CurrentToken.Column}")
        };
    }

    /// <summary>
    /// 处理标识符后面跟着左括号的情况，可能是函数定义或函数调用
    /// </summary>
    private OldStatement ParseIdentifierLeftParen()
    {
        // 先保存当前位置
        var savedIndex = CurrentIndex;

        try
        {
            // 尝试解析为函数定义
            return ParseFuncDeclaration();
        }
        catch
        {
            // 解析失败，回滚，尝试解析为函数调用
            CurrentIndex = savedIndex;
            return ParseFuncRunStatement();
        }
    }

    private ReturnStatement ParseReturnStatement()
    {
        var returnToken = CurrentToken;
        var position = new SourcePosition(returnToken.Line, returnToken.Column, tokenValue: returnToken.Value);
        Expect(LangTokenType.Return);
        var expression = ParseExpression();
        return new ReturnStatement(expression, position);
    }

    // lrBlock = "(" statement ")" ;
    private OldStatement ParseLrBlock()
    {
        Expect(LangTokenType.LeftParen);
        var statement = ParseStatement();
        Expect(LangTokenType.RightParen);
        return statement;
    }

    // declaration = identifier "<-" expression ;
    private SetStatement ParseSet()
    {
        var identifierToken = CurrentToken;
        var position = new SourcePosition(identifierToken.Line, identifierToken.Column, tokenValue: identifierToken.Value);
        var identifier = identifierToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.Assignment);
        var expression = ParseExpression();
        return new SetStatement(new OldId(identifier, "", position), expression, position);
    }

    // ifStatement = "if" expression block ( "elif" expression block )* ( "else" block )? ;
    private IfStatement ParseIfStatement()
    {
        var ifToken = CurrentToken;
        Expect(LangTokenType.If);
        var condition = ParseExpression();
        var ifBlock = ParseBlock();
        var oldIfs = new List<OldIf?>();
        while (CurrentToken.Type == LangTokenType.Elif)
        {
            var elifToken = CurrentToken;
            Expect(LangTokenType.Elif);
            var elifCondition = ParseExpression();
            var elifBlock = ParseBlock();
            var elifPosition = new Old8Lang.SourcePosition(elifToken.Line, elifToken.Column);
            oldIfs.Add(new OldIf(elifCondition, elifBlock, elifPosition));
        }

        BlockStatement? elseBlock = null;
        if (CurrentToken.Type == LangTokenType.Else)
        {
            Expect(LangTokenType.Else);
            elseBlock = ParseBlock();
        }

        var ifPosition = new Old8Lang.SourcePosition(ifToken.Line, ifToken.Column);
        return new IfStatement(new OldIf(condition, ifBlock, ifPosition), oldIfs, elseBlock, ifPosition);
    }

    // forStatement = "for" set "," expression "," statement block ;
    private ForStatement ParseForStatement()
    {
        var forToken = CurrentToken;
        Expect(LangTokenType.For);
        var set = ParseSet();
        Expect(LangTokenType.Comma);
        var condition = ParseExpression();
        Expect(LangTokenType.Comma);
        var statement = ParseStatement();
        var block = ParseBlock();
        var position = new Old8Lang.SourcePosition(forToken.Line, forToken.Column);
        return new ForStatement(set, condition, statement, block, position);
    }

    // forInStatement = "for" identifier "in" expression block ;
    private ForInStatement ParseForInStatement()
    {
        var forToken = CurrentToken;
        Expect(LangTokenType.For);
        var identifier = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.In);
        var expression = ParseExpression();
        var block = ParseBlock();
        
        var position = new Old8Lang.SourcePosition(forToken.Line, forToken.Column);
        return new ForInStatement(new OldId(identifier), expression, block, position);
    }

    // whileStatement = "while" expression block ;
    private WhileStatement ParseWhileStatement()
    {
        var whileToken = CurrentToken;
        Expect(LangTokenType.While);
        var condition = ParseExpression();
        var block = ParseBlock();
        var position = new Old8Lang.SourcePosition(whileToken.Line, whileToken.Column);
        return new WhileStatement(condition, block, position);
    }

    // switchStatement = "switch" expression "{" caseBlock* ( "default" block )? "}" ;
    private SwitchStatement ParseSwitchStatement()
    {
        Expect(LangTokenType.Switch);
        var expression = ParseExpression();
        Expect(LangTokenType.LeftBrace);
        var cases = new List<OldCase>();
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
    private OldCase ParseCaseBlock()
    {
        var caseToken = CurrentToken;
        var position = new SourcePosition(caseToken.Line, caseToken.Column, tokenValue: caseToken.Value);
        Expect(LangTokenType.Case);
        var expression = ParseExpression();
        var block = ParseBlock();
        return new OldCase(expression, block, position);
    }

    /// <summary>
    /// funcDeclaration = ( identifier | "func" identifier ) "(" idList? ")"  "->" block  ;
    /// </summary>
    /// <returns>声明函数</returns>
    private FuncInit ParseFuncDeclaration()
    {
        if (CurrentToken.Type == LangTokenType.Func)
        {
            Expect(LangTokenType.Func);
        }

        var funcName = ParseIdentifier();

        Expect(LangTokenType.LeftParen);
        var parameters = ParseIdList();
        Expect(LangTokenType.RightParen);
        if (CurrentToken.Type == LangTokenType.Arrow)
        {
            Expect(LangTokenType.Arrow);
        }

        var block = ParseBlock();

        return new FuncInit(new FuncValue(funcName, parameters, block));
    }

    /// <summary>
    /// classDeclaration = "class" identifier classBlock ;
    /// </summary>
    /// <returns>声明类</returns>
    private ClassInit ParseClassDeclaration()
    {
        Expect(LangTokenType.Class);
        var className = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        var classBlock = ParseClassBlock();
        return new ClassInit(new AnyValue(new OldId(className), classBlock.ToAnyData()));
    }

    /// <summary>
    /// classBlock = "{" [set | funcDeclaration]* "}" ;
    /// </summary>
    /// <returns>类块</returns>
    /// <exception cref="Exception">期望声明或函数声明</exception>
    private BlockStatement ParseClassBlock()
    {
        Expect(LangTokenType.LeftBrace);
        var statements = new List<IOldLangTree>();
        while (CurrentToken.Type != LangTokenType.RightBrace)
        {
            statements.Add(CurrentToken.Type switch
            {
                LangTokenType.Assignment => ParseSet(),
                LangTokenType.Func => ParseFuncDeclaration(),
                LangTokenType.Identifier when Peek().Type == LangTokenType.LeftParen => ParseFuncDeclaration(),
                // 支持类内部的声明语句：identifier "<-" expression
                LangTokenType.Identifier => ParseSet(),
                _ => throw new Exception($"语法错误：期望声明或函数声明，但得到了 {CurrentToken.Type}")
            });
        }

        Expect(LangTokenType.RightBrace);
        return new BlockStatement(statements);
    }

    /// <summary>
    /// funcRunStatement = identifier "(" argList? ")" ;
    /// </summary>
    /// <returns>函数调用</returns>
    private FuncRunStatement ParseFuncRunStatement()
    {
        var funcName = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.LeftParen);
        var arguments = ParseArgList();
        Expect(LangTokenType.RightParen);
        return new FuncRunStatement(new Instance(new OldId(funcName), arguments));
    }

    /// <summary>
    /// classFuncRunStatement = identifier "." identifier "(" argList? ")" ;
    /// </summary>
    /// <returns>类方法调用</returns>
    private FuncRunStatement ParseClassFuncRunStatement()
    {
        var className = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.Dot);
        var funcName = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.LeftParen);
        var arguments = ParseArgList();
        Expect(LangTokenType.RightParen);
        return new FuncRunStatement(new Operation(new OldId(className), OperationType.CONCAT,
            new Instance(new OldId(funcName), arguments)));
    }

    /// <summary>
    /// importStatement = "import" identifier ;
    /// </summary>
    /// <returns>引入模块</returns>
    private ImportStatement ParseImportStatement()
    {
        Expect(LangTokenType.Import);
        var moduleName = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        return new ImportStatement(moduleName);
    }

    /// <summary>
    /// nativeStatement = "[" "import" STRING identifier identifier identifier? "]" ;
    /// </summary>
    /// <returns>引入原生方法</returns>
    private NativeStatement ParseNativeStatement()
    {
        Expect(LangTokenType.LeftBracket);
        Expect(LangTokenType.Import);
        var dllName = CurrentToken.Value;
        Expect(LangTokenType.String);
        var className = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        var methodName = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        var alias = "";
        if (CurrentToken.Type == LangTokenType.Identifier)
        {
            alias = CurrentToken.Value;
            Expect(LangTokenType.Identifier);
        }

        Expect(LangTokenType.RightBracket);
        return new NativeStatement(dllName, className, methodName, alias);
    }

    /// <summary>
    /// nativeStatic = "[" "import" STRING identifier "]" "->" STRING ;
    /// </summary>
    /// <returns>引入原生静态类</returns>
    private NativeStatement ParseNativeStatic()
    {
        Expect(LangTokenType.LeftBracket);
        Expect(LangTokenType.Import);
        var dllName = CurrentToken.Value;
        Expect(LangTokenType.String);
        var className = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.RightBracket);
        Expect(LangTokenType.Arrow);
        var methodName = CurrentToken.Value;
        Expect(LangTokenType.String);
        return new NativeStatement(dllName, className, methodName);
    }

    /// <summary>
    ///  nativeClass = "[" "import" STRING identifier "]" ;
    /// </summary>
    /// <returns>引入原生类</returns>
    private NativeStatement ParseNativeClass()
    {
        Expect(LangTokenType.LeftBracket);
        Expect(LangTokenType.Import);
        var dllName = CurrentToken.Value;
        Expect(LangTokenType.String);
        var className = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.RightBracket);
        return new NativeStatement(dllName, className);
    }

    /// <summary>
    /// plusPlus = identifier "++"
    /// </summary>
    /// <returns>i++运算</returns>
    private SetStatement ParsePlusPlus()
    {
        var identifier = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.PlusPlus);
        return new SetStatement(new OldId(identifier),
            new Operation(new OldId(identifier), OperationType.PLUS, new IntValue(1)));
    }

    /// <summary>
    /// minusMinus = identifier "--"
    /// </summary>
    /// <returns>i--运算</returns>
    private SetStatement ParseMinusMinus()
    {
        var identifier = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.MinusMinus);
        return new SetStatement(new OldId(identifier),
            new Operation(new OldId(identifier), OperationType.MINUS, new IntValue(1)));
    }

    /// <summary>
    /// block = "{" statement* "}"
    ///       | statement
    /// </summary>
    /// <returns>块语句</returns>
    private BlockStatement ParseBlock()
    {
        if (CurrentToken.Type != LangTokenType.LeftBrace)
        {
            return new BlockStatement([ParseStatement()]);
        }

        Expect(LangTokenType.LeftBrace);
        var statements = new List<IOldLangTree>();
        while (CurrentToken.Type != LangTokenType.RightBrace)
        {
            statements.Add(ParseStatement());
        }

        Expect(LangTokenType.RightBrace);
        return new BlockStatement(statements);
    }

    #endregion

    #region Expression

    // expression = binaryExpression
    //            | dotExpr
    //            | numberOpera1
    //            | numberOpera2
    //            | boolOpera
    //            | notBool
    //            | minusPrefix
    //            | primary ;
    private OldExpr ParseExpression()
    {
        var left = ParsePrimary();

        while (true)
        {
            switch (CurrentToken.Type)
            {
                case LangTokenType.LessThanEquals:
                case LangTokenType.GreaterThanEquals:
                case LangTokenType.Equals:
                case LangTokenType.NotEquals:
                case LangTokenType.LessThan:
                case LangTokenType.GreaterThan:
                    left = ParseBinaryExpression(left);
                    break;

                case LangTokenType.Dot:
                    left = ParseDotExpr(left);
                    break;

                case LangTokenType.Plus:
                case LangTokenType.Minus when Peek().Type != LangTokenType.Assignment:
                    left = ParseNumberOpera1(left);
                    break;

                case LangTokenType.Star:
                case LangTokenType.Slash:
                    left = ParseNumberOpera2(left);
                    break;

                case LangTokenType.And:
                case LangTokenType.Or:
                case LangTokenType.Xor:
                    left = ParseBoolOpera(left);
                    break;

                default:
                    return left;
            }
        }
    }

// binaryExpression = expression ( ( "<" | ">" | "==" | "!=" | "<=" | ">=" ) expression )* ;
    private OldExpr ParseBinaryExpression(OldExpr left)
    {
        while (CurrentToken.Type is LangTokenType.LessThanEquals or LangTokenType.GreaterThanEquals
               or LangTokenType.Equals
               or LangTokenType.NotEquals or LangTokenType.LessThan or LangTokenType.GreaterThan)
        {
            var operatorToken = CurrentToken;
            var position = new SourcePosition(operatorToken.Line, operatorToken.Column, tokenValue: operatorToken.Value);
            Expect(operatorToken.Type);
            var right = ParsePrimary();
            left = new Operation(left, operatorToken.Type.GetGeneric(), right, position);
        }

        return left;
    }

// dotExpr = expression ( "." expression )* ;
    private OldExpr ParseDotExpr(OldExpr left)
    {
        while (CurrentToken.Type == LangTokenType.Dot)
        {
            var dotToken = CurrentToken;
            var position = new SourcePosition(dotToken.Line, dotToken.Column, tokenValue: dotToken.Value);
            Expect(LangTokenType.Dot);
            var right = ParsePrimary();
            left = new Operation(left, OperationType.CONCAT, right, position);
        }

        return left;
    }

// numberOpera1 = expression ( ( "+" | "-" ) expression )* ;
    private OldExpr ParseNumberOpera1(OldExpr left)
    {
        while (CurrentToken.Type == LangTokenType.Plus || CurrentToken.Type == LangTokenType.Minus)
        {
            var operatorToken = CurrentToken;
            var position = new SourcePosition(operatorToken.Line, operatorToken.Column, tokenValue: operatorToken.Value);
            Expect(operatorToken.Type);
            var right = ParsePrimary();
            left = new Operation(left, operatorToken.Type.GetGeneric(), right, position);
        }

        return left;
    }

// numberOpera2 = expression ( ( "*" | "/" ) expression )* ;
    private OldExpr ParseNumberOpera2(OldExpr left)
    {
        while (CurrentToken.Type == LangTokenType.Star || CurrentToken.Type == LangTokenType.Slash)
        {
            var operatorToken = CurrentToken;
            var position = new SourcePosition(operatorToken.Line, operatorToken.Column, tokenValue: operatorToken.Value);
            Expect(operatorToken.Type);
            var right = ParsePrimary();
            left = new Operation(left, operatorToken.Type.GetGeneric(), right, position);
        }

        return left;
    }

// boolOpera = expression ( ( "and" | "or" | "xor" ) expression )* ;
    private OldExpr ParseBoolOpera(OldExpr left)
    {
        while (CurrentToken.Type == LangTokenType.And || CurrentToken.Type == LangTokenType.Or ||
               CurrentToken.Type == LangTokenType.Xor)
        {
            var operatorToken = CurrentToken;
            var position = new SourcePosition(operatorToken.Line, operatorToken.Column, tokenValue: operatorToken.Value);
            Expect(operatorToken.Type);
            var right = ParsePrimary();
            left = new Operation(left, operatorToken.Type.GetGeneric(), right, position);
        }

        return left;
    }

    #endregion

    #region Primary

    // primary = stringLiteral
    //         | intLiteral
    //         | charLiteral
    //         | doubleLiteral
    //         | identifier
    //         | trueLiteral
    //         | falseLiteral
    //         | listInit
    //         | instantiate
    //         | stringTree
    //         | lambda
    //         | list
    //         | range
    //         | array
    //         | tuple
    //         | dictionary
    //         | slice
    //         | asStatement
    private OldExpr ParsePrimary()
    {
        // 处理 not 表达式
        if (CurrentToken.Type == LangTokenType.Not)
        {
            var notToken = CurrentToken;
            var position = new SourcePosition(notToken.Line, notToken.Column, tokenValue: notToken.Value);
            Expect(LangTokenType.Not);
            var expr = ParsePrimary();
            return new Operation(expr, OperationType.NOT, null, position);
        }

        // 处理前缀 minus 表达式
        if (CurrentToken.Type == LangTokenType.Minus)
        {
            var minusToken = CurrentToken;
            var position = new SourcePosition(minusToken.Line, minusToken.Column, tokenValue: minusToken.Value);
            Expect(LangTokenType.Minus);
            var expr = ParsePrimary();
            return new Operation(new IntValue(0), OperationType.MINUS, expr, position);
        }

        // 处理 list[...] 语法
        if (CurrentToken is { Type: LangTokenType.Identifier, Value: "list" } &&
            Peek().Type == LangTokenType.LeftBracket)
        {
            Expect(LangTokenType.Identifier); // 跳过 list 关键字
            return ParseList();
        }

        return CurrentToken.Type switch
        {
            LangTokenType.String => ParseStringLiteral(),
            LangTokenType.Number => ParseDoubleLiteral(),
            LangTokenType.LeftBracket => ParseArrayOrRange(),
            LangTokenType.LeftParen => ParseLambdaOrTuple(),
            LangTokenType.LeftBrace => ParseDictionary(),
            LangTokenType.Dollar when Peek().Type == LangTokenType.LeftBrace => ParseStringTree(),
            LangTokenType.Identifier when Peek().Type == LangTokenType.As => ParseAs(),
            LangTokenType.Identifier when Peek().Type == LangTokenType.LeftBracket => ParseListInitOrSlice(),
            LangTokenType.Identifier when Peek().Type == LangTokenType.LeftParen => ParseInstantiate(),
            LangTokenType.Identifier => ParseIdentifier(),
            LangTokenType.True or LangTokenType.False => ParseBoolLiteral(),
            _ => throw new Exception($"语法错误：无法识别的主表达式，但得到了 {CurrentToken.Type}")
        };
    }

    /// <summary>
    /// list = "list" "[" expression ( "," expression )* "]" ;
    /// </summary>
    /// <returns>列表初始化</returns>
    private ValueType ParseList()
    {
        // list关键字已经被跳过，所以使用当前token的位置（即左括号）
        var listToken = CurrentToken;
        var position = new SourcePosition(listToken.Line, listToken.Column, tokenValue: "list");
        Expect(LangTokenType.LeftBracket);
        var elements = new List<OldExpr>();

        if (CurrentToken.Type == LangTokenType.RightBracket)
        {
            Expect(LangTokenType.RightBracket);
            // 空列表，返回ListValue
            return new ListValue(elements, position);
        }

        elements.Add(ParseExpression());
        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            elements.Add(ParseExpression());
        }

        Expect(LangTokenType.RightBracket);
        // 返回ListValue表示列表
        return new ListValue(elements, position);
    }

    /// <summary>
    /// asStatement = identifier "as" identifier ;
    /// </summary>
    /// <returns></returns>
    private AsValue ParseAs()
    {
        var id = ParseIdentifier();
        var asToken = CurrentToken;
        var position = new SourcePosition(asToken.Line, asToken.Column, tokenValue: asToken.Value);
        Expect(LangTokenType.As);
        var asId = ParseIdentifier();
        return new AsValue(id, asId, position);
    }


    /// <summary>
    /// dictionary = "{" dicTuple ( "," dicTuple )* "}" ;
    /// dicTuple = expression ":" expression ;
    /// </summary>
    /// <returns>返回字典</returns>
    private ValueType ParseDictionary()
    {
        // 处理左括号，只支持 {}
        var leftBraceToken = CurrentToken;
        var dictPosition = new SourcePosition(leftBraceToken.Line, leftBraceToken.Column, tokenValue: leftBraceToken.Value);
        Expect(LangTokenType.LeftBrace);

        var rightType = LangTokenType.RightBrace;

        var elements = new List<TupleValue>();

        if (CurrentToken.Type == rightType)
        {
            Expect(rightType);
            return new DictionaryValue(elements, dictPosition);
        }

        // 解析字典元素
        while (true)
        {
            var key = ParseExpression();
            var colonToken = CurrentToken;
            var tuplePosition = new SourcePosition(colonToken.Line, colonToken.Column, tokenValue: colonToken.Value);
            Expect(LangTokenType.Colon);
            var value = ParseExpression();
            elements.Add(new TupleValue(key, value, tuplePosition));

            if (CurrentToken.Type != LangTokenType.Comma)
            {
                break;
            }

            Expect(LangTokenType.Comma);
        }

        Expect(rightType);

        return new DictionaryValue(elements, dictPosition);
    }

    /// <summary>
    /// array = "[" expression ( "," expression )* "]" ;
    /// range = "[" expression "~" expression "]" ;
    /// </summary>
    /// <returns>数组初始化或者Range</returns>
    private ValueType ParseArrayOrRange()
    {
        var leftBracketToken = CurrentToken;
        var position = new SourcePosition(leftBracketToken.Line, leftBracketToken.Column, tokenValue: leftBracketToken.Value);
        Expect(LangTokenType.LeftBracket);
        var elements = new List<OldExpr>();

        if (CurrentToken.Type == LangTokenType.RightBracket)
        {
            Expect(LangTokenType.RightBracket);
            // 空数组，返回ArrayValue
            return new ArrayValue(elements, position);
        }

        elements.Add(ParseExpression());
        if (CurrentToken.Type == LangTokenType.Wavy)
        {
            var wavyToken = CurrentToken;
            var rangePosition = new SourcePosition(wavyToken.Line, wavyToken.Column, tokenValue: wavyToken.Value);
            Expect(LangTokenType.Wavy);
            elements.Add(ParseExpression());
            Expect(LangTokenType.RightBracket);
            return new RangeValue(elements[0], elements[1], rangePosition);
        }

        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            elements.Add(ParseExpression());
        }

        Expect(LangTokenType.RightBracket);
        // 返回ArrayValue表示数组
        return new ArrayValue(elements, position);
    }

    /// <summary>
    /// lambda = "(" idList? ")" "->" expression ;
    /// tuple  = "(" expression "," expression ")" ;
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception">存在空元组或元组元素过多</exception>
    private OldExpr ParseLambdaOrTuple()
    {
        var leftParenToken = CurrentToken;
        var position = new SourcePosition(leftParenToken.Line, leftParenToken.Column, tokenValue: leftParenToken.Value);
        Expect(LangTokenType.LeftParen);

        // Check if it's an empty tuple
        if (CurrentToken.Type == LangTokenType.RightParen)
        {
            Expect(LangTokenType.RightParen);
            throw new Exception("语法错误：空元组");
        }

        // 解析参数列表
        var parameters = new List<OldId>();
        var firstExpr = ParseExpression();

        // 如果第一个表达式是标识符，可能是 lambda 参数
        if (firstExpr is OldId id)
        {
            parameters.Add(id);

            // 解析更多参数
            while (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);
                var param = ParseExpression();
                if (param is OldId paramId)
                {
                    parameters.Add(paramId);
                }
                else
                {
                    // 不是标识符，不是 lambda 参数，回滚，尝试解析为元组
                    throw new Exception("语法错误：lambda 参数必须是标识符");
                }
            }

            // 检查是否是 lambda: (params) -> ...
            if (CurrentToken.Type == LangTokenType.RightParen && Peek().Type == LangTokenType.Arrow)
            {
                Expect(LangTokenType.RightParen); // 匹配右括号
                var arrowToken = CurrentToken;
                var lambdaPosition = new SourcePosition(arrowToken.Line, arrowToken.Column, tokenValue: arrowToken.Value);
                Expect(LangTokenType.Arrow); // 匹配箭头

                // 解析 lambda 体，支持 block 或 expression
                if (CurrentToken.Type == LangTokenType.LeftBrace)
                {
                    var block = ParseBlock();
                    return new FuncValue(null, parameters, block, lambdaPosition);
                }
                else
                {
                    // 简单表达式作为 lambda 体
                    var expr = ParseExpression();
                    // 创建一个只包含 return 语句的 block
                    var block = new BlockStatement([new ReturnStatement(expr)]);
                    return new FuncValue(null, parameters, block, lambdaPosition);
                }
            }
        }

        // 不是 lambda，解析为元组
        var tupleExprs = new List<OldExpr> { firstExpr };

        // 解析更多元组元素
        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            tupleExprs.Add(ParseExpression());
        }

        Expect(LangTokenType.RightParen);

        return tupleExprs.Count switch
        {
            // If only one expression, it's a single value in parentheses, not a tuple
            1 => tupleExprs[0],
            2 => new TupleValue(tupleExprs[0], tupleExprs[1], position),
            _ => throw new Exception("语法错误：元组")
        };
    }

    /// <summary>
    /// stringTree = "$" "{" expression ("," expression )* "}" ("{" expression ("," expression )* "}")* ;
    /// </summary>
    /// <returns>字符串粘合</returns>
    private StringTreeList ParseStringTree()
    {
        var dollarToken = CurrentToken;
        var position = new SourcePosition(dollarToken.Line, dollarToken.Column, tokenValue: dollarToken.Value);
        Expect(LangTokenType.Dollar);
        var list = new List<OldExpr>();

        // 处理连续的 {...} 块
        do
        {
            Expect(LangTokenType.LeftBrace);
            while (CurrentToken.Type != LangTokenType.RightBrace)
            {
                list.Add(ParseExpression());
                if (CurrentToken.Type == LangTokenType.Comma)
                {
                    Expect(LangTokenType.Comma);
                }
            }

            Expect(LangTokenType.RightBrace);
        } while (CurrentToken.Type == LangTokenType.LeftBrace); // 如果下一个token是{，继续处理

        return new StringTreeList(list, position);
    }

    /// <summary>
    /// instantiate = identifier "(" argList ")" ;
    /// </summary>
    /// <returns>实例</returns>
    private Instance ParseInstantiate()
    {
        var identifierToken = CurrentToken;
        var position = new SourcePosition(identifierToken.Line, identifierToken.Column, tokenValue: identifierToken.Value);
        Expect(LangTokenType.Identifier);
        var name = identifierToken.Value;
        Expect(LangTokenType.LeftParen);
        var args = ParseArgList();
        Expect(LangTokenType.RightParen);
        return new Instance(new OldId(name, "", position), args);
    }

    /// <summary>
    /// listInit = identifier "[" expression "]" ;
    /// slice = identifier "[" expression ":" expression "]" ;
    /// </summary>
    /// <returns>切片</returns>
    private ValueType ParseListInitOrSlice()
    {
        var identifierToken = CurrentToken;
        var name = identifierToken.Value;
        var idPosition = new SourcePosition(identifierToken.Line, identifierToken.Column, tokenValue: identifierToken.Value);
        Expect(LangTokenType.Identifier);
        var bracketToken = CurrentToken;
        var bracketPosition = new SourcePosition(bracketToken.Line, bracketToken.Column, tokenValue: bracketToken.Value);
        Expect(LangTokenType.LeftBracket);

        // 处理 [:] 或 :2 或 0: 或 0:2 等情况
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            var colonToken = CurrentToken;
            var rangePosition = new SourcePosition(colonToken.Line, colonToken.Column, tokenValue: colonToken.Value);
            Expect(LangTokenType.Colon);
            if (CurrentToken.Type == LangTokenType.RightBracket)
            {
                Expect(LangTokenType.RightBracket);
                return new RangeValue(null, null, rangePosition);
            }

            var first = ParseExpression();
            Expect(LangTokenType.RightBracket);
            return new RangeValue(null, first, rangePosition);
        }

        var args = ParseExpression();

        if (CurrentToken.Type == LangTokenType.Colon)
        {
            var colonToken = CurrentToken;
            var rangePosition = new SourcePosition(colonToken.Line, colonToken.Column, tokenValue: colonToken.Value);
            Expect(LangTokenType.Colon);
            if (CurrentToken.Type == LangTokenType.RightBracket)
            {
                Expect(LangTokenType.RightBracket);
                return new RangeValue(args, null, rangePosition);
            }

            var second = ParseExpression();
            Expect(LangTokenType.RightBracket);
            return new RangeValue(args, second, rangePosition);
        }

        Expect(LangTokenType.RightBracket);
        return new OldItem(new OldId(name, "", idPosition), args, bracketPosition);
    }

    // stringLiteral = STRING ;
    private StringValue ParseStringLiteral()
    {
        var stringToken = CurrentToken;
        var position = new SourcePosition(stringToken.Line, stringToken.Column, tokenValue: stringToken.Value);
        var str = stringToken.Value;
        Expect(LangTokenType.String);
        return new StringValue(str, position);
    }

    // doubleLiteral = DOUBLE ;
    private DoubleValue ParseDoubleLiteral()
    {
        var numberToken = CurrentToken;
        var position = new SourcePosition(numberToken.Line, numberToken.Column, tokenValue: numberToken.Value);
        var number = double.Parse(numberToken.Value);
        Expect(LangTokenType.Number);
        return new DoubleValue(number, position);
    }

    // identifier = IDENTIFIER ;
    private OldId ParseIdentifier()
    {
        var identifierToken = CurrentToken;
        var position = new SourcePosition(identifierToken.Line, identifierToken.Column, tokenValue: identifierToken.Value);
        var identifier = identifierToken.Value;
        Expect(LangTokenType.Identifier);
        if (CurrentToken.Type != LangTokenType.Colon)
            return new OldId(identifier, "", position);

        Expect(LangTokenType.Colon);
        var type = CurrentToken.Value;
        Expect(LangTokenType.Identifier);

        return new OldId(identifier, type, position);
    }

    private BoolValue ParseBoolLiteral()
    {
        var boolToken = CurrentToken;
        var position = new SourcePosition(boolToken.Line, boolToken.Column, tokenValue: boolToken.Value);
        var value = boolToken.Value;
        Expect(value == "true" ? LangTokenType.True : LangTokenType.False);
        return new BoolValue(value == "true", position);
    }

    // argList =  (expression "," expression )* ;
    private List<OldExpr> ParseArgList()
    {
        var arguments = new List<OldExpr>();
        if (CurrentToken.Type == LangTokenType.RightParen) return arguments;
        arguments.Add(ParseExpression());
        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            arguments.Add(ParseExpression());
        }

        return arguments;
    }

    private List<OldId> ParseIdList()
    {
        var arguments = new List<OldId>();
        if (CurrentToken.Type == LangTokenType.RightParen) return arguments;
        arguments.Add(ParseIdentifier());
        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            arguments.Add(ParseIdentifier());
        }

        return arguments;
    }

    #endregion
}