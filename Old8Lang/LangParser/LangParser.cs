using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.CslyParser;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.LangParser;

public class LangParser(List<LangToken> tokens)
{
    #region 基础操作

    private int _currentIndex;

    private LangToken CurrentToken => _currentIndex >= tokens.Count
        ? new LangToken("", LangTokenType.EndOfFile, _currentIndex)
        : tokens[_currentIndex];

    private void Expect(LangTokenType type)
    {
        if (CurrentToken.Type == type)
        {
            _currentIndex++;
        }
        else
            throw new Exception($"语法错误：期望 {type}，但得到了 {CurrentToken.Type}。在 {CurrentToken.Line}:{CurrentToken.Column}");
    }

    private LangToken Peek(int offset = 1)
    {
        if (_currentIndex + offset >= tokens.Count)
        {
            return new LangToken("", LangTokenType.EndOfFile, _currentIndex + offset);
        }

        return tokens[_currentIndex + offset];
    }

    #endregion

    #region Root

    // root = statement* ;
    public BlockStatement ParseProgram()
    {
        var statements = new List<OldLangTree>();
        while (_currentIndex < tokens.Count)
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
            // Lambda
            LangTokenType.Identifier when Peek().Type == LangTokenType.Arrow => ParseFuncDeclaration(),
            LangTokenType.Identifier when Peek().Type == LangTokenType.Colon && Peek(2).Type == LangTokenType.Identifier
                => ParseFuncDeclaration(),
            // 类型实例调用属性/方法
            LangTokenType.Identifier when Peek().Type == LangTokenType.Dot => ParseClassFuncRunStatement(),
            LangTokenType.Identifier when Peek().Type == LangTokenType.LeftParen => ParseFuncRunStatement(),
            LangTokenType.Identifier when Peek().Type == LangTokenType.PlusPlus => ParsePlusPlus(),
            LangTokenType.Identifier when Peek().Type == LangTokenType.MinusMinus => ParseMinusMinus(),
            LangTokenType.Identifier => ParseSet(),
            LangTokenType.Class => ParseClassDeclaration(),
            LangTokenType.Import => ParseImportStatement(),
            LangTokenType.LeftBracket when Peek().Type == LangTokenType.Import => ParseNativeStatement(),
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
            _ => throw new Exception($"语法有误。在解析到ParseStatement时出现问题。在{CurrentToken.Line}:{CurrentToken.Column}")
        };
    }

    private ReturnStatement ParseReturnStatement()
    {
        Expect(LangTokenType.Return);
        var expression = ParseExpression();
        return new ReturnStatement(expression);
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
        var identifier = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.Assignment);
        var expression = ParseExpression();
        return new SetStatement(new OldID(identifier), expression);
    }

    // ifStatement = "if" expression block ( "elif" expression block )* ( "else" block )? ;
    private IfStatement ParseIfStatement()
    {
        Expect(LangTokenType.If);
        var condition = ParseExpression();
        var ifBlock = ParseBlock();
        var oldIfs = new List<OldIf?>();
        while (CurrentToken.Type == LangTokenType.Elif)
        {
            Expect(LangTokenType.Elif);
            var elifCondition = ParseExpression();
            var elifBlock = ParseBlock();
            oldIfs.Add(new OldIf(elifCondition, elifBlock));
        }

        BlockStatement? elseBlock = null;
        if (CurrentToken.Type == LangTokenType.Else)
        {
            Expect(LangTokenType.Else);
            elseBlock = ParseBlock();
        }

        return new IfStatement(new OldIf(condition, ifBlock), oldIfs, elseBlock);
    }

    // forStatement = "for" set "," expression "," statement block ;
    private ForStatement ParseForStatement()
    {
        Expect(LangTokenType.For);
        var set = ParseSet();
        Expect(LangTokenType.Comma);
        var condition = ParseExpression();
        Expect(LangTokenType.Comma);
        var statement = ParseStatement();
        var block = ParseBlock();
        return new ForStatement(set, condition, statement, block);
    }

    // forInStatement = "for" identifier "in" expression block ;
    private ForInStatement ParseForInStatement()
    {
        Expect(LangTokenType.For);
        var identifier = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.In);
        var expression = ParseExpression();
        var block = ParseBlock();
        return new ForInStatement(new OldID(identifier), expression, block);
    }

    // whileStatement = "while" expression block ;
    private WhileStatement ParseWhileStatement()
    {
        Expect(LangTokenType.While);
        var condition = ParseExpression();
        var block = ParseBlock();
        return new WhileStatement(condition, block);
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
        Expect(LangTokenType.Case);
        var expression = ParseExpression();
        var block = ParseBlock();
        return new OldCase(expression, block);
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
        return new ClassInit(new AnyValue(new OldID(className), classBlock.ToAnyData()));
    }

    /// <summary>
    /// classBlock = "{" [set | funcDeclaration]* "}" ;
    /// </summary>
    /// <returns>类块</returns>
    /// <exception cref="Exception">期望声明或函数声明</exception>
    private BlockStatement ParseClassBlock()
    {
        Expect(LangTokenType.LeftBrace);
        var statements = new List<OldLangTree>();
        while (CurrentToken.Type != LangTokenType.RightBrace)
        {
            statements.Add(CurrentToken.Type switch
            {
                LangTokenType.Assignment => ParseSet(),
                LangTokenType.Func or LangTokenType.Identifier when Peek().Type == LangTokenType.LeftParen =>
                    ParseFuncDeclaration(),
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
        return new FuncRunStatement(new Instance(new OldID(funcName), arguments));
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
        return new FuncRunStatement(new Operation(new OldID(className), OldTokenGeneric.CONCAT,
            new Instance(new OldID(funcName), arguments)));
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
        return new SetStatement(new OldID(identifier),
            new Operation(new OldID(identifier), OldTokenGeneric.PLUS, new IntValue(1)));
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
        return new SetStatement(new OldID(identifier),
            new Operation(new OldID(identifier), OldTokenGeneric.MINUS, new IntValue(1)));
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
        var statements = new List<OldLangTree>();
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
            var operatorToken = CurrentToken.Type;
            Expect(CurrentToken.Type);
            var right = ParsePrimary();
            left = new Operation(left, operatorToken.GetGeneric(), right);
        }

        return left;
    }

// dotExpr = expression ( "." expression )* ;
    private OldExpr ParseDotExpr(OldExpr left)
    {
        while (CurrentToken.Type == LangTokenType.Dot)
        {
            Expect(LangTokenType.Dot);
            var right = ParsePrimary();
            left = new Operation(left, OldTokenGeneric.CONCAT, right);
        }

        return left;
    }

// numberOpera1 = expression ( ( "+" | "-" ) expression )* ;
    private OldExpr ParseNumberOpera1(OldExpr left)
    {
        while (CurrentToken.Type == LangTokenType.Plus || CurrentToken.Type == LangTokenType.Minus)
        {
            var operatorToken = CurrentToken.Type;
            Expect(CurrentToken.Type);
            var right = ParsePrimary();
            left = new Operation(left, operatorToken.GetGeneric(), right);
        }

        return left;
    }

// numberOpera2 = expression ( ( "*" | "/" ) expression )* ;
    private OldExpr ParseNumberOpera2(OldExpr left)
    {
        while (CurrentToken.Type == LangTokenType.Star || CurrentToken.Type == LangTokenType.Slash)
        {
            var operatorToken = CurrentToken.Type;
            Expect(CurrentToken.Type);
            var right = ParsePrimary();
            left = new Operation(left, operatorToken.GetGeneric(), right);
        }

        return left;
    }

// boolOpera = expression ( ( "and" | "or" | "xor" ) expression )* ;
    private OldExpr ParseBoolOpera(OldExpr left)
    {
        while (CurrentToken.Type == LangTokenType.And || CurrentToken.Type == LangTokenType.Or ||
               CurrentToken.Type == LangTokenType.Xor)
        {
            var operatorToken = CurrentToken.Type;
            Expect(CurrentToken.Type);
            var right = ParsePrimary();
            left = new Operation(left, operatorToken.GetGeneric(), right);
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
        return CurrentToken.Type switch
        {
            LangTokenType.String => ParseStringLiteral(),
            LangTokenType.Number => ParseDoubleLiteral(),
            LangTokenType.LeftBracket => ParseArrayOrRange(),
            LangTokenType.LeftParen => ParseLambdaOrTuple(),
            LangTokenType.LeftBrace => ParseDictionaryOrList(),
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
    /// asStatement = identifier "as" identifier ;
    /// </summary>
    /// <returns></returns>
    private AsValue ParseAs()
    {
        var id = ParseIdentifier();
        Expect(LangTokenType.As);
        var asId = ParseIdentifier();
        return new AsValue(id, asId);
    }


    /// <summary>
    /// dictionary = "{" dicTuple ( "," dicTuple )* "}" ;
    /// dicTuple = expression ":" expression ;
    /// list = "{" expression ( "," expression )* "}" ;
    /// </summary>
    /// <returns>返回列表或者字典</returns>
    private ValueType ParseDictionaryOrList()
    {
        Expect(LangTokenType.LeftBracket);

        var elements = new List<OldExpr>();

        if (CurrentToken.Type == LangTokenType.RightBracket)
        {
            Expect(LangTokenType.RightBracket);
            return new ListValue(elements);
        }

        var key = ParseExpression();
        if (CurrentToken.Type != LangTokenType.Colon || CurrentToken.Type == LangTokenType.RightBracket)
        {
            while (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);
                elements.Add(ParseExpression());
            }

            Expect(LangTokenType.RightBracket);
            return new ListValue(elements);
        }

        Expect(LangTokenType.Colon);
        var value = ParseExpression();
        elements.Add(new TupleValue(key, value));

        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            key = ParseExpression();
            Expect(LangTokenType.Colon);
            value = ParseExpression();
            elements.Add(new TupleValue(key, value));
        }

        Expect(LangTokenType.RightBracket);

        return new DictionaryValue(elements.OfType<TupleValue>().ToList());
    }

    /// <summary>
    /// array = "[" expression ( "," expression )* "]" ;
    /// range = "[" expression ".." expression "]" ;
    /// </summary>
    /// <returns>数列初始化或者Range</returns>
    private ValueType ParseArrayOrRange()
    {
        Expect(LangTokenType.LeftBracket);
        var list = new List<OldExpr>();

        if (CurrentToken.Type == LangTokenType.RightBracket)
        {
            Expect(LangTokenType.RightBracket);
            return new ArrayValue(list);
        }

        list.Add(ParseExpression());
        if (CurrentToken.Type == LangTokenType.Wavy)
        {
            Expect(LangTokenType.Wavy);
            list.Add(ParseExpression());
            Expect(LangTokenType.RightBracket);
            return new RangeValue(list[0], list[1]);
        }

        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            list.Add(ParseExpression());
        }

        return new ArrayValue(list);
    }

    /// <summary>
    /// lambda = "(" idList? ")" "->" expression ;
    /// tuple  = "(" expression "," expression ")" ;
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception">存在空元组或元组元素过多</exception>
    private OldExpr ParseLambdaOrTuple()
    {
        Expect(LangTokenType.LeftParen);

        // Check if it's an empty tuple
        if (CurrentToken.Type == LangTokenType.RightParen)
        {
            Expect(LangTokenType.RightParen);
            throw new Exception("语法错误：空元组");
        }

        var expressions = new List<OldExpr> { ParseExpression() };

        // Parse additional expressions for tuple
        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            expressions.Add(ParseExpression());
        }

        // Check if it's a lambda
        if (CurrentToken.Type == LangTokenType.Arrow)
        {
            Expect(LangTokenType.Arrow);
            var block = ParseBlock();
            var idList = expressions.OfType<OldID>().ToList();
            return new FuncValue(null, idList, block);
        }

        Expect(LangTokenType.RightParen);

        return expressions.Count switch
        {
            // If only one expression, it's a single value in parentheses, not a tuple
            1 => expressions[0],
            2 => new TupleValue(expressions[0], expressions[1]),
            _ => throw new Exception("语法错误：元组")
        };
    }

    /// <summary>
    /// stringTree = "$" "{" expression ( "," expression )* "}" ;
    /// </summary>
    /// <returns>字符串粘合</returns>
    private StringTreeList ParseStringTree()
    {
        Expect(LangTokenType.Dollar);
        Expect(LangTokenType.LeftBrace);
        var list = new List<OldExpr>();
        while (CurrentToken.Type != LangTokenType.RightBrace)
        {
            list.Add(ParseExpression());
            if (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);
            }
        }

        Expect(LangTokenType.RightBrace);
        return new StringTreeList(list);
    }

    /// <summary>
    /// instantiate = identifier "(" argList ")" ;
    /// </summary>
    /// <returns>实例</returns>
    private Instance ParseInstantiate()
    {
        Expect(LangTokenType.Identifier);
        var name = CurrentToken.Value;
        Expect(LangTokenType.LeftParen);
        var args = ParseArgList();
        Expect(LangTokenType.RightParen);
        return new Instance(new OldID(name), args);
    }

    /// <summary>
    /// listInit = identifier "[" expression "]" ;
    /// slice = identifier "[" expression "," expression "]" ;
    /// </summary>
    /// <returns>切片</returns>
    private ValueType ParseListInitOrSlice()
    {
        var name = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.LeftBracket);
        if (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            if (CurrentToken.Type == LangTokenType.RightBracket)
            {
                Expect(LangTokenType.RightBracket);
                return new RangeValue(null, null);
            }

            var first = ParseExpression();
            Expect(LangTokenType.RightBracket);
            return new RangeValue(null, first);
        }

        var args = ParseExpression();

        if (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            if (CurrentToken.Type == LangTokenType.RightBracket)
            {
                Expect(LangTokenType.RightBracket);
                return new RangeValue(args, null);
            }

            var second = ParseExpression();
            Expect(LangTokenType.RightBracket);
            return new RangeValue(args, second);
        }

        Expect(LangTokenType.RightBracket);
        return new OldItem(new OldID(name), args);
    }

    // stringLiteral = STRING ;
    private StringValue ParseStringLiteral()
    {
        var str = CurrentToken.Value;
        Expect(LangTokenType.String);
        return new StringValue(str);
    }

    // doubleLiteral = DOUBLE ;
    private DoubleValue ParseDoubleLiteral()
    {
        var number = double.Parse(CurrentToken.Value);
        Expect(LangTokenType.Number);
        return new DoubleValue(number);
    }

    // identifier = IDENTIFIER ;
    private OldID ParseIdentifier()
    {
        var identifier = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        if (CurrentToken.Type != LangTokenType.Colon)
            return new OldID(identifier);

        Expect(LangTokenType.Colon);
        var type = CurrentToken.Value;
        Expect(LangTokenType.Identifier);

        return new OldID(identifier, type);
    }

    private BoolValue ParseBoolLiteral()
    {
        var value = CurrentToken.Value;
        Expect(value == "true" ? LangTokenType.True : LangTokenType.False);
        return new BoolValue(value == "true");
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

    private List<OldID> ParseIdList()
    {
        var arguments = new List<OldID>();
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