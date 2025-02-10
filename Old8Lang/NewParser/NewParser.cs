using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.CslyParser;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.NewParser;

public class NewParser(List<NewToken> tokens)
{
    #region 基础操作

    private int _currentIndex;

    private NewToken CurrentToken => _currentIndex >= tokens.Count
        ? new NewToken("", NewTokenType.EndOfFile, _currentIndex)
        : tokens[_currentIndex];

    private void Expect(NewTokenType type)
    {
        if (CurrentToken.Type == type)
        {
            _currentIndex++;
        }
        else
            throw new Exception($"语法错误：期望 {type}，但得到了 {CurrentToken.Type}。在 {CurrentToken.Line}:{CurrentToken.Column}");
    }

    private NewToken Peek(int offset = 1)
    {
        if (_currentIndex + offset >= tokens.Count)
        {
            return new NewToken("", NewTokenType.EndOfFile, _currentIndex + offset);
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
            NewTokenType.LeftParen => ParseLrBlock(),
            NewTokenType.If => ParseIfStatement(),
            NewTokenType.For when Peek().Type == NewTokenType.Identifier => ParseForStatement(),
            NewTokenType.For when Peek().Type == NewTokenType.In => ParseForInStatement(),
            NewTokenType.While => ParseWhileStatement(),
            NewTokenType.Switch => ParseSwitchStatement(),
            NewTokenType.Func when Peek().Type == NewTokenType.Identifier && Peek(2).Type == NewTokenType.LeftParen =>
                ParseFuncDeclaration(),
            NewTokenType.Return => ParseReturnStatement(),
            // Lambda
            NewTokenType.Identifier when Peek().Type == NewTokenType.Arrow => ParseFuncDeclaration(),
            NewTokenType.Identifier when Peek().Type == NewTokenType.Colon && Peek(2).Type == NewTokenType.Identifier
                => ParseFuncDeclaration(),
            // 类型实例调用属性/方法
            NewTokenType.Identifier when Peek().Type == NewTokenType.Dot => ParseClassFuncRunStatement(),
            NewTokenType.Identifier when Peek().Type == NewTokenType.LeftParen => ParseFuncRunStatement(),
            NewTokenType.Identifier => ParseSet(),
            NewTokenType.Class => ParseClassDeclaration(),
            NewTokenType.Import => ParseImportStatement(),
            NewTokenType.LeftBracket when Peek().Type == NewTokenType.Import => ParseNativeStatement(),
            NewTokenType.PlusPlus => ParsePlusPlus(),
            NewTokenType.MinusMinus => ParseMinusMinus(),
            NewTokenType.LeftBracket when Peek().Type == NewTokenType.Import && Peek(2).Type == NewTokenType.String &&
                                          Peek(3).Type == NewTokenType.Identifier &&
                                          Peek(4).Type == NewTokenType.RightBracket &&
                                          Peek(5).Type == NewTokenType.Arrow && Peek(6).Type == NewTokenType.String
                => ParseNativeStatic(),
            NewTokenType.LeftBracket when Peek().Type == NewTokenType.Import && Peek(2).Type == NewTokenType.String &&
                                          Peek(3).Type == NewTokenType.Identifier &&
                                          Peek(4).Type == NewTokenType.RightBracket => ParseNativeClass(),
            _ => throw new Exception($"语法有误。在解析到ParseStatement时出现问题。在{CurrentToken.Line}:{CurrentToken.Column}")
        };
    }

    private ReturnStatement ParseReturnStatement()
    {
        Expect(NewTokenType.Return);
        var expression = ParseExpression();
        return new ReturnStatement(expression);
    }

    // lrBlock = "(" statement ")" ;
    private OldStatement ParseLrBlock()
    {
        Expect(NewTokenType.LeftParen);
        var statement = ParseStatement();
        Expect(NewTokenType.RightParen);
        return statement;
    }

    // declaration = identifier "<-" expression ;
    private SetStatement ParseSet()
    {
        var identifier = CurrentToken.Value;
        Expect(NewTokenType.Identifier);
        Expect(NewTokenType.Assignment);
        var expression = ParseExpression();
        return new SetStatement(new OldID(identifier), expression);
    }

