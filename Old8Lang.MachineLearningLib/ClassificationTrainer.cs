using Microsoft.ML;
using Microsoft.ML.Data;

namespace Old8Lang.MachineLearningLib;

/// <summary>
/// 分类模型训练器，支持多种分类算法
/// </summary>
public class ClassificationTrainer
{
    private readonly MLContext _mlContext;

    public ClassificationTrainer(MLContext mlContext)
    {
        _mlContext = mlContext;
    }

    /// <summary>
    /// 使用逻辑回归训练二分类模型
    /// </summary>
    /// <param name="trainData">训练数据</param>
    /// <param name="labelColumn">标签列名</param>
    /// <param name="featureColumns">特征列名</param>
    /// <returns>训练好的模型</returns>
    public ITransformer TrainLogisticRegression(IDataView trainData, string labelColumn = "Label", params string[] featureColumns)
    {
        var pipeline = CreateFeaturePipeline(featureColumns)
            .Append(_mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(
                labelColumnName: labelColumn,
                featureColumnName: "Features"));

        return pipeline.Fit(trainData);
    }

    /// <summary>
    /// 使用快速树训练二分类模型
    /// </summary>
    /// <param name="trainData">训练数据</param>
    /// <param name="labelColumn">标签列名</param>
    /// <param name="featureColumns">特征列名</param>
    /// <param name="numberOfTrees">树的数量</param>
    /// <param name="numberOfLeaves">叶子节点数量</param>
    /// <returns>训练好的模型</returns>
    public ITransformer TrainFastTree(IDataView trainData, string labelColumn = "Label", int numberOfTrees = 100, int numberOfLeaves = 20, params string[] featureColumns)
    {
        var pipeline = CreateFeaturePipeline(featureColumns)
            .Append(_mlContext.BinaryClassification.Trainers.FastTree(
                labelColumnName: labelColumn,
                featureColumnName: "Features",
                numberOfTrees: numberOfTrees,
                numberOfLeaves: numberOfLeaves));

        return pipeline.Fit(trainData);
    }

    /// <summary>
    /// 使用 LightGBM 训练二分类模型
    /// </summary>
    /// <param name="trainData">训练数据</param>
    /// <param name="labelColumn">标签列名</param>
    /// <param name="featureColumns">特征列名</param>
    /// <param name="numberOfIterations">迭代次数</param>
    /// <param name="learningRate">学习率</param>
    /// <returns>训练好的模型</returns>
    public ITransformer TrainLightGbm(IDataView trainData, string labelColumn = "Label", int numberOfIterations = 100, double learningRate = 0.1, params string[] featureColumns)
    {
        var pipeline = CreateFeaturePipeline(featureColumns)
            .Append(_mlContext.BinaryClassification.Trainers.LightGbm(
                labelColumnName: labelColumn,
                featureColumnName: "Features",
                numberOfIterations: numberOfIterations,
                learningRate: learningRate));

        return pipeline.Fit(trainData);
    }

    /// <summary>
    /// 使用 SDCA 训练多分类模型
    /// </summary>
    /// <param name="trainData">训练数据</param>
    /// <param name="labelColumn">标签列名</param>
    /// <param name="featureColumns">特征列名</param>
    /// <returns>训练好的模型</returns>
    public ITransformer TrainMulticlassSdca(IDataView trainData, string labelColumn = "Label", params string[] featureColumns)
    {
        var pipeline = CreateFeaturePipeline(featureColumns)
            .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                labelColumnName: labelColumn,
                featureColumnName: "Features"));

        return pipeline.Fit(trainData);
    }

    /// <summary>
    /// 使用 LightGBM 训练多分类模型
    /// </summary>
    /// <param name="trainData">训练数据</param>
    /// <param name="labelColumn">标签列名</param>
    /// <param name="featureColumns">特征列名</param>
    /// <param name="numberOfIterations">迭代次数</param>
    /// <returns>训练好的模型</returns>
    public ITransformer TrainMulticlassLightGbm(IDataView trainData, string labelColumn = "Label", int numberOfIterations = 100, params string[] featureColumns)
    {
        var pipeline = CreateFeaturePipeline(featureColumns)
            .Append(_mlContext.MulticlassClassification.Trainers.LightGbm(
                labelColumnName: labelColumn,
                featureColumnName: "Features",
                numberOfIterations: numberOfIterations));

        return pipeline.Fit(trainData);
    }

    /// <summary>
    /// 评估二分类模型
    /// </summary>
    /// <param name="model">模型</param>
    /// <param name="testData">测试数据</param>
    /// <param name="labelColumn">标签列名</param>
    /// <returns>评估指标</returns>
    public CalibratedBinaryClassificationMetrics EvaluateBinaryClassification(ITransformer model, IDataView testData, string labelColumn = "Label")
    {
        var predictions = model.Transform(testData);
        return _mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: labelColumn);
    }

    /// <summary>
    /// 评估多分类模型
    /// </summary>
    /// <param name="model">模型</param>
    /// <param name="testData">测试数据</param>
    /// <param name="labelColumn">标签列名</param>
    /// <returns>评估指标</returns>
    public MulticlassClassificationMetrics EvaluateMulticlassClassification(ITransformer model, IDataView testData, string labelColumn = "Label")
    {
        var predictions = model.Transform(testData);
        return _mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: labelColumn);
    }

    /// <summary>
    /// 创建特征工程管道
    /// </summary>
    private IEstimator<ITransformer> CreateFeaturePipeline(params string[] featureColumns)
    {
        if (featureColumns == null || featureColumns.Length == 0)
        {
            // 默认特征列
            featureColumns = ["Feature1", "Feature2", "Feature3", "Feature4"];
        }

        return _mlContext.Transforms.Concatenate("Features", featureColumns)
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"));
    }
}
