namespace Old8Lang.AST.Expression.StaticValues;

/// <summary>
/// Mock 工具库 - 提供测试 Mock 功能
/// </summary>
public static class MockLib
{
    /// <summary>
    /// Mock 对象类
    /// </summary>
    public class MockObject(string name)
    {
        public string Name { get; } = name;
        private readonly Dictionary<string, List<object?[]>> _callHistory = new();
        private readonly Dictionary<string, object?> _returnValues = new();

        /// <summary>
        /// 记录方法调用
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <param name="args">方法参数</param>
        public void RecordCall(string methodName, params object?[] args)
        {
            if (!_callHistory.TryGetValue(methodName, out var value))
            {
                value = [];
                _callHistory[methodName] = value;
            }

            value.Add(args);
        }

        /// <summary>
        /// 设置方法返回值
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <param name="returnValue">返回值</param>
        public void SetReturnValue(string methodName, object? returnValue)
        {
            _returnValues[methodName] = returnValue;
        }

        /// <summary>
        /// 获取方法返回值
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <returns>返回值</returns>
        public object? GetReturnValue(string methodName)
        {
            return _returnValues.GetValueOrDefault(methodName);
        }

        /// <summary>
        /// 调用 Mock 方法
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <param name="args">方法参数</param>
        /// <returns>返回值</returns>
        public object? Call(string methodName, params object?[] args)
        {
            RecordCall(methodName, args);
            return GetReturnValue(methodName);
        }

        /// <summary>
        /// 获取方法调用次数
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <returns>调用次数</returns>
        public int GetCallCount(string methodName)
        {
            return _callHistory.GetValueOrDefault(methodName)?.Count ?? 0;
        }

        /// <summary>
        /// 获取方法所有调用记录
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <returns>调用记录列表</returns>
        public List<object?[]> GetCalls(string methodName)
        {
            return _callHistory.GetValueOrDefault(methodName) ?? [];
        }

        /// <summary>
        /// 获取方法最后一次调用的参数
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <returns>参数数组</returns>
        public object?[] GetLastCall(string methodName)
        {
            var calls = GetCalls(methodName);
            return calls.Count > 0 ? calls[^1] : [];
        }

        /// <summary>
        /// 清除所有调用记录
        /// </summary>
        public void Reset()
        {
            _callHistory.Clear();
        }

        /// <summary>
        /// 清除指定方法的调用记录
        /// </summary>
        /// <param name="methodName">方法名称</param>
        public void ResetMethod(string methodName)
        {
            _callHistory.Remove(methodName);
        }
    }

    /// <summary>
    /// 方法调用记录类
    /// </summary>
    public class MethodCall
    {
        public string MethodName { get; set; } = "";
        public object?[] Arguments { get; set; } = [];
        public DateTime Timestamp { get; set; }
    }

    // ===== Mock 工厂方法 =====

    /// <summary>
    /// 创建一个 Mock 对象
    /// </summary>
    /// <param name="name">Mock 对象名称</param>
    /// <returns>Mock 对象</returns>
    public static MockObject CreateMock(string name = "MockObject")
    {
        return new MockObject(name);
    }

    /// <summary>
    /// 创建一个带有预设返回值的 Mock 对象
    /// </summary>
    /// <param name="name">Mock 对象名称</param>
    /// <param name="returnValues">方法名和返回值的字典</param>
    /// <returns>Mock 对象</returns>
    public static MockObject CreateMockWithReturns(string name, Dictionary<string, object?> returnValues)
    {
        var mock = new MockObject(name);
        foreach (var kvp in returnValues)
        {
            mock.SetReturnValue(kvp.Key, kvp.Value);
        }

        return mock;
    }

    // ===== Mock 验证方法 =====

    /// <summary>
    /// 验证方法是否被调用过
    /// </summary>
    /// <param name="mock">Mock 对象</param>
    /// <param name="methodName">方法名称</param>
    /// <exception cref="AssertLib.AssertionException">验证失败时抛出</exception>
    public static void VerifyCalled(MockObject mock, string methodName)
    {
        var count = mock.GetCallCount(methodName);
        if (count == 0)
        {
            throw new AssertLib.AssertionException($"Mock 验证失败: 方法 '{methodName}' 未被调用");
        }
    }

    /// <summary>
    /// 验证方法是否未被调用过
    /// </summary>
    /// <param name="mock">Mock 对象</param>
    /// <param name="methodName">方法名称</param>
    /// <exception cref="AssertLib.AssertionException">验证失败时抛出</exception>
    public static void VerifyNotCalled(MockObject mock, string methodName)
    {
        var count = mock.GetCallCount(methodName);
        if (count > 0)
        {
            throw new AssertLib.AssertionException($"Mock 验证失败: 方法 '{methodName}' 被调用了 {count} 次");
        }
    }

