using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.LangParser.Core;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// 语句解析器，负责解析各种语句类型
/// </summary>
public class StatementParser(
    ParserContext context,
    ExpressionParser expressionParser,
    FunctionParser functionParser,
    ClassParser classParser,
    PrimaryParser primaryParser)
    : ParserBase(context)
{
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
    public OldStatement ParseStatement(List<AccessModifierType>? modifiers = null)
    {
        // 跳过结束符
        if (CurrentToken.Type == LangTokenType.EndOfFile)
        {
            CurrentIndex++;
            throw CreateSyntaxError("语法错误：意外的文件结束符。建议检查是否缺少结束符号或语句。");
        }

        // 处理访问修饰符：public、private、static 或它们的组合
        if (CurrentToken.Type is LangTokenType.Public or LangTokenType.Private or LangTokenType.Static)
        {
            var parsedModifiers = classParser.ParseAccessModifiers();

            // 合并修饰符
            var combinedModifiers = modifiers != null
                ? new List<AccessModifierType>(modifiers)
                : new List<AccessModifierType>();
            combinedModifiers.AddRange(parsedModifiers);

            // 解析后面的语句（set 或 funcDeclaration）
            return ParseStatement(combinedModifiers);
        }



        // 处理括号块：(statement)
        if (CurrentToken.Type == LangTokenType.LeftParen)
        {
            return ParseLrBlock();
        }

        // 处理控制流语句
        if (CurrentToken.Type == LangTokenType.If)
        {
            return ParseIfStatement();
        }

        // 处理异常处理语句
        if (CurrentToken.Type == LangTokenType.Try)
        {
            return ParseTryStatement();
        }

        // 处理循环语句 For 和 For in
        if (CurrentToken.Type == LangTokenType.For)
        {
            // 检查是否是 for-in 语句，格式为：for 标识符 (, 标识符)* in 表达式
            // 需要确保 "in" 关键字前面只有标识符和逗号
            var tempIndex = CurrentIndex + 1;
            var foundIn = false;
            var inPosition = 0;
            var scanLimit = Math.Min(tempIndex + 20, Tokens.Count); // 限制前瞻深度

            // 跳过所有标识符和逗号，查找 "in" 关键字
            while (tempIndex < scanLimit)
            {
                var token = Tokens[tempIndex];
                if (token.Type == LangTokenType.In)
                {
                    foundIn = true;
                    inPosition = tempIndex;
                    break;
                }

                // 遇到非标识符/逗号，说明不是 for-in 语句
                if (token.Type != LangTokenType.Identifier && token.Type != LangTokenType.Comma)
                {
                    break;
                }

                tempIndex++;
            }

            // 只有当 "in" 关键字前面只有标识符和逗号时，才是 for-in 语句
            if (foundIn)
            {
                return ParseForInStatement();
            }

            // 解析普通 for 循环
            return ParseForStatement();
        }

        if (CurrentToken.Type == LangTokenType.While)
        {
            return ParseWhileStatement();
        }

        if (CurrentToken.Type == LangTokenType.Switch)
        {
            return ParseSwitchStatement();
        }

        // 处理异步函数定义和异步 for-in 循环：async func / async for
        if (CurrentToken.Type == LangTokenType.Async)
        {
            Expect(LangTokenType.Async);

            // 检查是否是 async for-in
            if (CurrentToken.Type == LangTokenType.For)
            {
                return ParseAsyncForInStatement();
            }

            // 否则是 async func
            Expect(LangTokenType.Func);
            return functionParser.ParseAsyncFuncDeclaration();
        }

        // 处理函数定义
        if (CurrentToken.Type == LangTokenType.Func)
        {
            return functionParser.ParseFuncDeclaration();
        }

        // 处理return语句：return expression
        if (CurrentToken.Type == LangTokenType.Return)
        {
            return ParseReturnStatement();
        }

        // 处理yield语句：yield expression
        if (CurrentToken.Type == LangTokenType.Yield)
        {
            return ParseYieldStatement();
        }

        // 处理native语句：native "dll" class method
        if (CurrentToken.Type == LangTokenType.Native)
        {
            var peek1 = Peek();
            var peek2 = Peek(2);
            var peek3 = Peek(3);
            var peek4 = Peek(4);

            // 先处理更具体的语法模式，再处理更通用的
            // 1. nativeStatic: native "dll" class -> "alias"
            if (peek1.Type == LangTokenType.String &&
                peek2.Type == LangTokenType.Identifier &&
                peek3.Type == LangTokenType.Arrow)
            {
                return ParseNativeStatic();
            }

            // 2. 批量导入所有方法: native "dll" class *
            if (peek1.Type == LangTokenType.String &&
                peek2.Type == LangTokenType.Identifier &&
                peek3.Type == LangTokenType.Star)
            {
                return ParseNativeStatement();
            }

            // 3. 选择性导入: native "dll" class { method1, method2 }
            if (peek1.Type == LangTokenType.String &&
                peek2.Type == LangTokenType.Identifier &&
                peek3.Type == LangTokenType.LeftBrace)
            {
                return ParseNativeStatement();
            }

            // 4. nativeClass with alias: native "dll" class as alias
            if (peek1.Type == LangTokenType.String &&
                peek2.Type == LangTokenType.Identifier &&
                peek3.Type == LangTokenType.As)
            {
                return ParseNativeClass();
            }

            // 5. 单个方法导入: native "dll" class method alias?
            // 如果第三个 token 是 identifier，说明是方法导入
            if (peek1.Type == LangTokenType.String &&
                peek2.Type == LangTokenType.Identifier &&
                peek3.Type == LangTokenType.Identifier)
            {
                return ParseNativeStatement();
            }

            // 6. nativeClass: native "dll" class（最后兜底，第三个 token 不是上述任何特殊类型）
            if (peek1.Type == LangTokenType.String &&
                peek2.Type == LangTokenType.Identifier)
            {
                return ParseNativeClass();
            }

            // 其他情况，尝试解析为 native statement
            return ParseNativeStatement();
        }

        // 处理break语句：break
        if (CurrentToken.Type == LangTokenType.Break)
        {
            return ParseBreakStatement();
        }

        // 处理continue语句：continue
        if (CurrentToken.Type == LangTokenType.Continue)
        {
            return ParseContinueStatement();
        }

        // 处理throw语句：throw expression
        if (CurrentToken.Type == LangTokenType.Throw)
        {
            return ParseThrowStatement();
        }

        // 处理class或mixin定义：class identifier block 或 mixin identifier block
        if (CurrentToken.Type == LangTokenType.Class || CurrentToken.Type == LangTokenType.Mixin)
        {
            return classParser.ParseClassDeclaration();
        }
        
        // 处理interface定义：interface identifier block
        if (CurrentToken.Type == LangTokenType.Interface)
        {
            return classParser.ParseInterfaceDeclaration();
        }

        // 处理import语句：import module
        if (CurrentToken.Type == LangTokenType.Import)
        {
            return ParseImportStatement();
        }

        // 处理赋值语句：identifier｜this <- expression 或 a.name <- value 或 this.name <- value 或 a[b] <- value
        // 先尝试解析可能的左值表达式开头
        if (CurrentToken.Type is LangTokenType.Identifier or LangTokenType.This or LangTokenType.LeftBrace
            or LangTokenType.LeftBracket)
        {
            // 检查是否是赋值语句，允许左值表达式包含成员访问或索引访问
            // 例如：identifier <- value, this.name <- value, a[b] <- value
            var savedIndex = CurrentIndex;

            try
            {
                // 尝试解析左值表达式（不包括三元表达式）
                // 先解析主要表达式
                var expr = primaryParser.ParsePrimary();
                // 处理点访问和索引访问等复杂左值表达式
                expressionParser.ParseDotExpr(expr);

                // 检查是否是类型注解
                if (CurrentToken.Type == LangTokenType.Colon)
                {
                    var nextToken = Peek();
                    if (nextToken.Type == LangTokenType.Identifier)
                    {
                        var thirdToken = Peek(2);
                        if (thirdToken.Type == LangTokenType.Assignment)
                        {
                            // 这是带有类型注解的赋值语句
                            CurrentIndex = savedIndex;
                            return ParseSet();
                        }
                        // 检查是否是函数声明：identifier:returnType (params) -> { ... }
                        else if (thirdToken.Type == LangTokenType.LeftParen)
                        {
                            // 这是带有返回类型注解的函数声明
                            CurrentIndex = savedIndex;
                            return functionParser.ParseFuncDeclaration();
                        }
                    }
                }
                // 检查下一个token是否是赋值符号
                else if (CurrentToken.Type == LangTokenType.Assignment)
                {
                    // 这是普通赋值语句
                    CurrentIndex = savedIndex;
                    return ParseSet();
                }

                // 如果不是赋值语句，回退到原始位置
                CurrentIndex = savedIndex;
            }
            catch (Old8Exception)
            {
                throw;
            }
            catch (Exception)
            {
                // 如果解析左值表达式失败，回退到原始位置
                CurrentIndex = savedIndex;
            }
        }

        // 处理增量/减量语句：i++, i--
        if (CurrentToken.Type == LangTokenType.Identifier && Peek().Type == LangTokenType.PlusPlus)
        {
            return ParsePlusPlus();
        }

        if (CurrentToken.Type == LangTokenType.Identifier && Peek().Type == LangTokenType.MinusMinus)
        {
            return ParseMinusMinus();
        }

        // 处理函数调用或函数定义：identifier(params) block
        if (CurrentToken.Type == LangTokenType.Identifier && Peek().Type == LangTokenType.LeftParen)
        {
            return ParseIdentifierLeftParen();
        }

        // 处理表达式语句：允许将函数运行表达式作为语句执行
        // 例如：funcCall(), (lambda)(args), t.test()
        var i = CurrentIndex;
        try
        {
            // 尝试解析为表达式
            var expr = expressionParser.ParseExpression();
            if (expr != null!)
            {
                // 如果是函数调用表达式（Instance），返回 FuncRunStatement
                if (expr is Instance instance)
                {
                    return new FuncRunStatement(instance, expr.Position);
                }

                // 如果是成员方法调用（Operation，且操作符为 Dot，右侧为 Instance），返回 FuncRunStatement
                if (expr is Operation { Opera: LangTokenType.Dot, Right: Instance } operation)
                    // 检查右侧是否是函数调用（Instance）
                {
                    return new FuncRunStatement(operation, expr.Position);
                }

                // 如果是 await 表达式，允许作为独立语句
                if (expr is Old8Lang.AST.Expression.AwaitExpression awaitExpr)
                {
                    // 创建一个 FuncRunStatement 来执行 await 表达式
                    return new FuncRunStatement(awaitExpr, expr.Position);
                }

                // 其他表达式（Operation、LangId 等）不能作为语句
                throw CreateSyntaxError(
                    $"语法错误：表达式 '{expr}' 不能作为独立语句使用。\n" +
                    $"建议：\n" +
                    $"  1. 如果要赋值，请使用 'variable <- {expr}' 格式\n" +
                    $"  2. 如果要调用函数，请添加函数调用 '{expr}(arguments)' 格式\n" +
                    $"  3. 如果要返回值，请使用 'return {expr}' 格式");
            }
        }
        catch (Old8Exception)
        {
            // 重新抛出语法错误和其他 Old8 异常
            throw;
        }
        catch (Exception)
        {
            // 解析失败，回滚，尝试解析为其他语句类型
            CurrentIndex = i;
        }

        // 处理右大括号的情况，这通常意味着当前块结束
        if (CurrentToken.Type == LangTokenType.RightBrace)
        {
            // 不是错误，而是块结束的标志，直接返回空语句
            return new SetStatement(new LangId("", position: new SourcePosition(0, 0)),
                new LangId("", position: new SourcePosition(0, 0)));
        }

        // 无法识别的语句类型，直接抛出语法错误
        throw CreateSyntaxError(
            $"语法错误：无法识别的语句类型 '{CurrentToken.Type}'，值为 '{CurrentToken.Value}'。建议检查语句结构是否正确。");
    }

    /// <summary>
    /// 处理标识符后面跟着左括号的情况，可能是函数定义或函数调用
    /// </summary>
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
    public SetStatement ParseSet()
    {
        // 特殊处理带有类型注解的赋值语句：a:int <- value
        if (CurrentToken.Type == LangTokenType.Identifier && Peek().Type == LangTokenType.Colon)
        {
            // 带有类型注解的赋值语句
            var id = functionParser.ParseTypedIdentifier(false); // 使用 ParseTypedIdentifier 处理类型注解
            Expect(LangTokenType.Assignment);
            var expr = expressionParser.ParseExpression();
            return new SetStatement(id, expr, id.Position);
        }

        // 解析左值表达式
        var leftExpr = expressionParser.ParseExpression();

        // 检查是否有类型注解
        var assumptionType = "";
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);
            assumptionType = CurrentToken.Value;
            Expect(LangTokenType.Identifier);
        }

        Expect(LangTokenType.Assignment);
        var expression = expressionParser.ParseExpression();

        // 处理不同类型的左值表达式
        if (leftExpr is LangId langId)
        {
            // 普通标识符赋值：identifier <- value
            return new SetStatement(new LangId(langId.IdName, assumptionType, position: leftExpr.Position), expression,
                leftExpr.Position);
        }

        // 复杂左值表达式赋值：a[b] <- value 或 a.a <- value 或 this.a <- value
        return new SetStatement(leftExpr, expression, leftExpr.Position);
    }

    // ifStatement = "if" expression block ( "elif" expression block )* ( "else" block )? ;
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

    /// <summary>
    /// funcRunStatement = identifier "(" argList? ")" ;
    /// </summary>
    /// <returns>函数调用</returns>
    public FuncRunStatement ParseFuncRunStatement()
    {
        var funcName = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.LeftParen);
        var arguments = functionParser.ParseArgList();
        Expect(LangTokenType.RightParen);
        return new FuncRunStatement(new Instance(new LangId(funcName), arguments));
    }

    /// <summary>
    /// importStatement = "import" ( importSpecifier "from" )? ( identifier | STRING ) ;
    /// importSpecifier = "{" importItem ( "," importItem )* "}";
    /// importItem = identifier ( "as" identifier )?;
    /// </summary>
    /// <returns>引入模块</returns>
    public ImportStatement ParseImportStatement()
    {
        var importToken = CurrentToken;
        var position = new SourcePosition(importToken.Line, importToken.Column, tokenValue: importToken.Value);
        Expect(LangTokenType.Import);

        List<ImportItem>? importSpecifiers = null;
        bool fromClause = false;
        string moduleName;

        // 检查是否有导入指定项
        if (CurrentToken.Type == LangTokenType.LeftBrace)
        {
            // 解析命名导入：{ item1, item2 as alias2, ... }
            importSpecifiers = new List<ImportItem>();
            Expect(LangTokenType.LeftBrace);

            do
            {
                // 解析导入项
                string name = CurrentToken.Value;
                Expect(LangTokenType.Identifier);

                string? alias = null;
                if (CurrentToken.Type == LangTokenType.As)
                {
                    Expect(LangTokenType.As);
                    alias = CurrentToken.Value;
                    Expect(LangTokenType.Identifier);
                }

                importSpecifiers.Add(new ImportItem(name, alias));
            } while (CurrentToken.Type == LangTokenType.Comma && (CurrentIndex++ > -1));

            Expect(LangTokenType.RightBrace);

            // 解析 from 子句
            fromClause = true;
            Expect(LangTokenType.From);

            // 解析模块名
            if (CurrentToken.Type == LangTokenType.String)
            {
                moduleName = CurrentToken.Value;
                Expect(LangTokenType.String);
            }
            else
            {
                moduleName = CurrentToken.Value;
                Expect(LangTokenType.Identifier);
            }
        }
        else if (CurrentToken.Type == LangTokenType.Identifier)
        {
            // 传统导入：import module
            moduleName = CurrentToken.Value;
            Expect(LangTokenType.Identifier);
        }
        else if (CurrentToken.Type == LangTokenType.String)
        {
            // 传统导入：import "module"
            moduleName = CurrentToken.Value;
            Expect(LangTokenType.String);
        }
        else
        {
            throw CreateSyntaxError("Expected identifier, string, or left brace after import");
        }

        return new ImportStatement(moduleName, position, importSpecifiers, fromClause);
    }

    /// <summary>
    /// nativeStatement = "native" STRING identifier identifier identifier?
    ///                 | "native" STRING identifier "*"
    ///                 | "native" STRING identifier "{" identifierList "}" ;
    /// </summary>
    /// <returns>引入原生方法</returns>
    public NativeStatement ParseNativeStatement()
    {
        Expect(LangTokenType.Native);
        var dllName = CurrentToken.Value;
        Expect(LangTokenType.String);
        var className = CurrentToken.Value;
        Expect(LangTokenType.Identifier);

        // 检查是否是批量导入所有方法：native "DllName" ClassName *
        if (CurrentToken.Type == LangTokenType.Star)
        {
            Expect(LangTokenType.Star);
            return new NativeStatement(dllName, className, importAll: true);
        }

        // 检查是否是选择性导入多个方法：native "DllName" ClassName { Method1, Method2 }
        if (CurrentToken.Type == LangTokenType.LeftBrace)
        {
            Expect(LangTokenType.LeftBrace);
            var methodList = new List<string>();

            // 解析方法列表
            while (CurrentToken.Type != LangTokenType.RightBrace)
            {
                if (CurrentToken.Type == LangTokenType.Identifier)
                {
                    methodList.Add(CurrentToken.Value);
                    Expect(LangTokenType.Identifier);

                    // 如果下一个token是逗号，跳过它
                    if (CurrentToken.Type == LangTokenType.Comma)
                    {
                        Expect(LangTokenType.Comma);
                    }
                }
                else
                {
                    throw CreateSyntaxError($"期望标识符或右大括号，但得到 {CurrentToken.Type}");
                }
            }

            Expect(LangTokenType.RightBrace);

            if (methodList.Count == 0)
            {
                throw CreateSyntaxError("批量导入的方法列表不能为空");
            }

            return new NativeStatement(dllName, className, methodList);
        }

        // 原有的单个方法导入：native "DllName" ClassName MethodName Alias?
        var methodName = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        var alias = "";
        if (CurrentToken.Type == LangTokenType.Identifier)
        {
            alias = CurrentToken.Value;
            Expect(LangTokenType.Identifier);
        }

        return new NativeStatement(dllName, className, methodName, alias);
    }

    /// <summary>
    /// nativeStatic = "native" STRING identifier "->" STRING ;
    /// </summary>
    /// <returns>引入原生静态类</returns>
    public NativeStatement ParseNativeStatic()
    {
        Expect(LangTokenType.Native);
        var dllName = CurrentToken.Value;
        Expect(LangTokenType.String);
        var className = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.Arrow);
        var methodName = CurrentToken.Value;
        Expect(LangTokenType.String);
        return new NativeStatement(dllName, className, methodName);
    }

    /// <summary>
    ///  nativeClass = "native" STRING identifier ("as" identifier)? ;
    /// </summary>
    /// <returns>引入原生类</returns>
    public NativeStatement ParseNativeClass()
    {
        Expect(LangTokenType.Native);
        var dllName = CurrentToken.Value;
        Expect(LangTokenType.String);
        var className = CurrentToken.Value;
        Expect(LangTokenType.Identifier);

        // 检查是否有 as 别名：native "DllName" ClassName as Alias
        if (CurrentToken.Type == LangTokenType.As)
        {
            Expect(LangTokenType.As);
            var alias = CurrentToken.Value;
            Expect(LangTokenType.Identifier);
            return new NativeStatement(dllName, className, alias, isAliasImport: true);
        }

        return new NativeStatement(dllName, className);
    }

    /// <summary>
    /// plusPlus = identifier "++"
    /// </summary>
    /// <returns>i++运算</returns>
    public SetStatement ParsePlusPlus()
    {
        var identifier = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.PlusPlus);
        return new SetStatement(new LangId(identifier),
            new Operation(new LangId(identifier), LangTokenType.Plus, new IntLangValue(1)));
    }

    /// <summary>
    /// minusMinus = identifier "--"
    /// </summary>
    /// <returns>i--运算</returns>
    public SetStatement ParseMinusMinus()
    {
        var identifier = CurrentToken.Value;
        Expect(LangTokenType.Identifier);
        Expect(LangTokenType.MinusMinus);
        return new SetStatement(new LangId(identifier),
            new Operation(new LangId(identifier), LangTokenType.Minus, new IntLangValue(1)));
    }

    /// <summary>
    /// block = "{" statement* "}"
    ///        | statement
    /// </summary>
    /// <returns>块语句</returns>
    public BlockStatement ParseBlock()
    {
        // 处理大括号包围的块
        if (CurrentToken.Type == LangTokenType.LeftBrace)
        {
            Expect(LangTokenType.LeftBrace);
            var statements = new List<IOldLangTree>();

            try
            {
                while (CurrentToken.Type != LangTokenType.RightBrace)
                {
                    // 跳过开头的分号（空语句）
                    SkipOptionalSemicolons();

                    // 如果跳过分号后遇到右大括号，退出循环
                    if (CurrentToken.Type == LangTokenType.RightBrace)
                    {
                        break;
                    }

                    // 尝试解析语句
                    var statement = ParseStatement();

                    // 只有当语句不是空语句时才添加到列表中
                    if (!(statement is SetStatement { Id.IdName: "", Value: LangId { IdName: "" } }))
                    {
                        statements.Add(statement);
                    }

                    SkipOptionalSemicolons(); // 跳过可选的分号分隔符
                }
            }
            catch (SyntaxError)
            {
                // 直接抛出语法错误，不再包装
                throw;
            }
            catch (Exception ex)
            {
                if (ex.Message != "EndOfBlock")
                {
                    throw;
                }
            }

            Expect(LangTokenType.RightBrace);
            return new BlockStatement(statements);
        }

        // 处理单个语句 - 没有大括号的情况
        // 保存当前索引，以便在解析失败时恢复
        var savedIndex = CurrentIndex;

        try
        {
            // 解析单个语句
            var statement = ParseStatement();
            // 返回包含这个语句的BlockStatement
            return new BlockStatement([statement]);
        }
        catch (SyntaxError)
        {
            // 如果解析失败，恢复索引并重新抛出错误
            CurrentIndex = savedIndex;
            throw;
        }
    }

    /// <summary>
    /// 解析try语句
    /// </summary>
    /// <returns>TryStatement对象</returns>
    public TryStatement ParseTryStatement()
    {
        Expect(LangTokenType.Try);

        // 解析try块
        var tryBlock = ParseBlock();

        // 解析catch块列表
        var catchBlocks = new List<(string? exceptionType, LangId? exceptionVar, BlockStatement catchBlock)>();

        // 循环解析catch块
        while (CurrentToken.Type == LangTokenType.Catch)
        {
            Expect(LangTokenType.Catch);

            string? exceptionType = null;
            LangId? exceptionVar = null;

            // 检查是否有异常类型和变量
            if (CurrentToken.Type == LangTokenType.LeftParen)
            {
                Expect(LangTokenType.LeftParen);

                // 解析异常类型（如果有）
                if (CurrentToken.Type == LangTokenType.Identifier)
                {
                    exceptionType = CurrentToken.Value;
                    CurrentIndex++;

                    // 解析异常变量（如果有）
                    if (CurrentToken.Type == LangTokenType.Identifier)
                    {
                        exceptionVar = new LangId(CurrentToken.Value, position: CreateSourcePosition(CurrentToken));
                        CurrentIndex++;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(exceptionType) && !exceptionType.Contains("Exception"))
                        {
                            exceptionVar = new LangId(exceptionType, position: CreateSourcePosition(CurrentToken));
                            exceptionType = null;
                        }
                    }
                }

                Expect(LangTokenType.RightParen);
            }

            // 解析catch块
            var catchBlock = ParseBlock();
            catchBlocks.Add((exceptionType, exceptionVar, catchBlock));
        }

        // 解析finally块（可选）
        BlockStatement? finallyBlock = null;
        if (CurrentToken.Type == LangTokenType.Finally)
        {
            Expect(LangTokenType.Finally);
            // 直接解析finally块，不使用ParseBlock，避免finally被视为单独的语句
            var statements = new List<IOldLangTree>();
            if (CurrentToken.Type == LangTokenType.LeftBrace)
            {
                CurrentIndex++;
                while (CurrentToken.Type != LangTokenType.RightBrace)
                {
                    statements.Add(ParseStatement());
                }

                CurrentIndex++;
            }
            else
            {
                statements.Add(ParseStatement());
            }

            finallyBlock = new BlockStatement(statements);
        }

        // 创建TryStatement对象
        return new TryStatement(tryBlock, catchBlocks, finallyBlock, CreateSourcePosition(CurrentToken));
    }

    #endregion
}