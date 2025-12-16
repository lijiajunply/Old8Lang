using Old8LangLib;

namespace Old8Lang.Tests.Library;

public class CollectionLibTests
{
    // ========== 列表操作测试 ==========

    [Fact]
    public void ListFilter_ShouldFilterElements()
    {
        // 测试 ListFilter 功能
        var list = new List<int> { 1, 2, 3, 4, 5 };

        // 过滤偶数
        var result = CollectionLib.ListFilter(list, x => x % 2 == 0);

        // 验证结果
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(2, result);
        Assert.Contains(4, result);
    }

    [Fact]
    public void ListMap_ShouldTransformElements()
    {
        // 测试 ListMap 功能
        var list = new List<int> { 1, 2, 3 };

        // 将每个元素转换为字符串
        var result = CollectionLib.ListMap(list, x => x.ToString());

        // 验证结果
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("1", result[0]);
        Assert.Equal("2", result[1]);
        Assert.Equal("3", result[2]);
    }

    [Fact]
    public void ListFold_ShouldFoldElements()
    {
        // 测试 ListFold 功能
        var list = new List<int> { 1, 2, 3, 4, 5 };

        // 计算总和
        var result = CollectionLib.ListFold(list, 0, (acc, x) => acc + x);

        // 验证结果
        Assert.Equal(15, result);
    }

    [Fact]
    public void ListFind_ShouldFindFirstMatchingElement()
    {
        // 测试 ListFind 功能
        var list = new List<int> { 1, 2, 3, 4, 5 };

        // 查找第一个偶数
        var result = CollectionLib.ListFind(list, x => x % 2 == 0);

        // 验证结果
        Assert.Equal(2, result);
    }

    [Fact]
    public void ListSort_ShouldSortElements()
    {
        // 测试 ListSort 功能
        var list = new List<int> { 3, 1, 4, 1, 5, 9, 2, 6 };

        // 排序
        var result = CollectionLib.ListSort(list);

        // 验证结果
        Assert.NotNull(result);
        Assert.Equal(8, result.Count);
        Assert.Equal(1, result[0]);
        Assert.Equal(1, result[1]);
        Assert.Equal(2, result[2]);
        Assert.Equal(3, result[3]);
        Assert.Equal(4, result[4]);
        Assert.Equal(5, result[5]);
        Assert.Equal(6, result[6]);
        Assert.Equal(9, result[7]);
    }

    [Fact]
    public void ListReverse_ShouldReverseElements()
    {
        // 测试 ListReverse 功能
        var list = new List<int> { 1, 2, 3, 4, 5 };

        // 反转
        var result = CollectionLib.ListReverse(list);

        // 验证结果
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        Assert.Equal(5, result[0]);
        Assert.Equal(4, result[1]);
        Assert.Equal(3, result[2]);
        Assert.Equal(2, result[3]);
        Assert.Equal(1, result[4]);
    }

    [Fact]
    public void ListDistinct_ShouldRemoveDuplicates()
    {
        // 测试 ListDistinct 功能
        var list = new List<int> { 1, 2, 2, 3, 3, 3, 4, 5 };

        // 去重
        var result = CollectionLib.ListDistinct(list);

        // 验证结果
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        Assert.Equal(1, result[0]);
        Assert.Equal(2, result[1]);
        Assert.Equal(3, result[2]);
        Assert.Equal(4, result[3]);
        Assert.Equal(5, result[4]);
    }

