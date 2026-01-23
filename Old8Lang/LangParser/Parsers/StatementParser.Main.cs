using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.LangParser.Core;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// 语句解析器，负责解析各种语句类型
/// </summary>
public partial class StatementParser(
    ParserContext context,
    ExpressionParser expressionParser,
    FunctionParser functionParser,
    ClassParser classParser,
    PrimaryParser primaryParser)
    : ParserBase(context)
{
    #region Statement

    public OldStatement ParseStatement(List<AccessModifierType>? modifiers = null)
    {
        // 跳过结束符
        if (CurrentToken.Type == LangTokenType.EndOfFile)
        {
            CurrentIndex++;
            throw CreateSyntaxError("语法错误：意外的文件结束符。建议检查是否缺少结束符号或语句。");
        }

        // 处理装饰器：@ 符号开头
        if (CurrentToken.Type == LangTokenType.At)
        {
            var decorators = functionParser.ParseDecorators();

            // 装饰器后面必须跟函数声明（func 或 async func）
            if (CurrentToken.Type == LangTokenType.Func)
            {
                return functionParser.ParseFuncDeclaration(decorators);
            }
            else if (CurrentToken.Type == LangTokenType.Async)
            {
                // 收集文档注释
                var docComment = CollectPrecedingDocComments();
                Expect(LangTokenType.Async);
                Expect(LangTokenType.Func);
                return functionParser.ParseAsyncFuncDeclaration(docComment, decorators);
            }
            else
            {
                throw CreateSyntaxError("装饰器后面必须跟函数声明（func 或 async func）");
            }
        }

        // 处理访问修饰符：public、private、static 或它们的组合
        if (CurrentToken.Type is LangTokenType.Public or LangTokenType.Private or LangTokenType.Static)
        {
            var parsedModifiers = classParser.ParseAccessModifiers();

            // 合并修饰符
            var combinedModifiers = modifiers is not null
                ? new List<AccessModifierType>(modifiers)
                : new List<AccessModifierType>();
            combinedModifiers.AddRange(parsedModifiers);

            // 解析后面的语句（set 或 funcDeclaration）
            return ParseStatement(combinedModifiers);
        }


        // 处理括号块：(statement) 或 元组解构赋值 (a, b) <- ...
        if (CurrentToken.Type == LangTokenType.LeftParen)
        {
            // 尝试解析为解构赋值
            var savedIndex = CurrentIndex;
            try
            {
                // 解析左侧表达式
                var expr = expressionParser.ParseExpression();
                
                // 检查是否是赋值符号
                if (CurrentToken.Type == LangTokenType.Assignment)
                {
                    // 是元组解构赋值，回退并调用 ParseSet
                    CurrentIndex = savedIndex;
                    return ParseSet();
                }
            }
            catch
            {
                // 忽略错误
            }
            
            // 回退并按原逻辑处理
            CurrentIndex = savedIndex;
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
            return functionParser.ParseAsyncFuncDeclaration(docComment, null);
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

        // 处理 extern 语句：extern "dll" class method 或 extern "dll" func ...
        if (CurrentToken.Type == LangTokenType.Extern)
        {
            var peek1 = Peek();
            var peek2 = Peek(2);
            var peek3 = Peek(3);
            var peek4 = Peek(4);

            // 检查是否是 P/Invoke/Python/JS 函数导入：extern "dll" func ...
            // 或者带调用约定的：extern "dll" cdecl/stdcall/winapi func ...
            // 或者函数块：extern "dll" { func1, func2 }
            // 或者带调用约定的函数块：extern "dll" cdecl/stdcall/winapi { func1, func2 }
            if (peek1.Type == LangTokenType.String &&
                (peek2.Type == LangTokenType.Func ||
                 peek2.Type == LangTokenType.LeftBrace ||
                 (peek2.Type == LangTokenType.Identifier && peek3.Type == LangTokenType.Func) ||
                 (peek2.Type == LangTokenType.Identifier && peek3.Type == LangTokenType.LeftBrace && IsCallingConvention(peek2.Value))))
            {
                return ParseExternStatement();
            }

            // 以下是 C# DLL 导入的各种模式（使用 NativeStatement）

            // 1. nativeStatic: extern "dll" class -> "alias"
            if (peek1.Type == LangTokenType.String &&
                peek2.Type == LangTokenType.Identifier &&
                peek3.Type == LangTokenType.Arrow)
            {
                return ParseNativeStatic();
            }

            // 2. 批量导入所有方法: extern "dll" class *
            if (peek1.Type == LangTokenType.String &&
                peek2.Type == LangTokenType.Identifier &&
                peek3.Type == LangTokenType.Star)
            {
                return ParseNativeStatement();
            }

            // 3. 选择性导入: extern "dll" class { method1, method2 }
            if (peek1.Type == LangTokenType.String &&
                peek2.Type == LangTokenType.Identifier &&
                peek3.Type == LangTokenType.LeftBrace)
            {
                return ParseNativeStatement();
            }

            // 4. nativeClass with alias: extern "dll" class as alias
            if (peek1.Type == LangTokenType.String &&
                peek2.Type == LangTokenType.Identifier &&
                peek3.Type == LangTokenType.As)
            {
                return ParseNativeClass();
            }

            // 5. 单个方法导入: extern "dll" class method alias?
            // 如果第三个 token 是 identifier，说明是方法导入
            if (peek1.Type == LangTokenType.String &&
                peek2.Type == LangTokenType.Identifier &&
                peek3.Type == LangTokenType.Identifier)
            {
                return ParseNativeStatement();
            }

            // 6. nativeClass: extern "dll" class（最后兜底，第三个 token 不是上述任何特殊类型）
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
            // 需要区分对象解构赋值和块语句
            // 对象解构赋值：{name, age} <- person
            // 块语句：{ statements }

            // 前瞻检查：查找是否有赋值符号 '<-'
            var tempIndex = CurrentIndex + 1;
            var foundAssignment = false;
            var scanLimit = Math.Min(tempIndex + 50, Tokens.Count); // 限制前瞻深度
            var braceDepth = 1; // 跟踪大括号嵌套深度

            while (tempIndex < scanLimit && braceDepth > 0)
            {
                var token = Tokens[tempIndex];

                if (token.Type == LangTokenType.LeftBrace)
                {
                    braceDepth++;
                }
                else if (token.Type == LangTokenType.RightBrace)
                {
                    braceDepth--;
                    if (braceDepth == 0)
                    {
                        // 找到匹配的右大括号，检查下一个 token
                        if (tempIndex + 1 < Tokens.Count)
                        {
                            var nextToken = Tokens[tempIndex + 1];
                            // 检查是否是赋值符号或类型注解后的赋值符号
                            if (nextToken.Type == LangTokenType.Assignment)
                            {
                                foundAssignment = true;
                                break;
                            }
                            else if (nextToken.Type == LangTokenType.Colon)
                            {
                                // 可能是类型注解：{name, age}:Person <- person
                                if (tempIndex + 3 < Tokens.Count &&
                                    Tokens[tempIndex + 3].Type == LangTokenType.Assignment)
                                {
                                    foundAssignment = true;
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }

                tempIndex++;
            }

            if (foundAssignment)
            {
                // 这是对象解构赋值
                return ParseObjectDestructuring();
            }
            else
            {
                // 这是块语句
                return ParseBlock();
            }
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
                            else
                            {
                                // 只有可空类型注解，没有赋值（例如类字段声明 "key: K?"）
                                CurrentIndex = savedIndex;
                                return ParseSet();
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

        // 处理 super.field <- value 赋值语句
        if (CurrentToken.Type == LangTokenType.Super)
        {
            var savedIndex = CurrentIndex;
            try
            {
                // 尝试解析 super.field 表达式
                var superExpr = primaryParser.ParsePrimary(); // 解析 super
                if (CurrentToken.Type == LangTokenType.Dot)
                {
                    superExpr = expressionParser.ParseDotExpr(superExpr); // 解析 .field

                    // 检查是否是赋值语句
                    if (CurrentToken.Type == LangTokenType.Assignment)
                    {
                        // 这是 super.field <- value 赋值语句
                        CurrentIndex = savedIndex;
                        return ParseSet();
                    }
                }

                // 不是赋值语句，回退
                CurrentIndex = savedIndex;
            }
            catch
            {
                // 解析失败，回退
                CurrentIndex = savedIndex;
            }
        }

        // 处理表达式语句：允许将函数运行表达式作为语句执行
        // 例如：funcCall(), (lambda)(args), t.test()
        var i = CurrentIndex;
        try
        {
            // 尝试解析为表达式
            var expr = expressionParser.ParseExpression();
            if (expr is not null)
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
    #endregion
}
