using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Error;

namespace Old8Lang.LangParser;

public class LangParser(List<LangToken> tokens, string? sourceCode = null, string? fileName = null)
{
    #region 基础操作

    private int CurrentIndex;

    private LangToken CurrentToken => CurrentIndex >= tokens.Count
        ? new LangToken("", LangTokenType.EndOfFile, CurrentIndex)
        : tokens[CurrentIndex];

    /// <summary>
    /// 获取错误位置附近的源代码上下文
    /// </summary>
    /// <param name="line">错误行号</param>
    /// <returns>错误位置附近的源代码上下文</returns>
    private string[] GetSourceContext(int line)
    {
        if (string.IsNullOrEmpty(sourceCode))
        {
            return [];
        }

        var lines = sourceCode.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var contextLines = new List<string>();

        // 获取错误行前后的上下文，最多显示3行上下文
        // 确保line至少为0，避免负数行号导致的问题
        var safeLine = Math.Max(0, line);
        var startLine = Math.Max(0, safeLine - 2);
        var endLine = Math.Min(lines.Length - 1, safeLine + 1);

        for (var i = startLine; i <= endLine; i++)
        {
            contextLines.Add(lines[i]);
        }

        return contextLines.ToArray();
    }

    /// <summary>
    /// 创建完整的位置信息
    /// </summary>
    /// <param name="token">令牌</param>
    /// <returns>完整的位置信息</returns>
    private SourcePosition CreateSourcePosition(LangToken token)
    {
        return new SourcePosition(
            token.Line,
            token.Column,
            fileName,
            token.Value);
    }

    private SyntaxError CreateSyntaxError(string message)
    {
        var context = GetSourceContext(CurrentToken.Line);
        return new SyntaxError(
            CurrentToken.Value,
            CurrentToken.Line,
            CurrentToken.Column,
            fileName,
            message,
            context);
    }

    private void Expect(LangTokenType type)
    {
        if (CurrentToken.Type == type)
        {
            CurrentIndex++;
        }
        else
        {
            var actualType = CurrentToken.Type;
            var actualValue = CurrentToken.Value;

            var detailedMessage = type switch
            {
                LangTokenType.RightParen => $"语法错误：缺少右括号 ')。在 '{actualValue}' 处期望右括号。",
                LangTokenType.RightBracket => $"语法错误：缺少右方括号 ']。在 '{actualValue}' 处期望右方括号。",
                LangTokenType.RightBrace => $"语法错误：缺少右大括号 '}}。在 '{actualValue}' 处期望右大括号。",
                LangTokenType.LeftParen => $"语法错误：缺少左括号 '。在 '{actualValue}' 处期望左括号。",
                LangTokenType.LeftBracket => $"语法错误：缺少左方括号 '[。在 '{actualValue}' 处期望左方括号。",
                LangTokenType.LeftBrace => $"语法错误：缺少左大括号 '{{。在 '{actualValue}' 处期望左大括号。",
                LangTokenType.Comma => $"语法错误：缺少逗号 ','。在 '{actualValue}' 处期望逗号。",
                LangTokenType.Arrow => $"语法错误：缺少箭头 '->。在 '{actualValue}' 处期望箭头。",
                LangTokenType.Colon => $"语法错误：缺少冒号 ':'。在 '{actualValue}' 处期望冒号。",
                LangTokenType.Assignment => $"语法错误：缺少赋值符号 '<-。在 '{actualValue}' 处期望赋值符号。",
                LangTokenType.Identifier => $"语法错误：缺少标识符。在 '{actualValue}' 处期望标识符。",
                LangTokenType.String => $"语法错误：缺少字符串字面量。在 '{actualValue}' 处期望字符串。",
                LangTokenType.Number => $"语法错误：缺少数字字面量。在 '{actualValue}' 处期望数字。",
                LangTokenType.If => $"语法错误：缺少 'if' 关键字。在 '{actualValue}' 处期望 'if'。",
                LangTokenType.Else => $"语法错误：缺少 'else' 关键字。在 '{actualValue}' 处期望 'else'。",
                LangTokenType.While => $"语法错误：缺少 'while' 关键字。在 '{actualValue}' 处期望 'while'。",
                LangTokenType.For => $"语法错误：缺少 'for' 关键字。在 '{actualValue}' 处期望 'for'。",
                LangTokenType.Func => $"语法错误：缺少 'func' 关键字。在 '{actualValue}' 处期望 'func'。",
                LangTokenType.Class => $"语法错误：缺少 'class' 关键字。在 '{actualValue}' 处期望 'class'。",
                LangTokenType.Import => $"语法错误：缺少 'import' 关键字。在 '{actualValue}' 处期望 'import'。",
                LangTokenType.Return => $"语法错误：缺少 'return' 关键字。在 '{actualValue}' 处期望 'return'。",
                _ => $"语法错误：期望 {type}，但得到了 {actualType} '{actualValue}'。",
            };

            var suggestion = type switch
            {
                LangTokenType.RightParen => "建议：检查是否缺少右括号或括号不匹配。",
                LangTokenType.RightBracket => "建议：检查是否缺少右方括号或方括号不匹配。",
                LangTokenType.RightBrace => "建议：检查是否缺少右大括号或大括号不匹配。",
                LangTokenType.LeftParen => "建议：检查是否缺少左括号或括号不匹配。",
                LangTokenType.LeftBracket => "建议：检查是否缺少左方括号或方括号不匹配。",
                LangTokenType.LeftBrace => "建议：检查是否缺少左大括号或大括号不匹配。",
                LangTokenType.Comma => "建议：检查是否缺少逗号分隔符。",
                LangTokenType.Arrow => "建议：检查 lambda 表达式是否缺少箭头符号。",
                LangTokenType.Colon => "建议：检查字典定义或类型注解是否缺少冒号。",
                LangTokenType.Assignment => "建议：检查变量赋值是否使用了正确的赋值符号 '<-。",
                LangTokenType.Identifier => "建议：检查是否需要添加标识符名称。",
                LangTokenType.String => "建议：检查字符串是否正确闭合。",
                LangTokenType.Number => "建议：检查是否需要添加数字值。",
                _ => "建议：检查语法结构是否正确。",
            };

            // 抛出带有上下文的错误
            throw new SyntaxError(
                CurrentToken.Value,
                CurrentToken.Line,
                CurrentToken.Column,
                fileName,
                detailedMessage + " " + suggestion,
                GetSourceContext(CurrentToken.Line));
        }
    }

    private LangToken Peek(int offset = 1)
    {
        return CurrentIndex + offset >= tokens.Count
            ? new LangToken("", LangTokenType.EndOfFile, CurrentIndex + offset)
            : tokens[CurrentIndex + offset];
    }

    #endregion

    #region Root

