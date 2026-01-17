using Old8Lang.AST;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Expression.Value;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using SymbolInfo = Old8Lang.LanguageServer.Models.SymbolInfo;
using SymbolKind = Old8Lang.LanguageServer.Models.SymbolKind;
using SourceLocation = Old8Lang.LanguageServer.Models.SourceLocation;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 作用域分析器 - 根据光标位置分析可见的符号
/// </summary>
public class ScopeAnalyzer
{
    private readonly BlockStatement _ast;
    private readonly Position _position;
    private readonly Dictionary<string, SymbolInfo> _globalSymbolTable;
    private readonly List<SymbolInfo> _visibleSymbols = new();
    private readonly string _uri;

    public ScopeAnalyzer(BlockStatement ast, Position position, Dictionary<string, SymbolInfo> globalSymbolTable, string uri = "")
    {
        _ast = ast;
        _position = position;
        _globalSymbolTable = globalSymbolTable;
        _uri = uri;
    }

    /// <summary>
    /// 获取当前位置可见的所有符号
    /// </summary>
    public List<SymbolInfo> GetVisibleSymbols()
    {
        _visibleSymbols.Clear();

        // 1. 添加全局符号（函数、类、全局变量）
        foreach (var symbol in _globalSymbolTable.Values)
        {
            _visibleSymbols.Add(symbol);
        }

        // 2. 查找包含当前位置的作用域并添加局部符号
        FindLocalSymbols(_ast);

        return _visibleSymbols;
    }

    /// <summary>
    /// 递归查找包含当前位置的作用域中的局部符号
    /// </summary>
    private void FindLocalSymbols(IOldLangTree node)
    {
        if (node == null) return;

        switch (node)
        {
            case BlockStatement block:
                FindLocalSymbolsInBlock(block);
                break;

            case FuncInit funcInit:
                FindLocalSymbolsInFunction(funcInit);
                break;

            case AsyncFuncInit asyncFuncInit:
                FindLocalSymbolsInAsyncFunction(asyncFuncInit);
                break;

            case ClassInit classInit:
                FindLocalSymbolsInClass(classInit);
                break;
        }
    }

    /// <summary>
    /// 在块语句中查找局部符号
    /// </summary>
    private void FindLocalSymbolsInBlock(BlockStatement block)
    {
        // 遍历所有语句
        foreach (var statement in block.ImportStatements.Concat(block.OtherStatements))
        {
            // 检查语句是否在光标位置之前或之上
            var statementLine = statement.Position.Line;
            var cursorLine = _position.Line + 1; // LSP 从 0 开始，AST 从 1 开始

            // 如果语句在光标之后，不可见
            if (statementLine > cursorLine)
            {
                continue;
            }

            // 如果是变量声明且在光标之前，添加到可见符号
            if (statement is SetStatement setStatement && setStatement.Id != null)
            {
                var varName = setStatement.Id.IdName;
                var varType = setStatement.Id.AssumptionType ?? "var";

                var varSymbol = new SymbolInfo
                {
                    Name = varName,
                    Kind = SymbolKind.Variable,
                    Type = varType,
                    Location = new SourceLocation
                    {
                        Uri = _uri,
                        Line = setStatement.Position.Line - 1,
                        Column = setStatement.Position.Column - 1,
                        EndLine = setStatement.Position.Line - 1,
                        EndColumn = setStatement.Position.Column - 1 + varName.Length
                    }
                };

                // 检查是否已经存在同名符号（局部变量会覆盖全局变量）
                var existingIndex = _visibleSymbols.FindIndex(s => s.Name == varName);
                if (existingIndex >= 0)
                {
                    _visibleSymbols[existingIndex] = varSymbol;
                }
                else
                {
                    _visibleSymbols.Add(varSymbol);
                }
            }

            // 递归检查嵌套作用域（如函数、类等）
            FindLocalSymbols(statement);
        }
    }

