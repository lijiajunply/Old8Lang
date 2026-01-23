using Microsoft.ML;
using Microsoft.ML.Data;

namespace Old8Lang.MachineLearningLib;

/// <summary>
/// 数据加载器，用于加载和准备机器学习数据
/// </summary>
public class DataLoader(MLContext mlContext)
{
    /// <summary>
    /// 从 CSV 文件加载数据
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="hasHeader">是否有表头</param>
    /// <param name="separatorChar">分隔符</param>
    /// <returns>IDataView</returns>
    public IDataView LoadFromCsv(string filePath, bool hasHeader = true, char separatorChar = ',')
    {
        var dataView = mlContext.Data.LoadFromTextFile<DynamicData>(
            filePath,
            hasHeader: hasHeader,
            separatorChar: separatorChar);

        return dataView;
    }

    /// <summary>
    /// 从内存中的字典列表加载数据
    /// </summary>
    /// <param name="data">数据列表</param>
    /// <returns>IDataView</returns>
    public IDataView LoadFromEnumerable<T>(IEnumerable<T> data) where T : class
    {
        return mlContext.Data.LoadFromEnumerable(data);
    }

    /// <summary>
    /// 拆分数据为训练集和测试集
    /// </summary>
    /// <param name="data">原始数据</param>
    /// <param name="testFraction">测试集比例（默认 0.2）</param>
    /// <returns>训练集和测试集</returns>
    public (IDataView TrainSet, IDataView TestSet) SplitData(IDataView data, double testFraction = 0.2)
    {
        var split = mlContext.Data.TrainTestSplit(data, testFraction: testFraction);
        return (split.TrainSet, split.TestSet);
    }

    /// <summary>
    /// 打乱数据顺序
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="seed">随机种子</param>
    /// <returns>打乱后的数据</returns>
    public IDataView ShuffleData(IDataView data, int? seed = null)
    {
        return mlContext.Data.ShuffleRows(data, seed: seed);
    }

    /// <summary>
    /// 过滤数据
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="columnName">列名</param>
    /// <param name="lowerBound">下界</param>
    /// <param name="upperBound">上界</param>
    /// <returns>过滤后的数据</returns>
    public IDataView FilterData(IDataView data, string columnName, double lowerBound = double.MinValue, double upperBound = double.MaxValue)
    {
        return mlContext.Data.FilterRowsByColumn(data, columnName, lowerBound, upperBound);
    }

    /// <summary>
    /// 动态数据类，用于 CSV 加载
    /// </summary>
    public class DynamicData
    {
        [LoadColumn(0)]
        public float Feature1 { get; set; }

        [LoadColumn(1)]
        public float Feature2 { get; set; }

        [LoadColumn(2)]
        public float Feature3 { get; set; }

        [LoadColumn(3)]
        public float Feature4 { get; set; }

        [LoadColumn(4)]
        public float Label { get; set; }
    }
}
