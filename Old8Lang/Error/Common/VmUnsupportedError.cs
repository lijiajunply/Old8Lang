using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 虚拟机模式不支持的特性
/// </summary>
/// <remarks>
/// 用于替换字节码访问者中的静默空实现。静默返回 null 会让表达式不产出任何字节码，
/// 栈因此失衡，最终在无关指令处抛出 "Stack empty" 之类的错误，报错位置和原因都与真实问题无关。
/// 这里显式报出真实原因，让用户直接看到是哪个特性在虚拟机模式下不可用。
/// </remarks>
public class VmUnsupportedError : RuntimeError
{
    /// <summary>
    /// 虚拟机不支持特性的错误代码
    /// </summary>
    public new const string ErrorCode = "VM_UNSUPPORTED_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="feature">特性名称</param>
    public VmUnsupportedError(IOldLangTree node, string feature)
        : base(
            node,
            ErrorCode,
            $"虚拟机模式暂不支持 {feature}",
            $"请在解释模式(-f)下使用 {feature}，或改用等效的替代写法")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="feature">特性名称</param>
    public VmUnsupportedError(SourcePosition position, string feature)
        : base(
            position,
            ErrorCode,
            $"虚拟机模式暂不支持 {feature}",
            $"请在解释模式(-f)下使用 {feature}，或改用等效的替代写法")
    {
    }
}