    /// <summary>
    /// 在函数中查找局部符号
    /// </summary>
    private void FindLocalSymbolsInFunction(FuncInit funcInit)
    {
        var funcValue = funcInit.FuncLangValue;

        // 粗略检查：如果光标行号大于函数开始行号，可能在函数内
        var cursorLine = _position.Line + 1;
        var funcStartLine = funcValue.Position.Line;

        if (cursorLine < funcStartLine)
        {
            return; // 光标在函数之前
        }

        // 添加函数参数到可见符号
        if (funcValue.Ids != null)
        {
            foreach (var param in funcValue.Ids)
            {
                var paramSymbol = new SymbolInfo
                {
                    Name = param.IdName,
                    Kind = SymbolKind.Parameter,
                    Type = param.AssumptionType ?? "var",
                    Location = new SourceLocation
                    {
                        Uri = _uri,
                        Line = param.Position.Line - 1,
                        Column = param.Position.Column - 1,
                        EndLine = param.Position.Line - 1,
                        EndColumn = param.Position.Column - 1 + param.IdName.Length
                    }
                };
                _visibleSymbols.Add(paramSymbol);
            }
        }

        // 递归处理函数体
        if (funcValue.BlockStatement != null)
        {
            FindLocalSymbols(funcValue.BlockStatement);
        }
    }

    /// <summary>
    /// 在异步函数中查找局部符号
    /// </summary>
    private void FindLocalSymbolsInAsyncFunction(AsyncFuncInit asyncFuncInit)
    {
        var funcValue = asyncFuncInit.AsyncFuncValue;

        // 粗略检查：如果光标行号大于函数开始行号，可能在函数内
        var cursorLine = _position.Line + 1;
        var funcStartLine = funcValue.Position.Line;

        if (cursorLine < funcStartLine)
        {
            return; // 光标在函数之前
        }

        // 添加函数参数到可见符号
        if (funcValue.Ids != null)
        {
            foreach (var param in funcValue.Ids)
            {
                var paramSymbol = new SymbolInfo
                {
                    Name = param.IdName,
                    Kind = SymbolKind.Parameter,
                    Type = param.AssumptionType ?? "var",
                    Location = new SourceLocation
                    {
                        Uri = _uri,
                        Line = param.Position.Line - 1,
                        Column = param.Position.Column - 1,
                        EndLine = param.Position.Line - 1,
                        EndColumn = param.Position.Column - 1 + param.IdName.Length
                    }
                };
                _visibleSymbols.Add(paramSymbol);
            }
        }

        // 异步函数的 BlockStatement 可能无法直接访问，这里暂时跳过
        // 未来可以通过反射或其他方式访问
    }

