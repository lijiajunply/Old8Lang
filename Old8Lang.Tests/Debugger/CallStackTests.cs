using Xunit;
using Old8Lang.Debugger;

namespace Old8Lang.Tests.Debugger;

/// <summary>
/// 调用栈测试
/// </summary>
public class CallStackTests
{
    private readonly CallStack _callStack;

    public CallStackTests()
    {
        _callStack = new CallStack();
    }

    [Fact]
    public void PushFrame_ShouldAddFrameToStack()
    {
        // Arrange
        var frame = new StackFrame
        {
            FunctionName = "testFunction",
            FilePath = "test.old8",
            Line = 10,
            Column = 5
        };

        // Act
        _callStack.PushFrame(frame);

        // Assert
        Assert.Equal(1, _callStack.Depth);
        Assert.Equal(frame, _callStack.CurrentFrame);
    }

    [Fact]
    public void PopFrame_ShouldRemoveAndReturnFrame()
    {
        // Arrange
        var frame = new StackFrame
        {
            FunctionName = "testFunction",
            FilePath = "test.old8",
            Line = 10,
            Column = 5
        };
        _callStack.PushFrame(frame);

        // Act
        var poppedFrame = _callStack.PopFrame();

        // Assert
        Assert.Equal(0, _callStack.Depth);
        Assert.Equal(frame, poppedFrame);
        Assert.Null(_callStack.CurrentFrame);
    }

    [Fact]
    public void PopFrame_ShouldReturnNullForEmptyStack()
    {
        // Act
        var result = _callStack.PopFrame();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAllFrames_ShouldReturnFramesInCorrectOrder()
    {
        // Arrange
        var frame1 = new StackFrame { FunctionName = "main" };
        var frame2 = new StackFrame { FunctionName = "func1" };
        var frame3 = new StackFrame { FunctionName = "func2" };

        // Act
        _callStack.PushFrame(frame1);
        _callStack.PushFrame(frame2);
        _callStack.PushFrame(frame3);

        var frames = _callStack.GetAllFrames();

        // Assert
        Assert.Equal(3, frames.Count);
        Assert.Equal(frame3, frames[0]); // 栈顶
        Assert.Equal(frame2, frames[1]);
        Assert.Equal(frame1, frames[2]); // 栈底
    }

    [Fact]
    public void Clear_ShouldRemoveAllFrames()
    {
        // Arrange
        _callStack.PushFrame(new StackFrame { FunctionName = "main" });
        _callStack.PushFrame(new StackFrame { FunctionName = "func1" });

        // Act
        _callStack.Clear();

        // Assert
        Assert.Equal(0, _callStack.Depth);
        Assert.Null(_callStack.CurrentFrame);
        Assert.Empty(_callStack.GetAllFrames());
    }

    [Fact]
    public void CurrentFrame_ShouldReturnTopFrame()
    {
        // Arrange
        var frame1 = new StackFrame { FunctionName = "main" };
        var frame2 = new StackFrame { FunctionName = "func1" };

        // Act & Assert
        _callStack.PushFrame(frame1);
        Assert.Equal(frame1, _callStack.CurrentFrame);

        _callStack.PushFrame(frame2);
        Assert.Equal(frame2, _callStack.CurrentFrame);
    }
}