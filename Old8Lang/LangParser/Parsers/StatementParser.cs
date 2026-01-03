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

        // 处理 using 语句（资源管理）
        if (CurrentToken.Type == LangTokenType.Using)
        {
            return ParseUsingStatement();
        }

        // 处理 defer 语句（延迟执行）
        if (CurrentToken.Type == LangTokenType.Defer)
        {
            return ParseDeferStatement();
        }

        // 处理循环语句 For 和 For in
        if (CurrentToken.Type == LangTokenType.For)
        {
            // 检查是否是 for-in 语句，格式为：for 标识符 (, 标识符)* in 表达式
            // 需要确保 "in" 关键字前面只有标识符和逗号
            var tempIndex = CurrentIndex + 1;
            var foundIn = false;
            var scanLimit = Math.Min(tempIndex + 20, Tokens.Count); // 限制前瞻深度

            // 跳过所有标识符和逗号，查找 "in" 关键字
            while (tempIndex < scanLimit)
            {
                var token = Tokens[tempIndex];
                if (token.Type == LangTokenType.In)
                {
                    foundIn = true;
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

        // 处理 select 语句（Channel 多路选择）
        // 注意：在语句位置的 select 是 select 语句，而不是 LINQ 的 select
        if (CurrentToken.Type == LangTokenType.Select)
        {
            return ParseSelectStatement();
        }

        // 处理异步函数定义和异步 for-in 循环：async func / async for
        if (CurrentToken.Type == LangTokenType.Async)
        {
            // 在消费 async token 之前收集文档注释
            var docComment = CollectPrecedingDocComments();

            Expect(LangTokenType.Async);

            // 检查是否是 async for-in
            if (CurrentToken.Type == LangTokenType.For)
            {
                return ParseAsyncForInStatement();
            }

            // 否则是 async func
            Expect(LangTokenType.Func);
            return functionParser.ParseAsyncFuncDeclaration(docComment);
        }

        // 处理函数定义（包括带有修饰符的函数定义）
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

        // 处理class或mixin定义：[abstract] class identifier block 或 mixin identifier block
        if (CurrentToken.Type == LangTokenType.Class ||
            CurrentToken.Type == LangTokenType.Mixin ||
            CurrentToken.Type == LangTokenType.Abstract)
        {
            return classParser.ParseClassDeclaration();
        }

        // 处理interface定义：interface identifier block
        if (CurrentToken.Type == LangTokenType.Interface)
        {
            return classParser.ParseInterfaceDeclaration();
        }

        // 处理enum定义：enum identifier { member1, member2 = value, ... }
        if (CurrentToken.Type == LangTokenType.Enum)
        {
            return ParseEnumDeclaration();
        }

        // 处理import语句：import module, lazy import module, 或 dynamic import module
        if (CurrentToken.Type == LangTokenType.Import ||
            CurrentToken.Type == LangTokenType.Lazy ||
            CurrentToken.Type == LangTokenType.Dynamic)
        {
            return ParseImportStatement();
        }

        // 特殊处理数组解构赋值和对象解构赋值
        if (CurrentToken.Type == LangTokenType.LeftBracket)
        {
            // 数组解构赋值：[a, b] <- [1, 2]
            return ParseArrayDestructuring();
        }
        else if (CurrentToken.Type == LangTokenType.LeftBrace)
        {
            // 对象解构赋值：{name, age} <- person
            return ParseObjectDestructuring();
        }

        // 处理赋值语句：identifier｜this <- expression 或 a.name <- value 或 this.name <- value 或 a[b] <- value
        // 先尝试解析可能的左值表达式开头
        if (CurrentToken.Type is LangTokenType.Identifier or LangTokenType.This)
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

                        // 检查是否为可空类型（例如 "a:int? <- value"）
                        if (thirdToken.Type == LangTokenType.Question)
                        {
                            var fourthToken = Peek(3);
                            if (fourthToken.Type == LangTokenType.Assignment)
                            {
                                // 这是带有可空类型注解的赋值语句
                                CurrentIndex = savedIndex;
                                return ParseSet();
                            }
                            // 检查是否是可空联合类型或可空交叉类型（例如 "a:int? | string" 或 "a:T? & U"）
                            else if (fourthToken.Type == LangTokenType.Pipe ||
                                     fourthToken.Type == LangTokenType.Ampersand)
                            {
                                // 这是带有联合类型或交叉类型注解的赋值语句
                                CurrentIndex = savedIndex;
                                return ParseSet();
                            }
                            // 检查是否是可空返回类型的函数声明：identifier:returnType? (params) -> { ... }
                            else if (fourthToken.Type == LangTokenType.LeftParen)
                            {
                                // 这是带有可空返回类型注解的函数声明
                                CurrentIndex = savedIndex;
                                return functionParser.ParseFuncDeclaration();
                            }
                        }
                        else if (thirdToken.Type == LangTokenType.Assignment)
                        {
                            // 这是带有类型注解的赋值语句
                            CurrentIndex = savedIndex;
                            return ParseSet();
                        }
                        // 检查是否是联合类型或交叉类型（例如 "a:int | string" 或 "a:A & B"）
                        else if (thirdToken.Type == LangTokenType.Pipe || thirdToken.Type == LangTokenType.Ampersand)
                        {
                            // 这是带有联合类型或交叉类型注解的赋值语句
                            CurrentIndex = savedIndex;
                            return ParseSet();
                        }
                        // 检查是否是泛型类型（例如 "items:List<T>"）
                        else if (thirdToken.Type == LangTokenType.LessThan)
                        {
                            // 跳过泛型类型注解，找到泛型结束位置
                            var tokenIndex = SkipGenericTypeAnnotation(2); // 从 < 开始
                            var tokenAfterGeneric = Peek(tokenIndex);

                            // 检查泛型类型后面是否有赋值符号或左括号
                            if (tokenAfterGeneric.Type == LangTokenType.Assignment)
                            {
                                // 这是带有泛型类型注解的赋值语句：items:List<T> <- value
                                CurrentIndex = savedIndex;
                                return ParseSet();
                            }
                            else if (tokenAfterGeneric.Type == LangTokenType.LeftParen)
                            {
                                // 这是带有泛型返回类型的函数声明：func<T>() -> List<T>
                                CurrentIndex = savedIndex;
                                return functionParser.ParseFuncDeclaration();
                            }
                            else
                            {
                                // 只有类型注解，没有赋值，这是类字段声明（会初始化为 Null）
                                CurrentIndex = savedIndex;
                                return ParseSet();
                            }
                        }
                        // 检查是否是函数声明：identifier:returnType (params) -> { ... }
                        else if (thirdToken.Type == LangTokenType.LeftParen)
                        {
                            // 这是带有返回类型注解的函数声明
                            CurrentIndex = savedIndex;
                            return functionParser.ParseFuncDeclaration();
                        }
                        else
                        {
                            // 其他情况，可能是只有类型注解没有赋值的字段声明
                            // 例如：private items:int 或 private value:T
                            CurrentIndex = savedIndex;
                            return ParseSet();
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

                // 如果是super方法调用（Operation，且操作符为 Dot，左侧为 SuperExpression，右侧为 Instance），返回 FuncRunStatement
                if (expr is Operation
                    {
                        Opera: LangTokenType.Dot, Left: SuperExpression, Right: Instance
                    } superOperation)
                {
                    return new FuncRunStatement(superOperation, expr.Position);
                }

                // 如果是this方法调用（Operation，且操作符为 Dot，左侧为 LangId { IdName: "this" }，右侧为 Instance），返回 FuncRunStatement
                if (expr is Operation
                    {
                        Opera: LangTokenType.Dot, Left: LangId { IdName: "this" }, Right: Instance
                    } thisOperation)
                {
                    return new FuncRunStatement(thisOperation, expr.Position);
                }

                // 如果是 await 表达式，允许作为独立语句
                if (expr is AwaitExpression awaitExpr)
                {
                    // 创建一个 FuncRunStatement 来执行 await 表达式
                    return new FuncRunStatement(awaitExpr, expr.Position);
                }

                // 如果是泛型实例化表达式（且是函数调用），允许作为独立语句
                if (expr is GenericInstanceExpression genericExpr && genericExpr.IsFunctionCall)
                {
                    return new FuncRunStatement(genericExpr, expr.Position);
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
        // 特殊处理带有类型注解的赋值语句：a:int <- value 或 a:int (只有类型注解)
        if (CurrentToken.Type == LangTokenType.Identifier && Peek().Type == LangTokenType.Colon)
        {
            // 带有类型注解的赋值语句
            var id = functionParser.ParseTypedIdentifier(false); // 使用 ParseTypedIdentifier 处理类型注解

            // 检查是否有赋值符号
            if (CurrentToken.Type == LangTokenType.Assignment)
            {
                Expect(LangTokenType.Assignment);
                var expr = expressionParser.ParseExpression();
                return new SetStatement(id, expr, id.Position);
            }
            else
            {
                // 只有类型注解，没有赋值，创建赋值 Null 的语句
                var nullExpr = new NullLangValue(id.Position);
                return new SetStatement(id, nullExpr, id.Position);
            }
        }

        // 解析左值表达式 - 只能是标识符、this或复杂左值（如a.b或a[b]），不能是解构模式
        var leftExpr = primaryParser.ParsePrimary();

        // 处理点访问和索引访问等复杂左值表达式
        leftExpr = expressionParser.ParseDotExpr(leftExpr);

        // 检查是否有类型注解
        var assumptionType = "";
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);

            // 使用 FunctionParser 的 ParseComplexTypeAnnotation 处理复杂类型注解
            assumptionType = functionParser.ParseComplexTypeAnnotation();
        }

        // 检查是否有赋值符号
        // 如果没有赋值符号，说明这是一个只有类型注解的字段声明（会初始化为 Null）
        if (CurrentToken.Type != LangTokenType.Assignment)
        {
            // 只有类型注解，没有赋值，创建一个赋值 Null 的语句
            if (leftExpr is LangId leftLangId)
            {
                var nullExpr = new NullLangValue(leftLangId.Position);
                return new SetStatement(
                    new LangId(leftLangId.IdName, assumptionType, null, position: leftLangId.Position), nullExpr,
                    leftLangId.Position);
            }
            else
            {
                throw CreateSyntaxError("只有类型注解没有赋值时，左值必须是标识符");
            }
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

    /// <summary>
    /// 解析数组解构赋值：[a, b] &lt;- [1, 2]
    /// </summary>
    private SetStatement ParseArrayDestructuring()
    {
        Expect(LangTokenType.LeftBracket);
        var position = CreateSourcePosition(CurrentToken);

        // 解析解构的标识符列表
        var identifiers = new List<string>();
        while (CurrentToken.Type != LangTokenType.RightBracket)
        {
            // 允许跳过空元素，如 [a, , b]
            if (CurrentToken.Type == LangTokenType.Comma)
            {
                identifiers.Add("null");
                Expect(LangTokenType.Comma);
                continue;
            }

            // 解析标识符，只能是简单标识符，不能是复杂表达式
            if (CurrentToken.Type != LangTokenType.Identifier)
            {
                throw CreateSyntaxError("数组解构赋值中的标识符必须是简单标识符");
            }

            var ident = CurrentToken.Value;
            identifiers.Add(ident);
            Expect(LangTokenType.Identifier);

            // 检查是否还有更多元素
            if (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);
            }
        }

        Expect(LangTokenType.RightBracket);

        // 检查是否有类型注解
        var assumptionType = "";
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);
            assumptionType = CurrentToken.Value;
            Expect(LangTokenType.Identifier);

            // 检查是否为可空类型（例如 "int?"）
            if (CurrentToken.Type == LangTokenType.Question)
            {
                assumptionType += "?";
                Expect(LangTokenType.Question);
            }
        }

        Expect(LangTokenType.Assignment);
        var expression = expressionParser.ParseExpression();

        // 创建一个特殊的SetStatement，其中Id是一个Operation，表示数组解构
        var destructExpr = new Operation(
            new LangId("array_destruct"),
            LangTokenType.LeftBracket,
            new LangId(string.Join(",", identifiers)),
            position);

        return new SetStatement(destructExpr, expression, position);
    }

    /// <summary>
    /// 解析对象解构赋值：{name, age} &lt;- person
    /// </summary>
    private SetStatement ParseObjectDestructuring()
    {
        Expect(LangTokenType.LeftBrace);
        var position = CreateSourcePosition(CurrentToken);

        // 解析解构的属性列表
        var properties = new List<string>();
        while (CurrentToken.Type != LangTokenType.RightBrace)
        {
            // 解析属性名
            if (CurrentToken.Type != LangTokenType.Identifier)
            {
                throw CreateSyntaxError("对象解构赋值中的属性名必须是简单标识符");
            }

            var propertyName = CurrentToken.Value;
            Expect(LangTokenType.Identifier);

            // 检查是否有别名，如 {name: newName}
            var aliasName = propertyName;
            if (CurrentToken.Type == LangTokenType.Colon)
            {
                Expect(LangTokenType.Colon);
                if (CurrentToken.Type != LangTokenType.Identifier)
                {
                    throw CreateSyntaxError("对象解构赋值中的别名必须是简单标识符");
                }

                aliasName = CurrentToken.Value;
                Expect(LangTokenType.Identifier);

                // 格式：propertyName:aliasName
                properties.Add($"{propertyName}:{aliasName}");
            }
            else
            {
                // 只有属性名，没有别名
                properties.Add(propertyName);
            }

            // 检查是否还有更多属性
            if (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);
            }
        }

        Expect(LangTokenType.RightBrace);

        // 检查是否有类型注解
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);
            Expect(LangTokenType.Identifier);
        }

        Expect(LangTokenType.Assignment);
        var expression = expressionParser.ParseExpression();

        // 创建一个特殊的SetStatement，其中Id是一个Operation，表示对象解构
        var destructExpr = new Operation(
            new LangId("object_destruct"),
            LangTokenType.LeftBrace,
            new LangId(string.Join(",", properties)),
            position);

        return new SetStatement(destructExpr, expression, position);
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
        functionParser.ParseArgList(out var positionalArgs, out var namedArgs);
        Expect(LangTokenType.RightParen);
        return new FuncRunStatement(new Instance(new LangId(funcName), positionalArgs, namedArgs));
    }

    /// <summary>
    /// importStatement = "import" ( "lazy" )? ( importSpecifier "from" )? ( identifier | STRING ) ;
    /// importSpecifier = "{" importItem ( "," importItem )* "}" | importItem ( "," importItem )* ;
    /// importItem = identifier ( "as" identifier )?;
    ///
    /// 支持的语法：
    /// - import module
    /// - import "module"
    /// - import module as alias
    /// - import { item1, item2 as alias2 } from module
    /// - import item1, item2 from module
    /// - lazy import module
    /// - lazy import module as alias
    /// - lazy import { item1, item2 } from module
    /// - lazy import item1, item2 from module
    /// </summary>
    /// <returns>引入模块</returns>
    public ImportStatement ParseImportStatement()
    {
        var importToken = CurrentToken;
        var position = new SourcePosition(importToken.Line, importToken.Column, tokenValue: importToken.Value);

        List<ImportItem>? importSpecifiers = null;
        var fromClause = false;
        var isSelective = false;
        var isLazy = false;
        var isDynamic = false;
        LangExpression? dynamicModuleExpression = null;
        string moduleName;

        // 检查导入修饰符：lazy import 或 dynamic import
        if (CurrentToken.Type == LangTokenType.Lazy)
        {
            Expect(LangTokenType.Lazy); // 消耗 "lazy"
            isLazy = true;
            // 消耗 "import"
        }
        else if (CurrentToken.Type == LangTokenType.Dynamic)
        {
            Expect(LangTokenType.Dynamic); // 消耗 "dynamic"
            isDynamic = true;
        }

        // 普通导入
        // 消耗 "import"
        Expect(LangTokenType.Import); // 消耗 "import"

        // 检查是否是通配符导入：import * from "module"
        if (CurrentToken.Type == LangTokenType.Star)
        {
            Expect(LangTokenType.Star); // 消耗 "*"

            // 必须有 from 子句
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

            // 通配符导入：importSpecifiers 为空列表表示导入所有
            importSpecifiers = new List<ImportItem>();
        }
        // 检查是否有导入指定项
        else if (CurrentToken.Type == LangTokenType.LeftBrace)
        {
            // 解析命名导入：{ item1, item2 as alias2, ... }
            importSpecifiers = new List<ImportItem>();
            Expect(LangTokenType.LeftBrace);

            do
            {
                // 解析导入项
                var name = CurrentToken.Value;
                Expect(LangTokenType.Identifier);

                string? alias = null;
                if (CurrentToken.Type == LangTokenType.As)
                {
                    Expect(LangTokenType.As);
                    alias = CurrentToken.Value;
                    Expect(LangTokenType.Identifier);
                }

                importSpecifiers.Add(new ImportItem(name, alias));
            } while (CurrentToken.Type == LangTokenType.Comma && CurrentIndex++ > -1);

            Expect(LangTokenType.RightBrace);

            // 这是选择性导入
            isSelective = true;

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
        else if (isDynamic && CurrentToken.Type == LangTokenType.String)
        {
            // 动态导入：字符串字面量应该创建为动态表达式
            dynamicModuleExpression = new StringLangValue(CurrentToken.Value,
                new SourcePosition(CurrentToken.Line, CurrentToken.Column));
            Expect(LangTokenType.String); // 消耗字符串
            moduleName = "__dynamic_module__";
        }
        else if (isDynamic && CurrentToken.Type == LangTokenType.Identifier)
        {
            // 动态导入：创建标识符表达式，避免在解析阶段调用表达式解析器
            dynamicModuleExpression = new LangId(CurrentToken.Value, "", null,
                position: new SourcePosition(CurrentToken.Line, CurrentToken.Column));
            Expect(LangTokenType.Identifier); // 消耗标识符
            moduleName = "__dynamic_module__";
        }
        else if (isDynamic)
        {
            // 动态导入：解析模块名表达式（复杂表达式）
            dynamicModuleExpression ??= expressionParser.ParseExpression();
            moduleName = "__dynamic_module__";
        }
        else if (CurrentToken.Type == LangTokenType.Identifier)
        {
            // 可能是传统导入：import module
            // 或者是选择导入：import item1, item2 from module

            // 保存当前位置，用于前瞻检查
            var savedIndex = CurrentIndex;

            // 尝试解析第一个标识符
            var firstIdentifier = CurrentToken.Value;
            Expect(LangTokenType.Identifier);

            // 检查是否是选择导入：item1, item2 from module
            if (CurrentToken.Type == LangTokenType.Comma ||
                CurrentToken is { Type: LangTokenType.Identifier, Value: "from" })
            {
                // 这是选择导入：import item1, item2 from module
                isSelective = true;
                importSpecifiers =
                [
                    new ImportItem(firstIdentifier)
                    // 解析更多的导入项
                ];

                // 解析更多的导入项
                while (CurrentToken.Type == LangTokenType.Comma)
                {
                    Expect(LangTokenType.Comma);
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
                }

                // 必须有 from 子句
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
            else
            {
                // 这是传统导入：import module
                // 回退第一个标识符作为模块名
                CurrentIndex = savedIndex;
                moduleName = CurrentToken.Value;
                Expect(LangTokenType.Identifier);
            }
        }
        else if (CurrentToken.Type == LangTokenType.String)
        {
            // 传统导入：import "module"
            moduleName = CurrentToken.Value;
            Expect(LangTokenType.String);
        }
        else
        {
            throw CreateSyntaxError("Expected identifier, string, dynamic expression, or left brace after import");
        }

        // 检查是否有模块别名：as alias
        string? moduleAlias = null;
        if (CurrentToken.Type == LangTokenType.As)
        {
            Expect(LangTokenType.As);
            moduleAlias = CurrentToken.Value;
            Expect(LangTokenType.Identifier);
        }

        return new ImportStatement(moduleName, position, importSpecifiers, fromClause, moduleAlias, isLazy, isSelective,
            isDynamic, dynamicModuleExpression);
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

    /// <summary>
    /// 跳过泛型类型注解，返回泛型结束后的token索引偏移量
    /// 例如：对于 "List<T>"，从 < 开始跳过，返回 > 后的token位置
    /// 支持嵌套泛型：List<List<int>>
    /// </summary>
    /// <param name="startOffset">起始偏移量（相对于当前位置）</param>
    /// <returns>泛型结束后的token索引偏移量</returns>
    private int SkipGenericTypeAnnotation(int startOffset)
    {
        var offset = startOffset;
        var depth = 0;
        var started = false;

        while (offset < 100) // 防止无限循环
        {
            var token = Peek(offset);

            if (token.Type == LangTokenType.EndOfFile)
            {
                break;
            }

            if (token.Type == LangTokenType.LessThan)
            {
                depth++;
                started = true;
            }
            else if (token.Type == LangTokenType.GreaterThan)
            {
                depth--;
                if (depth == 0 && started)
                {
                    // 找到匹配的右尖括号，返回下一个位置
                    return offset + 1;
                }
            }

            offset++;
        }

        // 如果没找到匹配的右尖括号，返回当前偏移量
        return offset;
    }

    /// <summary>
    /// 解析并消费泛型类型注解，返回类型字符串（不包括 < 和 >）
    /// 例如：对于 "<int>"，返回 "int"
    /// 支持嵌套泛型：<List<int>> 返回 "List<int>"
    /// </summary>
    /// <returns>泛型类型字符串</returns>
    private string SkipAndParseGenericTypeAnnotation()
    {
        var result = "";
        Expect(LangTokenType.LessThan);
        var depth = 1;

        while (depth > 0)
        {
            if (CurrentToken.Type == LangTokenType.EndOfFile)
            {
                throw CreateSyntaxError("意外的文件结束符，期望 '>'");
            }

            if (CurrentToken.Type == LangTokenType.LessThan)
            {
                result += "<";
                depth++;
                CurrentIndex++;
            }
            else if (CurrentToken.Type == LangTokenType.GreaterThan)
            {
                depth--;
                if (depth > 0)
                {
                    result += ">";
                }

                CurrentIndex++;
            }
            else if (CurrentToken.Type == LangTokenType.Comma)
            {
                result += ", ";
                CurrentIndex++;
            }
            else if (CurrentToken.Type == LangTokenType.Question)
            {
                result += "?";
                CurrentIndex++;
            }
            else
            {
                result += CurrentToken.Value;
                CurrentIndex++;
            }
        }

        return result;
    }

    #endregion

    #region Enum Declaration

    // enumDeclaration = "enum" identifier "{" enumMember ("," enumMember)* "}" ;
    // enumMember = identifier ("=" expression)? ;
    private EnumInit ParseEnumDeclaration()
    {
        var startToken = CurrentToken;
        var startPos = CreateSourcePosition(startToken);

        // 消费 "enum" 关键字
        Expect(LangTokenType.Enum);

        // 解析枚举名称
        if (CurrentToken.Type != LangTokenType.Identifier)
        {
            throw CreateSyntaxError($"枚举定义需要枚举名称，但得到 {CurrentToken.Type}");
        }

        var enumName = CurrentToken.Value;
        CurrentIndex++;

        // 消费左花括号
        Expect(LangTokenType.LeftBrace);

        // 解析枚举成员
        var members = new List<(string name, LangExpression? value)>();

        // 处理空枚举
        if (CurrentToken.Type == LangTokenType.RightBrace)
        {
            CurrentIndex++;
            return new EnumInit(enumName, members, startPos);
        }

        // 解析第一个成员
        while (true)
        {
            // 解析成员名称
            if (CurrentToken.Type != LangTokenType.Identifier)
            {
                throw CreateSyntaxError($"枚举成员需要标识符，但得到 {CurrentToken.Type}");
            }

            var memberName = CurrentToken.Value;
            CurrentIndex++;

            // 检查是否有显式赋值
            LangExpression? memberValue = null;
            if (CurrentToken.Type == LangTokenType.Assignment)
            {
                CurrentIndex++;
                // 解析值表达式（必须是常量表达式，通常是整数字面量）
                memberValue = expressionParser.ParseExpression();
            }

            members.Add((memberName, memberValue));

            // 检查是否有更多成员
            if (CurrentToken.Type == LangTokenType.Comma)
            {
                CurrentIndex++;
                // 允许尾随逗号
                if (CurrentToken.Type == LangTokenType.RightBrace)
                {
                    break;
                }

                continue;
            }

            break;
        }

        // 消费右花括号
        Expect(LangTokenType.RightBrace);

        return new EnumInit(enumName, members, startPos);
    }

    #endregion

    #region Using Statement

    /// <summary>
    /// 解析 using 语句（资源管理）
    /// 语法：
    ///   using varName <- expr { statements }
    ///   using expr { statements }
    /// </summary>
    private UsingStatement ParseUsingStatement()
    {
        var startPos = new SourcePosition(CurrentToken.Line, CurrentToken.Column);
        Expect(LangTokenType.Using);

        string? variableName = null;
        LangExpression resourceExpression;

        // 检查是否是变量声明形式: using varName <- expr
        if (CurrentToken.Type == LangTokenType.Identifier)
        {
            var nextToken = Peek();
            if (nextToken.Type == LangTokenType.Assignment)
            {
                // using varName <- expr { ... }
                variableName = CurrentToken.Value;
                CurrentIndex++; // 消费标识符
                Expect(LangTokenType.Assignment); // 消费 <-
                resourceExpression = expressionParser.ParseExpression();
            }
            else
            {
                // using expr { ... }
                resourceExpression = expressionParser.ParseExpression();
            }
        }
        else
        {
            // using expr { ... }
            resourceExpression = expressionParser.ParseExpression();
        }

        // 解析 using 块（ParseBlock 会自己处理花括号）
        var blockStatement = ParseBlock();

        return new UsingStatement(variableName, resourceExpression, blockStatement, startPos);
    }

    /// <summary>
    /// 解析 defer 语句
    /// 语法：
    ///   defer statement
    ///   defer { ... }
    /// </summary>
    private DeferStatement ParseDeferStatement()
    {
        var startPos = new SourcePosition(CurrentToken.Line, CurrentToken.Column);
        Expect(LangTokenType.Defer);

        OldStatement statement;

        // 检查是否是代码块形式
        if (CurrentToken.Type == LangTokenType.LeftBrace)
        {
            // defer { ... }
            statement = ParseBlock();
        }
        else
        {
            // defer statement
            statement = ParseStatement();
        }

        return new DeferStatement(statement, startPos);
    }

    #endregion

    #region Select Statement

    /// <summary>
    /// 解析 select 语句（Channel 多路选择）
    /// 语法：
    ///   select {
    ///     case value from channel -> { ... }    // 接收操作
    ///     case channel <- value -> { ... }      // 发送操作
    ///     default -> { ... }
    ///   }
    ///
    /// 注意：
    /// - 接收操作使用 "from" 关键字：case value from ch -> { }
    /// - 发送操作使用 "<-" 运算符：case ch <- value -> { }
    /// - 这样可以明确区分两种操作，避免语法歧义
    /// </summary>
    private SelectStatement ParseSelectStatement()
    {
        var startPos = new SourcePosition(CurrentToken.Line, CurrentToken.Column);
        Expect(LangTokenType.Select);
        Expect(LangTokenType.LeftBrace);

        var cases = new List<SelectCase>();
        BlockStatement? defaultCase = null;

        while (CurrentToken.Type != LangTokenType.RightBrace)
        {
            if (CurrentToken.Type == LangTokenType.Case)
            {
                Expect(LangTokenType.Case);

                // 解析第一个表达式
                var firstExpr = expressionParser.ParseExpression();

                // 判断是接收还是发送操作
                if (CurrentToken.Type == LangTokenType.From)
                {
                    // 接收操作：case value from channel -> { ... }
                    Expect(LangTokenType.From);

                    // 解析 channel 表达式
                    var channelExpr = expressionParser.ParseExpression();

                    Expect(LangTokenType.Arrow);

                    // 解析块
                    var block = ParseBlock();

                    // 提取变量名（如果第一个表达式是标识符）
                    string? variableName = null;
                    if (firstExpr is LangId id)
                    {
                        variableName = id.IdName;
                    }

                    cases.Add(new SelectCase(
                        channelExpression: channelExpr,
                        variableName: variableName,
                        blockStatement: block,
                        position: startPos
                    ));
                }
                else if (CurrentToken.Type == LangTokenType.Assignment)
                {
                    // 发送操作：case channel <- value -> { ... }
                    Expect(LangTokenType.Assignment);

                    // 解析发送值表达式
                    var sendValueExpr = expressionParser.ParseExpression();

                    Expect(LangTokenType.Arrow);

                    // 解析块
                    var block = ParseBlock();

                    cases.Add(new SelectCase(
                        channelExpression: firstExpr,
                        sendValueExpression: sendValueExpr,
                        blockStatement: block,
                        position: startPos
                    ));
                }
                else
                {
                    throw CreateSyntaxError(
                        $"select case 中期望 'from'（接收操作）或 '<-'（发送操作），但得到 {CurrentToken.Type}");
                }
            }
            else if (CurrentToken.Type == LangTokenType.Default)
            {
                Expect(LangTokenType.Default);
                Expect(LangTokenType.Arrow);
                defaultCase = ParseBlock();
            }
            else
            {
                throw CreateSyntaxError(
                    $"select 语句中期望 'case' 或 'default'，但得到 {CurrentToken.Type}");
            }
        }

        Expect(LangTokenType.RightBrace);
        return new SelectStatement(cases, defaultCase, startPos);
    }

    #endregion
}