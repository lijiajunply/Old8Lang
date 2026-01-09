using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;
using Old8Lang.AST.Statement;
using OmniSharp.Extensions.LanguageServer.Protocol;
using Old8Lang.ModuleSystem.Resolution;
using Old8Lang.StandardLibrary;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 文档链接处理器 - 提供 import 语句的文件链接
/// </summary>
public class DocumentLinkHandler(DocumentManager documentManager) : IDocumentLinkHandler
{
    private readonly ModuleResolver _moduleResolver = new();
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

            // 使用 ModuleResolver 进行统一解析
            var result = _moduleResolver.ResolveModule(moduleName, currentPath);

            if (!result.IsSuccess)
            {
                return null;
            }

            // 根据不同的模块类型返回不同的路径
            switch (result.ModuleType)
            {
                case ModuleType.StandardLibrary:
                    // 标准库：尝试定位到源代码文件
                    return ResolveStandardLibrarySourcePath(moduleName);

                case ModuleType.ThirdPartyPackage:
                case ModuleType.LocalFile:
                case ModuleType.Submodule:
                    // 第三方包和本地文件：直接返回解析后的路径
                    return result.ResolvedPath;

                case ModuleType.NetworkModule:
                    // 网络模块：暂不支持链接（可以考虑打开浏览器）
                    return null;

                default:
                    return null;
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 解析标准库的源代码路径
    /// </summary>
    private string? ResolveStandardLibrarySourcePath(string libraryName)
    {
        // 获取标准库信息
        var libInfo = StandardLibraryRegistry.GetLibraryInfo(libraryName);
        if (libInfo == null)
        {
            return null;
        }

        try
        {
            // 尝试定位到标准库项目的源代码文件
            // 1. 从程序集名称推断项目位置
            var assemblyName = libInfo.AssemblyName;

            // 2. 获取运行时目录
            var baseDirectory = AppContext.BaseDirectory;

            // 3. 根据库名称映射到源代码文件
            // Old8LangLib 中的库映射到对应的 .cs 文件
            if (assemblyName == "Old8LangLib")
            {
                var sourceFileMap = new Dictionary<string, string>
                {
                    ["OS"] = "OS.cs",
                    ["File"] = "FileLib.cs",
                    ["Terminal"] = "Terminal.cs",
                    ["Time"] = "Time.cs",
                    ["Math"] = "MathLib.cs",
                    ["Crypto"] = "CryptoLib.cs",
                    ["Json"] = "JsonLib.cs",
                    ["Csv"] = "Csv.cs",
                    ["Vector"] = "VectorLib.cs",
                    ["Regex"] = "RegexLib.cs",
                    ["ColorfulTerminal"] = "ColorfulTerminal.cs",
                    ["TemplateEngine"] = "TemplateEngine.cs",
                    ["Image"] = "ImageLib.cs",
                    ["AssertLib"] = "AssertLib.cs",
                    ["CollectionLib"] = "CollectionLib.cs",
                    ["MockLib"] = "MockLib.cs",
                    ["TestRunner"] = "TestRunner.cs"
                };

                if (sourceFileMap.TryGetValue(libraryName, out var sourceFileName))
                {
                    // 尝试多种路径策略定位源文件
                    var searchPaths = new[]
                    {
                        // 策略 1: 从运行时目录向上查找项目结构
                        // 例如: /path/to/Old8Lang.App/bin/Debug/net10.0/ -> /path/to/Old8LangLib/
                        Path.Combine(baseDirectory, "..", "..", "..", "..", "Old8LangLib", sourceFileName),

                        // 策略 2: 从当前工作目录查找
                        Path.Combine(Directory.GetCurrentDirectory(), "..", "Old8LangLib", sourceFileName),

                        // 策略 3: 直接在 Old8LangLib 目录中查找（如果在解决方案根目录运行）
                        Path.Combine(Directory.GetCurrentDirectory(), "Old8LangLib", sourceFileName)
                    };

                    foreach (var path in searchPaths)
                    {
                        try
                        {
                            var fullPath = Path.GetFullPath(path);
                            if (File.Exists(fullPath))
                            {
                                return fullPath;
                            }
                        }
                        catch
                        {
                            // 忽略路径解析错误，继续下一个
                        }
                    }
                }
            }
            else if (assemblyName == "Old8Lang.NetLib")
            {
                // Net 库的特殊处理 - 包含多个类
                // 由于无法准确定位到具体类文件，返回项目目录
                var netLibPaths = new[]
                {
                    Path.Combine(baseDirectory, "..", "..", "..", "..", "Old8Lang.NetLib"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "Old8Lang.NetLib"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Old8Lang.NetLib")
                };

                foreach (var path in netLibPaths)
                {
                    try
                    {
                        var fullPath = Path.GetFullPath(path);
                        if (Directory.Exists(fullPath))
                        {
                            // 返回项目根目录或主入口文件
                            var mainFile = Path.Combine(fullPath, "SocketClient.cs");
                            if (File.Exists(mainFile))
                            {
                                return mainFile;
                            }
                        }
                    }
                    catch
                    {
                        // 忽略
                    }
                }
            }

            // 其他扩展库暂不支持源码链接
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
