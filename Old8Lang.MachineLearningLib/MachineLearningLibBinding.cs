using Microsoft.ML;

namespace Old8Lang.MachineLearningLib;

/// <summary>
/// Old8Lang 机器学习库绑定类
/// </summary>
public class MachineLearningLibBinding
{
    private static MLContextWrapper? _mlContext;

    /// <summary>
    /// 初始化 ML 上下文
    /// </summary>
    public static void InitMlContext(int? seed = null)
    {
        _mlContext = new MLContextWrapper(seed);
    }

    /// <summary>
    /// 从 CSV 文件加载数据
    /// </summary>
    public static object LoadDataFromCsv(string filePath, bool hasHeader = true, string separatorChar = ",")
    {
        EnsureMlContextInitialized();
        return _mlContext!.DataLoader.LoadFromCsv(filePath, hasHeader, separatorChar[0]);
    }

    /// <summary>
    /// 拆分数据为训练集和测试集
    /// </summary>
    public static List<object?> SplitData(IDataView data, double testFraction = 0.2)
    {
        EnsureMlContextInitialized();
        var (trainSet, testSet) = _mlContext!.DataLoader.SplitData(data, testFraction);

        return new List<object?> { trainSet, testSet };
    }

    /// <summary>
    /// 训练逻辑回归二分类模型
    /// </summary>
    public static ITransformer TrainLogisticRegression(IDataView trainData, string labelColumn = "Label")
    {
        EnsureMlContextInitialized();
        return _mlContext!.ClassificationTrainer.TrainLogisticRegression(trainData, labelColumn);
    }

    /// <summary>
    /// 训练快速树二分类模型
    /// </summary>
    public static ITransformer TrainFastTreeClassification(IDataView trainData, string labelColumn = "Label",
        int numberOfTrees = 100, int numberOfLeaves = 20)
    {
        EnsureMlContextInitialized();
        return _mlContext!.ClassificationTrainer.TrainFastTree(trainData, labelColumn, numberOfTrees, numberOfLeaves);
    }

    /// <summary>
    /// 训练 SDCA 回归模型
    /// </summary>
    public static ITransformer TrainSdcaRegression(IDataView trainData, string labelColumn = "Label")
    {
        EnsureMlContextInitialized();
        return _mlContext!.RegressionTrainer.TrainSdca(trainData, labelColumn);
    }

    /// <summary>
    /// 训练快速树回归模型
    /// </summary>
    public static ITransformer TrainFastTreeRegression(IDataView trainData, string labelColumn = "Label",
        int numberOfTrees = 100, int numberOfLeaves = 20)
    {
        EnsureMlContextInitialized();
        return _mlContext!.RegressionTrainer.TrainFastTree(trainData, labelColumn, numberOfTrees, numberOfLeaves);
    }

    /// <summary>
    /// 训练 K-Means 聚类模型
    /// </summary>
    public static ITransformer TrainKMeans(IDataView trainData, int numberOfClusters = 3)
    {
        EnsureMlContextInitialized();
        return _mlContext!.ClusteringTrainer.TrainKMeans(trainData, numberOfClusters);
    }

    /// <summary>
    /// 评估二分类模型
    /// </summary>
    public static Dictionary<object, object?> EvaluateBinaryClassification(ITransformer model, IDataView testData,
        string labelColumn = "Label")
    {
        EnsureMlContextInitialized();
        var metrics = _mlContext!.ClassificationTrainer.EvaluateBinaryClassification(model, testData, labelColumn);

        return new Dictionary<object, object?>
        {
            ["Accuracy"] = metrics.Accuracy,
            ["AreaUnderRocCurve"] = metrics.AreaUnderRocCurve,
            ["F1Score"] = metrics.F1Score,
            ["PositivePrecision"] = metrics.PositivePrecision,
            ["PositiveRecall"] = metrics.PositiveRecall,
            ["NegativePrecision"] = metrics.NegativePrecision,
            ["NegativeRecall"] = metrics.NegativeRecall
        };
    }

    /// <summary>
    /// 评估回归模型
    /// </summary>
    public static Dictionary<object, object?> EvaluateRegression(ITransformer model, IDataView testData,
        string labelColumn = "Label")
    {
        EnsureMlContextInitialized();
        var metrics = _mlContext!.RegressionTrainer.Evaluate(model, testData, labelColumn);

        return new Dictionary<object, object?>
        {
            ["MeanAbsoluteError"] = metrics.MeanAbsoluteError,
            ["MeanSquaredError"] = metrics.MeanSquaredError,
            ["RootMeanSquaredError"] = metrics.RootMeanSquaredError,
            ["RSquared"] = metrics.RSquared
        };
    }

    /// <summary>
    /// 评估聚类模型
    /// </summary>
    public static Dictionary<object, object?> EvaluateClustering(ITransformer model, IDataView testData)
    {
        EnsureMlContextInitialized();
        var metrics = _mlContext!.ClusteringTrainer.Evaluate(model, testData);

        return new(new Dictionary<object, object?>
        {
            ["AverageDistance"] = metrics.AverageDistance,
            ["DaviesBouldinIndex"] = metrics.DaviesBouldinIndex
        });
    }

    /// <summary>
    /// 保存模型到文件
    /// </summary>
    public static void SaveModel(ITransformer model, IDataView data, string filePath)
    {
        EnsureMlContextInitialized();
        _mlContext!.ModelPredictor.SaveModel(model, data.Schema, filePath);
    }

    /// <summary>
    /// 从文件加载模型
    /// </summary>
    public static ITransformer LoadModel(string filePath)
    {
        EnsureMlContextInitialized();
        var (model, _) = _mlContext!.ModelPredictor.LoadModel(filePath);
        return model;
    }

    private static void EnsureMlContextInitialized()
    {
        if (_mlContext == null)
        {
            throw new InvalidOperationException("ML Context 未初始化，请先调用 InitMLContext()");
        }
    }
}