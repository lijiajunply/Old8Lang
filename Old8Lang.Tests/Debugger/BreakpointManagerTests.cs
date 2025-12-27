using Xunit;
using Old8Lang.Debugger;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Debugger;

/// <summary>
/// 断点管理器测试
/// </summary>
public class BreakpointManagerTests
{
    private readonly BreakpointManager _manager;
    private readonly VariateManager _variableManager;

    public BreakpointManagerTests()
    {
        _manager = new BreakpointManager();
        _variableManager = new VariateManager();
    }

    [Fact]
    public void AddLineBreakpoint_ShouldCreateBreakpoint()
    {
        // Arrange
        var filePath = "test.old8";
        var line = 10;

        // Act
        var bpId = _manager.AddLineBreakpoint(filePath, line);

        // Assert
        Assert.True(bpId > 0);
        var breakpoints = _manager.GetAllBreakpoints();
        Assert.Single(breakpoints);
        Assert.Equal(BreakpointType.Line, breakpoints[0].Type);
        Assert.Equal(filePath, breakpoints[0].FilePath);
        Assert.Equal(line, breakpoints[0].Line);
    }

    [Fact]
    public void AddConditionalBreakpoint_ShouldCreateConditionalBreakpoint()
    {
        // Arrange
        var filePath = "test.old8";
        var line = 10;
        var condition = "x > 5";

        // Act
        var bpId = _manager.AddLineBreakpoint(filePath, line, condition);

        // Assert
        Assert.True(bpId > 0);
        var breakpoints = _manager.GetAllBreakpoints();
        Assert.Single(breakpoints);
        Assert.Equal(BreakpointType.Conditional, breakpoints[0].Type);
        Assert.Equal(condition, breakpoints[0].Condition);
    }

    [Fact]
    public void AddFunctionBreakpoint_ShouldCreateFunctionBreakpoint()
    {
        // Arrange
        var functionName = "testFunction";

        // Act
        var bpId = _manager.AddFunctionBreakpoint(functionName);

        // Assert
        Assert.True(bpId > 0);
        var breakpoints = _manager.GetAllBreakpoints();
        Assert.Single(breakpoints);
        Assert.Equal(BreakpointType.Function, breakpoints[0].Type);
        Assert.Equal(functionName, breakpoints[0].FunctionName);
    }

    [Fact]
    public void RemoveBreakpoint_ShouldRemoveExistingBreakpoint()
    {
        // Arrange
        var bpId = _manager.AddLineBreakpoint("test.old8", 10);

        // Act
        var result = _manager.RemoveBreakpoint(bpId);

        // Assert
        Assert.True(result);
        Assert.Empty(_manager.GetAllBreakpoints());
    }

    [Fact]
    public void RemoveBreakpoint_ShouldReturnFalseForNonExistentBreakpoint()
    {
        // Act
        var result = _manager.RemoveBreakpoint(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CheckBreakpoint_ShouldHitLineBreakpoint()
    {
        // Arrange
        var filePath = "test.old8";
        var line = 10;
        _manager.AddLineBreakpoint(filePath, line);
        var position = new SourcePosition(line, 1);

        // Act
        var hitBreakpoint = _manager.CheckBreakpoint(position, filePath, null, _variableManager);

        // Assert
        Assert.NotNull(hitBreakpoint);
        Assert.Equal(filePath, hitBreakpoint.FilePath);
        Assert.Equal(line, hitBreakpoint.Line);
        Assert.Equal(1, hitBreakpoint.HitCount);
    }

    [Fact]
    public void CheckBreakpoint_ShouldHitFunctionBreakpoint()
    {
        // Arrange
        var functionName = "testFunction";
        _manager.AddFunctionBreakpoint(functionName);
        var position = new SourcePosition(5, 1);

        // Act
        var hitBreakpoint = _manager.CheckBreakpoint(position, "test.old8", functionName, _variableManager);

        // Assert
        Assert.NotNull(hitBreakpoint);
        Assert.Equal(functionName, hitBreakpoint.FunctionName);
        Assert.Equal(1, hitBreakpoint.HitCount);
    }

    [Fact]
    public void SetBreakpointEnabled_ShouldToggleBreakpointState()
    {
        // Arrange
        var bpId = _manager.AddLineBreakpoint("test.old8", 10);

        // Act
        var disableResult = _manager.SetBreakpointEnabled(bpId, false);
        var enableResult = _manager.SetBreakpointEnabled(bpId, true);

        // Assert
        Assert.True(disableResult);
        Assert.True(enableResult);
        
        var breakpoint = _manager.GetAllBreakpoints().First();
        Assert.True(breakpoint.IsEnabled);
    }

    [Fact]
    public void ClearAllBreakpoints_ShouldRemoveAllBreakpoints()
    {
        // Arrange
        _manager.AddLineBreakpoint("test1.old8", 10);
        _manager.AddLineBreakpoint("test2.old8", 20);
        _manager.AddFunctionBreakpoint("func1");

        // Act
        _manager.ClearAllBreakpoints();

        // Assert
        Assert.Empty(_manager.GetAllBreakpoints());
    }
}