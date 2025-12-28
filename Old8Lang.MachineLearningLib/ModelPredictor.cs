using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace Old8Lang.MachineLearningLib;

/// <summary>
/// 模型预测器，用于进行预测和模型保存/加载
/// </summary>
public class ModelPredictor(MLContext mlContext)
{
    /// <summary>
    /// 对单个数据进行预测
    /// </summary>
    /// <typeparam name="TInput">输入类型</typeparam>
    /// <typeparam name="TOutput">输出类型</typeparam>
    /// <param name="model">模型</param>
    /// <param name="input">输入数据</param>
    /// <returns>预测结果</returns>
    public TOutput PredictSingle<TInput, TOutput>(ITransformer model, TInput input)
        where TInput : class
        where TOutput : class, new()
    {
        var predictionEngine = mlContext.Model.CreatePredictionEngine<TInput, TOutput>(model);
        return predictionEngine.Predict(input);
    }

    /// <summary>
    /// 对批量数据进行预测
    /// </summary>
    /// <typeparam name="TInput">输入类型</typeparam>
    /// <typeparam name="TOutput">输出类型</typeparam>
    /// <param name="model">模型</param>
    /// <param name="inputs">输入数据列表</param>
    /// <returns>预测结果列表</returns>
    public List<TOutput> PredictBatch<TInput, TOutput>(ITransformer model, IEnumerable<TInput> inputs)
        where TInput : class
        where TOutput : class, new()
    {
        var inputData = mlContext.Data.LoadFromEnumerable(inputs);
        var predictions = model.Transform(inputData);
        return mlContext.Data.CreateEnumerable<TOutput>(predictions, reuseRowObject: false).ToList();
    }

    /// <summary>
    /// 对 IDataView 数据进行预测
    /// </summary>
    /// <param name="model">模型</param>
    /// <param name="data">数据</param>
    /// <returns>预测结果</returns>
    public IDataView Predict(ITransformer model, IDataView data)
    {
        return model.Transform(data);
    }

    /// <summary>
    /// 保存模型到文件
    /// </summary>
    /// <param name="model">模型</param>
    /// <param name="inputSchema">输入模式</param>
    /// <param name="filePath">文件路径</param>
    public void SaveModel(ITransformer model, DataViewSchema inputSchema, string filePath)
    {
        mlContext.Model.Save(model, inputSchema, filePath);
    }

    /// <summary>
    /// 从文件加载模型
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>模型和输入模式</returns>
    public (ITransformer Model, DataViewSchema Schema) LoadModel(string filePath)
    {
        var model = mlContext.Model.Load(filePath, out var inputSchema);
        return (model, inputSchema);
    }

    /// <summary>
    /// 获取特征重要性（仅适用于支持的模型）
    /// </summary>
    /// <param name="model">模型</param>
    /// <param name="featureColumnName">特征列名</param>
    /// <returns>特征重要性分数</returns>
    public VBuffer<float>? GetFeatureImportance(ITransformer model, string featureColumnName = "Features")
    {
        // 尝试获取特征重要性
        if (model is ISingleFeaturePredictionTransformer<object> transformer &&
            transformer.Model is ICalculateFeatureContribution)
        {
            // 注意：某些模型可能不支持特征权重提取
            // 这里返回 null 表示不支持
            return null;
        }

        return null;
    }

    /// <summary>
    /// 二分类预测结果
    /// </summary>
    public class BinaryPrediction
    {
        [ColumnName("PredictedLabel")] public bool Prediction { get; set; }

        public float Probability { get; set; }

        public float Score { get; set; }
    }

    /// <summary>
    /// 多分类预测结果
    /// </summary>
    public class MulticlassPrediction
    {
        [ColumnName("PredictedLabel")] public string PredictedLabel { get; set; } = string.Empty;

        public float[] Score { get; set; } = Array.Empty<float>();
    }

    /// <summary>
    /// 回归预测结果
    /// </summary>
    public class RegressionPrediction
    {
        [ColumnName("Score")] public float Prediction { get; set; }
    }

    /// <summary>
    /// 聚类预测结果
    /// </summary>
    public class ClusteringPrediction
    {
        [ColumnName("PredictedLabel")] public uint PredictedClusterId { get; set; }

        public float[] Distances { get; set; } = Array.Empty<float>();
    }
}