    // ifStatement = "if" expression block ( "elif" expression block )* ( "else" block )? ;
    private IfStatement ParseIfStatement()
    {
        Expect(NewTokenType.If);
        var condition = ParseExpression();
        var ifBlock = ParseBlock();
        var oldIfs = new List<OldIf?>();
        while (CurrentToken.Type == NewTokenType.Elif)
        {
            Expect(NewTokenType.Elif);
            var elifCondition = ParseExpression();
            var elifBlock = ParseBlock();
            oldIfs.Add(new OldIf(elifCondition, elifBlock));
        }

        BlockStatement? elseBlock = null;
        if (CurrentToken.Type == NewTokenType.Else)
        {
            Expect(NewTokenType.Else);
            elseBlock = ParseBlock();
        }

        return new IfStatement(new OldIf(condition, ifBlock), oldIfs, elseBlock);
    }

    // forStatement = "for" set "," expression "," statement block ;
    private ForStatement ParseForStatement()
    {
        Expect(NewTokenType.For);
        var set = ParseSet();
        Expect(NewTokenType.Comma);
        var condition = ParseExpression();
        Expect(NewTokenType.Comma);
        var statement = ParseStatement();
        var block = ParseBlock();
        return new ForStatement(set, condition, statement, block);
    }

    // forInStatement = "for" identifier "in" expression block ;
    private ForInStatement ParseForInStatement()
    {
        Expect(NewTokenType.For);
        var identifier = CurrentToken.Value;
        Expect(NewTokenType.Identifier);
        Expect(NewTokenType.In);
        var expression = ParseExpression();
        var block = ParseBlock();
        return new ForInStatement(new OldID(identifier), expression, block);
    }

    // whileStatement = "while" expression block ;
    private WhileStatement ParseWhileStatement()
    {
        Expect(NewTokenType.While);
        var condition = ParseExpression();
        var block = ParseBlock();
        return new WhileStatement(condition, block);
    }

    // switchStatement = "switch" expression "{" caseBlock* ( "default" block )? "}" ;
    private SwitchStatement ParseSwitchStatement()
    {
        Expect(NewTokenType.Switch);
        var expression = ParseExpression();
        Expect(NewTokenType.LeftBrace);
        var cases = new List<OldCase>();
        while (CurrentToken.Type == NewTokenType.Case)
        {
            cases.Add(ParseCaseBlock());
        }

        BlockStatement? defaultBlock = null;
        if (CurrentToken.Type == NewTokenType.Default)
        {
            Expect(NewTokenType.Default);
            defaultBlock = ParseBlock();
        }

        Expect(NewTokenType.RightBrace);
        return new SwitchStatement(expression, cases, defaultBlock);
    }

