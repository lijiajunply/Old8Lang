using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 枚举定义语句，用于处理Old8Lang中的枚举声明
/// </summary>
/// <param name="enumName">枚举名称</param>
/// <param name="members">枚举成员列表</param>
/// <param name="position">源代码位置信息，用于错误报告</param>
public partial class EnumInit(
    string enumName,
    List<(string name, LangExpression? value)> members,
    SourcePosition position = default) : OldStatement(position)
{
    /// <summary>
    /// 枚举名称
    /// </summary>
    private readonly string enumName = enumName;

    /// <summary>
    /// 枚举成员列表（成员名和可选的显式值）
    /// </summary>
    private readonly List<(string name, LangExpression? value)> members = members;

    /// <summary>
    /// 公共属性，用于访问枚举名称
    /// </summary>
    public string EnumName => enumName;

    /// <summary>
    /// 公共属性，用于访问枚举成员
    /// </summary>
    public List<(string name, LangExpression? value)> Members => members;

    /// <summary>
    /// 在解释模式下执行枚举定义
    /// </summary>
    /// <param name="manager">变量管理器，用于管理枚举的声明和访问</param>
    /// <exception cref="DuplicateNameError">当枚举名已存在时抛出</exception>
    public override void Run(VariateManager manager)
    {
        // 检查枚举是否已存在
        var existingEnum = manager.GetAny(new LangId(enumName));
        if (existingEnum is not null)
        {
            throw new DuplicateNameError(this, enumName, "枚举");
        }

        // 计算枚举成员的实际值
        var enumValues = new Dictionary<string, int>();
        int currentValue = 0;

        foreach (var (memberName, memberValueExpr) in members)
        {
            if (memberValueExpr is not null)
            {
                // 有显式赋值，计算表达式的值
                var result = memberValueExpr.Run(manager);
                if (result is IntLangValue intValue)
                {
                    currentValue = intValue.Value;
                }
                else
                {
                    throw new SyntaxError(Position, $"枚举成员 '{memberName}' 的值必须是整数");
                }
            }

            // 检查成员名是否重复
            if (!enumValues.TryAdd(memberName, currentValue))
            {
                throw new DuplicateNameError(this, memberName, "枚举成员");
            }

            currentValue++; // 下一个未赋值的成员值自动递增
        }

        // 创建枚举模板并注册
        var enumTemplate = new EnumTemplate(enumName, enumValues, Position);
        manager.AddClassAndFunc(enumTemplate);

        // 注册枚举类型到类型系统
        try
        {
            TypeChecker.RegisterEnumType(enumName, enumValues.Keys.ToList());
        }
        catch
        {
            // 类型注册失败不影响枚举定义的正常执行
        }
    }

    /// <summary>
    /// 在编译模式下生成枚举的IL代码
    /// </summary>
    /// <param name="ilGenerator">IL指令生成器</param>
    /// <param name="local">局部变量管理器</param>
    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 检查枚举是否已经存在
        if (local.ClassVar.ContainsKey(enumName))
        {
            return;
        }

        // 确保有动态程序集和模块
        if (local.DynamicAssembly is null || local.DynamicModule is null)
        {
            var assemblyName = new AssemblyName("Old8LangDynamicAssembly");
            local.DynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            local.DynamicModule = local.DynamicAssembly.DefineDynamicModule("Old8LangDynamicModule");
        }

        var moduleBuilder = local.DynamicModule;

        // 定义枚举类型
        var enumBuilder = moduleBuilder.DefineEnum(
            enumName,
            TypeAttributes.Public,
            typeof(int)); // 枚举基础类型为 int

        // 计算枚举成员的实际值
        int currentValue = 0;
        var enumValues = new Dictionary<string, int>();

        foreach (var (memberName, memberValueExpr) in members)
        {
            if (memberValueExpr is not null)
            {
                // 有显式赋值，计算表达式的值
                // 注意：编译模式下，表达式必须是常量
                if (memberValueExpr is IntLangValue intValue)
                {
                    currentValue = intValue.Value;
                }
                else
                {
                    throw new SyntaxError(Position, $"枚举成员 '{memberName}' 的值必须是整数常量");
                }
            }

            // 定义枚举成员
            enumBuilder.DefineLiteral(memberName, currentValue);
            enumValues[memberName] = currentValue;
            currentValue++; // 下一个未赋值的成员值自动递增
        }

        // 创建枚举类型
        var createdEnumType = enumBuilder.CreateType();

        // 将枚举类型添加到 LocalManager
        local.ClassVar[enumName] = createdEnumType;
    }

    /// <summary>
    /// 获取指定索引处的语句
    /// </summary>
    /// <param name="index">语句索引</param>
    /// <returns>返回当前语句本身</returns>
    public override OldStatement this[int index] => this;

    /// <summary>
    /// 获取语句数量
    /// </summary>
    /// <returns>返回0，因为EnumInit是单个语句</returns>
    public override int Count => 0;

    /// <summary>
    /// 将枚举定义转换为字符串表示
    /// </summary>
    /// <returns>枚举定义的字符串表示</returns>
    public override string ToString()
    {
        var memberStrings = members.Select(m =>
        {
            if (m.value is not null)
            {
                return $"{m.name} = {m.value}";
            }

            return m.name;
        });
        return $"enum {enumName} {{ {string.Join(", ", memberStrings)} }}";
    }
}