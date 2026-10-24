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
            // 属性：Task.Factory（返回 TaskFactory 对象）
            ["Factory"] = "Factory",
        },
        ["Thread"] = new(StringComparer.Ordinal)
        {
            ["Sleep"] = "Sleep",
            // 属性 / 无参方法两种写法，与解释器一致
            ["CurrentThread"] = "CurrentThread",
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
        ["TestRunner"] = new(StringComparer.Ordinal)
        {
            ["Describe"] = "Describe",
            ["It"] = "It",
            ["Test"] = "Test",
            ["RunTest"] = "RunTest",
            ["RunAll"] = "RunAll",
            ["RunSuite"] = "RunSuite",
            ["Clear"] = "Clear",
            ["GetSuiteCount"] = "GetSuiteCount",
            ["GetTestCount"] = "GetTestCount",
            ["PrintSummary"] = "PrintSummary",
            ["PrintDetailed"] = "PrintDetailed",
            ["RunAllWithReport"] = "RunAllWithReport",
            ["GetStatistics"] = "GetStatistics",
        },
        ["Mock"] = new(StringComparer.Ordinal)
        {
            ["CreateMock"] = "CreateMock",
            ["CreateMockWithReturns"] = "CreateMockWithReturns",
            ["CreateStub"] = "CreateStub",
            ["CreateSpy"] = "CreateSpy",
            ["VerifyCalled"] = "VerifyCalled",
            ["VerifyNotCalled"] = "VerifyNotCalled",
            ["VerifyCallCount"] = "VerifyCallCount",
            ["VerifyCalledOnce"] = "VerifyCalledOnce",
            ["VerifyLastCallArgs"] = "VerifyLastCallArgs",
            ["StubReturns"] = "StubReturns",
            ["GetCallSummary"] = "GetCallSummary",
            ["PrintCalls"] = "PrintCalls",
        },
        ["TaskScheduler"] = new(StringComparer.Ordinal)
        {
            ["Default"] = "Default",
        },
    };

    /// <summary>
    /// 判断名称是否是虚拟机支持的静态类名。
    /// </summary>
    public static bool IsStaticClass(string name) => Methods.ContainsKey(name);

    /// <summary>
    /// 「静态类方法作为值」的限定名前缀。
    /// </summary>
    /// <remarks>
    /// 编译期把裸引用 <c>Task.Delay</c> 改写成 <c>CallNative 0, 前缀 + "Task.Delay"</c>，
    /// 运行期据此产出 <c>VmStaticMethodValue</c>，而不是真的调用。
    /// 用控制字符开头，确保不会和用户/标准库的函数名冲突。
    /// </remarks>
    public const string StaticMethodValuePrefix = "\u0001staticmethod:";

    /// <summary>
    /// 类名 → 以「属性」形式访问的成员（不带括号）。
    /// </summary>
    /// <remarks>
    /// 这些成员在解释器里既可以写成 <c>Thread.CurrentThread</c> 也可以写成
    /// <c>Thread.CurrentThread()</c>；虚拟机把裸形式当作 0 参数调用处理。
    /// 其余裸成员名一律视作「取方法值」，赋给变量后再调用。
    /// </remarks>
    private static readonly Dictionary<string, HashSet<string>> PropertyLikeMembers = new(StringComparer.Ordinal)
    {
        ["Task"] = new(StringComparer.Ordinal) { "Factory" },
        ["Thread"] = new(StringComparer.Ordinal) { "CurrentThread" },
        ["TaskScheduler"] = new(StringComparer.Ordinal) { "Default" },
    };

    /// <summary>
    /// 判断成员是否按「属性」读取（裸形式即取值，而不是取方法值）。
    /// </summary>
    public static bool IsPropertyLikeMember(string className, string member) =>
        PropertyLikeMembers.TryGetValue(className, out var members) && members.Contains(member);

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