    // caseBlock = "case" expression block ;
    private OldCase ParseCaseBlock()
    {
        Expect(NewTokenType.Case);
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
        if (CurrentToken.Type == NewTokenType.Func)
        {
            Expect(NewTokenType.Func);
        }

        var funcName = ParseIdentifier();

        Expect(NewTokenType.LeftParen);
        var parameters = ParseIdList();
        Expect(NewTokenType.RightParen);
        if (CurrentToken.Type == NewTokenType.Arrow)
        {
            Expect(NewTokenType.Arrow);
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
        Expect(NewTokenType.Class);
        var className = CurrentToken.Value;
        Expect(NewTokenType.Identifier);
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
        Expect(NewTokenType.LeftBrace);
        var statements = new List<OldLangTree>();
        while (CurrentToken.Type != NewTokenType.RightBrace)
        {
            statements.Add(CurrentToken.Type switch
            {
                NewTokenType.Assignment => ParseSet(),
                NewTokenType.Func or NewTokenType.Identifier when Peek().Type == NewTokenType.LeftParen =>
                    ParseFuncDeclaration(),
                _ => throw new Exception($"语法错误：期望声明或函数声明，但得到了 {CurrentToken.Type}")
            });
        }

        Expect(NewTokenType.RightBrace);
        return new BlockStatement(statements);
    }

    /// <summary>
    /// funcRunStatement = identifier "(" argList? ")" ;
    /// </summary>
    /// <returns>函数调用</returns>
    private FuncRunStatement ParseFuncRunStatement()
    {
        var funcName = CurrentToken.Value;
        Expect(NewTokenType.Identifier);
        Expect(NewTokenType.LeftParen);
        var arguments = ParseArgList();
        Expect(NewTokenType.RightParen);
        return new FuncRunStatement(new Instance(new OldID(funcName), arguments));
    }

    /// <summary>
    /// classFuncRunStatement = identifier "." identifier "(" argList? ")" ;
    /// </summary>
    /// <returns>类方法调用</returns>
    private FuncRunStatement ParseClassFuncRunStatement()
    {
        var className = CurrentToken.Value;
        Expect(NewTokenType.Identifier);
        Expect(NewTokenType.Dot);
        var funcName = CurrentToken.Value;
        Expect(NewTokenType.Identifier);
        Expect(NewTokenType.LeftParen);
        var arguments = ParseArgList();
        Expect(NewTokenType.RightParen);
        return new FuncRunStatement(new Operation(new OldID(className), OldTokenGeneric.CONCAT,
            new Instance(new OldID(funcName), arguments)));
    }

    /// <summary>
    /// importStatement = "import" identifier ;
    /// </summary>
    /// <returns>引入模块</returns>
    private ImportStatement ParseImportStatement()
    {
        Expect(NewTokenType.Import);
        var moduleName = CurrentToken.Value;
        Expect(NewTokenType.Identifier);
        return new ImportStatement(moduleName);
    }

    /// <summary>
    /// nativeStatement = "[" "import" STRING identifier identifier identifier? "]" ;
    /// </summary>
    /// <returns>引入原生方法</returns>
    private NativeStatement ParseNativeStatement()
    {
        Expect(NewTokenType.LeftBracket);
        Expect(NewTokenType.Import);
        var dllName = CurrentToken.Value;
        Expect(NewTokenType.String);
        var className = CurrentToken.Value;
        Expect(NewTokenType.Identifier);
        var methodName = CurrentToken.Value;
        Expect(NewTokenType.Identifier);
        var alias = "";
        if (CurrentToken.Type == NewTokenType.Identifier)
        {
            alias = CurrentToken.Value;
            Expect(NewTokenType.Identifier);
        }

        Expect(NewTokenType.RightBracket);
        return new NativeStatement(dllName, className, methodName, alias);
    }

    /// <summary>
    /// nativeStatic = "[" "import" STRING identifier "]" "->" STRING ;
    /// </summary>
    /// <returns>引入原生静态类</returns>
    private NativeStatement ParseNativeStatic()
    {
        Expect(NewTokenType.LeftBracket);
        Expect(NewTokenType.Import);
        var dllName = CurrentToken.Value;
        Expect(NewTokenType.String);
        var className = CurrentToken.Value;
        Expect(NewTokenType.Identifier);
        Expect(NewTokenType.RightBracket);
        Expect(NewTokenType.Arrow);
        var methodName = CurrentToken.Value;
        Expect(NewTokenType.String);
        return new NativeStatement(dllName, className, methodName);
    }

    /// <summary>
    ///  nativeClass = "[" "import" STRING identifier "]" ;
    /// </summary>
    /// <returns>引入原生类</returns>
    private NativeStatement ParseNativeClass()
    {
        Expect(NewTokenType.LeftBracket);
        Expect(NewTokenType.Import);
        var dllName = CurrentToken.Value;
        Expect(NewTokenType.String);
        var className = CurrentToken.Value;
        Expect(NewTokenType.Identifier);
        Expect(NewTokenType.RightBracket);
        return new NativeStatement(dllName, className);
    }

    /// <summary>
    /// plusPlus = identifier "++"
    /// </summary>
    /// <returns>i++运算</returns>
    private SetStatement ParsePlusPlus()
    {
        var identifier = CurrentToken.Value;
        Expect(NewTokenType.Identifier);
        Expect(NewTokenType.PlusPlus);
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
        Expect(NewTokenType.Identifier);
        Expect(NewTokenType.MinusMinus);
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
        if (CurrentToken.Type != NewTokenType.LeftBrace)
        {
            return new BlockStatement([ParseStatement()]);
        }

        Expect(NewTokenType.LeftBrace);
        var statements = new List<OldLangTree>();
        while (CurrentToken.Type != NewTokenType.RightBrace)
        {
            statements.Add(ParseStatement());
        }

        Expect(NewTokenType.RightBrace);
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
        if (CurrentToken.Type == NewTokenType.LeftParen)
        {
            return ParseBinaryExpression();
        }

        var a = CurrentToken.Type switch
        {
            NewTokenType.Not => ParseNotBool(),
            NewTokenType.Minus when Peek().Type != NewTokenType.Assignment => ParseMinusPrefix(),
            _ => null
        };

        if (a != null) return a;

        var next = Peek();
        return next.Type switch
        {
            NewTokenType.LessThanEquals or NewTokenType.GreaterThanEquals or NewTokenType.Equals
                or NewTokenType.NotEquals or NewTokenType.LessThan
                or NewTokenType.GreaterThan => ParseBinaryExpression(),
            NewTokenType.Dot => ParseDotExpr(),
            NewTokenType.Plus or NewTokenType.Minus when Peek().Type != NewTokenType.Assignment => ParseNumberOpera1(),
            NewTokenType.Star or NewTokenType.Slash => ParseNumberOpera2(),
            NewTokenType.And or NewTokenType.Or or NewTokenType.Xor => ParseBoolOpera(),
            _ => ParsePrimary()
        };
    }

    // numberOpera1 = expression ( ( "+" | "-" ) expression )* ;
    private OldExpr ParseNumberOpera1()
    {
        var left = ParseTerm();
        while (CurrentToken.Type == NewTokenType.Plus || CurrentToken.Type == NewTokenType.Minus)
        {
            var operatorToken = CurrentToken.Type;
            Expect(CurrentToken.Type);
            var right = ParseTerm();
            left = new Operation(left, operatorToken.GetGeneric(), right);
        }

        return left;
    }

    // term = factor { ("*" | "/") factor } ;
    private OldExpr ParseTerm()
    {
        var left = ParsePrimary();
        while (CurrentToken.Type == NewTokenType.Star || CurrentToken.Type == NewTokenType.Slash ||
               CurrentToken.Type == NewTokenType.Dot)
        {
            var operatorToken = CurrentToken.Type;
            Expect(CurrentToken.Type);
            var right = ParsePrimary();
            left = new Operation(left, operatorToken.GetGeneric(), right);
        }

        return left;
    }


    // binaryExpression = expression ( ( "<" | ">" | "==" | "!=" | "<=" | ">=" ) expression )* ;
    private OldExpr ParseBinaryExpression()
    {
        var left = ParseTerm();
        while (CurrentToken.Type is NewTokenType.LessThanEquals or NewTokenType.GreaterThanEquals or NewTokenType.Equals
               or NewTokenType.NotEquals or NewTokenType.LessThan or NewTokenType.GreaterThan)
        {
            var operatorToken = CurrentToken.Type;
            Expect(CurrentToken.Type);
            var right = ParseTerm();
            left = new Operation(left, operatorToken.GetGeneric(), right);
        }

        return left;
    }

    // dotExpr = expression ( "." expression )* ;
    private OldExpr ParseDotExpr()
    {
        var left = ParsePrimary();
        while (CurrentToken.Type == NewTokenType.Dot)
        {
            Expect(NewTokenType.Dot);
            var right = ParsePrimary();
            left = new Operation(left, OldTokenGeneric.CONCAT, right);
        }

        return left;
    }

    // numberOpera2 = expression ( ( "*" | "/" ) expression )* ;
    private OldExpr ParseNumberOpera2()
    {
        var left = ParsePrimary();
        while (CurrentToken.Type == NewTokenType.Star || CurrentToken.Type == NewTokenType.Slash)
        {
            var operatorToken = CurrentToken.Type;
            Expect(CurrentToken.Type);
            var right = ParsePrimary();
            left = new Operation(left, operatorToken.GetGeneric(), right);
        }

        return left;
    }

    // boolOpera = expression ( ( "and" | "or" | "xor" ) expression )* ;
    private OldExpr ParseBoolOpera()
    {
        var left = ParseExpression();
        while (CurrentToken.Type == NewTokenType.And || CurrentToken.Type == NewTokenType.Or ||
               CurrentToken.Type == NewTokenType.Xor)
        {
            var operatorToken = CurrentToken.Type;
            Expect(CurrentToken.Type);
            var right = ParseExpression();
            left = new Operation(left, operatorToken.GetGeneric(), right);
        }

        return left;
    }

    // notBool = "not" expression ;
    private Operation ParseNotBool()
    {
        Expect(NewTokenType.Not);
        var expression = ParseExpression();
        return new Operation(null, OldTokenGeneric.NOT, expression);
    }

    // minusPrefix = "-" expression ;
    private Operation ParseMinusPrefix()
    {
        Expect(NewTokenType.Minus);
        var expression = ParseExpression();
        return new Operation(null, OldTokenGeneric.MINUS, expression);
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
    //         | asStatement ;
    private OldExpr ParsePrimary()
    {
        return CurrentToken.Type switch
        {
            NewTokenType.String => ParseStringLiteral(),
            NewTokenType.Number => ParseDoubleLiteral(),
            NewTokenType.LeftBracket => ParseArrayOrRange(),
            NewTokenType.LeftParen => ParseLambdaOrTuple(),
            NewTokenType.LeftBrace => ParseDictionaryOrList(),
            NewTokenType.Dollar when Peek().Type == NewTokenType.LeftBrace => ParseStringTree(),
            NewTokenType.Identifier when Peek().Type == NewTokenType.As => ParseAs(),
            NewTokenType.Identifier when Peek().Type == NewTokenType.LeftBracket => ParseListInitOrSlice(),
            NewTokenType.Identifier when Peek().Type == NewTokenType.LeftParen => ParseInstantiate(),
            NewTokenType.Identifier => ParseIdentifier(),
            NewTokenType.True or NewTokenType.False => ParseBoolLiteral(),
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
        Expect(NewTokenType.As);
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
        Expect(NewTokenType.LeftBracket);

        var elements = new List<OldExpr>();

        if (CurrentToken.Type == NewTokenType.RightBracket)
        {
            Expect(NewTokenType.RightBracket);
            return new ListValue(elements);
        }

        var key = ParseExpression();
        if (CurrentToken.Type != NewTokenType.Colon || CurrentToken.Type == NewTokenType.RightBracket)
        {
            while (CurrentToken.Type == NewTokenType.Comma)
            {
                Expect(NewTokenType.Comma);
                elements.Add(ParseExpression());
            }

            Expect(NewTokenType.RightBracket);
            return new ListValue(elements);
        }

        Expect(NewTokenType.Colon);
        var value = ParseExpression();
        elements.Add(new TupleValue(key, value));

        while (CurrentToken.Type == NewTokenType.Comma)
        {
            Expect(NewTokenType.Comma);
            key = ParseExpression();
            Expect(NewTokenType.Colon);
            value = ParseExpression();
            elements.Add(new TupleValue(key, value));
        }

        Expect(NewTokenType.RightBracket);

        return new DictionaryValue(elements.OfType<TupleValue>().ToList());
    }

    /// <summary>
    /// array = "[" expression ( "," expression )* "]" ;
    /// range = "[" expression ".." expression "]" ;
    /// </summary>
    /// <returns>数列初始化或者Range</returns>
    private ValueType ParseArrayOrRange()
    {
        Expect(NewTokenType.LeftBracket);
        var list = new List<OldExpr>();

        if (CurrentToken.Type == NewTokenType.RightBracket)
        {
            Expect(NewTokenType.RightBracket);
            return new ArrayValue(list);
        }

        list.Add(ParseExpression());
        if (CurrentToken.Type == NewTokenType.DotDot)
        {
            Expect(NewTokenType.DotDot);
            list.Add(ParseExpression());
            Expect(NewTokenType.RightBracket);
            return new RangeValue(list[0], list[1]);
        }

        while (CurrentToken.Type == NewTokenType.Comma)
        {
            Expect(NewTokenType.Comma);
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
        Expect(NewTokenType.LeftParen);

        // Check if it's an empty tuple
        if (CurrentToken.Type == NewTokenType.RightParen)
        {
            Expect(NewTokenType.RightParen);
            throw new Exception("语法错误：空元组");
        }

        var expressions = new List<OldExpr> { ParseExpression() };

        // Parse additional expressions for tuple
        while (CurrentToken.Type == NewTokenType.Comma)
        {
            Expect(NewTokenType.Comma);
            expressions.Add(ParseExpression());
        }

        // Check if it's a lambda
        if (CurrentToken.Type == NewTokenType.Arrow)
        {
            Expect(NewTokenType.Arrow);
            var block = ParseBlock();
            var idList = expressions.OfType<OldID>().ToList();
            return new FuncValue(null, idList, block);
        }

        Expect(NewTokenType.RightParen);

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
        Expect(NewTokenType.Dollar);
        Expect(NewTokenType.LeftBrace);
        var list = new List<OldExpr>();
        while (CurrentToken.Type != NewTokenType.RightBrace)
        {
            list.Add(ParseExpression());
            if (CurrentToken.Type == NewTokenType.Comma)
            {
                Expect(NewTokenType.Comma);
            }
        }

        Expect(NewTokenType.RightBrace);
        return new StringTreeList(list);
    }

    /// <summary>
    /// instantiate = identifier "(" argList ")" ;
    /// </summary>
    /// <returns>实例</returns>
    private Instance ParseInstantiate()
    {
        Expect(NewTokenType.Identifier);
        var name = CurrentToken.Value;
        Expect(NewTokenType.LeftParen);
        var args = ParseArgList();
        Expect(NewTokenType.RightParen);
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
        Expect(NewTokenType.Identifier);
        Expect(NewTokenType.LeftBracket);
        if (CurrentToken.Type == NewTokenType.Comma)
        {
            Expect(NewTokenType.Comma);
            if (CurrentToken.Type == NewTokenType.RightBracket)
            {
                Expect(NewTokenType.RightBracket);
                return new RangeValue(null, null);
            }

            var first = ParseExpression();
            Expect(NewTokenType.RightBracket);
            return new RangeValue(null, first);
        }

        var args = ParseExpression();

        if (CurrentToken.Type == NewTokenType.Comma)
        {
            Expect(NewTokenType.Comma);
            if (CurrentToken.Type == NewTokenType.RightBracket)
            {
                Expect(NewTokenType.RightBracket);
                return new RangeValue(args, null);
            }

            var second = ParseExpression();
            Expect(NewTokenType.RightBracket);
            return new RangeValue(args, second);
        }

        Expect(NewTokenType.RightBracket);
        return new OldItem(new OldID(name), args);
    }

    // stringLiteral = STRING ;
    private StringValue ParseStringLiteral()
    {
        var str = CurrentToken.Value;
        Expect(NewTokenType.String);
        return new StringValue(str);
    }

    // doubleLiteral = DOUBLE ;
    private DoubleValue ParseDoubleLiteral()
    {
        var number = double.Parse(CurrentToken.Value);
        Expect(NewTokenType.Number);
        return new DoubleValue(number);
    }

    // identifier = IDENTIFIER ;
    private OldID ParseIdentifier()
    {
        var identifier = CurrentToken.Value;
        Expect(NewTokenType.Identifier);
        if (CurrentToken.Type != NewTokenType.Colon)
            return new OldID(identifier);

        Expect(NewTokenType.Colon);
        var type = CurrentToken.Value;
        Expect(NewTokenType.Identifier);

        return new OldID(identifier, type);
    }

    private BoolValue ParseBoolLiteral()
    {
        var value = CurrentToken.Value;
        Expect(value == "true" ? NewTokenType.True : NewTokenType.False);
        return new BoolValue(value == "true");
    }

    // argList =  (expression "," expression )* ;
    private List<OldExpr> ParseArgList()
    {
        var arguments = new List<OldExpr>();
        if (CurrentToken.Type == NewTokenType.RightParen) return arguments;
        arguments.Add(ParseExpression());
        while (CurrentToken.Type == NewTokenType.Comma)
        {
            Expect(NewTokenType.Comma);
            arguments.Add(ParseExpression());
        }

        return arguments;
    }

    private List<OldID> ParseIdList()
    {
        var arguments = new List<OldID>();
        if (CurrentToken.Type == NewTokenType.RightParen) return arguments;
        arguments.Add(ParseIdentifier());
        while (CurrentToken.Type == NewTokenType.Comma)
        {
            Expect(NewTokenType.Comma);
            arguments.Add(ParseIdentifier());
        }

        return arguments;
    }

    #endregion
}