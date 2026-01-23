using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression;

/// <summary>
/// This表达式类，用于表示当前类实例的引用
/// </summary>
/// <param name="position">源代码位置信息，用于错误报告</param>
/// <remarks>
/// 该类用于处理this关键字，只能在类的实例方法中使用。
/// this关键字引用当前对象实例，允许访问实例的字段和方法。
/// </remarks>
public partial class ThisExpression(SourcePosition position = default) : LangExpression(position)
{
    /// <summary>
    /// 在当前上下文中解析this表达式
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <returns>当前实例的值</returns>
    /// <exception cref="NameError">当this不在类实例方法中使用时抛出</exception>
    public override LangValueType Run(VariateManager manager)
    {
        // 直接从变量储存器中获取名为"this"的变量
        if (manager is null)
        {
            throw new NameError(Position, "this");
        }

        var thisValue = manager.GetValue(new LangId("this"));
        if (thisValue is not null)
        {
            return thisValue;
        }

        // 如果没有找到，抛出NameError异常，因为this关键字只能在类的方法中使用
        throw new NameError(Position, "this");
    }

    /// <summary>
    /// 生成 IL 代码加载 this 引用
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="local">局部变量管理器</param>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 加载 this 指针（ldarg.0）
        // 在实例方法中，第一个参数（索引 0）总是 this 指针
        ilGenerator.Emit(OpCodes.Ldarg_0);
    }

    /// <summary>
    /// 返回 this 表达式的类型
    /// </summary>
    /// <param name="local">局部变量管理器</param>
    /// <returns>当前类的类型</returns>
    public override Type OutputType(LocalManager local)
    {
        if (local.InClassEnv is not null)
        {
            // 返回当前类的类型（可能是 TypeBuilder 或 Type）
            return local.InClassEnv;
        }

        // 如果不在类环境中，返回 object 类型
        return typeof(object);
    }

    public override string ToString()
    {
        return "this";
    }
}