    // root = statement* ;
    public BlockStatement ParseProgram()
    {
        var statements = new List<IOldLangTree>();
        try
        {
            while (CurrentIndex < tokens.Count)
            {
                statements.Add(ParseStatement());
            }

            return new BlockStatement(statements);
        }
        catch (SyntaxError)
        {
            // 直接返回原始异常，不再重新包装
            throw;
        }
        catch (Exception ex)
        {
            // 处理其他类型的异常，添加上下文信息
            var currentToken = CurrentToken;

            // 检查 currentToken 是否为无效
            var tokenValue = currentToken.Type == LangTokenType.EndOfFile ? "<unknown>" : currentToken.Value;
            var line = currentToken.Line;
            var column = currentToken.Column;

            string[] context;
            try
            {
                context = GetSourceContext(line);
            }
            catch
            {
                // 如果获取上下文失败，使用空数组
                context = [];
            }

            if (ex is Old8Exception old8Ex)
            {
                // 如果已经是 Old8Exception，添加上下文信息
                throw new SyntaxError(
                    tokenValue,
                    line,
                    column,
                    $"解析错误：{old8Ex.Message}",
                    context);
            }

            // 其他类型的异常，转换为 SyntaxError
            throw new SyntaxError(
                tokenValue,
                line,
                column,
                $"解析时出现代码错误：{ex.Message}",
                context);
        }
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
    private OldStatement ParseStatement(List<AccessModifierType>? modifiers = null)
    {
        // 跳过空行和结束符
        if (CurrentToken.Type == LangTokenType.EndOfFile)
        {
            CurrentIndex++;
            return ParseStatement(modifiers);
        }

        // 处理访问修饰符：public、private、static 或它们的组合
        if (CurrentToken.Type is LangTokenType.Public or LangTokenType.Private or LangTokenType.Static)
        {
            var parsedModifiers = ParseAccessModifiers();

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
            // 检查是否是 for-in 语句，支持 key, value in dict 格式
            // 需要查找 "in" 关键字的位置
            var tempIndex = CurrentIndex + 1;
            var foundIn = false;

            // 跳过所有标识符和逗号，查找 "in" 关键字
            while (tempIndex < tokens.Count)
            {
                var token = tokens[tempIndex];
                if (token.Type == LangTokenType.In)
                {
                    foundIn = true;
                    break;
                }

                if (token.Type != LangTokenType.Identifier && token.Type != LangTokenType.Comma)
                {
                    break;
                }

                tempIndex++;
            }

            if (foundIn)
            {
                return ParseForInStatement();
            }

            if (Peek().Type == LangTokenType.Identifier || Peek().Type == LangTokenType.Assignment)
            {
                return ParseForStatement();
            }
        }

        if (CurrentToken.Type == LangTokenType.While)
        {
            return ParseWhileStatement();
        }

        if (CurrentToken.Type == LangTokenType.Switch)
        {
            return ParseSwitchStatement();
        }

        // 处理函数定义
        if (CurrentToken.Type == LangTokenType.Func)
        {
            return ParseFuncDeclaration();
        }

        // 处理return语句：return expression
        if (CurrentToken.Type == LangTokenType.Return)
        {
            return ParseReturnStatement();
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

        // 处理class定义：class identifier block
        if (CurrentToken.Type == LangTokenType.Class)
        {
            return ParseClassDeclaration();
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
                var expr = ParsePrimary();
                // 处理点访问和索引访问等复杂左值表达式
                expr = ParseDotExpr(expr);
                
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
            catch
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

        // 处理native语句：[import "dll" class method]
        if (CurrentToken.Type == LangTokenType.LeftBracket && Peek().Type == LangTokenType.Import)
        {
            // 先处理更具体的 nativeStatic 和 nativeClass，再处理更通用的 nativeStatement
            if (Peek(2).Type == LangTokenType.String &&
                Peek(3).Type == LangTokenType.Identifier &&
                Peek(4).Type == LangTokenType.RightBracket &&
                Peek(5).Type == LangTokenType.Arrow &&
                Peek(6).Type == LangTokenType.String)
            {
                return ParseNativeStatic();
            }

            if (Peek(2).Type == LangTokenType.String &&
                Peek(3).Type == LangTokenType.Identifier &&
                Peek(4).Type == LangTokenType.RightBracket)
            {
                return ParseNativeClass();
            }

            return ParseNativeStatement();
        }

        // 处理表达式语句：允许将函数运行表达式作为语句执行
        // 例如：funcCall(), (lambda)(args)
        var i = CurrentIndex;
        try
        {
            // 尝试解析为表达式
            var expr = ParseExpression();
            if (expr != null!)
            {
                // 表达式语句，返回一个空的 SetStatement 包装，或者直接返回表达式
                return new SetStatement(new LangId("", "", expr.Position), expr);
            }
        }
        catch
        {
            // 解析失败，回滚，尝试解析为其他语句类型
            CurrentIndex = i;
        }

        // 处理右大括号的情况，这通常意味着当前块结束
        if (CurrentToken.Type == LangTokenType.RightBrace)
        {
            // 不是错误，而是块结束的标志，直接返回空语句
            return new SetStatement(new LangId("", "", new SourcePosition(0, 0)),
                new LangId("", "", new SourcePosition(0, 0)));
        }

        // 无法识别的语句类型，尝试跳过并继续解析
        try
        {
            // 记录当前位置，用于调试
            var currentPos = CurrentIndex;

            // 尝试跳过当前语句，寻找下一个可能的语句开始
            while (CurrentIndex < tokens.Count &&
                   CurrentToken.Type != LangTokenType.If &&
                   CurrentToken.Type != LangTokenType.For &&
                   CurrentToken.Type != LangTokenType.While &&
                   CurrentToken.Type != LangTokenType.Switch &&
                   CurrentToken.Type != LangTokenType.Func &&
                   CurrentToken.Type != LangTokenType.Class &&
                   CurrentToken.Type != LangTokenType.Return &&
                   CurrentToken.Type != LangTokenType.Import &&
                   CurrentToken.Type != LangTokenType.Try &&
                   CurrentToken.Type != LangTokenType.RightBrace &&
                   CurrentToken.Type != LangTokenType.Identifier)
            {
                CurrentIndex++;
            }

            // 如果没有找到下一个语句开始，返回一个空语句
            if (CurrentIndex >= tokens.Count || CurrentIndex == currentPos)
            {
                CurrentIndex++;
                return new SetStatement(new LangId("", "", new SourcePosition(0, 0)),
                    new LangId("", "", new SourcePosition(0, 0)));
            }

            // 递归解析下一个语句
            return ParseStatement();
        }
        catch
        {
            // 作为最后的手段，抛出无法识别的语句类型异常
            throw CreateSyntaxError(
                $"语法错误：无法识别的语句类型 '{CurrentToken.Type}'，值为 '{CurrentToken.Value}'。建议检查语句结构是否正确。");
        }
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
            // 检查是否是函数定义：
            // 1. 只有当标识符前面没有赋值符号时，才可能是函数定义
            // 2. 如果前面有赋值符号（<-），则是函数调用
            // 3. 只有当标识符后面跟着左括号，并且接下来有箭头或左大括号时，才是函数定义
            var isAfterAssignment = false;
            if (savedIndex > 0)
            {
                var prevToken = tokens[savedIndex - 1];
                isAfterAssignment = prevToken.Type == LangTokenType.Assignment;
            }

            // 如果是函数调用，直接解析
            if (isAfterAssignment)
            {
                return ParseFuncRunStatement();
            }

            // 解析标识符和左括号
            ParseIdentifier();
            Expect(LangTokenType.LeftParen);
            ParseIdList();
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
                return ParseFuncDeclaration();
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

    private ReturnStatement ParseReturnStatement()
    {
        var returnToken = CurrentToken;
        var position = new SourcePosition(returnToken.Line, returnToken.Column, tokenValue: returnToken.Value);
        Expect(LangTokenType.Return);
        var expression = ParseExpression();
        return new ReturnStatement(expression, position);
    }

    private BreakStatement ParseBreakStatement()
    {
        var breakToken = CurrentToken;
        var position = new SourcePosition(breakToken.Line, breakToken.Column, tokenValue: breakToken.Value);
        Expect(LangTokenType.Break);
        return new BreakStatement(position);
    }

    private ContinueStatement ParseContinueStatement()
    {
        var continueToken = CurrentToken;
        var position = new SourcePosition(continueToken.Line, continueToken.Column, tokenValue: continueToken.Value);
        Expect(LangTokenType.Continue);
        return new ContinueStatement(position);
    }

    private ThrowStatement ParseThrowStatement()
    {
        var throwToken = CurrentToken;
        var position = new SourcePosition(throwToken.Line, throwToken.Column, tokenValue: throwToken.Value);
        Expect(LangTokenType.Throw);
        var expression = ParseExpression();
        return new ThrowStatement(expression, position);
    }

    // lrBlock = "(" statement ")" ;
    private OldStatement ParseLrBlock()
    {
        Expect(LangTokenType.LeftParen);
        var statement = ParseStatement();
        Expect(LangTokenType.RightParen);
        return statement;
    }

    // declaration = identifier ":" type "<-" expression | identifier "<-" expression | memberAccess ":" type "<-" expression | memberAccess "<-" expression ;
    private SetStatement ParseSet()
    {
        // 特殊处理带有类型注解的赋值语句：a:int <- value
        if (CurrentToken.Type == LangTokenType.Identifier && Peek().Type == LangTokenType.Colon)
        {
            // 带有类型注解的赋值语句
            var id = ParseTypedIdentifier(); // 使用 ParseTypedIdentifier 处理类型注解
            Expect(LangTokenType.Assignment);
            var expr = ParseExpression();
            return new SetStatement(id, expr, id.Position);
        }

        // 解析左值表达式
        var leftExpr = ParseExpression();

        // 检查是否有类型注解
        var assumptionType = "";
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);
            assumptionType = CurrentToken.Value;
            Expect(LangTokenType.Identifier);
        }

        Expect(LangTokenType.Assignment);
        var expression = ParseExpression();

        // 处理不同类型的左值表达式
        if (leftExpr is LangId langId)
        {
            // 普通标识符赋值：identifier <- value
            return new SetStatement(new LangId(langId.IdName, assumptionType, leftExpr.Position), expression,
                leftExpr.Position);
        }

        // 复杂左值表达式赋值：a[b] <- value 或 a.a <- value 或 this.a <- value
        return new SetStatement(leftExpr, expression, leftExpr.Position);
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
            var elifPosition = new SourcePosition(elifToken.Line, elifToken.Column);
            oldIfs.Add(new OldIf(elifCondition, elifBlock, elifPosition));
        }

        BlockStatement? elseBlock = null;
        if (CurrentToken.Type == LangTokenType.Else)
        {
            Expect(LangTokenType.Else);
            elseBlock = ParseBlock();
        }

        var ifPosition = new SourcePosition(ifToken.Line, ifToken.Column);
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
        var position = new SourcePosition(forToken.Line, forToken.Column);
        return new ForStatement(set, condition, statement, block, position);
    }

    // forInStatement = "for" identifier ( "," identifier )* "in" expression block ;
    private ForInStatement ParseForInStatement()
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
        var expression = ParseExpression();
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

    // whileStatement = "while" expression block ;
    private WhileStatement ParseWhileStatement()
    {
        var whileToken = CurrentToken;
        Expect(LangTokenType.While);
        var condition = ParseExpression();
        var block = ParseBlock();
        var position = new SourcePosition(whileToken.Line, whileToken.Column);
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
    /// funcDeclaration = ( "func" identifier | identifier ) "(" idList? ")" ( "->" )? block  ;
    /// </summary>
    /// <returns>声明函数</returns>
    private FuncInit ParseFuncDeclaration()
    {
        var isUseFunc = CurrentToken.Type == LangTokenType.Func;
        if (isUseFunc)
        {
            Expect(LangTokenType.Func);
        }

        var funcName = ParseIdentifier();

        Expect(LangTokenType.LeftParen);
        var parameters = ParseIdList();
        Expect(LangTokenType.RightParen);

        var returnType = string.Empty;
        // 检查是否有箭头语法用于返回类型注解
        if (CurrentToken.Type == LangTokenType.Arrow)
        {
            Expect(LangTokenType.Arrow);
            // 解析返回类型标识符
            if (isUseFunc)
            {
                if (CurrentToken.Type != LangTokenType.Identifier)
                {
                    throw CreateSyntaxError("请返回类型标识符");
                }

                returnType = CurrentToken.Value;
                Expect(LangTokenType.Identifier);
            }
        }

        // 如果有返回类型注解，创建新的LangId并设置AssumptionType
        var updatedFuncName = funcName;
        if (!string.IsNullOrEmpty(returnType))
        {
            updatedFuncName = new LangId(funcName.IdName, returnType, position: funcName.Position);
        }

        var block = ParseBlock();

        // 普通函数声明，生成 FuncInit
        return new FuncInit(new FuncLangValue(updatedFuncName, parameters.Args, block));
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

        string? parentClassName = null;
        // 处理继承语法：class Name extends ParentClass {
        if (CurrentToken is { Type: LangTokenType.Extends })
        {
            // 跳过 extends 关键字
            Expect(LangTokenType.Extends);

            // 获取父类名称
            if (CurrentToken.Type == LangTokenType.Identifier)
            {
                parentClassName = CurrentToken.Value;
                CurrentIndex++;
            }
        }

        var classBlock = ParseClassBlock();
        return new ClassInit(new TypeTemplate(className, classBlock.ToAnyData(), classBlock.ToStaticData(),
            parentClassName));
    }

    /// <summary>
    /// 解析访问修饰符
    /// </summary>
    /// <returns>访问修饰符列表</returns>
    private List<AccessModifierType> ParseAccessModifiers()
    {
        var modifiers = new List<AccessModifierType>();

        while (true)
        {
            switch (CurrentToken.Type)
            {
                case LangTokenType.Public:
                    modifiers.Add(AccessModifierType.Public);
                    Expect(LangTokenType.Public);
                    break;
                case LangTokenType.Private:
                    modifiers.Add(AccessModifierType.Private);
                    Expect(LangTokenType.Private);
                    break;
                case LangTokenType.Static:
                    modifiers.Add(AccessModifierType.Static);
                    Expect(LangTokenType.Static);
                    break;
                default:
                    return modifiers;
            }
        }
    }

    /// <summary>
    /// classBlock = "{" [set | funcDeclaration | importStatement]* "}" ;
    /// </summary>
    /// <returns>类块</returns>
    /// <exception cref="Exception">期望声明或函数声明</exception>
    private BlockStatement ParseClassBlock()
    {
        Expect(LangTokenType.LeftBrace);
        var statements = new List<IOldLangTree>();
        while (CurrentToken.Type != LangTokenType.RightBrace)
        {
            // 跳过空白行和注释
            if (CurrentToken.Type == LangTokenType.EndOfFile)
            {
                CurrentIndex++;
                continue;
            }

            // 尝试解析类成员，先解析修饰符，再解析语句
            try
            {
                // 保存当前位置，用于回滚
                // var savedIndex = CurrentIndex;

                // 尝试解析修饰符
                List<AccessModifierType> modifiers = [];
                if (CurrentToken.Type is LangTokenType.Public or LangTokenType.Private or LangTokenType.Static)
                {
                    modifiers = ParseAccessModifiers();
                }

                // 解析语句
                var statement = ParseStatement();

                // 根据语句类型和修饰符生成相应的类成员节点
                if (modifiers.Count != 0)
                {
                    OldStatement classMemberStatement;

                    switch (statement)
                    {
                        case SetStatement { Id: not null } setStmt:
                            // 带有修饰符的类字段声明
                            var memberId = new ClassMemberId(setStmt.Id.IdName, setStmt.Id.AssumptionType, modifiers,
                                setStmt.Position);
                            classMemberStatement =
                                new ClassFieldSetStatement(memberId, setStmt.Value, setStmt.Position);
                            break;
                        case FuncInit { FuncLangValue.Id: not null } funcInit:
                            // 带有修饰符的类函数声明
                            var memberId2 = new ClassMemberId(funcInit.FuncLangValue.Id.IdName,
                                funcInit.FuncLangValue.Id.AssumptionType, modifiers, funcInit.Position);
                            classMemberStatement =
                                new ClassFuncInitStatement(memberId2, funcInit.FuncLangValue, funcInit.Position);
                            break;
                        default:
                            classMemberStatement = statement;
                            break;
                    }

                    statements.Add(classMemberStatement);
                }
                else
                {
                    // 没有修饰符，直接添加原始语句
                    statements.Add(statement);
                }
            }
            catch (Exception ex)
            {
                // 如果是块结束异常，跳出循环
                if (ex.Message == "EndOfBlock")
                {
                    break;
                }

                // 否则重新抛出异常
                throw;
            }
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
        return new FuncRunStatement(new Instance(new LangId(funcName), arguments.Args));
    }

    /// <summary>
    /// importStatement = "import" STRING | IDENTIFIER;
    /// </summary>
    /// <returns>引入模块</returns>
    private ImportStatement ParseImportStatement()
    {
        var importToken = CurrentToken;
        var position = new SourcePosition(importToken.Line, importToken.Column, tokenValue: importToken.Value);
        Expect(LangTokenType.Import);
        string moduleName;

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

        return new ImportStatement(moduleName, position);
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
        return new SetStatement(new LangId(identifier),
            new Operation(new LangId(identifier), OperationType.PLUS, new IntLangValue(1)));
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
        return new SetStatement(new LangId(identifier),
            new Operation(new LangId(identifier), OperationType.MINUS, new IntLangValue(1)));
    }

    /// <summary>
    /// block = "{" statement* "}"
    ///        | statement
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
        try
        {
            while (CurrentToken.Type != LangTokenType.RightBrace)
            {
                // 尝试解析语句
                var statement = ParseStatement();

                // 只有当语句不是空语句时才添加到列表中
                if (!(statement is SetStatement { Id.IdName: "", Value: LangId { IdName: "" } }))
                {
                    statements.Add(statement);
                }
            }
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

    /// <summary>
    /// 解析try语句
    /// </summary>
    /// <returns>TryStatement对象</returns>
    private TryStatement ParseTryStatement()
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

    #region Expression

    // expression = boolOpera
    //            | ternaryExpression
    //            | binaryExpression
    //            | dotExpr
    //            | numberOpera1
    //            | numberOpera2
    //            | notBool
    //            | minusPrefix
    //            | primary ;
    private OldExpr ParseExpression()
    {
        // 1. 解析逻辑表达式
        var expr = ParseBoolOpera();
        // 2. 解析三元表达式（最低优先级）
        expr = ParseTernaryExpression(expr);
        return expr;
    }

    // 逻辑表达式
    private OldExpr ParseBoolOpera()
    {
        var left = ParseBinaryExpression();

        while (CurrentToken.Type == LangTokenType.And || CurrentToken.Type == LangTokenType.Or ||
               CurrentToken.Type == LangTokenType.Xor)
        {
            var operatorToken = CurrentToken;
            var position = new SourcePosition(operatorToken.Line, operatorToken.Column, tokenValue: operatorToken.Value);
            Expect(operatorToken.Type);
            var right = ParseBinaryExpression();
            left = new Operation(left, operatorToken.Type.GetGeneric(), right, position);
        }

        return left;
    }
    
    /// <summary>
    /// 解析三元表达式
    /// ternaryExpression = expression "?" expression ":" expression ;
    /// 注意：需要与类型注解区分开，类型注解的形式是 "identifier : type"
    /// </summary>
    /// <returns>三元表达式节点</returns>
    private OldExpr ParseTernaryExpression(OldExpr condition)
    {
        // 检查是否有 ?，这是三元表达式的标志
        if (CurrentToken.Type == LangTokenType.Question)
        {
            var questionToken = CurrentToken;
            Expect(LangTokenType.Question);
            
            // 解析问号后的表达式（true分支）
            var trueExpr = ParseExpression();
            
            // 检查是否有 :，这是三元表达式的分支分隔符
            if (CurrentToken.Type == LangTokenType.Colon)
            {
                Expect(LangTokenType.Colon);
                
                // 解析冒号后的表达式（false分支）
                var falseExpr = ParseExpression();
                
                // 创建三元表达式节点
                // 语法：condition ? trueExpr : falseExpr
                return new TernaryExpression(
                    condition, 
                    trueExpr, 
                    falseExpr, 
                    new SourcePosition(questionToken.Line, questionToken.Column));
            }
        }
        
        // 不是三元表达式，返回原始条件表达式
        return condition;
    }

    // 比较表达式
    private OldExpr ParseBinaryExpression()
    {
        var left = ParseNumberOpera1();

        while (CurrentToken.Type is LangTokenType.LessThanEquals or LangTokenType.GreaterThanEquals
               or LangTokenType.Equals
               or LangTokenType.NotEquals or LangTokenType.LessThan or LangTokenType.GreaterThan)
        {
            var operatorToken = CurrentToken;
            var position = new SourcePosition(operatorToken.Line, operatorToken.Column, tokenValue: operatorToken.Value);
            Expect(operatorToken.Type);
            var right = ParseNumberOpera1();
            left = new Operation(left, operatorToken.Type.GetGeneric(), right, position);
        }

        return left;
    }

    // 加减表达式
    private OldExpr ParseNumberOpera1()
    {
        var left = ParseNumberOpera2();

        while (CurrentToken.Type == LangTokenType.Plus || CurrentToken.Type == LangTokenType.Minus)
        {
            var operatorToken = CurrentToken;
            var position =
                new SourcePosition(operatorToken.Line, operatorToken.Column, tokenValue: operatorToken.Value);
            Expect(operatorToken.Type);
            var right = ParseNumberOpera2();
            left = new Operation(left, operatorToken.Type.GetGeneric(), right, position);
        }

        return left;
    }

    // 乘除表达式
    private OldExpr ParseNumberOpera2()
    {
        var left = ParsePrimary();

        // 处理点运算符（最高优先级）
        left = ParseDotExpr(left);

        while (CurrentToken.Type == LangTokenType.Star || CurrentToken.Type == LangTokenType.Slash ||
               CurrentToken.Type == LangTokenType.Percent)
        {
            var operatorToken = CurrentToken;
            var position =
                new SourcePosition(operatorToken.Line, operatorToken.Column, tokenValue: operatorToken.Value);
            Expect(operatorToken.Type);
            var right = ParsePrimary();
            // 处理右操作数的点运算符
            right = ParseDotExpr(right);
            left = new Operation(left, operatorToken.Type.GetGeneric(), right, position);
        }

        // 处理后置自增自减
        if (CurrentToken.Type == LangTokenType.PlusPlus)
        {
            var operatorToken = CurrentToken;
            var position =
                new SourcePosition(operatorToken.Line, operatorToken.Column, tokenValue: operatorToken.Value);
            Expect(LangTokenType.PlusPlus);
            left = new Operation(left, OperationType.PLUS, new IntLangValue(1), position);
        }
        else if (CurrentToken.Type == LangTokenType.MinusMinus)
        {
            var operatorToken = CurrentToken;
            var position =
                new SourcePosition(operatorToken.Line, operatorToken.Column, tokenValue: operatorToken.Value);
            Expect(LangTokenType.MinusMinus);
            left = new Operation(left, OperationType.MINUS, new IntLangValue(1), position);
        }

        // 处理 as 操作符
        while (CurrentToken.Type == LangTokenType.As)
        {
            var operatorToken = CurrentToken;
            var position =
                new SourcePosition(operatorToken.Line, operatorToken.Column, tokenValue: operatorToken.Value);
            Expect(LangTokenType.As);
            var right = ParsePrimary();
            // 处理右操作数的点运算符
            right = ParseDotExpr(right);
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
            return new Operation(null, OperationType.NOT, expr, position);
        }

        // 处理前缀 minus 表达式
        if (CurrentToken.Type == LangTokenType.Minus)
        {
            var minusToken = CurrentToken;
            var position = new SourcePosition(minusToken.Line, minusToken.Column, tokenValue: minusToken.Value);
            Expect(LangTokenType.Minus);
            var expr = ParsePrimary();
            return new Operation(null, OperationType.MINUS, expr, position);
        }

        // 处理前缀自增 ++i
        if (CurrentToken.Type == LangTokenType.PlusPlus)
        {
            var plusPlusToken = CurrentToken;
            var position =
                new SourcePosition(plusPlusToken.Line, plusPlusToken.Column, tokenValue: plusPlusToken.Value);
            Expect(LangTokenType.PlusPlus);
            var expr = ParsePrimary();
            return new Operation(expr, OperationType.PLUS, new IntLangValue(1), position);
        }

        // 处理前缀自减 --i
        if (CurrentToken.Type == LangTokenType.MinusMinus)
        {
            var minusMinusToken = CurrentToken;
            var position = new SourcePosition(minusMinusToken.Line, minusMinusToken.Column,
                tokenValue: minusMinusToken.Value);
            Expect(LangTokenType.MinusMinus);
            var expr = ParsePrimary();
            return new Operation(expr, OperationType.MINUS, new IntLangValue(1), position);
        }

        // 处理 list[...] 语法
        if (CurrentToken.Type == LangTokenType.List && Peek().Type == LangTokenType.LeftBracket)
        {
            Expect(LangTokenType.List); // 跳过 list 关键字
            return ParseList();
        }

        // 处理关键字作为标识符的情况
        if (CurrentToken.Type is LangTokenType.Func or
            LangTokenType.Class or
            LangTokenType.If or
            LangTokenType.Else or
            LangTokenType.While or
            LangTokenType.For or
            LangTokenType.Return or
            LangTokenType.Import or
            LangTokenType.List)
        {
            // 检查是否是列表初始化：list[...]
            if (Peek().Type == LangTokenType.LeftBracket)
            {
                return ParseListInitOrSlice();
            }

            // 检查是否是函数调用：func(...)
            if (Peek().Type == LangTokenType.LeftParen)
            {
                return ParseInstantiate();
            }

            // 否则作为普通标识符处理
            return ParseIdentifier();
        }

        if (CurrentToken.Type == LangTokenType.This)
        {
            // 直接创建一个 LangId 对象来处理 this 关键字
            var thisToken = CurrentToken;
            var position = new SourcePosition(thisToken.Line, thisToken.Column, tokenValue: thisToken.Value);
            Expect(LangTokenType.This);
            return new LangId(thisToken.Value, "", position);
        }

        return CurrentToken.Type switch
        {
            LangTokenType.String => ParseStringLiteral(),
            LangTokenType.Char => ParseCharLiteral(),
            LangTokenType.Number => CurrentToken.Value.Contains('.') ? ParseDoubleLiteral() : ParseIntLiteral(),
            LangTokenType.LeftBracket => ParseArrayOrRange(),
            LangTokenType.LeftParen => ParseLambdaOrTuple(),
            LangTokenType.LeftBrace => ParseDictionary(),
            LangTokenType.Dollar => ParseStringTree(), // 处理字符串模板：$"string", ${expression}, $($"string")
            LangTokenType.Identifier when Peek().Type == LangTokenType.LeftBracket => ParseListInitOrSlice(),
            LangTokenType.Identifier when Peek().Type == LangTokenType.LeftParen => ParseInstantiate(),
            LangTokenType.Identifier => ParseIdentifier(),
            LangTokenType.True or LangTokenType.False => ParseBoolLiteral(),
            LangTokenType.Null => ParseNullLiteral(),
            _ => throw CreateSyntaxError(
                $"语法错误：无法识别的主表达式类型 '{CurrentToken.Type}'，值为 '{CurrentToken.Value}'。建议检查表达式结构是否正确。")
        };
    }

    /// <summary>
    /// list = "list" "[" expression ( "," expression )* "]" ;
    /// </summary>
    /// <returns>列表初始化</returns>
    private LangValueType ParseList()
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
            return new ListLangValue(elements, position);
        }

        elements.Add(ParseExpression());
        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            elements.Add(ParseExpression());
        }

        Expect(LangTokenType.RightBracket);
        // 返回ListValue表示列表
        return new ListLangValue(elements, position);
    }


    /// <summary>
    /// dictionary = "{" dicTuple ( "," dicTuple )* "}" ;
    /// dicTuple = expression ":" expression ;
    /// </summary>
    /// <returns>返回字典</returns>
    private LangValueType ParseDictionary()
    {
        // 处理左括号，只支持 {}
        var leftBraceToken = CurrentToken;
        var dictPosition =
            new SourcePosition(leftBraceToken.Line, leftBraceToken.Column, tokenValue: leftBraceToken.Value);
        Expect(LangTokenType.LeftBrace);

        var rightType = LangTokenType.RightBrace;

        var elements = new List<TupleLangValue>();

        if (CurrentToken.Type == rightType)
        {
            Expect(rightType);
            return new DictionaryLangValue(elements, dictPosition);
        }

        // 解析字典元素
        while (true)
        {
            var key = ParseExpression();
            var colonToken = CurrentToken;
            var tuplePosition = new SourcePosition(colonToken.Line, colonToken.Column, tokenValue: colonToken.Value);
            Expect(LangTokenType.Colon);
            var value = ParseExpression();
            elements.Add(new TupleLangValue(key, value, tuplePosition));

            if (CurrentToken.Type != LangTokenType.Comma)
            {
                break;
            }

            Expect(LangTokenType.Comma);
        }

        Expect(rightType);

        return new DictionaryLangValue(elements, dictPosition);
    }

    /// <summary>
    /// array = "[" expression ( "," expression )* "]" ;
    /// range = "[" expression "~" expression "]" ;
    /// list_comprehension = "[" expression ( "if" expression "else" expression )? "for" identifier "in" expression ( "if" expression )* ( "for" identifier "in" expression ( "if" expression )* )* "]" ;
    /// </summary>
    /// <returns>数组初始化、Range或者列表推导式</returns>
    private LangValueType ParseArrayOrRange()
    {
        var leftBracketToken = CurrentToken;
        var position = new SourcePosition(leftBracketToken.Line, leftBracketToken.Column,
            tokenValue: leftBracketToken.Value);
        Expect(LangTokenType.LeftBracket);
        var elements = new List<OldExpr>();

        if (CurrentToken.Type == LangTokenType.RightBracket)
        {
            Expect(LangTokenType.RightBracket);
            // 空数组，返回ArrayValue
            return new ArrayLangValue(elements, position);
        }

        // 保存当前位置，用于回退
        var exprStartIndex = CurrentIndex;
        
        elements.Add(ParseExpression());
        if (CurrentToken.Type == LangTokenType.Wavy)
        {
            var wavyToken = CurrentToken;
            var rangePosition = new SourcePosition(wavyToken.Line, wavyToken.Column, tokenValue: wavyToken.Value);
            Expect(LangTokenType.Wavy);
            elements.Add(ParseExpression());
            Expect(LangTokenType.RightBracket);
            return new RangeLangValue(elements[0], elements[1], rangePosition);
        }

        // 检查是否是列表推导式
        // 列表推导式的特征是: 包含 for 关键字
        // 例如: [expr for var in iterable]
        // 或: [expr if condition else expr for var in iterable]
        var isListComprehension = false;

        // 扫描剩余的令牌，查找 for 关键字
        for (int i = CurrentIndex; i < tokens.Count; i++)
        {
            if (tokens[i].Type == LangTokenType.RightBracket)
                break; // 到达右括号，不是列表推导式
            if (tokens[i].Type != LangTokenType.For) continue;
            isListComprehension = true;
            break;
        }

        if (isListComprehension)
        {
            // 回退到表达式开始位置，准备解析列表推导式
            CurrentIndex = exprStartIndex;
            elements.Clear();
            
            // 解析列表推导式
            return ParseListComprehension(position);
        }

        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            elements.Add(ParseExpression());
        }

        Expect(LangTokenType.RightBracket);
        // 返回ArrayValue表示数组
        return new ArrayLangValue(elements, position);
    }
    
