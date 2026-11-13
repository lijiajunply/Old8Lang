namespace Old8Lang.AST;

/// <summary>
/// 标记接口：表示该 AST 节点是「语法字面量」节点，只携带源码中的字面数据与位置，
/// 与承担运算/成员访问/模块行为的运行时值对象相区分。
/// 阶段 2 拆分程序集时，字面量语法节点应能与执行后端分离；
/// 盘点与迁移建议见 Docs/architecture-migration/literal-runtime-boundary.md。
/// </summary>
public interface ILiteralSyntax
{
    /// <summary>
    /// 字面量的原始语法值（int/double/string/bool/char；Null/Void 为 null）。
    /// </summary>
    object? LiteralValue { get; }
}
