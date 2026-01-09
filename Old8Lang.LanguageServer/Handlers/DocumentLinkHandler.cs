using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;
using Old8Lang.AST.Statement;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 文档链接处理器 - 提供 import 语句的文件链接
/// </summary>
public class DocumentLinkHandler(DocumentManager documentManager) : IDocumentLinkHandler
{
    public DocumentLinkRegistrationOptions GetRegistrationOptions(
        DocumentLinkCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new DocumentLinkRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang"),
            ResolveProvider = false
        };
    }

    public Task<DocumentLinkContainer?> Handle(DocumentLinkParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = documentManager.GetDocument(uri);

        if (document?.Ast == null)
        {
            return Task.FromResult<DocumentLinkContainer?>(new DocumentLinkContainer());
        }

        var links = new List<DocumentLink>();

        // 遍历所有 import 语句
        foreach (var statement in document.Ast.ImportStatements)
        {
            if (statement is ImportStatement importStatement)
            {
                var link = CreateDocumentLink(importStatement, uri);
                if (link != null)
                {
                    links.Add(link);
                }
            }
        }

        return Task.FromResult<DocumentLinkContainer?>(new DocumentLinkContainer(links));
    }

    /// <summary>
    /// 创建文档链接
    /// </summary>
    private DocumentLink? CreateDocumentLink(ImportStatement importStatement, string currentUri)
    {
        try
        {
            // 解析导入的模块路径
            var importPath = importStatement.ToString();

            // 提取 import 语句中的模块名
            // 支持多种导入语法:
            // import "module"
            // import { ... } from "module"
            // import ... from "module"
            var moduleName = ExtractModuleName(importPath);
            if (string.IsNullOrEmpty(moduleName))
            {
                return null;
            }

            // 解析模块文件路径
            var resolvedPath = ResolveModulePath(moduleName, currentUri);
            if (string.IsNullOrEmpty(resolvedPath))
            {
                return null;
            }

            // 查找模块名在源代码中的位置
            var range = FindImportRange(importStatement, moduleName);
            if (range == null)
            {
                return null;
            }

            // 创建文档链接
            return new DocumentLink
            {
                Range = range,
                Target = DocumentUri.From(resolvedPath),
                Tooltip = $"跳转到 {moduleName}"
            };
        }
        catch
        {
            // 如果解析失败，返回 null
            return null;
        }
    }

    /// <summary>
    /// 从 import 语句中提取模块名
    /// </summary>
    private string? ExtractModuleName(string importStatement)
    {
        // 匹配引号中的模块名
        // import "module"
        // import { ... } from "module"
        var match = System.Text.RegularExpressions.Regex.Match(
            importStatement,
            @"""([^""]+)""");

        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return null;
    }

    /// <summary>
    /// 解析模块路径
    /// </summary>
    private string? ResolveModulePath(string moduleName, string currentUri)
    {
        try
        {
            // 移除 file:// 前缀
            var currentPath = currentUri.Replace("file://", "");
            var currentDir = Path.GetDirectoryName(currentPath);

            if (string.IsNullOrEmpty(currentDir))
            {
                return null;
            }

            // 处理相对路径
            if (moduleName.StartsWith("./") || moduleName.StartsWith("../"))
            {
                var fullPath = Path.GetFullPath(Path.Combine(currentDir, moduleName));

                // 尝试 .old8 扩展名
                if (File.Exists(fullPath + ".old8"))
                {
                    return fullPath + ".old8";
                }

                // 尝试作为目录，查找 __init__.old8 或 index.old8
                if (Directory.Exists(fullPath))
                {
                    var initFile = Path.Combine(fullPath, "__init__.old8");
                    if (File.Exists(initFile))
                    {
                        return initFile;
                    }

                    var indexFile = Path.Combine(fullPath, "index.old8");
                    if (File.Exists(indexFile))
                    {
                        return indexFile;
                    }
                }

                return null;
            }

            // 处理绝对路径或模块名
            // 尝试在当前目录查找
            var localPath = Path.Combine(currentDir, moduleName + ".old8");
            if (File.Exists(localPath))
            {
                return localPath;
            }

            // TODO: 支持标准库和包查找
            // 这里可以扩展支持 Old8LangLib 等标准库的路径解析

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 查找 import 语句中模块名的范围
    /// </summary>
    private OmniSharp.Extensions.LanguageServer.Protocol.Models.Range? FindImportRange(
        ImportStatement importStatement,
        string moduleName)
    {
        // 从 import 语句的位置开始查找
        var line = importStatement.Position.Line;
        var column = importStatement.Position.Column;

        // import 语句的模块名通常在引号中
        // 这里简化处理，假设从语句开始后的某个位置
        // 实际位置需要通过 tokens 精确定位

        // 默认返回一个估算的范围
        return new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
        {
            Start = new Position(line, column),
            End = new Position(line, column + moduleName.Length + 2) // 加上引号
        };
    }
}
