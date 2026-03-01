using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Performance;

/// <summary>
/// 变量缓存性能测试
/// </summary>
public class VariableCacheTests
{
    [Fact]
    public void VariableCache_BasicOperations_ShouldWork()
    {
        // Arrange
        var cache = new VariableCache(10);

        // Act & Assert - 添加和获取
        cache.Set("x", 42, 0);
        Assert.True(cache.TryGet("x", out var value));
        Assert.Equal(42, value);

        // Assert - 缓存命中
        Assert.Equal(1, cache.HitCount);
        Assert.Equal(0, cache.MissCount);
    }

    [Fact]
    public void VariableCache_CacheMiss_ShouldIncrement()
    {
        // Arrange
        var cache = new VariableCache(10);

        // Act
        var found = cache.TryGet("nonexistent", out _);

        // Assert
        Assert.False(found);
        Assert.Equal(0, cache.HitCount);
        Assert.Equal(1, cache.MissCount);
    }

    [Fact]
    public void VariableCache_LRU_ShouldEvictLeastRecentlyUsed()
    {
        // Arrange
        var cache = new VariableCache(3); // 小缓存用于测试

        // Act - 添加4个变量，应该淘汰第一个
        cache.Set("a", 1, 0);
        cache.Set("b", 2, 0);
        cache.Set("c", 3, 0);
        cache.Set("d", 4, 0); // 这会淘汰 "a"

        // Assert
        Assert.False(cache.TryGet("a", out _)); // "a" 应该被淘汰
        Assert.True(cache.TryGet("b", out _));
        Assert.True(cache.TryGet("c", out _));
        Assert.True(cache.TryGet("d", out _));
    }

    [Fact]
    public void VariableCache_ClearScope_ShouldRemoveScopeVariables()
    {
        // Arrange
        var cache = new VariableCache(10);
        cache.Set("global", 1, 0);
        cache.Set("local1", 2, 1);
        cache.Set("local2", 3, 2);

        // Act - 清除作用域1及以上
        cache.ClearScope(1);

        // Assert
        Assert.True(cache.TryGet("global", out _)); // 全局变量保留
        Assert.False(cache.TryGet("local1", out _)); // 作用域1变量被清除
        Assert.False(cache.TryGet("local2", out _)); // 作用域2变量被清除
    }

    [Fact]
    public void VariableCache_HitRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var cache = new VariableCache(10);
        cache.Set("x", 42, 0);

        // Act
        cache.TryGet("x", out _); // 命中
        cache.TryGet("x", out _); // 命中
        cache.TryGet("y", out _); // 未命中
        cache.TryGet("z", out _); // 未命中

        // Assert
        Assert.Equal(2, cache.HitCount);
        Assert.Equal(2, cache.MissCount);
        Assert.Equal(0.5, cache.HitRate); // 2/4 = 0.5
    }

    [Fact]
    public void VariateManager_EnhancedCache_ShouldWork()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var manager = interpreter.Manager;
        manager.EnableEnhancedCache(100);

        // Act - 设置和获取变量
        var id = new LangId("testVar");
        manager.Set(id, new AST.Expression.Value.IntLangValue(42));
        var value = manager.GetValue(id);

        // Assert
        Assert.NotNull(value);
        Assert.IsType<AST.Expression.Value.IntLangValue>(value);
        Assert.Equal(42, ((AST.Expression.Value.IntLangValue)value).Value);

        // 检查缓存统计
        var (hitCount, missCount, hitRate) = manager.GetCacheStats();
        Assert.True(hitCount >= 0);
        Assert.True(missCount >= 0);
    }

    [Fact]
    public void VariateManager_GlobalVariableCache_ShouldWork()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var manager = interpreter.Manager;
        manager.EnableEnhancedCache(100);

        // Act - 在全局作用域设置变量
        var id = new LangId("globalVar");
        manager.Set(id, new AST.Expression.Value.StringLangValue("test"));

        // 进入新作用域
        manager.AddChildren();

        // 从新作用域访问全局变量
        var value = manager.GetValue(id);

        // Assert
        Assert.NotNull(value);
        Assert.IsType<AST.Expression.Value.StringLangValue>(value);
        Assert.Equal("test", ((AST.Expression.Value.StringLangValue)value).Value);
    }
}
