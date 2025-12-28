using Old8Lang.Debugger;

namespace Old8Lang.Tests.Debugger;

/// <summary>
/// 调用栈测试
/// </summary>
public class CallStackTests
{
    private readonly CallStack CallStack = new();

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
        CallStack.PushFrame(frame);

        // Assert
        Assert.Equal(1, CallStack.Depth);
        Assert.Equal(frame, CallStack.CurrentFrame);
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
        CallStack.PushFrame(frame);

        // Act
        var poppedFrame = CallStack.PopFrame();

        // Assert
        Assert.Equal(0, CallStack.Depth);
        Assert.Equal(frame, poppedFrame);
        Assert.Null(CallStack.CurrentFrame);
    }

    [Fact]
    public void PopFrame_ShouldReturnNullForEmptyStack()
    {
        // Act
        var result = CallStack.PopFrame();

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
        CallStack.PushFrame(frame1);
        CallStack.PushFrame(frame2);
        CallStack.PushFrame(frame3);

        var frames = CallStack.GetAllFrames();

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
        CallStack.PushFrame(new StackFrame { FunctionName = "main" });
        CallStack.PushFrame(new StackFrame { FunctionName = "func1" });

        // Act
        CallStack.Clear();

        // Assert
        Assert.Equal(0, CallStack.Depth);
        Assert.Null(CallStack.CurrentFrame);
        Assert.Empty(CallStack.GetAllFrames());
    }

    [Fact]
    public void CurrentFrame_ShouldReturnTopFrame()
    {
        // Arrange
        var frame1 = new StackFrame { FunctionName = "main" };
        var frame2 = new StackFrame { FunctionName = "func1" };

        // Act & Assert
        CallStack.PushFrame(frame1);
        Assert.Equal(frame1, CallStack.CurrentFrame);

        CallStack.PushFrame(frame2);
        Assert.Equal(frame2, CallStack.CurrentFrame);
    }
}