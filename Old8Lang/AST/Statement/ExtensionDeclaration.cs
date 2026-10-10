using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 扩展方法声明语句，用于为现有类型添加扩展方法
/// </summary>
/// <param name="targetTypeName">目标类型名称</param>
/// <param name="extensionMethods">扩展方法列表</param>
/// <param name="position">源代码位置信息</param>
public class ExtensionDeclaration(
    string targetTypeName,
    List<FuncLangValue> extensionMethods,
    SourcePosition position = default) : OldStatement(position)
{
    /// <summary>
    /// 目标类型名称（被扩展的类型）
    /// </summary>
    public string TargetTypeName { get; } = targetTypeName;

    /// <summary>
    /// 扩展方法列表
    /// </summary>
    public List<FuncLangValue> ExtensionMethods { get; } = extensionMethods;

    /// <summary>
    /// 在解释模式下执行扩展方法声明
    /// </summary>
    /// <param name="manager">变量管理器</param>
    public override void Run(VariateManager manager)
    {
        // 确保实例方法系统已初始化
        InstanceMethodInitializer.EnsureInitialized();

        // 获取目标类型
        var targetType = ResolveTargetType(TargetTypeName);
        if (targetType == null)
        {
            throw new InvalidOperationError(this, $"无法找到类型 '{TargetTypeName}'");
        }

        // 注册每个扩展方法
        foreach (var method in ExtensionMethods)
        {
            // 创建扩展方法包装器
            var extensionMethod = new ExtensionMethodWrapper(
                targetType,
                method,
                manager
            );

            // 注册到实例方法注册器
            InstanceMethodRegistry.Instance.Register(extensionMethod);
        }
    }

    /// <summary>
    /// 在 IL 模式下生成扩展方法声明的 IL 代码
    /// </summary>
    /// <param name="ilGenerator">IL 生成器</param>
    /// <param name="local">局部变量管理器</param>
    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 扩展方法的注册发生在编译前的解释器预执行阶段（Compiler.Compile 里的
        // statement.ExecuteModule 会调用 Run），调用点此时已经能在实例方法表里找到
        // ExtensionMethodWrapper。因此这里不需要生成任何 IL：
        // 扩展方法本身的调用由 ExtensionMethodWrapper.GenerateIl 生成。
    }

    public override OldStatement? this[int index] => this;

    public override int Count => 0;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        return visitor.VisitExtensionDeclaration(this);
    }

    /// <summary>
    /// 解析目标类型名称到 .NET Type
    /// </summary>
    /// <param name="typeName">类型名称</param>
    /// <returns>解析后的类型，如果无法解析则返回 null</returns>
    private static Type? ResolveTargetType(string typeName)
    {
        // 内置类型映射到 Old8Lang 的包装类型
        return typeName.ToLower() switch
        {
            "string" => typeof(StringLangValue), // 【修复】映射到 StringLangValue 而不是 string
            "int" => typeof(IntLangValue),
            "double" => typeof(DoubleLangValue),
            "bool" => typeof(BoolLangValue),
            "char" => typeof(CharLangValue),
            "byte" => typeof(byte),
            "short" => typeof(short),
            "decimal" => typeof(decimal),
            "object" => typeof(object),
            "list" => typeof(ListLangValue),
            "array" => typeof(Array),
            "dict" => typeof(DictionaryLangValue),
            _ => Type.GetType(typeName) // 尝试通过完全限定名解析
        };
    }
}
