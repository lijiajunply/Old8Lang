using Old8Lang.MachineLearningLib;
using Microsoft.ML.Data;

namespace Old8Lang.Tests.MachineLearningLib;

public class MlContextWrapperTests
{
    [Fact]
    public void Constructor_WithSeed_CreatesContext()
    {
        // Arrange & Act
        var mlContext = new MLContextWrapper(seed: 42);

        // Assert
        Assert.NotNull(mlContext);
        Assert.NotNull(mlContext.Context);
        Assert.NotNull(mlContext.DataLoader);
        Assert.NotNull(mlContext.ClassificationTrainer);
        Assert.NotNull(mlContext.RegressionTrainer);
        Assert.NotNull(mlContext.ClusteringTrainer);
        Assert.NotNull(mlContext.ModelPredictor);
    }

    [Fact]
    public void Constructor_WithoutSeed_CreatesContext()
    {
        // Arrange & Act
        var mlContext = new MLContextWrapper();

        // Assert
        Assert.NotNull(mlContext);
        Assert.NotNull(mlContext.Context);
    }
}

public class DataLoaderTests
{
    [Fact]
    public void LoadFromEnumerable_WithValidData_ReturnsDataView()
    {
        // Arrange
        var mlContext = new MLContextWrapper(seed: 0);
        var data = new List<TestData>
        {
            new TestData { Feature1 = 1.0f, Feature2 = 2.0f, Feature3 = 3.0f, Feature4 = 4.0f, Label = false },
            new TestData { Feature1 = 5.0f, Feature2 = 6.0f, Feature3 = 7.0f, Feature4 = 8.0f, Label = true }
        };

        // Act
        var dataView = mlContext.DataLoader.LoadFromEnumerable(data);

        // Assert
        Assert.NotNull(dataView);
        Assert.NotNull(dataView.Schema);
    }

    [Fact]
    public void SplitData_WithValidData_ReturnsSplitData()
    {
        // Arrange
        var mlContext = new MLContextWrapper(seed: 0);
        var data = GenerateTestData(100);
        var dataView = mlContext.DataLoader.LoadFromEnumerable(data);

        // Act
        var (trainSet, testSet) = mlContext.DataLoader.SplitData(dataView, testFraction: 0.2);

        // Assert
        Assert.NotNull(trainSet);
        Assert.NotNull(testSet);
    }

    [Fact]
    public void ShuffleData_WithValidData_ReturnsShuffledData()
    {
        // Arrange
        var mlContext = new MLContextWrapper(seed: 0);
        var data = GenerateTestData(10);
        var dataView = mlContext.DataLoader.LoadFromEnumerable(data);

        // Act
        var shuffledData = mlContext.DataLoader.ShuffleData(dataView, seed: 42);

        // Assert
        Assert.NotNull(shuffledData);
    }

    private static List<TestData> GenerateTestData(int count)
    {
        var random = new Random(0);
        var data = new List<TestData>();

        for (int i = 0; i < count; i++)
        {
            data.Add(new TestData
            {
                Feature1 = (float)random.NextDouble() * 10,
                Feature2 = (float)random.NextDouble() * 10,
                Feature3 = (float)random.NextDouble() * 10,
                Feature4 = (float)random.NextDouble() * 10,
                Label = random.Next(0, 2) == 1
            });
        }

        return data;
    }
}