    /// <summary>
    /// 验证方法被调用的次数
    /// </summary>
    /// <param name="mock">Mock 对象</param>
    /// <param name="methodName">方法名称</param>
    /// <param name="expectedCount">期望的调用次数</param>
    /// <exception cref="AssertLib.AssertionException">验证失败时抛出</exception>
    public static void VerifyCallCount(MockObject mock, string methodName, int expectedCount)
    {
        var actualCount = mock.GetCallCount(methodName);
        if (actualCount != expectedCount)
        {
            throw new AssertLib.AssertionException(
                $"Mock 验证失败: 方法 '{methodName}' 期望调用 {expectedCount} 次，实际调用 {actualCount} 次");
        }
    }

    /// <summary>
    /// 验证方法被调用过一次
    /// </summary>
    /// <param name="mock">Mock 对象</param>
    /// <param name="methodName">方法名称</param>
    /// <exception cref="AssertLib.AssertionException">验证失败时抛出</exception>
    public static void VerifyCalledOnce(MockObject mock, string methodName)
    {
        VerifyCallCount(mock, methodName, 1);
    }

    /// <summary>
    /// 验证方法最后一次调用的参数
    /// </summary>
    /// <param name="mock">Mock 对象</param>
    /// <param name="methodName">方法名称</param>
    /// <param name="expectedArgs">期望的参数</param>
    /// <exception cref="AssertLib.AssertionException">验证失败时抛出</exception>
    public static void VerifyLastCallArgs(MockObject mock, string methodName, params object?[] expectedArgs)
    {
        var actualArgs = mock.GetLastCall(methodName);

        if (actualArgs.Length != expectedArgs.Length)
        {
            throw new AssertLib.AssertionException(
                $"Mock 验证失败: 方法 '{methodName}' 期望 {expectedArgs.Length} 个参数，实际 {actualArgs.Length} 个参数");
        }

        for (int i = 0; i < expectedArgs.Length; i++)
        {
            if (!Equals(expectedArgs[i], actualArgs[i]))
            {
                throw new AssertLib.AssertionException(
                    $"Mock 验证失败: 方法 '{methodName}' 第 {i + 1} 个参数期望为 '{expectedArgs[i]}'，实际为 '{actualArgs[i]}'");
            }
        }
    }

    // ===== 存根 (Stub) 功能 =====

    /// <summary>
    /// 创建一个简单的存根对象（无调用记录）
    /// </summary>
    /// <param name="name">存根名称</param>
    /// <returns>Mock 对象（作为存根使用）</returns>
    public static MockObject CreateStub(string name = "Stub")
    {
        return new MockObject(name);
    }

    /// <summary>
    /// 为存根设置多个方法的返回值
    /// </summary>
    /// <param name="stub">存根对象</param>
    /// <param name="methodReturns">方法名和返回值的字典</param>
    public static void StubReturns(MockObject stub, Dictionary<string, object?> methodReturns)
    {
        foreach (var kvp in methodReturns)
        {
            stub.SetReturnValue(kvp.Key, kvp.Value);
        }
    }

    // ===== 实用工具方法 =====

    /// <summary>
    /// 获取 Mock 对象的调用摘要
    /// </summary>
    /// <param name="mock">Mock 对象</param>
    /// <returns>调用摘要字符串</returns>
    public static string GetCallSummary(MockObject mock)
    {
        var summary = $"Mock '{mock.Name}' 调用摘要:\n";
        var allCalls = mock.GetCalls("");

        if (allCalls.Count == 0)
        {
            summary += "  (无调用记录)\n";
        }

        return summary;
    }

    /// <summary>
    /// 打印 Mock 对象的所有调用记录
    /// </summary>
    /// <param name="mock">Mock 对象</param>
    /// <param name="methodName">方法名称</param>
    public static void PrintCalls(MockObject mock, string methodName)
    {
        var calls = mock.GetCalls(methodName);
        Console.WriteLine($"\n方法 '{methodName}' 的调用记录 (共 {calls.Count} 次):");

        for (int i = 0; i < calls.Count; i++)
        {
            var args = string.Join(", ", calls[i].Select(a => a?.ToString() ?? "null"));
            Console.WriteLine($"  [{i + 1}] ({args})");
        }
    }

    /// <summary>
    /// 创建一个简单的 spy（间谍对象，记录真实对象的方法调用）
    /// </summary>
    /// <param name="name">Spy 名称</param>
    /// <returns>Spy 对象</returns>
    public static MockObject CreateSpy(string name = "Spy")
    {
        return new MockObject(name);
    }
}