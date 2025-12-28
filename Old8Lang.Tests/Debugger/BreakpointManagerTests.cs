using Old8Lang.Debugger;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Debugger;

/// <summary>
/// 断点管理器测试
/// </summary>
public class BreakpointManagerTests
{
    private readonly BreakpointManager Manager = new();
    private readonly VariateManager VariableManager = new();

    [Fact]
    public void AddLineBreakpoint_ShouldCreateBreakpoint()
    {
        // Arrange
        var filePath = "test.old8";
        var line = 10;

        // Act
        var bpId = Manager.AddLineBreakpoint(filePath, line);

        // Assert
        Assert.True(bpId > 0);
        var breakpoints = Manager.GetAllBreakpoints();
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
        var bpId = Manager.AddLineBreakpoint(filePath, line, condition);

        // Assert
        Assert.True(bpId > 0);
        var breakpoints = Manager.GetAllBreakpoints();
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
        var bpId = Manager.AddFunctionBreakpoint(functionName);

        // Assert
        Assert.True(bpId > 0);
        var breakpoints = Manager.GetAllBreakpoints();
        Assert.Single(breakpoints);
        Assert.Equal(BreakpointType.Function, breakpoints[0].Type);
        Assert.Equal(functionName, breakpoints[0].FunctionName);
    }

    [Fact]
    public void RemoveBreakpoint_ShouldRemoveExistingBreakpoint()
    {
        // Arrange
        var bpId = Manager.AddLineBreakpoint("test.old8", 10);

        // Act
        var result = Manager.RemoveBreakpoint(bpId);

        // Assert
        Assert.True(result);
        Assert.Empty(Manager.GetAllBreakpoints());
    }

    [Fact]
    public void RemoveBreakpoint_ShouldReturnFalseForNonExistentBreakpoint()
    {
        // Act
        var result = Manager.RemoveBreakpoint(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CheckBreakpoint_ShouldHitLineBreakpoint()
    {
        // Arrange
        var filePath = "test.old8";
        var line = 10;
        Manager.AddLineBreakpoint(filePath, line);
        var position = new SourcePosition(line, 1);

        // Act
        var hitBreakpoint = Manager.CheckBreakpoint(position, filePath, null, VariableManager);

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
        Manager.AddFunctionBreakpoint(functionName);
        var position = new SourcePosition(5, 1);

        // Act
        var hitBreakpoint = Manager.CheckBreakpoint(position, "test.old8", functionName, VariableManager);

        // Assert
        Assert.NotNull(hitBreakpoint);
        Assert.Equal(functionName, hitBreakpoint.FunctionName);
        Assert.Equal(1, hitBreakpoint.HitCount);
    }

    [Fact]
    public void SetBreakpointEnabled_ShouldToggleBreakpointState()
    {
        // Arrange
        var bpId = Manager.AddLineBreakpoint("test.old8", 10);

        // Act
        var disableResult = Manager.SetBreakpointEnabled(bpId, false);
        var enableResult = Manager.SetBreakpointEnabled(bpId, true);

        // Assert
        Assert.True(disableResult);
        Assert.True(enableResult);
        
        var breakpoint = Manager.GetAllBreakpoints().First();
        Assert.True(breakpoint.IsEnabled);
    }

    [Fact]
    public void ClearAllBreakpoints_ShouldRemoveAllBreakpoints()
    {
        // Arrange
        Manager.AddLineBreakpoint("test1.old8", 10);
        Manager.AddLineBreakpoint("test2.old8", 20);
        Manager.AddFunctionBreakpoint("func1");

        // Act
        Manager.ClearAllBreakpoints();

        // Assert
        Assert.Empty(Manager.GetAllBreakpoints());
    }
}