    /// <summary>
    /// 在类中查找局部符号
    /// </summary>
    private void FindLocalSymbolsInClass(ClassInit classInit)
    {
        var typeTemplate = classInit.AnyLangValue;
        var cursorLine = _position.Line + 1;

        // 检查是否在类的某个方法内
        bool isInsideInstanceMethod = false;
        bool isInsideStaticMethod = false;

        // 检查实例方法
        foreach (var (_, memberExpr) in typeTemplate.Variates)
        {
            if (memberExpr is FuncLangValue funcValue && funcValue.BlockStatement != null)
            {
                var methodStartLine = funcValue.Position.Line;

                // 如果光标在方法范围内
                if (cursorLine >= methodStartLine)
                {
                    isInsideInstanceMethod = true;

                    // 添加方法参数
                    if (funcValue.Ids != null)
                    {
                        foreach (var param in funcValue.Ids)
                        {
                            var paramSymbol = new SymbolInfo
                            {
                                Name = param.IdName,
                                Kind = SymbolKind.Parameter,
                                Type = param.AssumptionType ?? "var",
                                Location = new SourceLocation
                                {
                                    Uri = _uri,
                                    Line = param.Position.Line - 1,
                                    Column = param.Position.Column - 1,
                                    EndLine = param.Position.Line - 1,
                                    EndColumn = param.Position.Column - 1 + param.IdName.Length
                                }
                            };
                            _visibleSymbols.Add(paramSymbol);
                        }
                    }

                    // 递归处理方法体
                    FindLocalSymbols(funcValue.BlockStatement);
                }
            }
        }

        // 检查静态方法
        foreach (var (_, memberExpr) in typeTemplate.StaticVariates)
        {
            if (memberExpr is FuncLangValue funcValue && funcValue.BlockStatement != null)
            {
                var methodStartLine = funcValue.Position.Line;

                // 如果光标在方法范围内
                if (cursorLine >= methodStartLine)
                {
                    isInsideStaticMethod = true;

                    // 添加方法参数
                    if (funcValue.Ids != null)
                    {
                        foreach (var param in funcValue.Ids)
                        {
                            var paramSymbol = new SymbolInfo
                            {
                                Name = param.IdName,
                                Kind = SymbolKind.Parameter,
                                Type = param.AssumptionType ?? "var",
                                Location = new SourceLocation
                                {
                                    Uri = _uri,
                                    Line = param.Position.Line - 1,
                                    Column = param.Position.Column - 1,
                                    EndLine = param.Position.Line - 1,
                                    EndColumn = param.Position.Column - 1 + param.IdName.Length
                                }
                            };
                            _visibleSymbols.Add(paramSymbol);
                        }
                    }

                    // 递归处理方法体
                    FindLocalSymbols(funcValue.BlockStatement);
                }
            }
        }

        // 如果在实例方法内，添加 this 关键字
        if (isInsideInstanceMethod)
        {
            var thisSymbol = new SymbolInfo
            {
                Name = "this",
                Kind = SymbolKind.Keyword,
                Type = typeTemplate.ClassName,
                Location = new SourceLocation
                {
                    Uri = _uri,
                    Line = classInit.Position.Line - 1,
                    Column = classInit.Position.Column - 1,
                    EndLine = classInit.Position.Line - 1,
                    EndColumn = classInit.Position.Column - 1 + 4 // "this" length
                }
            };
            _visibleSymbols.Add(thisSymbol);
        }

        // 如果在任何方法内（实例或静态），添加类字段
        if (isInsideInstanceMethod || isInsideStaticMethod)
        {
            // 添加实例字段（只在实例方法内可见）
            if (isInsideInstanceMethod)
            {
                foreach (var (memberId, fieldExpr) in typeTemplate.Variates)
                {
                    // 跳过方法（只添加字段）
                    if (fieldExpr is FuncLangValue)
                    {
                        continue;
                    }

                    var fieldSymbol = new SymbolInfo
                    {
                        Name = memberId.IdName,
                        Kind = SymbolKind.Field,
                        Type = "var", // 可以通过分析 fieldExpr 推断类型
                        Location = new SourceLocation
                        {
                            Uri = _uri,
                            Line = classInit.Position.Line - 1,
                            Column = classInit.Position.Column - 1,
                            EndLine = classInit.Position.Line - 1,
                            EndColumn = classInit.Position.Column - 1 + memberId.IdName.Length
                        }
                    };
                    _visibleSymbols.Add(fieldSymbol);
                }
            }

            // 添加静态字段（在实例和静态方法内都可见）
            foreach (var (memberId, fieldExpr) in typeTemplate.StaticVariates)
            {
                // 跳过方法（只添加字段）
                if (fieldExpr is FuncLangValue)
                {
                    continue;
                }

                var fieldSymbol = new SymbolInfo
                {
                    Name = memberId.IdName,
                    Kind = SymbolKind.Field,
                    Type = "var",
                    Location = new SourceLocation
                    {
                        Uri = _uri,
                        Line = classInit.Position.Line - 1,
                        Column = classInit.Position.Column - 1,
                        EndLine = classInit.Position.Line - 1,
                        EndColumn = classInit.Position.Column - 1 + memberId.IdName.Length
                    }
                };
                _visibleSymbols.Add(fieldSymbol);
            }
        }
    }
}
