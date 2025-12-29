using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser.Core;

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
    /// funcDeclaration = ( "func" identifier | identifier ) "(" idList? ")" ( "->" )? block
    /// </summary>
    public FuncInit ParseFuncDeclaration()
    {
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

            returnType = CurrentToken.Value;
            Expect(LangTokenType.Identifier);

            // 检查是否为可空类型（例如 "int?"）
            if (CurrentToken.Type == LangTokenType.Question)
            {
                returnType += "?";
                Expect(LangTokenType.Question);
            }
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

                returnType = CurrentToken.Value;
                Expect(LangTokenType.Identifier);

                // 检查是否为泛型类型（例如 "Box<T>"）
                if (CurrentToken.Type == LangTokenType.LessThan)
                {
                    returnType += ParseGenericTypeAnnotation();
                }
                // 检查是否为可空类型（例如 "int?"）
                else if (CurrentToken.Type == LangTokenType.Question)
                {
                    returnType += "?";
                    Expect(LangTokenType.Question);
                }
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
        return new FuncInit(new FuncLangValue(updatedFuncName, parameters, block, genericParameters, isLambda: false));
    }

    /// <summary>
    /// 解析异步函数声明
    /// asyncFuncDeclaration = "async" "func" identifier "(" idList? ")" ( "->" returnType )? block
    /// </summary>
    public AsyncFuncInit ParseAsyncFuncDeclaration()
    {
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

            returnType = CurrentToken.Value;
            Expect(LangTokenType.Identifier);

            // 检查是否为可空类型（例如 "int?"）
            if (CurrentToken.Type == LangTokenType.Question)
            {
                returnType += "?";
                Expect(LangTokenType.Question);
            }
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
        return new AsyncFuncInit(
            new AsyncFuncLangValue(updatedFuncName, parameters, block, updatedFuncName.Position),
            updatedFuncName.Position);
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
            or LangTokenType.Return or LangTokenType.Import or LangTokenType.True or LangTokenType.False)
        {
            // 解析第一个参数，允许类型注解
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

            // 解析更多参数，允许类型注解
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
    /// 解析参数列表（函数调用）
    /// </summary>
    public List<LangExpression> ParseArgList()
    {
        var args = new List<LangExpression>();

        if (CurrentToken.Type == LangTokenType.RightParen)
        {
            return args;
        }

        var exprParser = expressionParserFactory();
        args.Add(exprParser.ParseExpression());
        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            args.Add(exprParser.ParseExpression());
        }

        return args;
    }

    /// <summary>
    /// 解析带有类型注解或默认参数的标识符，用于赋值语句、函数参数和lambda参数
    /// </summary>
    public LangId ParseTypedIdentifier(bool isNeedDefaultValue)
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

        // 处理类型注解或默认参数：identifier:type 或 identifier:default_value
        var typeAnnotation = "";
        LangExpression? defaultValue = null;

        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);

            // 检查下一个token类型，判断是类型注解还是默认参数
            if (CurrentToken.Type is LangTokenType.Identifier)
            {
                // 类型注解：identifier:type 或 identifier:type?
                typeAnnotation = CurrentToken.Value;
                if (typeAnnotation == "")
                {
                    throw CreateSyntaxError("类型注解不能为空");
                }

                Expect(LangTokenType.Identifier);

                // 检查是否为泛型类型（例如 "List<int>"）
                if (CurrentToken.Type == LangTokenType.LessThan)
                {
                    typeAnnotation += ParseGenericTypeAnnotation();
                }
                // 检查是否为可空类型（例如 "int?"）
                else if (CurrentToken.Type == LangTokenType.Question)
                {
                    typeAnnotation += "?";
                    Expect(LangTokenType.Question);
                }
            }
            else if (isNeedDefaultValue)
            {
                // 默认参数：identifier:default_value
                var exprParser = expressionParserFactory();
                defaultValue = exprParser.ParseExpression();
            }
        }

        return new LangId(value, typeAnnotation, defaultValue, position);
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

    /// <summary>
    /// 解析 where 子句
    /// 语法：where T: IComparable | where T: IComparable & ICloneable | where T: IComparable, U: ICloneable
    /// </summary>
    /// <param name="genericParameters">泛型参数列表的引用，将在其中更新约束</param>
    private void ParseWhereClause(ref List<GenericParameter>? genericParameters)
    {
        if (genericParameters == null || genericParameters.Count == 0)
        {
            throw CreateSyntaxError("where 子句只能用于泛型函数");
        }

        Expect(LangTokenType.Where);

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
            if (genericParam == null)
            {
                throw CreateSyntaxError($"未定义的类型参数 '{typeParamName}'");
            }

            // 期望冒号
            if (CurrentToken.Type != LangTokenType.Colon)
            {
                throw CreateSyntaxError($"期望 ':', 但得到 {CurrentToken.Type}");
            }
            Expect(LangTokenType.Colon);

            // 解析约束列表
            var constraints = ParseGenericConstraints();

            // 更新泛型参数的约束
            // 如果已有约束，则合并
            var existingConstraints = genericParam.Constraints ?? new List<string>();
            existingConstraints.AddRange(constraints);

            // 创建新的 GenericParameter 对象替换旧的，保留原有的 IsNullable 属性
            var index = genericParameters.IndexOf(genericParam);
            genericParameters[index] = new GenericParameter(
                genericParam.Name,
                existingConstraints,
                genericParam.Position,
                genericParam.IsNullable  // 保留可空标记
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
}