    /// <summary>
    /// 解析列表推导式
    /// list_comprehension = "[" expression ( "if" expression "else" expression )? "for" identifier "in" expression ( "if" expression )* ( "for" identifier "in" expression ( "if" expression )* )* "]" ;
    /// </summary>
    /// <returns>列表推导式节点</returns>
    private ListComprehension ParseListComprehension(SourcePosition position)
    {
        // 解析表达式部分
        var expression = ParseExpression();
        
        // 三元表达式已经在 ParseExpression 中处理，这里不需要额外处理
        
        // 解析 for 循环部分
        var loops = new List<ListComprehension>();
        
        while (CurrentToken.Type == LangTokenType.For)
        {
            Expect(LangTokenType.For);
            
            // 解析变量
            var variable = ParseIdentifier();
            
            Expect(LangTokenType.In);
            
            // 解析可迭代对象
            var iterable = ParseExpression();
            
            // 解析条件筛选（可选）
            List<OldExpr> conditions = [];
            
            while (CurrentToken.Type == LangTokenType.If)
            {
                Expect(LangTokenType.If);
                conditions.Add(ParseExpression());
            }
            
            // 组合多个条件，使用 AND 操作符连接
            OldExpr? condition = null;
            if (conditions.Count > 0)
            {
                condition = conditions[0];
                for (int i = 1; i < conditions.Count; i++)
                {
                    condition = new Operation(
                        condition,
                        OperationType.AND,
                        conditions[i],
                        new SourcePosition(CurrentToken.Line, CurrentToken.Column));
                }
            }
            
            // 创建循环节点
            loops.Add(new ListComprehension(
                expression, 
                variable, 
                iterable, 
                condition, 
                null, 
                position));
        }
        
        Expect(LangTokenType.RightBracket);
        
        if (loops.Count == 0)
        {
            throw CreateSyntaxError("列表推导式必须包含至少一个 for 循环");
        }
        
        // 处理嵌套循环
        for (int i = loops.Count - 2; i >= 0; i--)
        {
            var currentLoop = loops[i];
            var nextLoop = loops[i + 1];
            
            // 将下一个循环作为当前循环的嵌套循环
            currentLoop = new ListComprehension(
                currentLoop.Expression, 
                currentLoop.Variable, 
                currentLoop.Iterable, 
                currentLoop.Condition, 
                [nextLoop], 
                currentLoop.Position);
            
            loops[i] = currentLoop;
        }
        
        return loops[0];
    }

