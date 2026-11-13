using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.ModuleSystem.StandardLibrary;
using Old8Lang.ModuleSystem.Core;
using Old8Lang.ModuleSystem.Resolution;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 导入项类，用于表示导入语句中的单个导入项
/// </summary>
/// <param name="name">导入项的原始名称</param>
/// <param name="alias">导入项的别名，默认与原始名称相同</param>
public class ImportItem(string name, string? alias = null)
{
    /// <summary>
    /// 导入项的原始名称
    /// </summary>
    public readonly string Name = name;

    /// <summary>
    /// 导入项的别名，默认与原始名称相同
    /// </summary>
    public readonly string Alias = alias ?? name;
}

/// <summary>
/// 导入语句类，用于处理Old8Lang中的import语句
/// </summary>
/// <param name="importString">导入的模块名称或路径</param>
/// <param name="position">源代码位置信息，用于错误报告</param>
/// <param name="importSpecifiers">导入指定符列表，用于命名导入</param>
/// <param name="fromClause">是否使用from子句，如import { ... } from "module"</param>
/// <param name="moduleAlias">模块别名，如import "module" as alias</param>
/// <param name="isLazy">是否为懒导入，只在首次使用时加载模块</param>
/// <param name="isSelective">是否为选择导入，如 from module import a, b, c</param>
/// <param name="isDynamic">是否为动态导入，模块名在运行时计算</param>
/// <param name="dynamicModuleExpression">动态模块表达式</param>
public partial class ImportStatement(
    string importString,
    SourcePosition position = default,
    List<ImportItem>? importSpecifiers = null,
    bool fromClause = false,
    string? moduleAlias = null,
    bool isLazy = false,
    bool isSelective = false,
    bool isDynamic = false,
    LangExpression? dynamicModuleExpression = null
) : OldStatement(position)
{
    // 静态模块系统服务实例
    internal static readonly ModuleSystemService ModuleService = new();

    /// <summary>
    /// 导入的模块名称或路径
    /// </summary>
    internal readonly string _importString = importString;

    /// <summary>
    /// 是否使用from子句
    /// </summary>
    internal readonly bool _fromClause = fromClause;

    /// <summary>
    /// 导入指定符列表，用于命名导入
    /// </summary>
    internal readonly List<ImportItem> _importSpecifiers = importSpecifiers ?? [];

    /// <summary>
    /// 模块别名，如import "module" as alias
    /// </summary>
    internal readonly string? _moduleAlias = moduleAlias;

    /// <summary>
    /// 是否为懒导入，只在首次使用时加载模块
    /// </summary>
    internal readonly bool _isLazy = isLazy;

    /// <summary>
    /// 是否为选择导入，如 from module import a, b, c
    /// </summary>
    internal readonly bool _isSelective = isSelective;

    /// <summary>
    /// 是否为动态导入，模块名在运行时计算
    /// </summary>
    internal readonly bool _isDynamic = isDynamic;

    /// <summary>
    /// 公共属性：是否为动态导入
    /// </summary>
    public bool IsDynamicImport => _isDynamic;

    /// <summary>
    /// 公共属性：导入的模块名称或路径
    /// </summary>
    public string GetImportString() => _importString;

    /// <summary>
    /// 公共属性：是否使用from子句
    /// </summary>
    public bool GetFromClause() => _fromClause;

    /// <summary>
    /// 公共属性：导入指定符列表
    /// </summary>
    public List<ImportItem> GetImportSpecifiers() => _importSpecifiers;

    /// <summary>
    /// 公共属性：模块别名
    /// </summary>
    public string? GetModuleAlias() => _moduleAlias;

    /// <summary>
    /// 动态模块表达式，用于在运行时计算模块名
    /// </summary>
    internal readonly LangExpression? _dynamicModuleExpression = dynamicModuleExpression;

    /// <summary>
    /// 获取指定索引处的语句（实现OldStatement接口）
    /// </summary>
    /// <param name="index">语句索引</param>
    /// <returns>返回当前语句本身，因为ImportStatement是单个语句</returns>
    public override OldStatement this[int index] => this;

    /// <summary>
    /// 获取语句数量（实现OldStatement接口）
    /// </summary>
    /// <returns>返回0，因为ImportStatement是单个语句</returns>
    public override int Count => 0;



    /// <summary>
    /// 将导入语句转换为字符串表示
    /// </summary>
    /// <returns>导入语句的字符串表示</returns>
    public override string ToString()
    {
        var lazyStr = _isLazy ? "lazy " : "";
        var dynamicStr = _isDynamic ? "dynamic " : "";

        if (_isSelective)
        {
            var specifiers = string.Join(", ",
                _importSpecifiers.Select(s => s.Name == s.Alias ? s.Name : $"{s.Name} as {s.Alias}"));
            var prefix = _isDynamic ? $"{dynamicStr}import {specifiers} from " : $"{lazyStr}import {specifiers} from ";
            return $"{prefix}{(_isDynamic ? _dynamicModuleExpression?.ToString() ?? _importString : _importString)}";
        }

        if (_importSpecifiers.Count > 0)
        {
            var specifiers = string.Join(", ",
                _importSpecifiers.Select(s => s.Name == s.Alias ? s.Name : $"{s.Name} as {s.Alias}"));
            var prefix = _isDynamic
                ? $"{dynamicStr}import {{ {specifiers} }} from "
                : $"{lazyStr}import {{ {specifiers} }} from ";
            return $"{prefix}{(_isDynamic ? _dynamicModuleExpression?.ToString() ?? _importString : _importString)}";
        }

        if (_moduleAlias is not null)
        {
            var prefix = _isDynamic ? $"{dynamicStr}import " : $"{lazyStr}import ";
            var modulePart = _isDynamic ? _dynamicModuleExpression?.ToString() ?? _importString : _importString;
            return $"{prefix}{modulePart} as {_moduleAlias}";
        }

        var basePrefix = _isDynamic ? $"{dynamicStr}import " : $"{lazyStr}import ";
        var baseModule = _isDynamic ? _dynamicModuleExpression?.ToString() ?? _importString : _importString;
        return $"{basePrefix}{baseModule}";
    }

}
