using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8LangLib;

namespace Old8Lang.AST.Expression.StaticValues;

/// <summary>
/// MockLib 类的全局对象,提供静态方法访问
/// </summary>
public partial class MockLibClassLangValue : LangValueType
{
    private static readonly MockLibClassLangValue Instance = new();

    /// <summary>
    /// 获取 MockLib 类的全局单例
    /// </summary>
    public static MockLibClassLangValue GetInstance() => Instance;

    public override string TypeToString() => "MockLibClass";

    public override string ToDisplayString() => "MockLib";

    /// <summary>
    /// 生成 IL 代码，返回 MockLib 类型本身
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 对于 MockLib 类静态方法，我们不需要加载实例
        // 直接返回 MockLib 类型本身
        ilGenerator.Emit(OpCodes.Ldtoken, typeof(MockLibClassLangValue));
        ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle")!);
    }

    /// <summary>
    /// 外部管理器，用于访问外部变量
    /// </summary>
    public VariateManager? ExternalManager { get; set; }

    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        // 处理 MockLib.CreateMock(...) 形式的调用
        if (dotExpression is Instance instance)
        {
            var methodName = instance.Id.IdName;

            // 返回一个包装函数,用于调用静态方法
            Func<List<LangValueType>, SourcePosition, LangValueType>? method = methodName switch
            {
                "CreateMock" => CreateMock,
                "CreateMockWithReturns" => CreateMockWithReturns,
                "CreateStub" => CreateStub,
                "CreateSpy" => CreateSpy,
                "VerifyCalled" => VerifyCalled,
                "VerifyNotCalled" => VerifyNotCalled,
                "VerifyCallCount" => VerifyCallCount,
                "VerifyCalledOnce" => VerifyCalledOnce,
                "VerifyLastCallArgs" => VerifyLastCallArgs,
                "StubReturns" => StubReturns,
                "GetCallSummary" => GetCallSummary,
                "PrintCalls" => PrintCalls,
                _ => null
            };

            if (method is null)
            {
                throw new AttributeError(dotExpression.Position, methodName, "MockLib");
            }

            // 使用 ExternalManager 或传入的 manager 执行参数
            var currentManager = ExternalManager ?? manager;
            var args = instance.Ids.Select(id => id.Run(currentManager)).ToList();
            return method(args, instance.Position);
        }

        // 处理 MockLib.CreateMock 形式的访问（不带调用）
        if (dotExpression is LangId memberId)
        {
            var methodName = memberId.IdName;

            // 返回一个包装函数,用于调用静态方法
            return methodName switch
            {
                "CreateMock" => new MockLibStaticMethodWrapper("CreateMock", CreateMock),
                "CreateMockWithReturns" => new MockLibStaticMethodWrapper("CreateMockWithReturns",
                    CreateMockWithReturns),
                "CreateStub" => new MockLibStaticMethodWrapper("CreateStub", CreateStub),
                "CreateSpy" => new MockLibStaticMethodWrapper("CreateSpy", CreateSpy),
                "VerifyCalled" => new MockLibStaticMethodWrapper("VerifyCalled", VerifyCalled),
                "VerifyNotCalled" => new MockLibStaticMethodWrapper("VerifyNotCalled", VerifyNotCalled),
                "VerifyCallCount" => new MockLibStaticMethodWrapper("VerifyCallCount", VerifyCallCount),
                "VerifyCalledOnce" => new MockLibStaticMethodWrapper("VerifyCalledOnce", VerifyCalledOnce),
                "VerifyLastCallArgs" => new MockLibStaticMethodWrapper("VerifyLastCallArgs", VerifyLastCallArgs),
                "StubReturns" => new MockLibStaticMethodWrapper("StubReturns", StubReturns),
                "GetCallSummary" => new MockLibStaticMethodWrapper("GetCallSummary", GetCallSummary),
                "PrintCalls" => new MockLibStaticMethodWrapper("PrintCalls", PrintCalls),
                _ => throw new AttributeError(dotExpression.Position, methodName, "MockLib")
            };
        }

        throw new AttributeError(dotExpression.Position,
            dotExpression.ToString() ?? "unknown", "MockLib");
    }

    /// <summary>
    /// CreateMock 静态方法实现
    /// </summary>
    private static LangValueType CreateMock(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is 0 or > 1)
        {
            throw new ArgumentError(position,
                $"CreateMock 期望 0-1 个参数(name)，但提供了 {args.Count} 个");
        }

        var name = args is [StringLangValue stringName] ? stringName.Value : "MockObject";

        try
        {
            var mock = MockLib.CreateMock(name);
            return ConvertMockObject(mock);
        }
        catch (Exception ex)
        {
            throw new Exception($"CreateMock 执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// CreateMockWithReturns 静态方法实现
    /// </summary>
    private static LangValueType CreateMockWithReturns(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is < 1 or > 2)
        {
            throw new ArgumentError(position,
                $"CreateMockWithReturns 期望 1-2 个参数(name, returnValues)，但提供了 {args.Count} 个");
        }

        var name = args[0] is StringLangValue stringName ? stringName.Value : "MockObject";

        Dictionary<string, object?> returnValues = new();
        if (args is [_, DictionaryLangValue dict])
        {
            foreach (var kvp in dict.Value)
            {
                if (kvp.Key is StringLangValue key)
                {
                    returnValues[key.Value] = ConvertLangValueToNative(kvp.Value);
                }
            }
        }

        try
        {
            var mock = MockLib.CreateMockWithReturns(name, returnValues);
            return ConvertMockObject(mock);
        }
        catch (Exception ex)
        {
            throw new Exception($"CreateMockWithReturns 执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// CreateStub 静态方法实现
    /// </summary>
    private static LangValueType CreateStub(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is 0 or > 1)
        {
            throw new ArgumentError(position,
                $"CreateStub 期望 0-1 个参数(name)，但提供了 {args.Count} 个");
        }

        var name = args is [StringLangValue stringName] ? stringName.Value : "Stub";

        try
        {
            var stub = MockLib.CreateStub(name);
            return ConvertMockObject(stub);
        }
        catch (Exception ex)
        {
            throw new Exception($"CreateStub 执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// CreateSpy 静态方法实现
    /// </summary>
    private static LangValueType CreateSpy(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count is 0 or > 1)
        {
            throw new ArgumentError(position,
                $"CreateSpy 期望 0-1 个参数(name)，但提供了 {args.Count} 个");
        }

        var name = args is [StringLangValue stringName] ? stringName.Value : "Spy";

        try
        {
            var spy = MockLib.CreateSpy(name);
            return ConvertMockObject(spy);
        }
        catch (Exception ex)
        {
            throw new Exception($"CreateSpy 执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// VerifyCalled 静态方法实现
    /// </summary>
    private static LangValueType VerifyCalled(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 2)
        {
            throw new ArgumentError(position,
                $"VerifyCalled 期望 2 个参数(mock, methodName)，但提供了 {args.Count} 个");
        }

        var mock = ExtractMockObject(args[0], position);
        var methodName = args[1] is StringLangValue stringMethod
            ? stringMethod.Value
            : throw new TypeError(new NullLangValue(position), "string", args[1].TypeToString());

        try
        {
            MockLib.VerifyCalled(mock, methodName);
        }
        catch (AssertLib.AssertionException ex)
        {
            throw new Exception(ex.Message);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// VerifyNotCalled 静态方法实现
    /// </summary>
    private static LangValueType VerifyNotCalled(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 2)
        {
            throw new ArgumentError(position,
                $"VerifyNotCalled 期望 2 个参数(mock, methodName)，但提供了 {args.Count} 个");
        }

        var mock = ExtractMockObject(args[0], position);
        var methodName = args[1] is StringLangValue stringMethod
            ? stringMethod.Value
            : throw new TypeError(new NullLangValue(position), "string", args[1].TypeToString());

        try
        {
            MockLib.VerifyNotCalled(mock, methodName);
        }
        catch (AssertLib.AssertionException ex)
        {
            throw new Exception(ex.Message);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// VerifyCallCount 静态方法实现
    /// </summary>
    private static LangValueType VerifyCallCount(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 3)
        {
            throw new ArgumentError(position,
                $"VerifyCallCount 期望 3 个参数(mock, methodName, expectedCount)，但提供了 {args.Count} 个");
        }

        var mock = ExtractMockObject(args[0], position);
        var methodName = args[1] is StringLangValue stringMethod
            ? stringMethod.Value
            : throw new TypeError(new NullLangValue(position), "string", args[1].TypeToString());
        var expectedCount = args[2] is IntLangValue intCount
            ? intCount.Value
            : throw new TypeError(new NullLangValue(position), "int", args[2].TypeToString());

        try
        {
            MockLib.VerifyCallCount(mock, methodName, expectedCount);
        }
        catch (AssertLib.AssertionException ex)
        {
            throw new Exception(ex.Message);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// VerifyCalledOnce 静态方法实现
    /// </summary>
    private static LangValueType VerifyCalledOnce(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 2)
        {
            throw new ArgumentError(position,
                $"VerifyCalledOnce 期望 2 个参数(mock, methodName)，但提供了 {args.Count} 个");
        }

        var mock = ExtractMockObject(args[0], position);
        var methodName = args[1] is StringLangValue stringMethod
            ? stringMethod.Value
            : throw new TypeError(new NullLangValue(position), "string", args[1].TypeToString());

        try
        {
            MockLib.VerifyCalledOnce(mock, methodName);
        }
        catch (AssertLib.AssertionException ex)
        {
            throw new Exception(ex.Message);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// VerifyLastCallArgs 静态方法实现
    /// </summary>
    private static LangValueType VerifyLastCallArgs(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count < 2)
        {
            throw new ArgumentError(position,
                $"VerifyLastCallArgs 期望至少 2 个参数(mock, methodName, ...expectedArgs)，但提供了 {args.Count} 个");
        }

        var mock = ExtractMockObject(args[0], position);
        var methodName = args[1] is StringLangValue stringMethod
            ? stringMethod.Value
            : throw new TypeError(new NullLangValue(position), "string", args[1].TypeToString());

        var expectedArgs = args.Skip(2).Select(ConvertLangValueToNative).ToArray();

        try
        {
            MockLib.VerifyLastCallArgs(mock, methodName, expectedArgs);
        }
        catch (AssertLib.AssertionException ex)
        {
            throw new Exception(ex.Message);
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// StubReturns 静态方法实现
    /// </summary>
    private static LangValueType StubReturns(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 2)
        {
            throw new ArgumentError(position,
                $"StubReturns 期望 2 个参数(stub, methodReturns)，但提供了 {args.Count} 个");
        }

        var stub = ExtractMockObject(args[0], position);

        if (args[1] is not DictionaryLangValue dict)
        {
            throw new TypeError(new NullLangValue(position), "dictionary", args[1].TypeToString());
        }

        var methodReturns = new Dictionary<string, object?>();
        foreach (var kvp in dict.Value)
        {
            if (kvp.Key is StringLangValue key)
            {
                methodReturns[key.Value] = ConvertLangValueToNative(kvp.Value);
            }
        }

        try
        {
            MockLib.StubReturns(stub, methodReturns);
        }
        catch (Exception ex)
        {
            throw new Exception($"StubReturns 执行失败: {ex.Message}");
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// GetCallSummary 静态方法实现
    /// </summary>
    private static LangValueType GetCallSummary(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 1)
        {
            throw new ArgumentError(position,
                $"GetCallSummary 期望 1 个参数(mock)，但提供了 {args.Count} 个");
        }

        var mock = ExtractMockObject(args[0], position);

        try
        {
            var summary = MockLib.GetCallSummary(mock);
            return new StringLangValue(summary);
        }
        catch (Exception ex)
        {
            throw new Exception($"GetCallSummary 执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// PrintCalls 静态方法实现
    /// </summary>
    private static LangValueType PrintCalls(List<LangValueType> args, SourcePosition position)
    {
        if (args.Count != 2)
        {
            throw new ArgumentError(position,
                $"PrintCalls 期望 2 个参数(mock, methodName)，但提供了 {args.Count} 个");
        }

        var mock = ExtractMockObject(args[0], position);
        var methodName = args[1] is StringLangValue stringMethod
            ? stringMethod.Value
            : throw new TypeError(new NullLangValue(position), "string", args[1].TypeToString());

        try
        {
            MockLib.PrintCalls(mock, methodName);
        }
        catch (Exception ex)
        {
            throw new Exception($"PrintCalls 执行失败: {ex.Message}");
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// 将 MockObject 转换为 LangValueType
    /// </summary>
    private static LangValueType ConvertMockObject(MockLib.MockObject mock)
    {
        // 创建一个包装对象来存储 MockObject 实例
        return new MockObjectLangValue(mock);
    }

    /// <summary>
    /// 从 LangValueType 中提取 MockObject
    /// </summary>
    private static MockLib.MockObject ExtractMockObject(LangValueType value, SourcePosition position)
    {
        if (value is MockObjectLangValue mockValue)
        {
            return mockValue.MockObject;
        }

        throw new TypeError(new NullLangValue(position), "MockObject", value.TypeToString());
    }

    /// <summary>
    /// 将 LangValueType 转换为原生 .NET 对象
    /// </summary>
    private static object? ConvertLangValueToNative(LangValueType value)
    {
        return value switch
        {
            StringLangValue str => str.Value,
            IntLangValue intVal => intVal.Value,
            DoubleLangValue doubleVal => doubleVal.Value,
            BoolLangValue boolVal => boolVal.Value,
            NullLangValue => null,
            _ => value.ToDisplayString()
        };
    }
}

/// <summary>
/// MockObject 的 LangValueType 包装器
/// </summary>
public partial class MockObjectLangValue(MockLib.MockObject mockObject, SourcePosition position = default)
    : LangValueType(position)
{
    public MockLib.MockObject MockObject { get; } = mockObject;

    public override string TypeToString() => "MockObject";

    public override string ToDisplayString() => $"MockObject('{MockObject.Name}')";

    public override Type OutputType(LocalManager local)
    {
        return typeof(MockLib.MockObject);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // MockObject 在 IL 中的加载是复杂的，这里简化处理
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        if (dotExpression is Instance instance)
        {
            var methodName = instance.Id.IdName;
            var args = instance.Ids.Select(id => id.Run(manager)).ToList();

            return methodName switch
            {
                "RecordCall" => RecordCall(args),
                "SetReturnValue" => SetReturnValue(args),
                "GetReturnValue" => GetReturnValue(args),
                "Call" => Call(args),
                "GetCallCount" => GetCallCount(args),
                "GetCalls" => GetCalls(args),
                "GetLastCall" => GetLastCall(args),
                "Reset" => Reset(),
                "ResetMethod" => ResetMethod(args),
                _ => throw new AttributeError(dotExpression.Position, methodName, "MockObject")
            };
        }

        if (dotExpression is LangId memberId)
        {
            return memberId.IdName switch
            {
                "Name" => new StringLangValue(MockObject.Name),
                _ => throw new AttributeError(dotExpression.Position, memberId.IdName, "MockObject")
            };
        }

        throw new AttributeError(dotExpression.Position, dotExpression.ToString() ?? "unknown", "MockObject");
    }

    private LangValueType RecordCall(List<LangValueType> args)
    {
        if (args.Count == 0)
            throw new ArgumentError(Position, "RecordCall 期望至少 1 个参数(methodName)");

        var methodName = args[0] is StringLangValue str
            ? str.Value
            : throw new TypeError(new NullLangValue(Position), "string", args[0].TypeToString());

        var nativeArgs = args.Skip(1).Select(ConvertToNative).ToArray();
        MockObject.RecordCall(methodName, nativeArgs);

        return new VoidLangValue();
    }

    private LangValueType SetReturnValue(List<LangValueType> args)
    {
        if (args.Count != 2)
            throw new ArgumentError(Position, "SetReturnValue 期望 2 个参数(methodName, returnValue)");

        var methodName = args[0] is StringLangValue str
            ? str.Value
            : throw new TypeError(new NullLangValue(Position), "string", args[0].TypeToString());

        var returnValue = ConvertToNative(args[1]);
        MockObject.SetReturnValue(methodName, returnValue);

        return new VoidLangValue();
    }

    private LangValueType GetReturnValue(List<LangValueType> args)
    {
        if (args.Count != 1)
            throw new ArgumentError(Position, "GetReturnValue 期望 1 个参数(methodName)");

        var methodName = args[0] is StringLangValue str
            ? str.Value
            : throw new TypeError(new NullLangValue(Position), "string", args[0].TypeToString());

        var result = MockObject.GetReturnValue(methodName);
        return ConvertFromNative(result);
    }

    private LangValueType Call(List<LangValueType> args)
    {
        if (args.Count == 0)
            throw new ArgumentError(Position, "Call 期望至少 1 个参数(methodName)");

        var methodName = args[0] is StringLangValue str
            ? str.Value
            : throw new TypeError(new NullLangValue(Position), "string", args[0].TypeToString());

        var nativeArgs = args.Skip(1).Select(ConvertToNative).ToArray();
        var result = MockObject.Call(methodName, nativeArgs);

        return ConvertFromNative(result);
    }

    private LangValueType GetCallCount(List<LangValueType> args)
    {
        if (args.Count != 1)
            throw new ArgumentError(Position, "GetCallCount 期望 1 个参数(methodName)");

        var methodName = args[0] is StringLangValue str
            ? str.Value
            : throw new TypeError(new NullLangValue(Position), "string", args[0].TypeToString());

        var count = MockObject.GetCallCount(methodName);
        return new IntLangValue(count);
    }

    private LangValueType GetCalls(List<LangValueType> args)
    {
        if (args.Count != 1)
            throw new ArgumentError(Position, "GetCalls 期望 1 个参数(methodName)");

        var methodName = args[0] is StringLangValue str
            ? str.Value
            : throw new TypeError(new NullLangValue(Position), "string", args[0].TypeToString());

        var calls = MockObject.GetCalls(methodName);
        var callLists = calls.Select(call =>
            new ListLangValue(call.Select(ConvertFromNative).ToList())
        ).ToList<LangValueType>();

        return new ListLangValue(callLists);
    }

    private LangValueType GetLastCall(List<LangValueType> args)
    {
        if (args.Count != 1)
            throw new ArgumentError(Position, "GetLastCall 期望 1 个参数(methodName)");

        var methodName = args[0] is StringLangValue str
            ? str.Value
            : throw new TypeError(new NullLangValue(Position), "string", args[0].TypeToString());

        var lastCall = MockObject.GetLastCall(methodName);
        return new ListLangValue(lastCall.Select(ConvertFromNative).ToList());
    }

    private LangValueType Reset()
    {
        MockObject.Reset();
        return new VoidLangValue();
    }

    private LangValueType ResetMethod(List<LangValueType> args)
    {
        if (args.Count != 1)
            throw new ArgumentError(Position, "ResetMethod 期望 1 个参数(methodName)");

        var methodName = args[0] is StringLangValue str
            ? str.Value
            : throw new TypeError(new NullLangValue(Position), "string", args[0].TypeToString());

        MockObject.ResetMethod(methodName);
        return new VoidLangValue();
    }

    private static object? ConvertToNative(LangValueType value)
    {
        return value switch
        {
            StringLangValue str => str.Value,
            IntLangValue intVal => intVal.Value,
            DoubleLangValue doubleVal => doubleVal.Value,
            BoolLangValue boolVal => boolVal.Value,
            NullLangValue => null,
            _ => value.ToDisplayString()
        };
    }

    private static LangValueType ConvertFromNative(object? value)
    {
        return value switch
        {
            string str => new StringLangValue(str),
            int intVal => new IntLangValue(intVal),
            double doubleVal => new DoubleLangValue(doubleVal),
            bool boolVal => new BoolLangValue(boolVal),
            null => NullLangValue.Instance,
            _ => new StringLangValue(value.ToString() ?? "")
        };
    }
}

/// <summary>
/// MockLib 静态方法的包装器
/// </summary>
public partial class MockLibStaticMethodWrapper(
    string methodName,
    Func<List<LangValueType>, SourcePosition, LangValueType> method)
    : LangValueType
{
    public override string TypeToString() => "MockLibStaticMethod";

    public override string ToDisplayString() => $"MockLib.{methodName}";

    /// <summary>
    /// 执行静态方法
    /// </summary>
    public LangValueType Invoke(List<LangValueType> args, SourcePosition position)
    {
        return method(args, position);
    }

    /// <summary>
    /// 生成 IL 代码，返回对应MockLib静态方法的委托
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // MockLib 静态方法在 IL 中通常通过直接调用来实现
        // 这里返回 null 作为占位符
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    /// <summary>
    /// 获取输出类型
    /// </summary>
    public override Type OutputType(LocalManager local)
    {
        return typeof(Delegate);
    }
}