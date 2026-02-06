using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;
using System.Reflection.Emit;
using Xunit;

namespace Old8Lang.Tests.InstanceMethods;

/// <summary>
/// 实例方法重载解析测试
/// </summary>
public class OverloadResolutionTests
{
    /// <summary>
    /// 测试用的简单实例方法 - 接受一个 int 参数
    /// </summary>
    private class TestMethodOneParam : BaseInstanceMethod
    {
        public override string[] Names => ["TestMethod"];
        public override Type TargetType => typeof(StringLangValue);
        public override int MinParameterCount => 1;
        public override int MaxParameterCount => 1;
        public override Type?[]? ParameterTypes => [typeof(IntLangValue)];
        public override Type? DeclaredReturnType => typeof(StringLangValue);

        protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
            VariateManager manager, SourcePosition position)
        {
            return new StringLangValue("OneParam");
        }

        protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
            ILGenerator ilGenerator, LocalManager local, SourcePosition position)
        {
            throw new NotImplementedException();
        }

        protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters,
            LocalManager local)
        {
            return typeof(StringLangValue);
        }

        protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
        {
            return "OneParam";
        }
    }

    /// <summary>
    /// 测试用的简单实例方法 - 接受两个 int 参数
    /// </summary>
    private class TestMethodTwoParams : BaseInstanceMethod
    {
        public override string[] Names => ["TestMethod"];
        public override Type TargetType => typeof(StringLangValue);
        public override int MinParameterCount => 2;
        public override int MaxParameterCount => 2;
        public override Type?[]? ParameterTypes => [typeof(IntLangValue), typeof(IntLangValue)];
        public override Type? DeclaredReturnType => typeof(StringLangValue);

        protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
            VariateManager manager, SourcePosition position)
        {
            return new StringLangValue("TwoParams");
        }

        protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
            ILGenerator ilGenerator, LocalManager local, SourcePosition position)
        {
            throw new NotImplementedException();
        }

        protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters,
            LocalManager local)
        {
            return typeof(StringLangValue);
        }

        protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
        {
            return "TwoParams";
        }
    }

    [Fact]
    public void TestOverloadGroup_AddOverload_ShouldAddMultipleOverloads()
    {
        // Arrange
        var overloadGroup = new InstanceMethodOverloadGroup("TestMethod", typeof(StringLangValue));
        var method1 = new TestMethodOneParam();
        var method2 = new TestMethodTwoParams();

        // Act
        overloadGroup.AddOverload(method1);
        overloadGroup.AddOverload(method2);

        // Assert
        Assert.Equal(2, overloadGroup.Count);
    }

    [Fact]
    public void TestOverloadGroup_ResolveOverload_ShouldSelectCorrectOverload()
    {
        // Arrange
        var overloadGroup = new InstanceMethodOverloadGroup("TestMethod", typeof(StringLangValue));
        var method1 = new TestMethodOneParam();
        var method2 = new TestMethodTwoParams();
        overloadGroup.AddOverload(method1);
        overloadGroup.AddOverload(method2);

        var oneParamCall = new List<LangExpression>
        {
            new IntLangValue(1)
        };

        var twoParamCall = new List<LangExpression>
        {
            new IntLangValue(1),
            new IntLangValue(2)
        };

        // Act
        var resolvedOne = overloadGroup.ResolveOverload(oneParamCall, null);
        var resolvedTwo = overloadGroup.ResolveOverload(twoParamCall, null);

        // Assert
        Assert.NotNull(resolvedOne);
        Assert.NotNull(resolvedTwo);
        Assert.Equal(1, resolvedOne.MinParameterCount);
        Assert.Equal(2, resolvedTwo.MinParameterCount);
    }

    [Fact]
    public void TestOverloadGroup_ResolveOverload_NoMatch_ShouldReturnNull()
    {
        // Arrange
        var overloadGroup = new InstanceMethodOverloadGroup("TestMethod", typeof(StringLangValue));
        var method1 = new TestMethodOneParam();
        overloadGroup.AddOverload(method1);

        var threeParamCall = new List<LangExpression>
        {
            new IntLangValue(1),
            new IntLangValue(2),
            new IntLangValue(3)
        };

        // Act
        var resolved = overloadGroup.ResolveOverload(threeParamCall, null);

        // Assert
        Assert.Null(resolved);
    }

    [Fact]
    public void TestInstanceMethodRegistry_Register_ShouldCreateOverloadGroup()
    {
        // Arrange
        var registry = InstanceMethodRegistry.Instance;
        var method1 = new TestMethodOneParam();
        var method2 = new TestMethodTwoParams();

        // Act
        registry.Register(method1);
        registry.Register(method2);

        // Assert
        var overloadGroup = registry.GetOverloadGroup(typeof(StringLangValue), "TestMethod");
        Assert.NotNull(overloadGroup);
        // 至少有 2 个重载（可能有更多，因为是单例）
        Assert.True(overloadGroup.Count >= 2);
    }

    [Fact]
    public void TestInstanceMethodRegistry_ResolveMethod_ShouldSelectCorrectOverload()
    {
        // Arrange
        var registry = InstanceMethodRegistry.Instance;
        var method1 = new TestMethodOneParam();
        var method2 = new TestMethodTwoParams();
        registry.Register(method1);
        registry.Register(method2);

        var oneParamCall = new List<LangExpression>
        {
            new IntLangValue(1)
        };

        var twoParamCall = new List<LangExpression>
        {
            new IntLangValue(1),
            new IntLangValue(2)
        };

        // Act
        var resolvedOne = registry.ResolveMethod(typeof(StringLangValue), "TestMethod", oneParamCall, null);
        var resolvedTwo = registry.ResolveMethod(typeof(StringLangValue), "TestMethod", twoParamCall, null);

        // Assert
        Assert.NotNull(resolvedOne);
        Assert.NotNull(resolvedTwo);
        Assert.Equal(1, resolvedOne.MinParameterCount);
        Assert.Equal(2, resolvedTwo.MinParameterCount);
    }

    [Fact]
    public void TestBaseInstanceMethod_CanAccept_ShouldCheckParameterCount()
    {
        // Arrange
        var method = new TestMethodOneParam();
        var validCall = new List<LangExpression> { new IntLangValue(1) };
        var invalidCall = new List<LangExpression> { new IntLangValue(1), new IntLangValue(2) };

        // Act
        var canAcceptValid = method.CanAccept(validCall, null);
        var canAcceptInvalid = method.CanAccept(invalidCall, null);

        // Assert
        Assert.True(canAcceptValid);
        Assert.False(canAcceptInvalid);
    }

    [Fact]
    public void TestBaseInstanceMethod_CalculateMatchScore_ShouldReturnCorrectScore()
    {
        // Arrange
        var method = new TestMethodOneParam();
        var validCall = new List<LangExpression> { new IntLangValue(1) };
        var invalidCall = new List<LangExpression> { new IntLangValue(1), new IntLangValue(2) };

        // Act
        var validScore = method.CalculateMatchScore(validCall, null);
        var invalidScore = method.CalculateMatchScore(invalidCall, null);

        // Assert
        Assert.True(validScore >= 0);
        Assert.Equal(-1, invalidScore);
    }

    [Fact]
    public void TestOverloadGroup_GetAllSignatures_ShouldReturnAllSignatures()
    {
        // Arrange
        var overloadGroup = new InstanceMethodOverloadGroup("TestMethod", typeof(StringLangValue));
        var method1 = new TestMethodOneParam();
        var method2 = new TestMethodTwoParams();
        overloadGroup.AddOverload(method1);
        overloadGroup.AddOverload(method2);

        // Act
        var signatures = overloadGroup.GetAllSignatures();

        // Assert
        Assert.Equal(2, signatures.Count);
        Assert.All(signatures, sig => Assert.Equal("TestMethod", sig.Name));
    }

    [Fact]
    public void TestInstanceMethodRegistry_BackwardCompatibility_TryGetMethod()
    {
        // Arrange
        var registry = InstanceMethodRegistry.Instance;
        var method1 = new TestMethodOneParam();
        registry.Register(method1);

        // Act
        var method = registry.TryGetMethod(typeof(StringLangValue), "TestMethod");

        // Assert
        Assert.NotNull(method);
        Assert.Equal("TestMethod", method.Names[0]);
    }
}
