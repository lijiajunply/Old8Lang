using Old8Lang.AST.Expression;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser.Core;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// 扩展方法解析器，负责解析扩展方法声明
/// </summary>
public class ExtensionParser(
    ParserContext context,
    Func<FunctionParser> functionParserFactory)
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

        List<GenericParameter>? targetGenericParameters = null;
        var targetTypeParser = functionParserFactory();
        if (CurrentToken.Type == LangTokenType.LessThan)
        {
            targetGenericParameters = targetTypeParser.ParseGenericParameters();
        }

        if (CurrentToken.Type == LangTokenType.Where)
        {
            targetTypeParser.ParseWhereClause(ref targetGenericParameters);
        }

        // 期望左花括号
        Expect(LangTokenType.LeftBrace);

        // 解析扩展方法列表
        var extensionMethods = new List<FunctionSyntax>();

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

                // 只收集语法模型，运行时函数值由工厂按需创建
                extensionMethods.Add(funcInit.Syntax);
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

        return new ExtensionDeclaration(targetTypeName, targetGenericParameters, extensionMethods, startPosition);
    }
}
