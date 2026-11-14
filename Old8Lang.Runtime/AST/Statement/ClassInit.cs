using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;
using LangObject = Old8Lang.Runtime.LangObject;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 类定义语句，用于处理Old8Lang中的类声明
/// </summary>
/// <param name="anyValue">类模板信息，包含类名、父类、成员变量和方法等</param>
/// <param name="position">源代码位置信息，用于错误报告</param>
public partial class ClassInit(TypeTemplate anyValue, SourcePosition position = default) : OldStatement(position), IBlockDeclaration
{

    /// <summary>
    /// 类模板信息，包含类的完整定义
    /// </summary>
    internal readonly TypeTemplate _anyValue = anyValue;

    /// <summary>
    /// 公共属性，用于访问类模板信息
    /// </summary>
    public TypeTemplate AnyValue => _anyValue;

    /// <summary>
    /// 注册类类型到类型假注系统
    /// </summary>
    internal void RegisterTypeToTypeSystem()
    {
        try
        {
            if (_anyValue.IsInterface)
            {
                // 注册接口类型
                // 接口的父接口存储在 ImplementsNames 中（见 ClassParser.ParseInterfaceDeclaration）
                List<string> parentInterfaceNames = _anyValue.ImplementsNames;
                TypeChecker.RegisterInterfaceType(_anyValue.ClassName, parentInterfaceNames);
            }
            else
            {
                // 注册类类型
                // 获取父类名称
                string? baseClassName = _anyValue.ParentClassName;

                // 获取实现的接口列表
                List<string> implementsNames = _anyValue.ImplementsNames;

                // 注册类类型到类型假注系统
                TypeChecker.RegisterClassType(_anyValue.ClassName, baseClassName, implementsNames);
            }
        }
        catch
        {
            // 如果类型注册失败，不影响类定义的正常执行
            // 这是为了向后兼容
        }
    }


    internal static readonly HashSet<string> OperatorNames =
    [
        "_add", "_sub", "_mul", "_div", "_mod", "_pow",
        "_eq", "_lt", "_gt", "_le", "_ge",
        "_getitem", "_setitem"
    ];


    
    





    /// <summary>
    /// 获取指定索引处的语句（实现OldStatement接口）
    /// </summary>
    /// <param name="index">语句索引</param>
    /// <returns>返回当前语句本身，因为ClassInit是单个语句</returns>
    public override OldStatement this[int index] => this;

    /// <summary>
    /// 获取语句数量（实现OldStatement接口）
    /// </summary>
    /// <returns>返回0，因为ClassInit是单个语句</returns>
    public override int Count => 0;

    /// <summary>
    /// 将类定义转换为字符串表示
    /// </summary>
    /// <returns>类定义的字符串表示，包含类名、字段和方法</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();

        // 根据类型确定前缀
        var typePrefix = _anyValue.IsInterface ? "interface" :
                        _anyValue.IsMixin ? "mixin" :
                        _anyValue.IsAbstract ? "abstract class" : "class";

        sb.AppendLine($"{typePrefix} {_anyValue.ClassName} {{");
        foreach (var variate in _anyValue.Variates)
        {
            if (variate.Value is FuncLangValue funcValue)
            {
                // 方法定义
                var paramList = funcValue.Ids is not null ? string.Join(", ", funcValue.Ids) : string.Empty;
                sb.AppendLine($"    func {funcValue.Id}({paramList}) {{");
                sb.AppendLine($"        {funcValue.BlockStatement}");
                sb.AppendLine("    }");
            }
            else
            {
                // 字段定义
                sb.AppendLine($"    {variate.Key} <- {variate.Value}");
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }
}
