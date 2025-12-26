using Old8Lang.MachineLearningLib;
using Microsoft.ML.Data;

namespace Old8Lang.MachineLearningLib;

/// <summary>
/// 机器学习库使用示例
/// </summary>
public class Examples
{
    /// <summary>
    /// 二分类示例：鸢尾花分类
    /// </summary>
    public static void BinaryClassificationExample()
    {
        // 1. 创建 ML 上下文
        var mlContext = new MLContextWrapper(seed: 0);

        // 2. 定义数据类
        var trainData = new List<IrisData>
        {
            new IrisData { SepalLength = 5.1f, SepalWidth = 3.5f, PetalLength = 1.4f, PetalWidth = 0.2f, Label = false },
            new IrisData { SepalLength = 4.9f, SepalWidth = 3.0f, PetalLength = 1.4f, PetalWidth = 0.2f, Label = false },
            new IrisData { SepalLength = 7.0f, SepalWidth = 3.2f, PetalLength = 4.7f, PetalWidth = 1.4f, Label = true },
            new IrisData { SepalLength = 6.4f, SepalWidth = 3.2f, PetalLength = 4.5f, PetalWidth = 1.5f, Label = true },
        };

        // 3. 加载数据
        var data = mlContext.DataLoader.LoadFromEnumerable(trainData);

        // 4. 拆分数据
        var (trainSet, testSet) = mlContext.DataLoader.SplitData(data, testFraction: 0.2);

        // 5. 训练模型
        var model = mlContext.ClassificationTrainer.TrainLogisticRegression(
            trainSet,
            labelColumn: "Label",
            "SepalLength", "SepalWidth", "PetalLength", "PetalWidth");

        // 6. 评估模型
        var metrics = mlContext.ClassificationTrainer.EvaluateBinaryClassification(model, testSet);

        Console.WriteLine($"准确率: {metrics.Accuracy:P2}");
        Console.WriteLine($"AUC: {metrics.AreaUnderRocCurve:P2}");
        Console.WriteLine($"F1 分数: {metrics.F1Score:P2}");

        // 7. 保存模型
        mlContext.ModelPredictor.SaveModel(model, data.Schema, "iris_model.zip");

        // 8. 进行预测
        var testSample = new IrisData
        {
            SepalLength = 5.5f,
            SepalWidth = 3.2f,
            PetalLength = 1.5f,
            PetalWidth = 0.3f
        };

        var prediction = mlContext.ModelPredictor.PredictSingle<IrisData, ModelPredictor.BinaryPrediction>(
            model, testSample);

        Console.WriteLine($"预测结果: {prediction.Prediction} (概率: {prediction.Probability:P2})");
    }

    /// <summary>
    /// 回归示例：房价预测
    /// </summary>
    public static void RegressionExample()
    {
        // 1. 创建 ML 上下文
        var mlContext = new MLContextWrapper(seed: 0);

        // 2. 定义数据类
        var trainData = new List<HousingData>
        {
            new HousingData { Size = 1000, Bedrooms = 2, Price = 250000 },
            new HousingData { Size = 1500, Bedrooms = 3, Price = 350000 },
            new HousingData { Size = 2000, Bedrooms = 4, Price = 450000 },
            new HousingData { Size = 2500, Bedrooms = 4, Price = 550000 },
        };

        // 3. 加载数据
        var data = mlContext.DataLoader.LoadFromEnumerable(trainData);

        // 4. 拆分数据
        var (trainSet, testSet) = mlContext.DataLoader.SplitData(data, testFraction: 0.2);

        // 5. 训练模型
        var model = mlContext.RegressionTrainer.TrainFastTree(
            trainSet,
            labelColumn: "Price",
            numberOfTrees: 100,
            numberOfLeaves: 20,
            "Size", "Bedrooms");

        // 6. 评估模型
        var metrics = mlContext.RegressionTrainer.Evaluate(model, testSet);

        Console.WriteLine($"平均绝对误差: {metrics.MeanAbsoluteError:N2}");
        Console.WriteLine($"均方根误差: {metrics.RootMeanSquaredError:N2}");
        Console.WriteLine($"R 平方: {metrics.RSquared:P2}");

        // 7. 进行预测
        var testSample = new HousingData { Size = 1800, Bedrooms = 3 };

        var prediction = mlContext.ModelPredictor.PredictSingle<HousingData, ModelPredictor.RegressionPrediction>(
            model, testSample);

        Console.WriteLine($"预测房价: ${prediction.Prediction:N2}");
    }

    /// <summary>
    /// 聚类示例：客户分群
    /// </summary>
    public static void ClusteringExample()
    {
        // 1. 创建 ML 上下文
        var mlContext = new MLContextWrapper(seed: 0);

        // 2. 定义数据类
        var data = new List<CustomerData>
        {
            new CustomerData { Age = 25, Income = 30000 },
            new CustomerData { Age = 30, Income = 40000 },
            new CustomerData { Age = 35, Income = 50000 },
            new CustomerData { Age = 50, Income = 80000 },
            new CustomerData { Age = 55, Income = 90000 },
        };

        // 3. 加载数据
        var dataView = mlContext.DataLoader.LoadFromEnumerable(data);

        // 4. 训练 K-Means 模型
        var model = mlContext.ClusteringTrainer.TrainKMeans(
            dataView,
            numberOfClusters: 2,
            "Age", "Income");

        // 5. 评估模型
        var metrics = mlContext.ClusteringTrainer.Evaluate(model, dataView);

        Console.WriteLine($"平均距离: {metrics.AverageDistance:N2}");
        Console.WriteLine($"Davies-Bouldin 指数: {metrics.DaviesBouldinIndex:N2}");

        // 6. 进行预测
        var testSample = new CustomerData { Age = 40, Income = 60000 };

        var prediction = mlContext.ModelPredictor.PredictSingle<CustomerData, ModelPredictor.ClusteringPrediction>(
            model, testSample);

        Console.WriteLine($"预测聚类 ID: {prediction.PredictedClusterId}");
    }

    // 数据类定义
    public class IrisData
    {
        [LoadColumn(0)]
        public float SepalLength { get; set; }

        [LoadColumn(1)]
        public float SepalWidth { get; set; }

        [LoadColumn(2)]
        public float PetalLength { get; set; }

        [LoadColumn(3)]
        public float PetalWidth { get; set; }

        [LoadColumn(4)]
        public bool Label { get; set; }
    }

    public class HousingData
    {
        [LoadColumn(0)]
        public float Size { get; set; }

        [LoadColumn(1)]
        public float Bedrooms { get; set; }

        [LoadColumn(2)]
        public float Price { get; set; }
    }

    public class CustomerData
    {
        [LoadColumn(0)]
        public float Age { get; set; }

        [LoadColumn(1)]
        public float Income { get; set; }
    }
}
