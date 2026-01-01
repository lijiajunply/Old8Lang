using Old8Lang.AST;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Expression;
using Old8Lang.LanguageServer.Models;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 符号表构建器 - 遍历AST构建符号表
/// </summary>
public class SymbolTableBuilder(string uri)
{
    private readonly Dictionary<string, SymbolInfo> _symbolTable = new();

    /// <summary>
    /// 构建符号表
    /// </summary>
    public Dictionary<string, SymbolInfo> Build(BlockStatement ast)
    {
        _symbolTable.Clear();
        VisitBlockStatement(ast);
        return _symbolTable;
    }

    /// <summary>
    /// 访问块语句
    /// </summary>
    private void VisitBlockStatement(BlockStatement block)
    {
        // 访问 import 语句（包括函数、类声明）
        foreach (var statement in block.ImportStatements)
        {
            VisitStatement(statement);
        }

        // 访问其他语句（包括变量声明）
        foreach (var statement in block.OtherStatements)
        {
            VisitStatement(statement);
        }
    }

    /// <summary>
    /// 访问单个语句
    /// </summary>
    private void VisitStatement(IOldLangTree statement)
    {
        switch (statement)
        {
            case FuncInit funcInit:
                VisitFunction(funcInit);
                break;
            case AsyncFuncInit asyncFuncInit:
                VisitAsyncFunction(asyncFuncInit);
                break;
            case ClassInit classInit:
                VisitClass(classInit);
                break;
            case SetStatement setStatement:
                VisitVariable(setStatement);
                break;
        }
    }

    /// <summary>
    /// 访问函数声明
    /// </summary>
    private void VisitFunction(FuncInit funcInit)
    {
        var funcValue = funcInit.FuncLangValue;
        if (funcValue.Id == null) return; // 跳过Lambda

        var funcName = funcValue.Id.IdName;
        var location = new SourceLocation
        {
            Uri = uri,
            Line = funcValue.Position.Line,
            Column = funcValue.Position.Column,
            EndLine = funcValue.Position.Line,
            EndColumn = funcValue.Position.Column
        };

        // 构建函数签名
        var paramList = funcValue.Ids != null
            ? string.Join(", ",
                funcValue.Ids.Select(p =>
                    $"{p.IdName}{(string.IsNullOrEmpty(p.AssumptionType) ? "" : ":" + p.AssumptionType)}"))
            : "";
        var returnType = funcValue.Id.AssumptionType ?? "void";
        var funcSignature = $"func {funcName}({paramList}) -> {returnType}";

        // 提取文档注释
        string? documentation = null;
        if (funcValue.DocComment != null)
        {
            documentation = FormatDocComment(funcValue.DocComment);
        }

        _symbolTable[funcName] = new SymbolInfo
        {
            Name = funcName,
            Kind = SymbolKind.Function,
            Type = funcSignature,
            Location = location,
            Documentation = documentation
        };

        // 访问函数体中的局部变量
        VisitBlockStatement(funcValue.BlockStatement);
    }

    /// <summary>
    /// 访问异步函数声明
    /// </summary>
    private void VisitAsyncFunction(AsyncFuncInit asyncFuncInit)
    {
        var funcValue = asyncFuncInit.AsyncFuncValue;
        if (funcValue.Id == null) return;

        var funcName = funcValue.Id.IdName;
        var location = new SourceLocation
        {
            Uri = uri,
            Line = funcValue.Position.Line,
            Column = funcValue.Position.Column,
            EndLine = funcValue.Position.Line,
            EndColumn = funcValue.Position.Column
        };

        var paramList = funcValue.Ids != null
            ? string.Join(", ",
                funcValue.Ids.Select(p =>
                    $"{p.IdName}{(string.IsNullOrEmpty(p.AssumptionType) ? "" : ":" + p.AssumptionType)}"))
            : "";
        var returnType = funcValue.Id.AssumptionType ?? "void";
        var funcSignature = $"async func {funcName}({paramList}) -> {returnType}";

        // 提取文档注释
        string? documentation = null;
        if (funcValue.DocComment != null)
        {
            documentation = FormatDocComment(funcValue.DocComment);
        }

        _symbolTable[funcName] = new SymbolInfo
        {
            Name = funcName,
            Kind = SymbolKind.Function,
            Type = funcSignature,
            Location = location,
            Documentation = documentation
        };

        // 注意：异步函数的 BlockStatement 是 internal 的，且函数体内的局部变量不应该被添加到全局符号表
    }

