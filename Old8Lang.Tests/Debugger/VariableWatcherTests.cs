using Xunit;
using Old8Lang.Debugger;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Debugger;

/// <summary>
/// 变量监视器测试
/// </summary>
public class VariableWatcherTests
{
    private readonly VariableWatcher Watcher;
    private readonly VariateManager VariableManager;

    public VariableWatcherTests()
    {
        Watcher = new VariableWatcher();
        VariableManager = new VariateManager();
        
        // 添加一些测试变量
        VariableManager.Set(new LangId("x"), new IntLangValue(10));
        VariableManager.Set(new LangId("name"), new StringLangValue("test"));
    }

    [Fact]
    public void AddWatch_ShouldCreateWatchedVariable()
    {
        // Arrange & Act
        var watch = Watcher.AddWatch("x", VariableManager);

        // Assert
        Assert.NotNull(watch);
        Assert.Equal("x", watch.Expression);
        Assert.Equal("x", watch.Name);
        Assert.Equal("10", watch.CurrentValue);
        Assert.Equal("IntLangValue", watch.VariableType);
    }

    [Fact]
    public void GetAllWatches_ShouldReturnAllWatches()
    {
        // Arrange
        Watcher.AddWatch("x", VariableManager);
        Watcher.AddWatch("name", VariableManager);

        // Act
        var watches = Watcher.GetAllWatches();

        // Assert
        Assert.Equal(2, watches.Count);
        Assert.Contains(watches, w => w.Expression == "x");
        Assert.Contains(watches, w => w.Expression == "name");
    }

    [Fact]
    public void RemoveWatch_ShouldRemoveExistingWatch()
    {
        // Arrange
        Watcher.AddWatch("x", VariableManager);

        // Act
        var result = Watcher.RemoveWatch("x");

        // Assert
        Assert.True(result);
        Assert.Empty(Watcher.GetAllWatches());
    }

    [Fact]
    public void RemoveWatch_ShouldReturnFalseForNonExistentWatch()
    {
        // Act
        var result = Watcher.RemoveWatch("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UpdateAllWatches_ShouldUpdateValues()
    {
        // Arrange
        var watch = Watcher.AddWatch("x", VariableManager);
        var originalValue = watch.CurrentValue;

        // 修改变量值
        VariableManager.Set(new LangId("x"), new IntLangValue(20));

        // Act
        Watcher.UpdateAllWatches(VariableManager);

        // Assert
        var updatedWatch = Watcher.GetAllWatches().First();
        Assert.Equal("20", updatedWatch.CurrentValue);
        Assert.NotEqual(originalValue, updatedWatch.CurrentValue);
    }

    [Fact]
    public void SetWatchEnabled_ShouldToggleWatchState()
    {
        // Arrange
        Watcher.AddWatch("x", VariableManager);

        // Act
        var disableResult = Watcher.SetWatchEnabled("x", false);
        var enableResult = Watcher.SetWatchEnabled("x", true);

        // Assert
        Assert.True(disableResult);
        Assert.True(enableResult);
        
        var watch = Watcher.GetAllWatches().First();
        Assert.True(watch.IsEnabled);
    }

    [Fact]
    public void ClearAllWatches_ShouldRemoveAllWatches()
    {
        // Arrange
        Watcher.AddWatch("x", VariableManager);
        Watcher.AddWatch("name", VariableManager);

        // Act
        Watcher.ClearAllWatches();

        // Assert
        Assert.Empty(Watcher.GetAllWatches());
    }
}