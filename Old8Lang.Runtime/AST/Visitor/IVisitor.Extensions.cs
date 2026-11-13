using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// Visitor 接口的手工扩展：这些运行时值类继承自 ImportInfo，
/// 生成器按基类字面名筛选时不会纳入，因此在这里显式声明访问方法。
/// </summary>
public partial interface IVisitor<out TResult>
{
    /// <summary>访问异步函数值节点</summary>
    TResult VisitAsyncFuncLangValue(AsyncFuncLangValue node);

    /// <summary>访问函数值节点</summary>
    TResult VisitFuncLangValue(FuncLangValue node);

    /// <summary>访问原生对象值节点</summary>
    TResult VisitNativeAnyLangValue(NativeAnyLangValue node);

    /// <summary>访问 super 代理节点</summary>
    TResult VisitSuperProxy(SuperProxy node);
}
