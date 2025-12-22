// Scanner/AstNodeScanner.cs
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Old8Lang.CodeGen.Scanner;

/// <summary>
/// AST 节点扫描器 - 扫描 AST 目录并提取节点信息
/// </summary>
public class AstNodeScanner
{
    private readonly string _scanDirectory;
    private readonly HashSet<string> _excludeClasses;
    private readonly List<string> _excludePatterns;

    public AstNodeScanner(
        string scanDirectory,
        HashSet<string> excludeClasses,
        List<string> excludePatterns)
    {
        _scanDirectory = scanDirectory;
        _excludeClasses = excludeClasses;
        _excludePatterns = excludePatterns;
    }

    /// <summary>
    /// 扫描所有 AST 节点
    /// </summary>
    public List<AstNodeInfo> ScanNodes()
    {
        var nodes = new List<AstNodeInfo>();

        if (!Directory.Exists(_scanDirectory))
        {
            throw new DirectoryNotFoundException($"扫描目录不存在: {_scanDirectory}");
        }

        var csFiles = Directory.GetFiles(_scanDirectory, "*.cs", SearchOption.AllDirectories);
        Console.WriteLine($"[INFO] 找到 {csFiles.Length} 个 .cs 文件");

        foreach (var file in csFiles)
        {
            // 检查是否匹配排除模式
            if (IsExcluded(file))
            {
                Console.WriteLine($"[DEBUG] 排除文件: {file}");
                continue;
            }

            try
            {
                var sourceCode = File.ReadAllText(file);
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = syntaxTree.GetRoot();

                var classDeclarations = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>();

                foreach (var classDecl in classDeclarations)
                {
                    if (ShouldIncludeNode(classDecl, file))
                    {
                        var nodeInfo = ExtractNodeInfo(classDecl, file);
                        if (nodeInfo != null)
                        {
                            nodes.Add(nodeInfo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 解析文件失败 {file}: {ex.Message}");
            }
        }

        return nodes;
    }

    /// <summary>
    /// 检查文件是否应该被排除
    /// </summary>
    private bool IsExcluded(string filePath)
    {
        foreach (var pattern in _excludePatterns)
        {
            // 简单的通配符匹配
            var regexPattern = pattern
                .Replace("**", ".*")
                .Replace("*", "[^/]*")
                .Replace("/", Path.DirectorySeparatorChar.ToString());

            if (System.Text.RegularExpressions.Regex.IsMatch(filePath, regexPattern))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断节点是否应该被包含
    /// </summary>
    private bool ShouldIncludeNode(ClassDeclarationSyntax classDecl, string filePath)
    {
        // 排除抽象类
        if (classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)))
        {
            return false;
        }

        // 排除特定类
        var className = classDecl.Identifier.Text;
        if (_excludeClasses.Contains(className))
        {
            return false;
        }

        // 必须继承自 OldStatement 或 LangExpression
        var baseList = classDecl.BaseList?.Types;
        if (baseList == null || !baseList.Value.Any())
        {
            return false;
        }

        var hasValidBase = false;
        foreach (var t in baseList.Value)
        {
            var typeName = t.Type.ToString();
            if (typeName.Contains("OldStatement") ||
                typeName.Contains("LangExpression") ||
                typeName.Contains("LangValueType"))
            {
                hasValidBase = true;
                break;
            }
        }

        return hasValidBase;
    }

    /// <summary>
    /// 提取节点信息
    /// </summary>
    private AstNodeInfo? ExtractNodeInfo(ClassDeclarationSyntax classDecl, string filePath)
    {
        var className = classDecl.Identifier.Text;
        var namespaceName = GetNamespace(classDecl);
        var category = DetermineCategory(classDecl, namespaceName, filePath);

        return new AstNodeInfo
        {
            ClassName = className,
            Namespace = namespaceName,
            FullTypeName = $"{namespaceName}.{className}",
            Category = category,
            FilePath = filePath
        };
    }

    /// <summary>
    /// 获取命名空间
    /// </summary>
    private string GetNamespace(SyntaxNode node)
    {
        // 支持文件作用域命名空间和传统命名空间
        var fileScopedNamespace = node.Ancestors()
            .OfType<FileScopedNamespaceDeclarationSyntax>()
            .FirstOrDefault();

        if (fileScopedNamespace != null)
        {
            return fileScopedNamespace.Name.ToString();
        }

        var namespaceDecl = node.Ancestors()
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault();

        return namespaceDecl?.Name.ToString() ?? "Old8Lang.AST";
    }

    /// <summary>
    /// 确定节点类别
    /// </summary>
    private AstNodeCategory DetermineCategory(
        ClassDeclarationSyntax classDecl,
        string namespaceName,
        string filePath)
    {
        // 根据命名空间判断
        if (namespaceName.Contains(".Statement"))
        {
            return AstNodeCategory.Statement;
        }

        if (namespaceName.Contains(".Value") || filePath.Contains("/Value/"))
        {
            return AstNodeCategory.Value;
        }

        // 根据基类判断
        var baseList = classDecl.BaseList?.Types;
        if (baseList != null)
        {
            foreach (var baseType in baseList)
            {
                var typeName = baseType.Type.ToString();
                if (typeName.Contains("LangValueType"))
                {
                    return AstNodeCategory.Value;
                }
            }
        }

        return AstNodeCategory.Expression;
    }
}
