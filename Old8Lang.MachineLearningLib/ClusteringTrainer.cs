using Microsoft.ML;
using Microsoft.ML.Data;

namespace Old8Lang.MachineLearningLib;

/// <summary>
/// 聚类模型训练器，支持多种聚类算法
/// </summary>
public class ClusteringTrainer
{
    private readonly MLContext _mlContext;

    public ClusteringTrainer(MLContext mlContext)
    {
        _mlContext = mlContext;
    }

    /// <summary>
    /// 使用 K-Means 训练聚类模型
    /// </summary>
    /// <param name="trainData">训练数据</param>
    /// <param name="numberOfClusters">聚类数量</param>
    /// <param name="featureColumns">特征列名</param>
    /// <returns>训练好的模型</returns>
    public ITransformer TrainKMeans(IDataView trainData, int numberOfClusters = 3, params string[] featureColumns)
    {
        var pipeline = CreateFeaturePipeline(featureColumns)
            .Append(_mlContext.Clustering.Trainers.KMeans(
                featureColumnName: "Features",
                numberOfClusters: numberOfClusters));

        return pipeline.Fit(trainData);
    }

    /// <summary>
    /// 评估聚类模型
    /// </summary>
    /// <param name="model">模型</param>
    /// <param name="testData">测试数据</param>
    /// <returns>评估指标</returns>
    public ClusteringMetrics Evaluate(ITransformer model, IDataView testData)
    {
        var predictions = model.Transform(testData);
        return _mlContext.Clustering.Evaluate(predictions);
    }

    /// <summary>
    /// 创建特征工程管道
    /// </summary>
    private IEstimator<ITransformer> CreateFeaturePipeline(params string[] featureColumns)
    {
        if (featureColumns == null || featureColumns.Length == 0)
        {
            // 默认特征列
            featureColumns = new[] { "Feature1", "Feature2", "Feature3", "Feature4" };
        }

        return _mlContext.Transforms.Concatenate("Features", featureColumns)
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"));
    }
}
