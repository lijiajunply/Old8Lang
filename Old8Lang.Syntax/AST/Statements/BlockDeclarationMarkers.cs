namespace Old8Lang.AST.Statement;

/// <summary>
/// 块级声明语句：在 <see cref="BlockStatement"/> 中进入导入/声明列表，
/// 先于普通语句执行或编译。
/// </summary>
/// <remarks>
/// 该标记接口让语法层的块节点无需引用各声明节点的具体类型，
/// 从而可在不依赖执行后端的情况下完成语句分类。
/// </remarks>
public interface IBlockDeclaration;

/// <summary>
/// 块级导入语句：静态导入进入导入列表，动态导入保留在普通语句流中按顺序执行。
/// </summary>
public interface IBlockImportDeclaration
{
    /// <summary>是否为动态导入（模块名在运行时计算）。</summary>
    bool IsDynamicImport { get; }
}