    /// <summary>
    /// lambda = "(" idList? ")" "->" block ;
    /// tuple = "(" expression ( "," expression )* ")" ;
    /// </summary>
    /// <returns>返回Lambda或元组</returns>
    private OldExpr ParseLambdaOrTuple()
    {
        var leftParenToken = CurrentToken;
        var position = new SourcePosition(leftParenToken.Line, leftParenToken.Column, tokenValue: leftParenToken.Value);
        Expect(LangTokenType.LeftParen);

        // 保存当前位置，用于回滚
        var savedIndex = CurrentIndex;

        // 检查是否是Lambda表达式：() -> block 或 (params) -> block
        if (CurrentToken.Type == LangTokenType.RightParen && Peek().Type == LangTokenType.Arrow)
        {
            // 无参数Lambda：() -> block 或 () -> expression
            Expect(LangTokenType.RightParen);
            Expect(LangTokenType.Arrow);

            BlockStatement block;

            // 检查是块语句还是表达式
            if (CurrentToken.Type == LangTokenType.LeftBrace)
            {
                // 块语句：() -> { ... }
                block = ParseBlock();
            }
            else
            {
                // 表达式：() -> expression
                // 我们需要将表达式转换为块语句，添加return
                var expr = ParseExpression();
                var returnStmt = new ReturnStatement(expr, position);
                block = new BlockStatement([returnStmt]);
            }

            return new FuncLangValue(null, [], block, position);
        }

        // 检查是否是有参数的Lambda表达式
        // 只有当括号内的内容是标识符列表时，才可能是Lambda表达式
        // 如果是其他表达式（如数字、字符串、表达式调用等），则是元组
        var isLambda = true;
        var ids = new List<LangId>();

        // 检查第一个元素是否是标识符
        if (CurrentToken.Type == LangTokenType.Identifier)
        {
            // 解析第一个参数，允许类型注解
            ids.Add(ParseTypedIdentifier());

            // 解析更多参数，允许类型注解
            while (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);
                if (CurrentToken.Type != LangTokenType.Identifier)
                {
                    // 不是标识符，不是Lambda表达式
                    isLambda = false;
                    break;
                }

                ids.Add(ParseTypedIdentifier());
            }

            // 检查是否有箭头符号
            if (isLambda && CurrentToken.Type == LangTokenType.RightParen && Peek().Type == LangTokenType.Arrow)
            {
                // Lambda表达式：(params) -> block 或 (params) -> expression
                Expect(LangTokenType.RightParen);
                Expect(LangTokenType.Arrow);

                BlockStatement block;

                // 检查是块语句还是表达式
                if (CurrentToken.Type == LangTokenType.LeftBrace)
                {
                    // 块语句：(params) -> { ... }
                    block = ParseBlock();
                }
                else
                {
                    // 表达式：(params) -> expression
                    // 我们需要将表达式转换为块语句，添加return
                    var expr = ParseExpression();
                    var returnStmt = new ReturnStatement(expr, position);
                    block = new BlockStatement([returnStmt]);
                }

                return new FuncLangValue(null, ids, block, position);
            }
        }
        else
        {
            // 第一个元素不是标识符，不是Lambda表达式
            // isLambda = false;
        }

