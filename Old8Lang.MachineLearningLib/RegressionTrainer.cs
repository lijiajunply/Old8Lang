using Microsoft.ML;
using Microsoft.ML.Data;

namespace Old8Lang.MachineLearningLib;

/// <summary>
/// 回归模型训练器，支持多种回归算法
/// </summary>
public class RegressionTrainer(MLContext mlContext)
{
    /// <summary>
    /// 使用 SDCA 训练回归模型
    /// </summary>
    /// <param name="trainData">训练数据</param>
    /// <param name="labelColumn">标签列名</param>
    /// <param name="featureColumns">特征列名</param>
    /// <returns>训练好的模型</returns>
    public ITransformer TrainSdca(IDataView trainData, string labelColumn = "Label", params string[] featureColumns)
    {
        var pipeline = CreateFeaturePipeline(featureColumns)
            .Append(mlContext.Regression.Trainers.Sdca(
                labelColumnName: labelColumn,
                featureColumnName: "Features"));

        return pipeline.Fit(trainData);
    }

    /// <summary>
    /// 使用快速树训练回归模型
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
            .Append(mlContext.Regression.Trainers.FastTree(
                labelColumnName: labelColumn,
                featureColumnName: "Features",
                numberOfTrees: numberOfTrees,
                numberOfLeaves: numberOfLeaves));

        return pipeline.Fit(trainData);
    }

    /// <summary>
    /// 使用 FastTreeTweedie 训练回归模型
    /// </summary>
    /// <param name="trainData">训练数据</param>
    /// <param name="labelColumn">标签列名</param>
    /// <param name="featureColumns">特征列名</param>
    /// <param name="numberOfTrees">树的数量</param>
    /// <returns>训练好的模型</returns>
    public ITransformer TrainFastTreeTweedie(IDataView trainData, string labelColumn = "Label", int numberOfTrees = 100, params string[] featureColumns)
    {
        var pipeline = CreateFeaturePipeline(featureColumns)
            .Append(mlContext.Regression.Trainers.FastTreeTweedie(
                labelColumnName: labelColumn,
                featureColumnName: "Features",
                numberOfTrees: numberOfTrees));

        return pipeline.Fit(trainData);
    }

    /// <summary>
    /// 使用 LightGBM 训练回归模型
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
            .Append(mlContext.Regression.Trainers.LightGbm(
                labelColumnName: labelColumn,
                featureColumnName: "Features",
                numberOfIterations: numberOfIterations,
                learningRate: learningRate));

        return pipeline.Fit(trainData);
    }

    /// <summary>
    /// 使用 LbfgsPoissonRegression 训练回归模型
    /// </summary>
    /// <param name="trainData">训练数据</param>
    /// <param name="labelColumn">标签列名</param>
    /// <param name="featureColumns">特征列名</param>
    /// <returns>训练好的模型</returns>
    public ITransformer TrainLbfgsPoissonRegression(IDataView trainData, string labelColumn = "Label", params string[] featureColumns)
    {
        var pipeline = CreateFeaturePipeline(featureColumns)
            .Append(mlContext.Regression.Trainers.LbfgsPoissonRegression(
                labelColumnName: labelColumn,
                featureColumnName: "Features"));

        return pipeline.Fit(trainData);
    }

    /// <summary>
    /// 使用在线梯度下降训练回归模型
    /// </summary>
    /// <param name="trainData">训练数据</param>
    /// <param name="labelColumn">标签列名</param>
    /// <param name="featureColumns">特征列名</param>
    /// <param name="learningRate">学习率</param>
    /// <returns>训练好的模型</returns>
    public ITransformer TrainOnlineGradientDescent(IDataView trainData, string labelColumn = "Label", double learningRate = 0.1, params string[] featureColumns)
    {
        var pipeline = CreateFeaturePipeline(featureColumns)
            .Append(mlContext.Regression.Trainers.OnlineGradientDescent(
                labelColumnName: labelColumn,
                featureColumnName: "Features",
                learningRate: (float)learningRate));

        return pipeline.Fit(trainData);
    }

    /// <summary>
    /// 评估回归模型
    /// </summary>
    /// <param name="model">模型</param>
    /// <param name="testData">测试数据</param>
    /// <param name="labelColumn">标签列名</param>
    /// <returns>评估指标</returns>
    public RegressionMetrics Evaluate(ITransformer model, IDataView testData, string labelColumn = "Label")
    {
        var predictions = model.Transform(testData);
        return mlContext.Regression.Evaluate(predictions, labelColumnName: labelColumn);
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

        return mlContext.Transforms.Concatenate("Features", featureColumns)
            .Append(mlContext.Transforms.NormalizeMinMax("Features"));
    }
}