    [Fact]
    public void ListTake_ShouldTakeFirstNElements()
    {
        // 测试 ListTake 功能
        var list = new List<int> { 1, 2, 3, 4, 5 };

        // 取前3个元素
        var result = CollectionLib.ListTake(list, 3);

        // 验证结果
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0]);
        Assert.Equal(2, result[1]);
        Assert.Equal(3, result[2]);
    }

    [Fact]
    public void ListSkip_ShouldSkipFirstNElements()
    {
        // 测试 ListSkip 功能
        var list = new List<int> { 1, 2, 3, 4, 5 };

        // 跳过前2个元素
        var result = CollectionLib.ListSkip(list, 2);

        // 验证结果
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(3, result[0]);
        Assert.Equal(4, result[1]);
        Assert.Equal(5, result[2]);
    }

    // ========== 字典操作测试 ==========

    [Fact]
    public void DictMerge_ShouldMergeDictionaries()
    {
        // 测试 DictMerge 功能
        var dict1 = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
        var dict2 = new Dictionary<string, int> { { "b", 3 }, { "c", 4 } };

        // 合并字典
        var result = CollectionLib.DictMerge(dict1, dict2);

        // 验证结果
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result["a"]);
        Assert.Equal(3, result["b"]); // 第二个字典的值覆盖第一个
        Assert.Equal(4, result["c"]);
    }

    [Fact]
    public void DictFilter_ShouldFilterDictionary()
    {
        // 测试 DictFilter 功能
        var dict = new Dictionary<string, int> { { "a", 1 }, { "b", 2 }, { "c", 3 } };

        // 过滤值大于1的键值对
        var result = CollectionLib.DictFilter(dict, pair => pair.Value > 1);

        // 验证结果
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("b", result);
        Assert.Contains("c", result);
    }

    [Fact]
    public void DictKeys_ShouldReturnKeys()
    {
        // 测试 DictKeys 功能
        var dict = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };

        // 获取键列表
        var result = CollectionLib.DictKeys(dict);

        // 验证结果
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("a", result);
        Assert.Contains("b", result);
    }

    [Fact]
    public void DictValues_ShouldReturnValues()
    {
        // 测试 DictValues 功能
        var dict = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };

        // 获取值列表
        var result = CollectionLib.DictValues(dict);

        // 验证结果
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(1, result);
        Assert.Contains(2, result);
    }

    // ========== 集合操作测试 ==========

    [Fact]
    public void SetIntersection_ShouldReturnIntersection()
    {
        // 测试 SetIntersection 功能
        var set1 = new HashSet<int> { 1, 2, 3, 4 };
        var set2 = new HashSet<int> { 3, 4, 5, 6 };

        // 计算交集
        var result = CollectionLib.SetIntersection(set1, set2);

        // 验证结果
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(3, result);
        Assert.Contains(4, result);
    }

    [Fact]
    public void SetUnion_ShouldReturnUnion()
    {
        // 测试 SetUnion 功能
        var set1 = new HashSet<int> { 1, 2, 3 };
        var set2 = new HashSet<int> { 3, 4, 5 };

        // 计算并集
        var result = CollectionLib.SetUnion(set1, set2);

        // 验证结果
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        Assert.Contains(1, result);
        Assert.Contains(2, result);
        Assert.Contains(3, result);
        Assert.Contains(4, result);
        Assert.Contains(5, result);
    }

    [Fact]
    public void SetDifference_ShouldReturnDifference()
    {
        // 测试 SetDifference 功能
        var set1 = new HashSet<int> { 1, 2, 3, 4 };
        var set2 = new HashSet<int> { 3, 4, 5, 6 };

        // 计算差集
        var result = CollectionLib.SetDifference(set1, set2);

        // 验证结果
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(1, result);
        Assert.Contains(2, result);
    }

    // ========== 通用集合操作测试 ==========

    [Fact]
    public void IsEmpty_ShouldReturnTrueForEmptyCollection()
    {
        // 测试 IsEmpty 功能
        var list = new List<int>();

        // 检查是否为空
        var result = CollectionLib.IsEmpty(list);

        // 验证结果
        Assert.True(result);
    }

    [Fact]
    public void Length_ShouldReturnCorrectLength()
    {
        // 测试 Length 功能
        var list = new List<int> { 1, 2, 3, 4, 5 };

        // 获取长度
        var result = CollectionLib.Length(list);

        // 验证结果
        Assert.Equal(5, result);
    }

    [Fact]
    public void ToArray_ShouldConvertToCorrectArray()
    {
        // 测试 ToArray 功能
        var list = new List<int> { 1, 2, 3 };

        // 转换为数组
        var result = CollectionLib.ToArray(list);

        // 验证结果
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Equal(1, result[0]);
        Assert.Equal(2, result[1]);
        Assert.Equal(3, result[2]);
    }

    [Fact]
    public void ToHashSet_ShouldConvertToHashSet()
    {
        // 测试 ToHashSet 功能
        var list = new List<int> { 1, 2, 2, 3 };

        // 转换为哈希集合
        var result = CollectionLib.ToHashSet(list);

        // 验证结果
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(1, result);
        Assert.Contains(2, result);
        Assert.Contains(3, result);
    }
}