        // 元组：(expr1, expr2, ...)
        // 回滚到左括号后，重新解析为表达式列表
        CurrentIndex = savedIndex;

        var elements = new List<OldExpr>();

        // 空括号情况：()
        if (CurrentToken.Type == LangTokenType.RightParen)
        {
            Expect(LangTokenType.RightParen);
            // 返回一个空的元组
            return new TupleLangValue(new LangId("", "", position), new LangId("", "", position), position);
        }

        // 解析第一个元素
        elements.Add(ParseExpression());

        // 检查是否有逗号
        bool hasComma = CurrentToken.Type == LangTokenType.Comma;

        // 解析更多元素
        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            elements.Add(ParseExpression());
        }

        // 检查是否是右括号，避免在错误位置调用Expect
        if (CurrentToken.Type == LangTokenType.RightParen)
        {
            Expect(LangTokenType.RightParen);
        }

        // 构建元组，支持任意数量元素
        if (elements.Count == 1)
        {
            // 检查是否是单元素元组还是括号表达式
            // 如果没有逗号，那么是括号表达式：(expr)
            // 如果有逗号，那么是单元素元组：(expr,)
            if (!hasComma)
            {
                // 单个表达式，返回表达式本身，不是元组
                return elements[0];
            }

            // 单元素元组：(expr,)
            return new TupleLangValue(elements[0], new NullLangValue(), position);
        }

        if (elements.Count == 2)
        {
            // 双元素元组：(expr1, expr2)
            return new TupleLangValue(elements[0], elements[1], position);
        }

        // 多元素元组：(expr1, expr2, expr3, ...) - 递归构建嵌套元组
        var tuple = new TupleLangValue(elements[0], elements[1], position);
        for (int i = 2; i < elements.Count; i++)
        {
            tuple = new TupleLangValue(tuple, elements[i], position);
        }

        return tuple;
    }

    /// <summary>
    /// 解析字符串树，支持模板字符串
    /// 支持格式：
    /// - $"string" 简单模板字符串
    /// - ${expression} 表达式模板
    /// - $($"string") 嵌套模板
    /// - $("string {placeholder}") 带占位符的模板
    /// - $"string ${expression} string" 混合模板
    /// </summary>
    /// <returns>字符串树</returns>
    private OldExpr ParseStringTree()
    {
        // 检查当前token是否是Dollar（用于字符串插值）
        if (CurrentToken.Type == LangTokenType.Dollar)
        {
            var dollarToken = CurrentToken;
            var position = new SourcePosition(dollarToken.Line, dollarToken.Column, tokenValue: dollarToken.Value);

            // 跳过$符号
            Expect(LangTokenType.Dollar);

            // 处理$"string" 格式（字符串插值）
            if (CurrentToken.Type == LangTokenType.String)
            {
                var stringValue = CurrentToken.Value;
                Expect(LangTokenType.String);

                // 完整的字符串模板解析
                var parts = new List<OldExpr>();
                var i = 0;
                var len = stringValue.Length;

                while (i < len)
                {
                    var c = stringValue[i];

                    if (c == '{' && i + 1 < len)
                    {
                        var next = stringValue[i + 1];

                        if (next == '{')
                        {
                            // 转义的 {{，添加一个 {
                            parts.Add(new StringLangValue("{", position));
                            i += 2;
                        }
                        else
                        {
                            // 普通的 {，开始解析表达式
                            i += 1;
                            var exprStart = i;
                            var braceCount = 1;

                            // 查找匹配的 }
                            var foundMatchingBrace = false;
                            while (i < len && braceCount > 0)
                            {
                                c = stringValue[i];
                                if (c == '{')
                                {
                                    braceCount++;
                                }
                                else if (c == '}')
                                {
                                    braceCount--;
                                    if (braceCount == 0)
                                    {
                                        foundMatchingBrace = true;
                                        break;
                                    }
                                }

                                i++;
                            }

                            if (foundMatchingBrace)
                            {
                                // 提取表达式字符串
                                var exprStr = stringValue.Substring(exprStart, i - exprStart).Trim();

                                // 完整的表达式解析：支持所有表达式类型，包括点操作符
                                if (!string.IsNullOrWhiteSpace(exprStr))
                                {
                                    // 将表达式字符串转换为Token流
                                    var exprTokens = LangTokenizer.Tokenize(exprStr);

                                    // 创建一个新的LangParser实例来解析这个表达式
                                    var exprParser = new LangParser(exprTokens, exprStr, fileName);

                                    // 解析完整表达式
                                    var expr = exprParser.ParseExpression();
                                    parts.Add(expr);
                                }

                                i++;
                            }
                            else
                            {
                                // 未找到匹配的 }，抛出语法错误
                                throw CreateSyntaxError("字符串模板中缺少匹配的右大括号 '}'");
                            }
                        }
                    }
                    else if (c == '}')
                    {
                        if (i + 1 < len && stringValue[i + 1] == '}')
                        {
                            // 转义的 }}，添加一个 }
                            parts.Add(new StringLangValue("}", position));
                            i += 2;
                        }
                        else
                        {
                            // 普通的 }，直接添加
                            parts.Add(new StringLangValue("}", position));
                            i++;
                        }
                    }
                    else
                    {
                        // 普通字符，添加到结果中
                        var start = i;
                        while (i < len && stringValue[i] != '{' && stringValue[i] != '}')
                        {
                            i++;
                        }

                        var text = stringValue.Substring(start, i - start);
                        if (!string.IsNullOrEmpty(text))
                        {
                            parts.Add(new StringLangValue(text, position));
                        }
                    }
                }

                return new StringTreeList(parts, position);
            }
        }

        // 如果不是字符串插值，返回普通表达式
        return ParsePrimary();
    }



    /// 解析标识符，支持带类型注解的标识符：identifier:type
    /// 允许将关键字用作标识符
    /// </summary>
    /// <returns>标识符</returns>
    private LangId ParseIdentifier()
    {
        var identifierToken = CurrentToken;
        var position = new SourcePosition(identifierToken.Line, identifierToken.Column,
            tokenValue: identifierToken.Value);
        var value = identifierToken.Value;

        // 检查当前token是否是标识符或关键字
        if (CurrentToken.Type is LangTokenType.Identifier or LangTokenType.Func or LangTokenType.Class
            or LangTokenType.If or LangTokenType.Else or LangTokenType.While or LangTokenType.For
            or LangTokenType.Return or LangTokenType.Import or LangTokenType.True or LangTokenType.False
            or LangTokenType.List)
        {
            CurrentIndex++;
        }
        else
        {
            Expect(LangTokenType.Identifier);
        }

        // 默认不处理类型注解
        return new LangId(value, "", position);
    }
    
    /// <summary>
    /// 解析带有类型注解的标识符，用于赋值语句、函数参数和lambda参数
    /// </summary>
    /// <returns>带有类型注解的标识符</returns>
    private LangId ParseTypedIdentifier()
    {
        var identifierToken = CurrentToken;
        var position = new SourcePosition(identifierToken.Line, identifierToken.Column,
            tokenValue: identifierToken.Value);
        var value = identifierToken.Value;

        // 检查当前token是否是标识符或关键字
        if (CurrentToken.Type is LangTokenType.Identifier or LangTokenType.Func or LangTokenType.Class
            or LangTokenType.If or LangTokenType.Else or LangTokenType.While or LangTokenType.For
            or LangTokenType.Return or LangTokenType.Import or LangTokenType.True or LangTokenType.False
            or LangTokenType.List)
        {
            CurrentIndex++;
        }
        else
        {
            Expect(LangTokenType.Identifier);
        }

        // 处理类型注解：identifier:type
        var typeAnnotation = "";
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);
            typeAnnotation = CurrentToken.Value;
            if (typeAnnotation == "")
            {
                throw CreateSyntaxError("类型注解不能为空");
            }

            Expect(CurrentToken.Type == LangTokenType.List ? LangTokenType.List : LangTokenType.Identifier);
        }

        return new LangId(value, typeAnnotation, position);
    }

    /// <summary>
    /// 解析字符串字面量
    /// </summary>
    /// <returns>字符串值</returns>
    private StringLangValue ParseStringLiteral()
    {
        var stringToken = CurrentToken;
        var position = new SourcePosition(stringToken.Line, stringToken.Column, tokenValue: stringToken.Value);
        var value = stringToken.Value;
        Expect(LangTokenType.String);
        return new StringLangValue(value, position);
    }

    /// <summary>
    /// 解析字符字面量
    /// </summary>
    /// <returns>字符值</returns>
    private CharLangValue ParseCharLiteral()
    {
        var charToken = CurrentToken;
        var position = new SourcePosition(charToken.Line, charToken.Column, tokenValue: charToken.Value);
        var value = charToken.Value;
        Expect(LangTokenType.Char);
        return new CharLangValue(value[0], position);
    }

    /// <summary>
    /// 解析整数字面量
    /// </summary>
    /// <returns>整数值</returns>
    private IntLangValue ParseIntLiteral()
    {
        var numberToken = CurrentToken;
        var position = new SourcePosition(numberToken.Line, numberToken.Column, tokenValue: numberToken.Value);
        var value = numberToken.Value;
        Expect(LangTokenType.Number);
        return new IntLangValue(int.Parse(value), position);
    }

    /// <summary>
    /// 解析双精度字面量
    /// </summary>
    /// <returns>双精度值</returns>
    private DoubleLangValue ParseDoubleLiteral()
    {
        var numberToken = CurrentToken;
        var position = new SourcePosition(numberToken.Line, numberToken.Column, tokenValue: numberToken.Value);
        var value = numberToken.Value;
        Expect(LangTokenType.Number);
        if (!value.Contains('E') && !value.Contains('e')) return new DoubleLangValue(double.Parse(value), position);
        var decimalValue = decimal.Parse(value, System.Globalization.NumberStyles.Float);
        return new DoubleLangValue((double)decimalValue, position);
    }

    /// <summary>
    /// 解析布尔字面量
    /// </summary>
    /// <returns>布尔值</returns>
    private BoolLangValue ParseBoolLiteral()
    {
        var boolToken = CurrentToken;
        var position = new SourcePosition(boolToken.Line, boolToken.Column, tokenValue: boolToken.Value);
        var value = boolToken.Type == LangTokenType.True;
        Expect(boolToken.Type);
        return new BoolLangValue(value, position);
    }

    private NullLangValue ParseNullLiteral()
    {
        var nullToken = CurrentToken;
        var position = new SourcePosition(nullToken.Line, nullToken.Column, tokenValue: nullToken.Value);
        Expect(LangTokenType.Null);
        return new NullLangValue(position);
    }

    /// <summary>
    /// 解析标识符列表
    /// 支持关键字作为标识符
    /// </summary>
    /// <returns>标识符列表</returns>
    private IdList ParseIdList()
    {
        var ids = new List<LangId>();

        // 检查当前token是否是标识符或关键字
        if (CurrentToken.Type is LangTokenType.Identifier or LangTokenType.Func or LangTokenType.Class
            or LangTokenType.If or LangTokenType.Else or LangTokenType.While or LangTokenType.For
            or LangTokenType.Return or LangTokenType.Import or LangTokenType.True or LangTokenType.False
            or LangTokenType.List)
        {
            // 解析第一个参数，允许类型注解
            ids.Add(ParseTypedIdentifier());

            // 跳过默认参数值（如果有）
            if (CurrentToken.Type == LangTokenType.Assignment)
            {
                // 跳过赋值符号和表达式
                while (CurrentToken.Type != LangTokenType.Comma && CurrentToken.Type != LangTokenType.RightParen)
                {
                    CurrentIndex++;
                }
            }

            // 解析更多参数，允许类型注解
            while (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);
                ids.Add(ParseTypedIdentifier());

                // 跳过默认参数值（如果有）
                if (CurrentToken.Type == LangTokenType.Assignment)
                {
                    // 跳过赋值符号和表达式
                    while (CurrentToken.Type != LangTokenType.Comma && CurrentToken.Type != LangTokenType.RightParen)
                    {
                        CurrentIndex++;
                    }
                }
            }
        }

        return new IdList(ids);
    }

    /// <summary>
    /// 解析参数列表
    /// </summary>
    /// <returns>参数列表</returns>
    private ArgList ParseArgList()
    {
        var args = new List<OldExpr>();

        if (CurrentToken.Type == LangTokenType.RightParen)
        {
            return new ArgList(args);
        }

        args.Add(ParseExpression());
        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            args.Add(ParseExpression());
        }

        return new ArgList(args);
    }

    /// <summary>
    /// 解析列表初始化或切片
    /// </summary>
    /// <returns>列表初始化或切片</returns>
    private OldExpr ParseListInitOrSlice()
    {
        var identifier = ParseIdentifier();
        var leftBracketToken = CurrentToken;
        var position = new SourcePosition(leftBracketToken.Line, leftBracketToken.Column,
            tokenValue: leftBracketToken.Value);
        Expect(LangTokenType.LeftBracket);

        // 检查是否是空列表初始化或访问：list[]
        if (CurrentToken.Type == LangTokenType.RightBracket)
        {
            Expect(LangTokenType.RightBracket);
            // 空列表访问，返回一个空的操作
            return new Operation(identifier, OperationType.CONCAT, new LangId("", "", position), position);
        }

        // 处理切片：list[start:end]
        if (CurrentToken.Type is LangTokenType.Identifier or LangTokenType.Number or LangTokenType.LeftBracket)
        {
            var start = ParseExpression();
            if (CurrentToken.Type == LangTokenType.Colon)
            {
                Expect(LangTokenType.Colon);
                var end = ParseExpression();
                Expect(LangTokenType.RightBracket);
                return new SliceLangValue(identifier, start, end);
            }

            if (CurrentToken.Type == LangTokenType.RightBracket)
            {
                // 列表访问：list[index] - 使用 OldItem
                Expect(LangTokenType.RightBracket);
                return new LangListItem(identifier, start, position);
            }
        }

        // 处理列表访问：list[index] （默认情况）- 使用 OldItem
        var index = ParseExpression();
        Expect(LangTokenType.RightBracket);
        return new LangListItem(identifier, index, position);
    }

    /// <summary>
    /// 解析函数调用或实例化
    /// </summary>
    /// <returns>函数调用或实例化</returns>
    private Instance ParseInstantiate()
    {
        var identifier = ParseIdentifier();
        Expect(LangTokenType.LeftParen);
        var args = ParseArgList();
        Expect(LangTokenType.RightParen);
        return new Instance(identifier, args.Args);
    }

    #endregion
}