    /// <summary>
    /// 访问类声明
    /// </summary>
    private void VisitClass(ClassInit classInit)
    {
        var typeTemplate = classInit.AnyLangValue;
        var className = typeTemplate.ClassName;
        var location = new SourceLocation
        {
            Uri = uri,
            Line = typeTemplate.Position.Line,
            Column = typeTemplate.Position.Column,
            EndLine = typeTemplate.Position.Line,
            EndColumn = typeTemplate.Position.Column
        };

        // 提取类文档注释
        string? documentation = null;
        if (typeTemplate.DocComment != null)
        {
            documentation = FormatDocComment(typeTemplate.DocComment);
        }

        _symbolTable[className] = new SymbolInfo
        {
            Name = className,
            Kind = SymbolKind.Class,
            Type = $"class {className}",
            Location = location,
            Documentation = documentation
        };

        // TODO: 访问类的成员（方法、属性）
    }

    /// <summary>
    /// 访问变量声明
    /// </summary>
    private void VisitVariable(SetStatement setStatement)
    {
        if (setStatement.Id == null) return;

        var varName = setStatement.Id.IdName;
        var location = new SourceLocation
        {
            Uri = uri,
            Line = setStatement.Position.Line,
            Column = setStatement.Position.Column,
            EndLine = setStatement.Position.Line,
            EndColumn = setStatement.Position.Column
        };

        var varType = setStatement.Id.AssumptionType ?? "var";

        _symbolTable[varName] = new SymbolInfo
        {
            Name = varName,
            Kind = SymbolKind.Variable,
            Type = varType,
            Location = location
        };
    }

    /// <summary>
    /// 格式化文档注释为Markdown
    /// </summary>
    private string FormatDocComment(DocCommentInfo docComment)
    {
        var lines = new List<string>();

        // 摘要
        if (!string.IsNullOrEmpty(docComment.Summary))
        {
            lines.Add(docComment.Summary);
            lines.Add("");
        }

        // 参数
        if (docComment.Parameters.Count > 0)
        {
            lines.Add("**参数:**");
            foreach (var param in docComment.Parameters)
            {
                var paramLine = $"- `{param.Name}`";
                if (!string.IsNullOrEmpty(param.Type))
                {
                    paramLine += $" *({param.Type})*";
                }

                if (!string.IsNullOrEmpty(param.Description))
                {
                    paramLine += $": {param.Description}";
                }

                lines.Add(paramLine);
            }

            lines.Add("");
        }

        // 返回值
        if (docComment.Returns != null)
        {
            var returnLine = "**返回:**";
            if (!string.IsNullOrEmpty(docComment.Returns.Type))
            {
                returnLine += $" *{docComment.Returns.Type}*";
            }

            if (!string.IsNullOrEmpty(docComment.Returns.Description))
            {
                returnLine += $" - {docComment.Returns.Description}";
            }

            lines.Add(returnLine);
            lines.Add("");
        }

        // 异常
        if (docComment.Throws.Count > 0)
        {
            lines.Add("**异常:**");
            foreach (var throwInfo in docComment.Throws)
            {
                var throwLine = $"- `{throwInfo.Type}`";
                if (!string.IsNullOrEmpty(throwInfo.Description))
                {
                    throwLine += $": {throwInfo.Description}";
                }

                lines.Add(throwLine);
            }

            lines.Add("");
        }

        // 示例
        if (docComment.Examples.Count > 0)
        {
            lines.Add("**示例:**");
            foreach (var example in docComment.Examples)
            {
                lines.Add("```old8lang");
                lines.Add(example);
                lines.Add("```");
            }
        }

        return string.Join("\n", lines);
    }
}