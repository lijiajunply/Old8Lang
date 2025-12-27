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
    private readonly VariableWatcher _watcher;
    private readonly VariateManager _variableManager;

    public VariableWatcherTests()
    {
        _watcher = new VariableWatcher();
        _variableManager = new VariateManager();
        
        // 添加一些测试变量
        _variableManager.Set(new LangId("x"), new AST.Expression.Value.IntLangValue(10));
        _variableManager.Set(new LangId("name"), new AST.Expression.Value.StringLangValue("test"));
    }

    [Fact]
    public void AddWatch_ShouldCreateWatchedVariable()
    {
        // Arrange & Act
        var watch = _watcher.AddWatch("x", _variableManager);

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
        _watcher.AddWatch("x", _variableManager);
        _watcher.AddWatch("name", _variableManager);

        // Act
        var watches = _watcher.GetAllWatches();

        // Assert
        Assert.Equal(2, watches.Count);
        Assert.Contains(watches, w => w.Expression == "x");
        Assert.Contains(watches, w => w.Expression == "name");
    }

    [Fact]
    public void RemoveWatch_ShouldRemoveExistingWatch()
    {
        // Arrange
        _watcher.AddWatch("x", _variableManager);

        // Act
        var result = _watcher.RemoveWatch("x");

        // Assert
        Assert.True(result);
        Assert.Empty(_watcher.GetAllWatches());
    }

    [Fact]
    public void RemoveWatch_ShouldReturnFalseForNonExistentWatch()
    {
        // Act
        var result = _watcher.RemoveWatch("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UpdateAllWatches_ShouldUpdateValues()
    {
        // Arrange
        var watch = _watcher.AddWatch("x", _variableManager);
        var originalValue = watch.CurrentValue;

        // 修改变量值
        _variableManager.Set(new LangId("x"), new AST.Expression.Value.IntLangValue(20));

        // Act
        _watcher.UpdateAllWatches(_variableManager);

        // Assert
        var updatedWatch = _watcher.GetAllWatches().First();
        Assert.Equal("20", updatedWatch.CurrentValue);
        Assert.NotEqual(originalValue, updatedWatch.CurrentValue);
    }

    [Fact]
    public void SetWatchEnabled_ShouldToggleWatchState()
    {
        // Arrange
        _watcher.AddWatch("x", _variableManager);

        // Act
        var disableResult = _watcher.SetWatchEnabled("x", false);
        var enableResult = _watcher.SetWatchEnabled("x", true);

        // Assert
        Assert.True(disableResult);
        Assert.True(enableResult);
        
        var watch = _watcher.GetAllWatches().First();
        Assert.True(watch.IsEnabled);
    }

    [Fact]
    public void ClearAllWatches_ShouldRemoveAllWatches()
    {
        // Arrange
        _watcher.AddWatch("x", _variableManager);
        _watcher.AddWatch("name", _variableManager);

        // Act
        _watcher.ClearAllWatches();

        // Assert
        Assert.Empty(_watcher.GetAllWatches());
    }
}