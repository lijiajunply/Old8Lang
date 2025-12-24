using Old8Lang.Compiler;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// 类型推断 Visitor - 替代原有的 OutputType() 方法
/// </summary>
public partial class TypeInferenceVisitor : IVisitor<Type?>
{
    private readonly LocalManager _local;

    public TypeInferenceVisitor(LocalManager local)
    {
        _local = local;
    }

    // Statement 访问方法将在后续实现
    // Expression 访问方法将在后续实现
    // Value 访问方法将在后续实现
}