public class ClassificationTrainerTests
{
    [Fact]
    public void TrainLogisticRegression_WithValidData_ReturnsModel()
    {
        // Arrange
        var mlContext = new MLContextWrapper(seed: 0);
        var data = GenerateClassificationData(50);
        var dataView = mlContext.DataLoader.LoadFromEnumerable(data);

        // Act
        var model = mlContext.ClassificationTrainer.TrainLogisticRegression(
            dataView,
            labelColumn: "Label",
            "Feature1", "Feature2", "Feature3", "Feature4");

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public void TrainFastTree_WithValidData_ReturnsModel()
    {
        // Arrange
        var mlContext = new MLContextWrapper(seed: 0);
        var data = GenerateClassificationData(50);
        var dataView = mlContext.DataLoader.LoadFromEnumerable(data);

        // Act
        var model = mlContext.ClassificationTrainer.TrainFastTree(
            dataView,
            labelColumn: "Label",
            numberOfTrees: 10,
            numberOfLeaves: 5,
            "Feature1", "Feature2", "Feature3", "Feature4");

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public void EvaluateBinaryClassification_WithValidModel_ReturnsMetrics()
    {
        // Arrange
        var mlContext = new MLContextWrapper(seed: 0);
        var data = GenerateClassificationData(50);
        var dataView = mlContext.DataLoader.LoadFromEnumerable(data);
        var (trainSet, testSet) = mlContext.DataLoader.SplitData(dataView, testFraction: 0.2);

        var model = mlContext.ClassificationTrainer.TrainLogisticRegression(
            trainSet,
            labelColumn: "Label",
            "Feature1", "Feature2", "Feature3", "Feature4");

        // Act
        var metrics = mlContext.ClassificationTrainer.EvaluateBinaryClassification(model, testSet);

        // Assert
        Assert.NotNull(metrics);
        Assert.InRange(metrics.Accuracy, 0, 1);
        Assert.InRange(metrics.AreaUnderRocCurve, 0, 1);
    }

    private static List<TestData> GenerateClassificationData(int count)
    {
        var random = new Random(0);
        var data = new List<TestData>();

        for (int i = 0; i < count; i++)
        {
            var feature1 = (float)random.NextDouble() * 10;
            var feature2 = (float)random.NextDouble() * 10;
            var label = (feature1 + feature2) > 10;

            data.Add(new TestData
            {
                Feature1 = (float)random.NextDouble() * 10,
                Feature2 = (float)random.NextDouble() * 10,
                Feature3 = (float)random.NextDouble() * 10,
                Feature4 = (float)random.NextDouble() * 10,
                Label = random.Next(0, 2) == 1
            });
        }

        return data;
    }
}

public class RegressionTrainerTests
{
    [Fact]
    public void TrainSdca_WithValidData_ReturnsModel()
    {
        // Arrange
        var mlContext = new MLContextWrapper(seed: 0);
        var data = GenerateRegressionData(50);
        var dataView = mlContext.DataLoader.LoadFromEnumerable(data);

        // Act
        var model = mlContext.RegressionTrainer.TrainSdca(
            dataView,
            labelColumn: "Label",
            "Feature1", "Feature2", "Feature3", "Feature4");

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public void TrainFastTree_WithValidData_ReturnsModel()
    {
        // Arrange
        var mlContext = new MLContextWrapper(seed: 0);
        var data = GenerateRegressionData(50);
        var dataView = mlContext.DataLoader.LoadFromEnumerable(data);

        // Act
        var model = mlContext.RegressionTrainer.TrainFastTree(
            dataView,
            labelColumn: "Label",
            numberOfTrees: 10,
            numberOfLeaves: 5,
            "Feature1", "Feature2", "Feature3", "Feature4");

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public void Evaluate_WithValidModel_ReturnsMetrics()
    {
        // Arrange
        var mlContext = new MLContextWrapper(seed: 0);
        var data = GenerateRegressionData(50);
        var dataView = mlContext.DataLoader.LoadFromEnumerable(data);
        var (trainSet, testSet) = mlContext.DataLoader.SplitData(dataView, testFraction: 0.2);

        var model = mlContext.RegressionTrainer.TrainSdca(
            trainSet,
            labelColumn: "Label",
            "Feature1", "Feature2", "Feature3", "Feature4");

        // Act
        var metrics = mlContext.RegressionTrainer.Evaluate(model, testSet);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.MeanAbsoluteError >= 0);
        Assert.True(metrics.RootMeanSquaredError >= 0);
    }

    private static List<RegressionTestData> GenerateRegressionData(int count)
    {
        var random = new Random(0);
        var data = new List<RegressionTestData>();

        for (int i = 0; i < count; i++)
        {
            var feature1 = (float)random.NextDouble() * 10;
            var feature2 = (float)random.NextDouble() * 10;
            var label = feature1 * 2 + feature2 * 3 + (float)random.NextDouble() * 2;

            data.Add(new RegressionTestData
            {
                Feature1 = feature1,
                Feature2 = feature2,
                Feature3 = (float)random.NextDouble() * 10,
                Feature4 = (float)random.NextDouble() * 10,
                Label = label
            });
        }

        return data;
    }
}

public class ClusteringTrainerTests
{
    [Fact]
    public void TrainKMeans_WithValidData_ReturnsModel()
    {
        // Arrange
        var mlContext = new MLContextWrapper(seed: 0);
        var data = GenerateClusteringData(50);
        var dataView = mlContext.DataLoader.LoadFromEnumerable(data);

        // Act
        var model = mlContext.ClusteringTrainer.TrainKMeans(
            dataView,
            numberOfClusters: 3,
            "Feature1", "Feature2", "Feature3", "Feature4");

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public void Evaluate_WithValidModel_ReturnsMetrics()
    {
        // Arrange
        var mlContext = new MLContextWrapper(seed: 0);
        var data = GenerateClusteringData(50);
        var dataView = mlContext.DataLoader.LoadFromEnumerable(data);

        var model = mlContext.ClusteringTrainer.TrainKMeans(
            dataView,
            numberOfClusters: 3,
            "Feature1", "Feature2", "Feature3", "Feature4");

        // Act
        var metrics = mlContext.ClusteringTrainer.Evaluate(model, dataView);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.AverageDistance >= 0);
    }

    private static List<ClusteringTestData> GenerateClusteringData(int count)
    {
        var random = new Random(0);
        var data = new List<ClusteringTestData>();

        for (int i = 0; i < count; i++)
        {
            data.Add(new ClusteringTestData
            {
                Feature1 = (float)random.NextDouble() * 10,
                Feature2 = (float)random.NextDouble() * 10,
                Feature3 = (float)random.NextDouble() * 10,
                Feature4 = (float)random.NextDouble() * 10
            });
        }

        return data;
    }
}

public class ModelPredictorTests
{
    [Fact]
    public void SaveAndLoadModel_WithValidModel_ReturnsModel()
    {
        // Arrange
        var mlContext = new MLContextWrapper(seed: 0);
        var data = GenerateTestData(50);
        var dataView = mlContext.DataLoader.LoadFromEnumerable(data);

        var model = mlContext.ClassificationTrainer.TrainLogisticRegression(
            dataView,
            labelColumn: "Label",
            "Feature1", "Feature2", "Feature3", "Feature4");

        var tempFile = Path.GetTempFileName() + ".zip";

        try
        {
            // Act - Save
            mlContext.ModelPredictor.SaveModel(model, dataView.Schema, tempFile);

            // Act - Load
            var (loadedModel, schema) = mlContext.ModelPredictor.LoadModel(tempFile);

            // Assert
            Assert.NotNull(loadedModel);
            Assert.NotNull(schema);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    private static List<TestData> GenerateTestData(int count)
    {
        var random = new Random(0);
        var data = new List<TestData>();

        for (int i = 0; i < count; i++)
        {
            data.Add(new TestData
            {
                Feature1 = (float)random.NextDouble() * 10,
                Feature2 = (float)random.NextDouble() * 10,
                Feature3 = (float)random.NextDouble() * 10,
                Feature4 = (float)random.NextDouble() * 10,
                Label = random.Next(0, 2) == 1
            });
        }

        return data;
    }
}

// 测试数据类
[Serializable]
public class TestData
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
    public bool Label { get; set; }
}

[Serializable]
public class RegressionTestData
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

[Serializable]
public class ClusteringTestData
{
    [LoadColumn(0)]
    public float Feature1 { get; set; }

    [LoadColumn(1)]
    public float Feature2 { get; set; }

    [LoadColumn(2)]
    public float Feature3 { get; set; }

    [LoadColumn(3)]
    public float Feature4 { get; set; }
}
