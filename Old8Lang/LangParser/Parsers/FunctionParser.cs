using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser.Core;
using Old8Lang.Error;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// 函数解析器，负责解析函数声明、参数列表、Lambda
/// </summary>
public class FunctionParser(
    ParserContext context,
    Func<StatementParser> statementParserFactory,
    Func<ExpressionParser> expressionParserFactory)
    : ParserBase(context)
{
    /// <summary>
    /// 解析函数声明
    /// funcDeclaration = decorators? ( "func" identifier | identifier ) "(" idList? ")" ( "->" )? block
    /// </summary>
    public FuncInit ParseFuncDeclaration(List<FunctionDecorator>? decorators = null)
    {
        // 如果没有提供装饰器，则解析装饰器
        decorators ??= ParseDecorators();

        // 收集前置的文档注释
        var docComment = CollectPrecedingDocComments();

        var isUseFunc = CurrentToken.Type == LangTokenType.Func;
        if (isUseFunc)
        {
            Expect(LangTokenType.Func);
        }

        var funcName = ParseIdentifier();
        var returnType = string.Empty;

        // 解析泛型参数（如果有）
        List<GenericParameter>? genericParameters = null;
        if (CurrentToken.Type == LangTokenType.LessThan)
        {
            genericParameters = ParseGenericParameters();
        }

        // 检查是否有类型注解语法：identifier:returnType
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);
            if (CurrentToken.Type != LangTokenType.Identifier)
            {
                throw CreateSyntaxError("请返回类型标识符");
            }

            // 使用 ParseComplexTypeAnnotation 处理复杂类型注解（包括联合类型和交叉类型）
            returnType = ParseComplexTypeAnnotation();
        }

        Expect(LangTokenType.LeftParen);
        var parameters = ParseIdList();
        Expect(LangTokenType.RightParen);

        // 检查是否有箭头语法用于返回类型注解或函数体
        if (CurrentToken.Type == LangTokenType.Arrow)
        {
            Expect(LangTokenType.Arrow);
            // 解析返回类型标识符（如果有）
            if (isUseFunc && CurrentToken.Type == LangTokenType.Identifier)
            {
                if (!string.IsNullOrEmpty(returnType))
                {
                    throw CreateSyntaxError("函数返回类型重复声明");
                }

                // 使用 ParseComplexTypeAnnotation 处理复杂类型注解（包括联合类型和交叉类型）
                returnType = ParseComplexTypeAnnotation();
            }
        }

        // 解析 where 子句（如果有）
        if (CurrentToken.Type == LangTokenType.Where)
        {
            ParseWhereClause(ref genericParameters);
        }

        // 如果有返回类型注解，创建新的LangId并设置AssumptionType
        var updatedFuncName = funcName;
        if (!string.IsNullOrEmpty(returnType))
        {
            updatedFuncName = new LangId(funcName.IdName, returnType, position: funcName.Position);
        }

        BlockStatement block;
        
        // 检查是否在接口中，接口方法只有签名，没有实现体
        // 如果下一个token是右大括号，说明是接口方法，不需要解析函数体
        if (CurrentToken.Type == LangTokenType.RightBrace)
        {
            // 接口方法，创建空的BlockStatement
            block = new BlockStatement(new List<IOldLangTree>());
        }
        else
        {
            // 普通函数，解析函数体
            var stmtParser = statementParserFactory();
            block = stmtParser.ParseBlock();
        }

        // 普通函数声明,生成 FuncInit，设置 IsLambda 为 false
        var funcLangValue = new FuncLangValue(updatedFuncName, parameters, block, genericParameters, isLambda: false);

        // 设置装饰器
        if (decorators is not null)
        {
            funcLangValue.Decorators = decorators;
        }

        // 设置文档注释
        if (docComment is not null)
        {
            funcLangValue.DocComment = docComment;
        }

        return new FuncInit(funcLangValue);
    }

    /// <summary>
    /// 解析异步函数声明
    /// asyncFuncDeclaration = decorators? "async" "func" identifier "(" idList? ")" ( "->" returnType )? block
    /// </summary>
    public AsyncFuncInit ParseAsyncFuncDeclaration(DocCommentInfo? providedDocComment = null, List<FunctionDecorator>? decorators = null)
    {
        // 如果提供了文档注释则使用提供的，否则收集前置的文档注释
        var docComment = providedDocComment ?? CollectPrecedingDocComments();

        // 这里假设 async 和 func 关键字已经被消费了
        var funcName = ParseIdentifier();
        var returnType = string.Empty;

        // 解析泛型参数（如果有）
        List<GenericParameter>? genericParameters = null;
        if (CurrentToken.Type == LangTokenType.LessThan)
        {
            genericParameters = ParseGenericParameters();
        }

        // 检查是否有类型注解语法：identifier:returnType
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);
            if (CurrentToken.Type != LangTokenType.Identifier)
            {
                throw CreateSyntaxError("请返回类型标识符");
            }

            // 使用 ParseComplexTypeAnnotation 处理复杂类型注解（包括联合类型和交叉类型）
            returnType = ParseComplexTypeAnnotation();
        }

        Expect(LangTokenType.LeftParen);
        var parameters = ParseIdList();
        Expect(LangTokenType.RightParen);

        // 检查是否有箭头语法用于返回类型注解或函数体
        if (CurrentToken.Type == LangTokenType.Arrow)
        {
            Expect(LangTokenType.Arrow);
            // 解析返回类型标识符（如果有）
            if (CurrentToken.Type == LangTokenType.Identifier)
            {
                if (!string.IsNullOrEmpty(returnType))
                {
                    throw CreateSyntaxError("函数返回类型重复声明");
                }

                returnType = CurrentToken.Value;
                Expect(LangTokenType.Identifier);
            }
        }

        // 解析 where 子句（如果有）
        if (CurrentToken.Type == LangTokenType.Where)
        {
            ParseWhereClause(ref genericParameters);
        }

        // 如果有返回类型注解，创建新的LangId并设置AssumptionType
        var updatedFuncName = funcName;
        if (!string.IsNullOrEmpty(returnType))
        {
            updatedFuncName = new LangId(funcName.IdName, returnType, position: funcName.Position);
        }

        var stmtParser = statementParserFactory();
        var block = stmtParser.ParseBlock();

        // 异步函数声明，生成 AsyncFuncInit
        var asyncFuncLangValue = new AsyncFuncLangValue(updatedFuncName, parameters, block, updatedFuncName.Position);

        // 设置装饰器
        if (decorators is not null)
        {
            asyncFuncLangValue.Decorators = decorators;
        }

        // 设置文档注释
        if (docComment is not null)
        {
            asyncFuncLangValue.DocComment = docComment;
        }

        return new AsyncFuncInit(asyncFuncLangValue, updatedFuncName.Position);
    }

    /// <summary>
    /// 解析标识符列表（函数参数）
    /// </summary>
    public List<LangId> ParseIdList()
    {
        var ids = new List<LangId>();

        // 检查当前token是否是标识符或关键字
        if (CurrentToken.Type is LangTokenType.Identifier or LangTokenType.Func or LangTokenType.Class
            or LangTokenType.If or LangTokenType.Else or LangTokenType.While or LangTokenType.For
            or LangTokenType.Return or LangTokenType.Import or LangTokenType.True or LangTokenType.False
            or LangTokenType.Params) // 支持 params 关键字
        {
            // 解析第一个参数，允许类型注解和 params 修饰符
            ids.Add(ParseTypedIdentifier(true));

            // 跳过默认参数值（如果有）
            if (CurrentToken.Type == LangTokenType.Assignment)
            {
                // 跳过赋值符号和表达式
                while (CurrentToken.Type != LangTokenType.Comma && CurrentToken.Type != LangTokenType.RightParen)
                {
                    CurrentIndex++;
                }
            }

            // 解析更多参数，允许类型注解和 params 修饰符
            while (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);
                ids.Add(ParseTypedIdentifier(true));

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

        return ids;
    }

    /// <summary>
    /// 解析参数列表（函数调用），支持命名参数
    /// </summary>
    /// <param name="positionalArgs">输出的位置参数列表</param>
    /// <param name="namedArgs">输出的命名参数列表</param>
    public void ParseArgList(out List<LangExpression> positionalArgs, out List<NamedArgument> namedArgs)
    {
        positionalArgs = [];
        namedArgs = [];

        if (CurrentToken.Type == LangTokenType.RightParen)
        {
            return;
        }

        var exprParser = expressionParserFactory();
        bool hasSeenNamedArg = false;

        while (true)
        {
            var startPosition = new SourcePosition(CurrentToken.Line, CurrentToken.Column);

            // 检查是否是命名参数：identifier: expression
            // 我们需要预读来判断是否是命名参数
            if (CurrentToken.Type == LangTokenType.Identifier)
            {
                var savedIndex = CurrentIndex;
                var identifierName = CurrentToken.Value;
                CurrentIndex++; // 移到下一个 token

                // 检查下一个 token 是否是冒号
                if (CurrentToken.Type == LangTokenType.Colon)
                {
                    // 这是一个命名参数
                    hasSeenNamedArg = true;
                    Expect(LangTokenType.Colon);
                    var valueExpr = exprParser.ParseExpression();
                    namedArgs.Add(new NamedArgument(identifierName, valueExpr, startPosition));
                }
                else
                {
                    // 不是命名参数，恢复位置并按普通表达式解析
                    CurrentIndex = savedIndex;

                    // 如果之前已经出现过命名参数，现在又出现位置参数，报错
                    if (hasSeenNamedArg)
                    {
                        throw new SyntaxError(startPosition,
                            "位置参数必须出现在所有命名参数之前");
                    }

                    var expr = exprParser.ParseExpression();
                    positionalArgs.Add(expr);
                }
            }
            else
            {
                // 不是标识符开头，按普通表达式解析
                if (hasSeenNamedArg)
                {
                    throw new SyntaxError(startPosition,
                        "位置参数必须出现在所有命名参数之前");
                }

                var expr = exprParser.ParseExpression();
                positionalArgs.Add(expr);
            }

            // 检查是否还有更多参数
            if (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// 解析参数列表（函数调用）- 向后兼容的方法
    /// </summary>
    public List<LangExpression> ParseArgList()
    {
        ParseArgList(out var positionalArgs, out var namedArgs);

        // 如果有命名参数，抛出错误（这个方法不支持命名参数）
        if (namedArgs.Count > 0)
        {
            throw new SyntaxError(new SourcePosition(CurrentToken.Line, CurrentToken.Column),
                "此上下文不支持命名参数");
        }

        return positionalArgs;
    }

    /// <summary>
    /// 解析带有类型注解或默认参数的标识符，用于赋值语句、函数参数和lambda参数
    /// </summary>
    public LangId ParseTypedIdentifier(bool isNeedDefaultValue)
    {
        // 检查是否有 params 修饰符
        bool isParams = false;
        if (CurrentToken.Type == LangTokenType.Params)
        {
            isParams = true;
            Expect(LangTokenType.Params);
        }

        var identifierToken = CurrentToken;
        var position = new SourcePosition(identifierToken.Line, identifierToken.Column,
            tokenValue: identifierToken.Value);
        var value = identifierToken.Value;

        // 检查当前token是否是标识符或关键字
        if (CurrentToken.Type is LangTokenType.Identifier or LangTokenType.Func or LangTokenType.Class
            or LangTokenType.If or LangTokenType.Else or LangTokenType.While or LangTokenType.For
            or LangTokenType.Return or LangTokenType.Import or LangTokenType.True or LangTokenType.False)
        {
            CurrentIndex++;
        }
        else
        {
            Expect(LangTokenType.Identifier);
        }

        // 处理类型注解或默认参数：identifier:type 或 identifier:default_value
        var typeAnnotation = "";
        LangExpression? defaultValue = null;

        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);

            // 检查下一个token类型，判断是类型注解还是默认参数
            if (CurrentToken.Type is LangTokenType.Identifier)
            {
                // 类型注解：支持联合类型 (A | B) 和交叉类型 (A & B)
                typeAnnotation = ParseComplexTypeAnnotation();
                if (typeAnnotation == "")
                {
                    throw CreateSyntaxError("类型注解不能为空");
                }
            }
            else if (isNeedDefaultValue)
            {
                // 默认参数：identifier:default_value
                var exprParser = expressionParserFactory();
                defaultValue = exprParser.ParseExpression();
            }
        }

        return new LangId(value, typeAnnotation, defaultValue, isParams, position);
    }

    /// <summary>
    /// 解析标识符
    /// </summary>
    public LangId ParseIdentifier()
    {
        var identifierToken = CurrentToken;
        var position = new SourcePosition(identifierToken.Line, identifierToken.Column,
            tokenValue: identifierToken.Value);
        var value = identifierToken.Value;

        // 检查当前token是否是标识符或关键字
        if (CurrentToken.Type is LangTokenType.Identifier or LangTokenType.Func or LangTokenType.Class
            or LangTokenType.If or LangTokenType.Else or LangTokenType.While or LangTokenType.For
            or LangTokenType.Return or LangTokenType.Import or LangTokenType.True or LangTokenType.False)
        {
            CurrentIndex++;
        }
        else
        {
            Expect(LangTokenType.Identifier);
        }

        // 默认不处理类型注解
        return new LangId(value, position: position);
    }

    /// <summary>
    /// 解析泛型参数列表
    /// 语法：<T, U, V> 或 <T: IComparable, U> 或 <T?, U?> 或 <T: new() & class, U>
    /// </summary>
    /// <returns>泛型参数列表</returns>
    private List<GenericParameter> ParseGenericParameters()
    {
        Expect(LangTokenType.LessThan);
        var parameters = new List<GenericParameter>();

        // 收集所有泛型参数名称（用于判断类型参数约束）
        var genericParamNames = new HashSet<string>();

        // 第一遍：收集所有参数名称
        var savedIndex = CurrentIndex;
        while (CurrentToken.Type != LangTokenType.GreaterThan && CurrentToken.Type != LangTokenType.EndOfFile)
        {
            if (CurrentToken.Type == LangTokenType.Identifier)
            {
                genericParamNames.Add(CurrentToken.Value);
            }
            CurrentIndex++;
        }
        CurrentIndex = savedIndex;

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

            List<GenericConstraint>? structuredConstraints = null;
            if (CurrentToken.Type == LangTokenType.Colon)
            {
                Expect(LangTokenType.Colon);
                structuredConstraints = ParseGenericConstraintsStructured(genericParamNames);
            }

            parameters.Add(new GenericParameter(paramName, structuredConstraints, position, isNullable));

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
    /// 解析泛型约束列表（结构化版本）
    /// 语法：new() | class | struct | IComparable | T
    /// 支持使用 | 或 & 作为分隔符
    /// </summary>
    /// <param name="genericParamNames">当前泛型参数名称集合（用于判断类型参数约束）</param>
    /// <returns>结构化约束列表</returns>
    private List<GenericConstraint> ParseGenericConstraintsStructured(HashSet<string>? genericParamNames)
    {
        var constraints = new List<GenericConstraint>();
        var position = CreateSourcePosition(CurrentToken);

        // 解析第一个约束
        var firstConstraint = ParseSingleConstraint(genericParamNames, position);
        constraints.Add(firstConstraint);

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

            position = CreateSourcePosition(CurrentToken);
            var constraint = ParseSingleConstraint(genericParamNames, position);
            constraints.Add(constraint);
        }

        return constraints;
    }

    /// <summary>
    /// 解析单个泛型约束
    /// </summary>
    /// <param name="genericParamNames">当前泛型参数名称集合</param>
    /// <param name="position">源代码位置</param>
    /// <returns>单个约束</returns>
    private GenericConstraint ParseSingleConstraint(HashSet<string>? genericParamNames, SourcePosition position)
    {
        // 检查 new() 约束
        if (CurrentToken.Type == LangTokenType.New)
        {
            Expect(LangTokenType.New);
            // 检查是否有括号 new()
            if (CurrentToken.Type == LangTokenType.LeftParen)
            {
                Expect(LangTokenType.LeftParen);
                Expect(LangTokenType.RightParen);
            }
            return GenericConstraint.CreateNew(position);
        }

        // 检查 class 约束
        if (CurrentToken.Type == LangTokenType.Class)
        {
            Expect(LangTokenType.Class);
            return GenericConstraint.CreateClass(position);
        }

        // 检查 struct 约束
        if (CurrentToken.Type == LangTokenType.Struct)
        {
            Expect(LangTokenType.Struct);
            return GenericConstraint.CreateStruct(position);
        }

        // 检查标识符（类型名称或类型参数约束）
        if (CurrentToken.Type == LangTokenType.Identifier)
        {
            var typeName = CurrentToken.Value;
            Expect(LangTokenType.Identifier);

            // 检查是否为类型参数约束（T: U）
            if (genericParamNames != null && genericParamNames.Contains(typeName))
            {
                return GenericConstraint.CreateTypeParameter(typeName, position);
            }

            // 否则为类型名称约束（接口或基类）
            return GenericConstraint.CreateTypeName(typeName, position);
        }

        throw CreateSyntaxError($"期望约束类型（new()、class、struct 或类型名称），但得到 {CurrentToken.Type}");
    }

    /// <summary>
    /// 解析泛型约束列表（向后兼容版本）
    /// 语法：IComparable | ICloneable 或 IComparable & ICloneable
    /// 支持使用 | 或 & 作为分隔符
    /// </summary>
    /// <returns>约束名称列表</returns>
    private List<string> ParseGenericConstraints()
    {
        var constraints = new List<string>();

        // 检查 new() 约束
        if (CurrentToken.Type == LangTokenType.New)
        {
            Expect(LangTokenType.New);
            if (CurrentToken.Type == LangTokenType.LeftParen)
            {
                Expect(LangTokenType.LeftParen);
                Expect(LangTokenType.RightParen);
            }
            constraints.Add("new()");
        }
        // 检查 class 约束
        else if (CurrentToken.Type == LangTokenType.Class)
        {
            Expect(LangTokenType.Class);
            constraints.Add("class");
        }
        // 检查 struct 约束
        else if (CurrentToken.Type == LangTokenType.Struct)
        {
            Expect(LangTokenType.Struct);
            constraints.Add("struct");
        }
        else if (CurrentToken.Type == LangTokenType.Identifier)
        {
            constraints.Add(CurrentToken.Value);
            Expect(LangTokenType.Identifier);
        }
        else
        {
            throw CreateSyntaxError($"期望约束类型名称，但得到 {CurrentToken.Type}");
        }

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

            // 检查 new() 约束
            if (CurrentToken.Type == LangTokenType.New)
            {
                Expect(LangTokenType.New);
                if (CurrentToken.Type == LangTokenType.LeftParen)
                {
                    Expect(LangTokenType.LeftParen);
                    Expect(LangTokenType.RightParen);
                }
                constraints.Add("new()");
            }
            // 检查 class 约束
            else if (CurrentToken.Type == LangTokenType.Class)
            {
                Expect(LangTokenType.Class);
                constraints.Add("class");
            }
            // 检查 struct 约束
            else if (CurrentToken.Type == LangTokenType.Struct)
            {
                Expect(LangTokenType.Struct);
                constraints.Add("struct");
            }
            else if (CurrentToken.Type == LangTokenType.Identifier)
            {
                constraints.Add(CurrentToken.Value);
                Expect(LangTokenType.Identifier);
            }
            else
            {
                throw CreateSyntaxError($"期望约束类型名称，但得到 {CurrentToken.Type}");
            }
        }

        return constraints;
    }

    /// <summary>
    /// 解析泛型类型注解
    /// 语法：&lt;int>, &lt;T>, &lt;List&lt;int>>
    /// </summary>
    /// <returns>泛型类型注解字符串（包括 &lt; 和 >）</returns>
    /// <summary>
    /// 解析复杂类型注解（支持联合类型 A | B 和交叉类型 A & B）
    /// 停止条件：遇到 &lt;-, ,, ), }, where, {, ; 等终止符
    /// </summary>
    public string ParseComplexTypeAnnotation()
    {
        var result = "";
        var justProcessedOperator = false; // 标记是否刚刚处理了 | 或 & 操作符

        // 持续读取类型注解，直到遇到终止符
        while (true)
        {
            // 检查是否遇到终止符
            if (CurrentToken.Type is LangTokenType.Assignment      // <-
                or LangTokenType.Comma                             // ,
                or LangTokenType.RightParen                        // )
                or LangTokenType.RightBrace                        // }
                or LangTokenType.Where                             // where
                or LangTokenType.LeftBrace                         // {
                or LangTokenType.Semicolon                         // ;
                or LangTokenType.EndOfFile)                        // EOF
            {
                break;
            }

            // 检查是否是下一个语句的开始（标识符 + 赋值符号或冒号）
            // 这种情况表示当前类型注解已经结束
            // 注意：只有在已经读取了一些类型内容后才进行此检查，避免在类型注解刚开始时就误判
            // 同时，如果刚刚处理了联合类型或交叉类型操作符，不进行此检查，因为操作符后面的类型是当前类型注解的一部分
            if (result.Length > 0 && !justProcessedOperator && CurrentToken.Type == LangTokenType.Identifier)
            {
                var nextToken = Peek();
                if (nextToken.Type == LangTokenType.Colon ||
                    nextToken.Type == LangTokenType.Assignment)
                {
                    // 这是下一个字段声明或赋值语句的开始，停止当前类型注解的解析
                    break;
                }
            }

            // 读取标识符（类型名）或 null 关键字
            if (CurrentToken.Type == LangTokenType.Identifier || CurrentToken.Type == LangTokenType.Null)
            {
                result += CurrentToken.Value;
                CurrentIndex++; // 跳过当前 token
                justProcessedOperator = false; // 读取了标识符，重置操作符标记

                // 处理泛型类型（例如 List<int>）- 仅对标识符有效
                if (CurrentToken.Type == LangTokenType.LessThan)
                {
                    result += ParseGenericTypeAnnotation();
                }

                // 处理可空类型（例如 int?）- 仅对标识符有效
                if (CurrentToken.Type == LangTokenType.Question)
                {
                    result += "?";
                    Expect(LangTokenType.Question);
                }
            }
            // 读取联合类型分隔符 |
            else if (CurrentToken.Type == LangTokenType.Pipe)
            {
                result += "|";
                Expect(LangTokenType.Pipe);
                justProcessedOperator = true; // 标记刚刚处理了操作符

                // 验证 | 后面必须有类型标识符或 null 关键字
                if (CurrentToken.Type != LangTokenType.Identifier && CurrentToken.Type != LangTokenType.Null)
                {
                    throw CreateSyntaxError($"联合类型操作符 '|' 后必须跟随类型标识符或 null，但得到 {CurrentToken.Type}");
                }
            }
            // 读取交叉类型分隔符 &
            else if (CurrentToken.Type == LangTokenType.Ampersand)
            {
                result += "&";
                Expect(LangTokenType.Ampersand);
                justProcessedOperator = true; // 标记刚刚处理了操作符

                // 验证 & 后面必须有类型标识符
                if (CurrentToken.Type != LangTokenType.Identifier)
                {
                    throw CreateSyntaxError($"交叉类型操作符 '&' 后必须跟随类型标识符，但得到 {CurrentToken.Type}");
                }
            }
            else
            {
                // 遇到未知token，停止解析
                break;
            }
        }

        return result.Trim();
    }

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

            // 读取一个完整的类型参数（可能包含 | 或 &）
            // 持续读取直到遇到 , 或 >
            while (CurrentToken.Type != LangTokenType.Comma && CurrentToken.Type != LangTokenType.GreaterThan)
            {
                if (CurrentToken.Type == LangTokenType.Identifier)
                {
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
                }
                // 处理联合类型 |
                else if (CurrentToken.Type == LangTokenType.Pipe)
                {
                    result += "|";
                    Expect(LangTokenType.Pipe);
                }
                // 处理交叉类型 &
                else if (CurrentToken.Type == LangTokenType.Ampersand)
                {
                    result += "&";
                    Expect(LangTokenType.Ampersand);
                }
                else
                {
                    break;
                }
            }

            // 处理参数分隔符
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

    /// <summary>
    /// 解析 where 子句
    /// 语法：where T: IComparable | where T: IComparable & ICloneable | where T: IComparable, U: ICloneable
    /// 支持：where T: new() & class & IComparable
    /// </summary>
    /// <param name="genericParameters">泛型参数列表的引用，将在其中更新约束</param>
    private void ParseWhereClause(ref List<GenericParameter>? genericParameters)
    {
        if (genericParameters is null || genericParameters.Count == 0)
        {
            throw CreateSyntaxError("where 子句只能用于泛型函数");
        }

        Expect(LangTokenType.Where);

        // 收集所有泛型参数名称（用于判断类型参数约束）
        var genericParamNames = new HashSet<string>(genericParameters.Select(p => p.Name));

        // 解析一个或多个约束，用逗号分隔
        // 例如: where T: IComparable, U: ICloneable
        while (true)
        {
            // 解析类型参数名称
            if (CurrentToken.Type != LangTokenType.Identifier)
            {
                throw CreateSyntaxError($"期望类型参数名称，但得到 {CurrentToken.Type}");
            }

            var typeParamName = CurrentToken.Value;
            Expect(LangTokenType.Identifier);

            // 查找对应的泛型参数
            var genericParam = genericParameters.FirstOrDefault(p => p.Name == typeParamName);
            if (genericParam is null)
            {
                throw CreateSyntaxError($"未定义的类型参数 '{typeParamName}'");
            }

            // 期望冒号
            if (CurrentToken.Type != LangTokenType.Colon)
            {
                throw CreateSyntaxError($"期望 ':', 但得到 {CurrentToken.Type}");
            }
            Expect(LangTokenType.Colon);

            // 解析结构化约束列表
            var constraints = ParseGenericConstraintsStructured(genericParamNames);

            // 更新泛型参数的约束
            // 如果已有约束，则合并
            var existingConstraints = genericParam.StructuredConstraints ?? [];
            existingConstraints.AddRange(constraints);

            // 创建新的 GenericParameter 对象替换旧的，保留原有的 IsNullable 属性
            var index = genericParameters.IndexOf(genericParam);
            genericParameters[index] = new GenericParameter(
                genericParam.Name,
                existingConstraints,
                genericParam.Position,
                genericParam.IsNullable
            );

            // 检查是否有更多约束（逗号分隔）
            if (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);
                continue;
            }

            // 没有更多约束，退出循环
            break;
        }
    }

    /// <summary>
    /// 解析单个装饰器
    /// decorator = "@" identifier ( "(" argList? ")" )?
    /// </summary>
    public FunctionDecorator ParseDecorator()
    {
        var position = new SourcePosition(CurrentToken.Line, CurrentToken.Column);

        // 消费 @ 符号
        Expect(LangTokenType.At);

        // 解析装饰器名称
        var decoratorName = ParseIdentifier().IdName;

        // 检查是否有参数
        List<LangExpression>? arguments = null;
        if (CurrentToken.Type == LangTokenType.LeftParen)
        {
            Expect(LangTokenType.LeftParen);

            // 使用 ParseArgList 解析参数列表（支持命名参数）
            if (CurrentToken.Type != LangTokenType.RightParen)
            {
                ParseArgList(out var positionalArgs, out var namedArgs);

                // 合并位置参数和命名参数
                arguments = [];
                arguments.AddRange(positionalArgs);

                // 将命名参数的值添加到参数列表
                // 注意：装饰器调用时，命名参数会被转换为位置参数
                foreach (var namedArg in namedArgs)
                {
                    arguments.Add(namedArg.Value);
                }
            }

            Expect(LangTokenType.RightParen);
        }

        return new FunctionDecorator(decoratorName, arguments, position);
    }

    /// <summary>
    /// 解析装饰器列表
    /// decorators = decorator*
    /// </summary>
    public List<FunctionDecorator>? ParseDecorators()
    {
        List<FunctionDecorator>? decorators = null;

        while (CurrentToken.Type == LangTokenType.At)
        {
            decorators ??= [];
            decorators.Add(ParseDecorator());
        }

        return decorators;
    }
}
