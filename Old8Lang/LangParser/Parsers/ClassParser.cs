using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser.Core;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// 类解析器，负责解析类声明、访问修饰符和类块
/// </summary>
public class ClassParser(
    ParserContext context,
    Func<StatementParser> statementParserFactory)
    : ParserBase(context)
{
    public ClassInit ParseClassDeclaration()
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
    public List<AccessModifierType> ParseAccessModifiers()
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
    public BlockStatement ParseClassBlock()
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

                // 检查是否是未初始化的字段声明：[modifiers] identifier
                if (CurrentToken.Type == LangTokenType.Identifier)
                {
                    var nextToken = Peek();
                    // 未初始化字段：后面不是赋值符号
                    if (nextToken.Type != LangTokenType.Assignment)
                    {
                        // 处理未初始化字段声明
                        var fieldName = CurrentToken.Value;
                        var position = CreateSourcePosition(CurrentToken);
                        CurrentIndex++;

                        // 创建类成员ID
                        var memberId = new ClassMemberId(fieldName, "", modifiers, position);
                        // 使用默认值 null 或适当的默认值
                        var defaultExpr = new NullLangValue(position);
                        // 创建 ClassFieldSetStatement
                        var classMemberStatement = new ClassFieldSetStatement(memberId, defaultExpr, position);
                        statements.Add(classMemberStatement);
                        continue;
                    }
                }

                // 解析语句，传递修饰符
                var statement = statementParserFactory().ParseStatement(modifiers);

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
                    // 直接添加原始语句
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
}
