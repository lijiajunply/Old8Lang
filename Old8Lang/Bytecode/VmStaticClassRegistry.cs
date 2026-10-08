namespace Old8Lang.Bytecode;

/// <summary>
/// 虚拟机模式下可用的静态类 API 注册表。
/// </summary>
/// <remarks>
/// 解释器把 <c>Task</c> / <c>Thread</c> / <c>Assert</c> 注册成运行期全局对象
/// （<c>Old8Lang/Interpreter/LangInterpreter.cs</c>），静态方法分发由各自的 <c>Dot(Instance, manager)</c> 完成。
/// 字节码虚拟机没有这套对象，改为在编译期把 <c>类名.方法(参数)</c> 改写成按限定名调用的原生函数
/// （例如 <c>Task.Delay</c>），并在运行期由 <c>VirtualMachine</c> 分发。
///
/// 本表是编译期（<c>BytecodeVisitor</c>）与运行期（<c>VirtualMachine</c>）共用的唯一真源：
/// 编译期据此决定能否改写成静态类调用、不支持时如何报错，运行期据此找到实现。
/// </remarks>
public static class VmStaticClassRegistry
{
    /// <summary>
    /// 类名 → (源码方法名 → 规范方法名)。
    /// </summary>
    /// <remarks>
    /// 规范名与解释器分发表（<c>TaskClassLangValue</c> / <c>ThreadClassLangValue</c> /
    /// <c>AssertClassLangValue</c>）及各类的静态实现方法名一致。
    /// Assert 同时接受短名（<c>Equal</c>）与长名（<c>AssertEqual</c>）两种写法，与解释器一致。
    ///
    /// 未列入的方法（<c>Task.Factory</c>、<c>Thread.CurrentThread</c>、<c>Thread.Delay</c>、
    /// <c>Thread.WhenAll</c>、<c>Thread.WhenAny</c>）在虚拟机下无法支持，调用时会报
    /// <c>VmUnsupportedError</c>。
    /// </remarks>
    private static readonly Dictionary<string, Dictionary<string, string>> Methods = new(StringComparer.Ordinal)
    {
        ["Task"] = new(StringComparer.Ordinal)
        {
            ["Delay"] = "Delay",
            ["WhenAll"] = "WhenAll",
            ["WhenAny"] = "WhenAny",
            ["FromResult"] = "FromResult",
            ["FromException"] = "FromException",
            ["Run"] = "Run",
            ["StartNew"] = "StartNew",
        },
        ["Thread"] = new(StringComparer.Ordinal)
        {
            ["Sleep"] = "Sleep",
        },
        ["Assert"] = new(StringComparer.Ordinal)
        {
            ["Equal"] = "AssertEqual",
            ["AssertEqual"] = "AssertEqual",
            ["NotEqual"] = "AssertNotEqual",
            ["AssertNotEqual"] = "AssertNotEqual",
            ["True"] = "AssertTrue",
            ["AssertTrue"] = "AssertTrue",
            ["False"] = "AssertFalse",
            ["AssertFalse"] = "AssertFalse",
            ["Null"] = "AssertNull",
            ["AssertNull"] = "AssertNull",
            ["NotNull"] = "AssertNotNull",
            ["AssertNotNull"] = "AssertNotNull",
            ["Greater"] = "AssertGreater",
            ["AssertGreater"] = "AssertGreater",
            ["GreaterOrEqual"] = "AssertGreaterOrEqual",
            ["AssertGreaterOrEqual"] = "AssertGreaterOrEqual",
            ["Less"] = "AssertLess",
            ["AssertLess"] = "AssertLess",
            ["LessOrEqual"] = "AssertLessOrEqual",
            ["AssertLessOrEqual"] = "AssertLessOrEqual",
            ["Contains"] = "AssertContains",
            ["AssertContains"] = "AssertContains",
            ["NotContains"] = "AssertNotContains",
            ["AssertNotContains"] = "AssertNotContains",
            ["StartsWith"] = "AssertStartsWith",
            ["AssertStartsWith"] = "AssertStartsWith",
            ["EndsWith"] = "AssertEndsWith",
            ["AssertEndsWith"] = "AssertEndsWith",
            ["Matches"] = "AssertMatches",
            ["AssertMatches"] = "AssertMatches",
            ["ContainsItem"] = "AssertContainsItem",
            ["AssertContainsItem"] = "AssertContainsItem",
            ["NotContainsItem"] = "AssertNotContainsItem",
            ["AssertNotContainsItem"] = "AssertNotContainsItem",
            ["Empty"] = "AssertEmpty",
            ["AssertEmpty"] = "AssertEmpty",
            ["NotEmpty"] = "AssertNotEmpty",
            ["AssertNotEmpty"] = "AssertNotEmpty",
            ["Length"] = "AssertLength",
            ["AssertLength"] = "AssertLength",
            ["Throws"] = "AssertThrows",
            ["AssertThrows"] = "AssertThrows",
            ["NotThrows"] = "AssertNotThrows",
            ["AssertNotThrows"] = "AssertNotThrows",
            ["InstanceOf"] = "AssertInstanceOf",
            ["AssertInstanceOf"] = "AssertInstanceOf",
            ["NotInstanceOf"] = "AssertNotInstanceOf",
            ["AssertNotInstanceOf"] = "AssertNotInstanceOf",
        },
    };

    /// <summary>
    /// 判断名称是否是虚拟机支持的静态类名。
    /// </summary>
    public static bool IsStaticClass(string name) => Methods.ContainsKey(name);

    /// <summary>
    /// 把源码里的 <c>类名.方法名</c> 解析成规范方法名与运行期限定名。
    /// </summary>
    /// <param name="className">静态类名，如 <c>Task</c></param>
    /// <param name="sourceMethod">源码中书写的方法名，如 <c>Equal</c> 或 <c>AssertEqual</c></param>
    /// <param name="canonicalMethod">规范方法名，用于查各类的 <c>VmReusableMethods</c> 表</param>
    /// <param name="qualifiedName">运行期限定名，如 <c>Task.Delay</c></param>
    /// <returns>该组合在虚拟机下受支持时返回 true</returns>
    public static bool TryCanonicalize(string className, string sourceMethod, out string canonicalMethod,
        out string qualifiedName)
    {
        canonicalMethod = "";
        qualifiedName = "";

        if (!Methods.TryGetValue(className, out var classMethods) ||
            !classMethods.TryGetValue(sourceMethod, out var canonical))
        {
            return false;
        }

        canonicalMethod = canonical;
        qualifiedName = $"{className}.{canonical}";
        return true;
    }

    /// <summary>
    /// 生成报错用的支持清单，如 <c>Task.Delay、Task.WhenAll</c>。
    /// </summary>
    public static string DescribeSupportedMethods(string className)
    {
        if (!Methods.TryGetValue(className, out var classMethods))
        {
            return "";
        }

        // 一个规范方法可能有短名/长名两种写法（Assert 的 Equal / AssertEqual），清单里只取最短的那个。
        var names = classMethods
            .GroupBy(pair => pair.Value, StringComparer.Ordinal)
            .Select(group => $"{className}." + group
                .Select(pair => pair.Key)
                .OrderBy(name => name.Length)
                .ThenBy(name => name, StringComparer.Ordinal)
                .First())
            .OrderBy(name => name, StringComparer.Ordinal);

        return string.Join("、", names);
    }
}
