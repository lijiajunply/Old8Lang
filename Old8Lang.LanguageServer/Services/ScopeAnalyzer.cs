using Old8Lang.AST.Statement;
using Old8Lang.LanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using SymbolInfo = Old8Lang.LanguageServer.Models.SymbolInfo;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 作用域分析器 - 基于词法作用域链分析指定光标位置可见的全部符号（支持遮蔽与精确块范围）。
/// </summary>
public class ScopeAnalyzer(
    BlockStatement ast,
    Position position,
    Dictionary<string, SymbolInfo> globalSymbolTable,
    string uri = "",
    SymbolScope? rootScope = null,
    WorkspaceIndex? workspaceIndex = null)
{
    /// <summary>
    /// 获取当前位置可见的所有符号（内层同名符号覆盖外层）。
    /// </summary>
    public List<SymbolInfo> GetVisibleSymbols()
    {
        var scope = rootScope;
        if (scope == null)
        {
            var builder = new SymbolTableBuilder(uri);
            scope = builder.BuildWithScope(ast);
        }

        var visible = scope.GetVisibleSymbols(position.Line, position.Character);
        var seen = new HashSet<string>(visible.Select(s => s.Name), StringComparer.Ordinal);

        // 补充仅由外部注入到 globalSymbolTable 的符号（例如测试场景）
        foreach (var symbol in globalSymbolTable.Values)
        {
            if (seen.Add(symbol.Name))
            {
                visible.Add(symbol);
            }
        }

        // 补充工作区其他文件的顶层符号
        if (workspaceIndex != null)
        {
            foreach (var file in workspaceIndex.GetFiles())
            {
                if (file.Uri == uri) continue;
                foreach (var symbol in file.Symbols.Values)
                {
                    if (seen.Add(symbol.Name))
                    {
                        visible.Add(symbol);
                    }
                }
            }
        }

        return visible;
    }
}
