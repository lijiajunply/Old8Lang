using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser.Core;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// 类解析器，负责解析类声明、访问修饰符和类块
/// </summary>
public class ClassParser(
    ParserContext context,
    Func<StatementParser> statementParserFactory,
    Func<ExpressionParser> expressionParserFactory,
    Func<FunctionParser> functionParserFactory)
    : ParserBase(context)
{
    public ClassInit ParseClassDeclaration()
    {
        // 收集前置的文档注释
        var docComment = CollectPrecedingDocComments();

        bool isAbstract = false;
        bool isMixin = false;

        // 检查 abstract 修饰符
        if (CurrentToken.Type == LangTokenType.Abstract)
        {
            isAbstract = true;
            Expect(LangTokenType.Abstract);
        }

        // 检查是 class 还是 mixin
        if (CurrentToken.Type == LangTokenType.Class)
        {
            Expect(LangTokenType.Class);
        }
        else if (CurrentToken.Type == LangTokenType.Mixin)
        {
            isMixin = true;
            Expect(LangTokenType.Mixin);
        }
        else
        {
            throw new InvalidOperationException("Expected class or mixin keyword");
        }

        var className = CurrentToken.Value;
        Expect(LangTokenType.Identifier);

        // 解析泛型参数（如果有）
        List<GenericParameter>? genericParameters = null;
        if (CurrentToken.Type == LangTokenType.LessThan)
        {
            genericParameters = ParseGenericParameters();
        }

        string? parentClassName = null;
        List<string>? parentGenericTypeParameters = null;
        List<string> mixinNames = [];
        List<string> implementsNames = [];

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

                // 处理父类的泛型参数（如果有）：extends List<T>
                if (CurrentToken.Type == LangTokenType.LessThan)
                {
                    parentGenericTypeParameters = [];
                    Expect(LangTokenType.LessThan);

                    while (CurrentToken.Type != LangTokenType.GreaterThan)
                    {
                        var funcParser = functionParserFactory();
                        // ParseComplexTypeAnnotation 会解析 T, List<T>, int 等
                        // 并且它会在逗号或 > 处停止
                        var typeArg = funcParser.ParseComplexTypeAnnotation();
                        if (string.IsNullOrEmpty(typeArg))
                        {
                            throw CreateSyntaxError("泛型参数不能为空");
                        }
                        parentGenericTypeParameters.Add(typeArg);

                        if (CurrentToken.Type == LangTokenType.Comma)
                        {
                            Expect(LangTokenType.Comma);
                            continue;
                        }

                        break;
                    }
                    Expect(LangTokenType.GreaterThan);
                }
            }
        }

        // 处理 implements 子句：class Name implements Interface1, Interface2 {
        if (CurrentToken is { Type: LangTokenType.Implements })
        {
            // 跳过 implements 关键字
            Expect(LangTokenType.Implements);

            // 解析多个接口，用逗号分隔
            while (true)
            {
                if (CurrentToken.Type == LangTokenType.Identifier)
                {
                    implementsNames.Add(CurrentToken.Value);
                    CurrentIndex++;
                }

                // 检查是否还有更多接口
                if (CurrentToken.Type == LangTokenType.Comma)
                {
                    CurrentIndex++;
                    continue;
                }

                break;
            }
        }

        // 处理 with 子句：class Name extends ParentClass with Mixin1, Mixin2 {
        if (CurrentToken is { Type: LangTokenType.With })
        {
            // 跳过 with 关键字
            Expect(LangTokenType.With);

            // 解析多个 mixin 类，用逗号分隔
            while (true)
            {
                if (CurrentToken.Type == LangTokenType.Identifier)
                {
                    mixinNames.Add(CurrentToken.Value);
                    CurrentIndex++;
                }

                // 检查是否还有更多 mixin
                if (CurrentToken.Type == LangTokenType.Comma)
                {
                    CurrentIndex++;
                    continue;
                }

                break;
            }
        }

        var classBlock = ParseClassBlock();
        var typeTemplate = new TypeTemplate(className, classBlock.ToAnyData(), classBlock.ToStaticData(),
            parentClassName, isMixin, mixinNames, implementsNames, isInterface: false, isAbstract: isAbstract,
            genericParameters: genericParameters, parentGenericTypeParameters: parentGenericTypeParameters);

        // 设置文档注释
        if (docComment is not null)
        {
            typeTemplate.DocComment = docComment;
        }

        return new ClassInit(typeTemplate);
    }

    public ClassInit ParseInterfaceDeclaration()
    {
        // 收集前置的文档注释
        var docComment = CollectPrecedingDocComments();

        // 检查是 interface
        if (CurrentToken.Type == LangTokenType.Interface)
        {
            Expect(LangTokenType.Interface);
        }
        else
        {
            throw new InvalidOperationException("Expected interface keyword");
        }

        var interfaceName = CurrentToken.Value;
        Expect(LangTokenType.Identifier);

        // 解析泛型参数（如果有）
        List<GenericParameter>? genericParameters = null;
        if (CurrentToken.Type == LangTokenType.LessThan)
        {
            genericParameters = ParseGenericParameters();
        }

        List<string> extendsNames = [];

        // 处理 extends 子句：interface Name extends Interface1, Interface2 {
        // 接口可以继承多个父接口
        if (CurrentToken is { Type: LangTokenType.Extends })
        {
            // 跳过 extends 关键字
            Expect(LangTokenType.Extends);

            // 解析多个父接口，用逗号分隔
            while (true)
            {
                if (CurrentToken.Type == LangTokenType.Identifier)
                {
                    extendsNames.Add(CurrentToken.Value);
                    CurrentIndex++;
                }

                // 检查是否还有更多父接口
                if (CurrentToken.Type == LangTokenType.Comma)
                {
                    CurrentIndex++;
                    continue;
                }

                break;
            }
        }

        // 解析接口块
        var interfaceBlock = ParseClassBlock();

        // 接口作为特殊的类处理，isInterface 标志为 true
        // 接口的父接口通过 implementsNames 参数传递（复用implements机制）
        var typeTemplate = new TypeTemplate(interfaceName, interfaceBlock.ToAnyData(), interfaceBlock.ToStaticData(),
            null, false, [], extendsNames, true, genericParameters: genericParameters);

        // 设置文档注释
        if (docComment is not null)
        {
            typeTemplate.DocComment = docComment;
        }

        return new ClassInit(typeTemplate);
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
                case LangTokenType.Protected:
                    modifiers.Add(AccessModifierType.Protected);
                    Expect(LangTokenType.Protected);
                    break;
                case LangTokenType.Static:
                    modifiers.Add(AccessModifierType.Static);
                    Expect(LangTokenType.Static);
                    break;
                case LangTokenType.Abstract:
                    modifiers.Add(AccessModifierType.Abstract);
                    Expect(LangTokenType.Abstract);
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

            // 跳过文档注释（文档注释会在后续解析时通过 CollectPrecedingDocComments 收集）
            if (CurrentToken.Type == LangTokenType.DocComment)
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
                if (CurrentToken.Type is LangTokenType.Public or LangTokenType.Private or LangTokenType.Protected
                    or LangTokenType.Static or LangTokenType.Abstract)
                {
                    modifiers = ParseAccessModifiers();
                }

                // 检查是否是抽象方法声明：[modifiers] abstract func identifier(...) -> returnType
                if (modifiers.Any(m => m == AccessModifierType.Abstract) && CurrentToken.Type == LangTokenType.Func)
                {
                    var abstractMethodDeclaration = ParseAbstractMethodDeclaration(modifiers);
                    statements.Add(abstractMethodDeclaration);
                    continue;
                }

                // 检查是否是字段声明（带类型假注或多字段）：[modifiers] identifier[:type] [, identifier[:type]]* [<- value]
                if (CurrentToken.Type == LangTokenType.Identifier)
                {
                    var nextToken = Peek();
                    // 字段声明的特征：
                    // 1. 后面是冒号（类型假注）：identifier:type
                    // 2. 标识符后面有冒号然后是逗号：identifier:type, ...
                    if (nextToken.Type == LangTokenType.Colon)
                    {
                        // 检查是否是类型假注（identifier:type）而非函数调用（identifier:default_value）
                        // 继续查看冒号后面的token
                        var tokenAfterColon = Peek(2);
                        if (tokenAfterColon.Type == LangTokenType.Identifier ||
                            tokenAfterColon.Type == LangTokenType.LeftBracket ||
                            tokenAfterColon.Type == LangTokenType.LeftBrace ||
                            tokenAfterColon.Type == LangTokenType.Question)  // 支持可空泛型类型参数（如 K?）
                        {
                            // 这是类型假注，解析字段声明列表
                            var fieldDeclarations = ParseFieldDeclarationList(modifiers);
                            statements.AddRange(fieldDeclarations);
                            continue;
                        }
                    }
                    // 3. 带修饰符但后面既没有赋值也没有函数调用，说明是未初始化字段
                    // 检查：有修饰符 && 后面不是赋值、冒号、左括号、逗号
                    else if (modifiers.Count > 0 &&
                             nextToken.Type != LangTokenType.Assignment &&
                             nextToken.Type != LangTokenType.Colon &&
                             nextToken.Type != LangTokenType.LeftParen &&
                             nextToken.Type != LangTokenType.Comma)
                    {
                        // 带修饰符的未初始化字段
                        var fieldName = CurrentToken.Value;
                        var position = CreateSourcePosition(CurrentToken);
                        CurrentIndex++;

                        var memberId = new ClassMemberId(fieldName, "", modifiers, position);
                        var defaultExpr = new NullLangValue(position);
                        var classMemberStatement = new ClassFieldSetStatement(memberId, defaultExpr, position);
                        statements.Add(classMemberStatement);
                        continue;
                    }
                    // 4. 无修饰符且后面不是赋值或函数调用，很可能是未初始化字段（但这种情况较少见）
                    else if (modifiers.Count == 0 && nextToken.Type != LangTokenType.Assignment &&
                             nextToken.Type != LangTokenType.LeftParen)
                    {
                        // 未初始化的字段（无修饰符、无类型假注）
                        var fieldName = CurrentToken.Value;
                        var position = CreateSourcePosition(CurrentToken);
                        CurrentIndex++;

                        var memberId = new ClassMemberId(fieldName, "", modifiers, position);
                        var defaultExpr = new NullLangValue(position);
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
                        case FuncInit { FuncValue.Id: not null } funcInit:
                            // 带有修饰符的类函数声明
                            var memberId2 = new ClassMemberId(funcInit.FuncValue.Id.IdName,
                                funcInit.FuncValue.Id.AssumptionType, modifiers, funcInit.Position);
                            classMemberStatement =
                                new ClassFuncInitStatement(memberId2, funcInit.FuncValue, funcInit.Position);
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

    /// <summary>
    /// 解析字段声明列表
    /// 支持的语法：
    /// - identifier:type <- value
    /// - identifier:type
    /// - identifier
    /// - identifier:type, identifier:type, identifier:type <- value
    /// </summary>
    /// <param name="modifiers">访问修饰符列表</param>
    /// <returns>字段语句列表</returns>
    private List<IOldLangTree> ParseFieldDeclarationList(List<AccessModifierType> modifiers)
    {
        var fields = new List<IOldLangTree>();
        var fieldInfos = new List<(string name, string type, SourcePosition position)>();

        // 解析第一个字段及后续用逗号分隔的字段
        while (true)
        {
            if (CurrentToken.Type != LangTokenType.Identifier)
            {
                throw CreateSyntaxError($"期望标识符，但得到 {CurrentToken.Type}");
            }

            var fieldName = CurrentToken.Value;
            var position = CreateSourcePosition(CurrentToken);
            CurrentIndex++;

            string typeAnnotation = "";

            // 检查类型假注
            if (CurrentToken.Type == LangTokenType.Colon)
            {
                CurrentIndex++; // 跳过冒号

                // 使用 FunctionParser 的 ParseComplexTypeAnnotation 处理复杂类型注解（包括联合类型和交叉类型）
                var funcParser = functionParserFactory();
                typeAnnotation = funcParser.ParseComplexTypeAnnotation();

                if (string.IsNullOrEmpty(typeAnnotation))
                {
                    throw CreateSyntaxError("类型注解不能为空");
                }
            }

            // 记录字段信息
            fieldInfos.Add((fieldName, typeAnnotation, position));

            // 检查是否有更多字段（逗号分隔）
            if (CurrentToken.Type == LangTokenType.Comma)
            {
                CurrentIndex++; // 跳过逗号
                continue;
            }

            // 没有更多字段，跳出循环
            break;
        }

        // 检查是否有初始化值
        LangExpression? initValue = null;
        if (CurrentToken.Type == LangTokenType.Assignment)
        {
            CurrentIndex++; // 跳过 <-

            // 解析初始化表达式
            var exprParser = expressionParserFactory();
            initValue = exprParser.ParseExpression();
        }

        // 为每个字段创建 ClassFieldSetStatement
        foreach (var (name, type, position) in fieldInfos)
        {
            var memberId = new ClassMemberId(name, type, modifiers, position);
            var fieldValue = initValue ?? new NullLangValue(position);
            var fieldStatement = new ClassFieldSetStatement(memberId, fieldValue, position);
            fields.Add(fieldStatement);
        }

        return fields;
    }

    /// <summary>
    /// 解析抽象方法声明
    /// 语法：[modifiers] abstract func identifier(...) -> returnType
    /// </summary>
    /// <param name="modifiers">修饰符列表</param>
    /// <returns>抽象方法声明语句</returns>
    private OldStatement ParseAbstractMethodDeclaration(List<AccessModifierType> modifiers)
    {
        var position = CreateSourcePosition(CurrentToken);

        // 跳过 func 关键字
        Expect(LangTokenType.Func);

        // 解析方法名
        if (CurrentToken.Type != LangTokenType.Identifier)
        {
            throw CreateSyntaxError($"期望方法名，但得到 {CurrentToken.Type}");
        }

        var methodName = CurrentToken.Value;
        Expect(LangTokenType.Identifier);

        // 解析参数列表
        Expect(LangTokenType.LeftParen);
        var parameters = new List<LangId>();

        while (CurrentToken.Type != LangTokenType.RightParen)
        {
            if (CurrentToken.Type == LangTokenType.EndOfFile)
            {
                throw CreateSyntaxError("意外的文件结束符，期望 ')'");
            }

            // 解析参数：[name]:type 或 name
            var paramPosition = CreateSourcePosition(CurrentToken);
            var paramName = CurrentToken.Value;
            Expect(LangTokenType.Identifier);

            string paramType = "";

            // 检查是否有类型注解
            if (CurrentToken.Type == LangTokenType.Colon)
            {
                Expect(LangTokenType.Colon);
                if (CurrentToken.Type == LangTokenType.Identifier)
                {
                    paramType = CurrentToken.Value;
                    Expect(LangTokenType.Identifier);
                }
                else
                {
                    throw CreateSyntaxError($"期望参数类型，但得到 {CurrentToken.Type}");
                }
            }

            // 检查是否有默认值
            LangExpression? defaultValue = null;
            if (CurrentToken.Type == LangTokenType.Assignment)
            {
                Expect(LangTokenType.Assignment);
                var exprParser = expressionParserFactory();
                defaultValue = exprParser.ParseExpression();
            }

            var paramId = new LangId(paramName, paramType, defaultValue, position: paramPosition);
            parameters.Add(paramId);

            // 检查是否有更多参数
            if (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);
                continue;
            }

            break;
        }

        Expect(LangTokenType.RightParen);

        // 解析返回类型
        string returnType = "";
        if (CurrentToken.Type == LangTokenType.Arrow)
        {
            Expect(LangTokenType.Arrow);
            if (CurrentToken.Type == LangTokenType.Identifier)
            {
                returnType = CurrentToken.Value;
                Expect(LangTokenType.Identifier);
            }
            else
            {
                throw CreateSyntaxError($"期望返回类型，但得到 {CurrentToken.Type}");
            }
        }

        // 抽象方法不能有方法体
        if (CurrentToken.Type == LangTokenType.LeftBrace)
        {
            throw CreateSyntaxError("抽象方法不能有方法体");
        }

        // 创建抽象方法的FuncLangValue（没有方法体）
        var methodId = new LangId(methodName, returnType, position: position);
        var funcLangValue =
            new FuncLangValue(methodId, parameters, new BlockStatement([]), null, position, isLambda: false);

        // 创建类成员ID
        var memberId = new ClassMemberId(methodName, returnType, modifiers, position);

        // 创建抽象方法声明
        return new ClassFuncInitStatement(memberId, funcLangValue, position);
    }

    /// <summary>
    /// 解析泛型参数列表
    /// 语法：<T, U, V> 或 <T: IComparable, U> 或 <T?, U?>
    /// </summary>
    /// <returns>泛型参数列表</returns>
    private List<GenericParameter> ParseGenericParameters()
    {
        Expect(LangTokenType.LessThan);
        var parameters = new List<GenericParameter>();

        while (CurrentToken.Type != LangTokenType.GreaterThan)
        {
            if (CurrentToken.Type == LangTokenType.EndOfFile)
            {
                throw CreateSyntaxError("意外的文件结束符，期望 '>'");
            }

            var position = CreateSourcePosition(CurrentToken);
            var paramName = CurrentToken.Value;
            Expect(LangTokenType.Identifier);

            // 检查是否为可空类型参数 T?
            bool isNullable = false;
            if (CurrentToken.Type == LangTokenType.Question)
            {
                isNullable = true;
                Expect(LangTokenType.Question);
            }

            List<string>? constraints = null;
            if (CurrentToken.Type == LangTokenType.Colon)
            {
                Expect(LangTokenType.Colon);
                constraints = ParseGenericConstraints();
            }

            parameters.Add(new GenericParameter(paramName, constraints, position, isNullable));

            if (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);
                continue;
            }

            break;
        }

        Expect(LangTokenType.GreaterThan);
        return parameters;
    }

    /// <summary>
    /// 解析泛型约束列表
    /// 语法：IComparable | ICloneable 或 IComparable & ICloneable
    /// 支持使用 | 或 & 作为分隔符
    /// </summary>
    /// <returns>约束名称列表</returns>
    private List<string> ParseGenericConstraints()
    {
        var constraints = new List<string>();

        if (CurrentToken.Type != LangTokenType.Identifier)
        {
            throw CreateSyntaxError($"期望约束类型名称，但得到 {CurrentToken.Type}");
        }

        constraints.Add(CurrentToken.Value);
        Expect(LangTokenType.Identifier);

        // 支持 | 或 & 作为分隔符
        while (CurrentToken.Type == LangTokenType.Pipe || CurrentToken.Type == LangTokenType.Ampersand)
        {
            if (CurrentToken.Type == LangTokenType.Pipe)
            {
                Expect(LangTokenType.Pipe);
            }
            else
            {
                Expect(LangTokenType.Ampersand);
            }

            if (CurrentToken.Type != LangTokenType.Identifier)
            {
                throw CreateSyntaxError($"期望约束类型名称，但得到 {CurrentToken.Type}");
            }

            constraints.Add(CurrentToken.Value);
            Expect(LangTokenType.Identifier);
        }

        return constraints;
    }

    /// <summary>
    /// 解析泛型类型注解
    /// 语法：<int>, <T>, <List<int>>
    /// </summary>
    /// <returns>泛型类型注解字符串（包括 < 和 >）</returns>
    private string ParseGenericTypeAnnotation()
    {
        var result = "<";
        Expect(LangTokenType.LessThan);

        while (CurrentToken.Type != LangTokenType.GreaterThan)
        {
            if (CurrentToken.Type == LangTokenType.EndOfFile)
            {
                throw CreateSyntaxError("意外的文件结束符，期望 '>'");
            }

            if (CurrentToken.Type != LangTokenType.Identifier)
            {
                throw CreateSyntaxError($"期望类型参数名称，但得到 {CurrentToken.Type}");
            }

            result += CurrentToken.Value;
            Expect(LangTokenType.Identifier);

            // 递归处理嵌套泛型
            if (CurrentToken.Type == LangTokenType.LessThan)
            {
                result += ParseGenericTypeAnnotation();
            }

            // 可空类型标记
            if (CurrentToken.Type == LangTokenType.Question)
            {
                result += "?";
                Expect(LangTokenType.Question);
            }

            if (CurrentToken.Type == LangTokenType.Comma)
            {
                result += ", ";
                Expect(LangTokenType.Comma);
                continue;
            }

            break;
        }

        result += ">";
        Expect(LangTokenType.GreaterThan);
        return result;
    }
}