using Old8Lang.AST;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser.Core;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// 扩展方法解析器，负责解析扩展方法声明
/// </summary>
public class ExtensionParser(
    ParserContext context,
    Func<FunctionParser> functionParserFactory,
    Func<StatementParser> statementParserFactory)
    : ParserBase(context)
{
    /// <summary>
    /// 解析扩展方法声明
    /// extension TypeName { func method1() {} func method2() {} }
    /// </summary>
    public ExtensionDeclaration ParseExtensionDeclaration()
    {
        // 保存起始位置
        var startPosition = CreateSourcePosition(CurrentToken);

        // 期望 extension 关键字
        Expect(LangTokenType.Extension);

        // 解析目标类型名称
        if (CurrentToken.Type != LangTokenType.Identifier)
        {
            throw CreateSyntaxError("extension 关键字后面必须跟类型名称");
        }

        var targetTypeName = CurrentToken.Value;
        CurrentIndex++;

        // ExtensionDeclaration 目前只保存基础目标类型名。消费泛型参数和约束，
        // 避免把声明级语法误认为扩展块内容，同时不在此处伪造尚未建模的约束语义。
        SkipGenericTargetParameters();
        SkipTargetConstraints();

        // 期望左花括号
        Expect(LangTokenType.LeftBrace);

        // 解析扩展方法列表
        var extensionMethods = new List<FuncLangValue>();

        while (CurrentToken.Type != LangTokenType.RightBrace)
        {
            // 跳过分号
            if (CurrentToken.Type == LangTokenType.Semicolon)
            {
                CurrentIndex++;
                continue;
            }

            // 解析函数声明
            if (CurrentToken.Type == LangTokenType.Func)
            {
                var funcParser = functionParserFactory();
                var funcInit = funcParser.ParseFuncDeclaration();

                // 提取 FuncLangValue
                extensionMethods.Add(funcInit.FuncValue);
            }
            else if (CurrentToken.Type == LangTokenType.Async)
            {
                // 异步扩展方法暂不支持
                throw CreateSyntaxError("扩展方法暂不支持 async 修饰符");
            }
            else
            {
                throw CreateSyntaxError($"extension 块中只能包含函数声明，但遇到了 {CurrentToken.Type}");
            }
        }

        // 期望右花括号
        Expect(LangTokenType.RightBrace);

        if (extensionMethods.Count == 0)
        {
            throw CreateSyntaxError("extension 块中至少需要一个扩展方法");
        }

        return new ExtensionDeclaration(targetTypeName, extensionMethods, startPosition);
    }

    /// <summary>
    /// 跳过目标类型的泛型参数列表，例如 &lt;T&gt;、&lt;K, V&gt; 或带约束的参数。
    /// </summary>
    private void SkipGenericTargetParameters()
    {
        if (CurrentToken.Type != LangTokenType.LessThan)
        {
            return;
        }

        Expect(LangTokenType.LessThan);
        var angleDepth = 1;
        var parenthesisDepth = 0;

        while (angleDepth > 0)
        {
            if (CurrentToken.Type == LangTokenType.EndOfFile)
            {
                throw CreateSyntaxError("泛型目标类型缺少结束符 '>'");
            }

            switch (CurrentToken.Type)
            {
                case LangTokenType.LessThan:
                    angleDepth++;
                    break;
                case LangTokenType.GreaterThan:
                    if (parenthesisDepth == 0)
                    {
                        angleDepth--;
                    }
                    break;
                case LangTokenType.LeftParen:
                    parenthesisDepth++;
                    break;
                case LangTokenType.RightParen:
                    if (parenthesisDepth == 0)
                    {
                        throw CreateSyntaxError("泛型目标类型中的括号不匹配");
                    }

                    parenthesisDepth--;
                    break;
            }

            CurrentIndex++;
        }

        if (parenthesisDepth != 0)
        {
            throw CreateSyntaxError("泛型目标类型中的括号不匹配");
        }
    }

    /// <summary>
    /// 跳过目标类型的声明级约束，包括冒号约束和 where 子句。
    /// </summary>
    private void SkipTargetConstraints()
    {
        if (CurrentToken.Type != LangTokenType.Colon && CurrentToken.Type != LangTokenType.Where)
        {
            return;
        }

        var angleDepth = 0;
        var parenthesisDepth = 0;
        var bracketDepth = 0;

        while (CurrentToken.Type != LangTokenType.LeftBrace ||
               angleDepth != 0 || parenthesisDepth != 0 || bracketDepth != 0)
        {
            if (CurrentToken.Type == LangTokenType.EndOfFile)
            {
                throw CreateSyntaxError("扩展声明缺少左花括号 '{'");
            }

            switch (CurrentToken.Type)
            {
                case LangTokenType.LessThan:
                    angleDepth++;
                    break;
                case LangTokenType.GreaterThan:
                    if (angleDepth == 0)
                    {
                        throw CreateSyntaxError("扩展声明中的泛型约束括号不匹配");
                    }

                    angleDepth--;
                    break;
                case LangTokenType.LeftParen:
                    parenthesisDepth++;
                    break;
                case LangTokenType.RightParen:
                    if (parenthesisDepth == 0)
                    {
                        throw CreateSyntaxError("扩展声明中的约束括号不匹配");
                    }

                    parenthesisDepth--;
                    break;
                case LangTokenType.LeftBracket:
                    bracketDepth++;
                    break;
                case LangTokenType.RightBracket:
                    if (bracketDepth == 0)
                    {
                        throw CreateSyntaxError("扩展声明中的约束括号不匹配");
                    }

                    bracketDepth--;
                    break;
            }

            CurrentIndex++;
        }
    }
}
