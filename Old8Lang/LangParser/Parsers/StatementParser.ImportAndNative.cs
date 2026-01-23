using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// 语句解析器 - 导入和原生绑定
/// </summary>
public partial class StatementParser
{
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
            importSpecifiers = [];
        }
        // 检查是否有导入指定项
        else if (CurrentToken.Type == LangTokenType.LeftBrace)
        {
            // 解析命名导入：{ item1, item2 as alias2, ... }
            importSpecifiers = [];
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
        Expect(LangTokenType.Extern);
        var dllName = CurrentToken.Value;
        Expect(LangTokenType.String);
        var className = CurrentToken.Value;
        Expect(LangTokenType.Identifier);

        // 检查是否是批量导入所有方法：extern "DllName" ClassName *
        if (CurrentToken.Type == LangTokenType.Star)
        {
            Expect(LangTokenType.Star);
            return new NativeStatement(dllName, className, importAll: true);
        }

        // 检查是否是选择性导入多个方法：extern "DllName" ClassName { Method1, Method2 }
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

        // 原有的单个方法导入：extern "DllName" ClassName MethodName Alias?
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
    /// nativeStatic = "extern" STRING identifier "->" STRING ;
    /// </summary>
    /// <returns>引入原生静态类</returns>

    public NativeStatement ParseNativeStatic()
    {
        Expect(LangTokenType.Extern);
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
    ///  nativeClass = "extern" STRING identifier ("as" identifier)? ;
    /// </summary>
    /// <returns>引入原生类</returns>

    public NativeStatement ParseNativeClass()
    {
        Expect(LangTokenType.Extern);
        var dllName = CurrentToken.Value;
        Expect(LangTokenType.String);
        var className = CurrentToken.Value;
        Expect(LangTokenType.Identifier);

        // 检查是否有 as 别名：extern "DllName" ClassName as Alias
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

}
