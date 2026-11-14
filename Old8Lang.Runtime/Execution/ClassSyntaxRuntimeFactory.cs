using System.Runtime.CompilerServices;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.TypeSystem;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 类声明语法节点到运行时 TypeTemplate 的适配工厂（阶段 2 语法/运行时分界）。
/// </summary>
public static class ClassSyntaxRuntimeFactory
{
    /// <summary>从类语法模型创建 TypeTemplate，并收集成员与文档注释。</summary>
    public static TypeTemplate Create(ClassSyntax syntax)
    {
        var template = new TypeTemplate(
            syntax.ClassName,
            syntax.Body.ToAnyData(),
            syntax.Body.ToStaticData(),
            syntax.ParentClassName,
            syntax.IsMixin,
            syntax.MixinNames,
            syntax.ImplementsNames,
            syntax.IsInterface,
            syntax.IsAbstract,
            syntax.GenericParameters,
            syntax.ParentGenericTypeParameters,
            syntax.Position);

        if (syntax.DocComment is not null) template.DocComment = syntax.DocComment;
        return template;
    }
}

/// <summary>
/// <see cref="ClassInit"/> 的运行时 TypeTemplate 访问入口；按语法节点缓存以保持原对象身份。
/// </summary>
public static partial class ClassInitRuntimeExtensions
{
    private static readonly ConditionalWeakTable<ClassSyntax, TypeTemplate> Cache = new();
    private static readonly ConditionalWeakTable<ClassInit, TypeTemplate> TemplateOverrides = new();

    /// <summary>获取该类声明的运行时 TypeTemplate（同一语法节点返回同一实例）。</summary>
    public static TypeTemplate GetTypeTemplate(this ClassInit init) =>
        TemplateOverrides.TryGetValue(init, out var overridden)
            ? overridden
            : Cache.GetValue(init.Syntax, ClassSyntaxRuntimeFactory.Create);

    /// <summary>
    /// 为内部重算的 TypeTemplate（如嵌套类的完整限定名）创建 ClassInit，并绑定指定模板。
    /// </summary>
    public static ClassInit CreateForTemplate(TypeTemplate template)
    {
        var syntax = new ClassSyntax(
            template.ClassName,
            new BlockStatement([]),
            template.ParentClassName,
            template.IsMixin,
            template.MixinNames,
            template.ImplementsNames,
            template.IsInterface,
            template.IsAbstract,
            template.GenericParameters,
            template.ParentGenericTypeParameters,
            template.DocComment,
            template.Position);
        var init = new ClassInit(syntax, template.Position);
        TemplateOverrides.Add(init, template);
        return init;
    }

    /// <summary>注册类类型到类型注解系统。</summary>
    public static void RegisterTypeToTypeSystem(this ClassInit init)
    {
        var template = init.GetTypeTemplate();
        try
        {
            if (template.IsInterface)
            {
                // 接口的父接口存储在 ImplementsNames 中（见 ClassParser.ParseInterfaceDeclaration）
                List<string> parentInterfaceNames = template.ImplementsNames;
                TypeChecker.RegisterInterfaceType(template.ClassName, parentInterfaceNames);
            }
            else
            {
                TypeChecker.RegisterClassType(
                    template.ClassName,
                    template.ParentClassName,
                    template.ImplementsNames);
            }
        }
        catch
        {
            // 如果类型注册失败，不影响类定义的正常执行（向后兼容）
        }
    }
}

/// <summary>
/// <see cref="ClassFuncInitStatement"/> 的运行时函数值访问入口；按语法节点缓存。
/// </summary>
public static class ClassFuncInitRuntimeExtensions
{
    /// <summary>获取该方法的运行时函数值（同一语法节点返回同一实例）。</summary>
    public static FuncLangValue GetFuncValue(this ClassFuncInitStatement init) =>
        FunctionSyntaxRuntimeFactory.GetOrCreateFunc(init.Function);
}
