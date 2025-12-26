# Old8Lang 机器学习库 (Machine Learning Library)

基于 ML.NET 的机器学习库，为 Old8Lang 提供完整的机器学习功能支持。

## 功能特性

### 支持的机器学习任务

1. **二分类 (Binary Classification)**
   - 逻辑回归 (Logistic Regression)
   - 快速树 (Fast Tree)
   - LightGBM

2. **多分类 (Multiclass Classification)**
   - SDCA Maximum Entropy
   - LightGBM

3. **回归 (Regression)**
   - SDCA
   - 快速树 (Fast Tree)
   - FastTree Tweedie
   - LightGBM
   - 普通最小二乘法 (OLS)
   - 在线梯度下降 (Online Gradient Descent)

4. **聚类 (Clustering)**
   - K-Means

### 核心功能

- 数据加载（CSV 文件、内存数据）
- 数据预处理（拆分、打乱、过滤）
- 模型训练
- 模型评估
- 模型预测
- 模型保存和加载

## 快速开始

### C# 使用示例

#### 1. 二分类示例

```csharp
using Old8Lang.MachineLearningLib;

// 创建 ML 上下文
var mlContext = new MLContextWrapper(seed: 0);

// 加载数据
var data = mlContext.DataLoader.LoadFromCsv("data.csv");

// 拆分数据
var (trainSet, testSet) = mlContext.DataLoader.SplitData(data, testFraction: 0.2);

// 训练模型
var model = mlContext.ClassificationTrainer.TrainLogisticRegression(trainSet);

// 评估模型
var metrics = mlContext.ClassificationTrainer.EvaluateBinaryClassification(model, testSet);

Console.WriteLine($"准确率: {metrics.Accuracy:P2}");

// 保存模型
mlContext.ModelPredictor.SaveModel(model, data.Schema, "model.zip");
```

#### 2. 回归示例

```csharp
// 创建 ML 上下文
var mlContext = new MLContextWrapper(seed: 0);

// 加载数据
var data = mlContext.DataLoader.LoadFromCsv("housing.csv");

// 拆分数据
var (trainSet, testSet) = mlContext.DataLoader.SplitData(data);

// 训练快速树回归模型
var model = mlContext.RegressionTrainer.TrainFastTree(
    trainSet,
    numberOfTrees: 100,
    numberOfLeaves: 20);

// 评估模型
var metrics = mlContext.RegressionTrainer.Evaluate(model, testSet);

Console.WriteLine($"均方根误差: {metrics.RootMeanSquaredError:N2}");
```

#### 3. 聚类示例

```csharp
// 创建 ML 上下文
var mlContext = new MLContextWrapper(seed: 0);

// 加载数据
var data = mlContext.DataLoader.LoadFromCsv("customers.csv");

// 训练 K-Means 模型
var model = mlContext.ClusteringTrainer.TrainKMeans(data, numberOfClusters: 3);

// 评估模型
var metrics = mlContext.ClusteringTrainer.Evaluate(model, data);

Console.WriteLine($"平均距离: {metrics.AverageDistance:N2}");
```

### Old8Lang 使用示例

在 Old8Lang 中使用机器学习库：

```old8
import MachineLearningLib

// 初始化 ML 上下文
InitMLContext(0)

// 加载数据
data <- LoadDataFromCsv("data.csv", true, ",")

// 拆分数据
splitResult <- SplitData(data, 0.2)
trainSet <- splitResult[0]
testSet <- splitResult[1]

// 训练逻辑回归模型
model <- TrainLogisticRegression(trainSet, "Label")

// 评估模型
metrics <- EvaluateBinaryClassification(model, testSet, "Label")
PrintLine("准确率: " + metrics["Accuracy"].ToStr())
PrintLine("AUC: " + metrics["AreaUnderRocCurve"].ToStr())

// 保存模型
SaveModel(model, trainSet, "model.zip")

// 加载模型
loadedModel <- LoadModel("model.zip")
```

## API 参考

### MLContextWrapper

主要入口类，提供对所有功能的访问。

```csharp
// 构造函数
MLContextWrapper(int? seed = null)

// 属性
MLContext Context
DataLoader DataLoader
ClassificationTrainer ClassificationTrainer
RegressionTrainer RegressionTrainer
ClusteringTrainer ClusteringTrainer
ModelPredictor ModelPredictor
```

### DataLoader

数据加载和预处理。

```csharp
// 从 CSV 加载
IDataView LoadFromCsv(string filePath, bool hasHeader = true, char separatorChar = ',')

// 从内存加载
IDataView LoadFromEnumerable<T>(IEnumerable<T> data)

// 拆分数据
(IDataView TrainSet, IDataView TestSet) SplitData(IDataView data, double testFraction = 0.2)

// 打乱数据
IDataView ShuffleData(IDataView data, int? seed = null)

// 过滤数据
IDataView FilterData(IDataView data, string columnName, double lowerBound, double upperBound)
```

### ClassificationTrainer

分类模型训练。

```csharp
// 二分类
ITransformer TrainLogisticRegression(IDataView trainData, string labelColumn = "Label", params string[] featureColumns)
ITransformer TrainFastTree(IDataView trainData, string labelColumn = "Label", int numberOfTrees = 100, int numberOfLeaves = 20, params string[] featureColumns)
ITransformer TrainLightGbm(IDataView trainData, string labelColumn = "Label", int numberOfIterations = 100, double learningRate = 0.1, params string[] featureColumns)

// 多分类
ITransformer TrainMulticlassSdca(IDataView trainData, string labelColumn = "Label", params string[] featureColumns)
ITransformer TrainMulticlassLightGbm(IDataView trainData, string labelColumn = "Label", int numberOfIterations = 100, params string[] featureColumns)

// 评估
CalibratedBinaryClassificationMetrics EvaluateBinaryClassification(ITransformer model, IDataView testData, string labelColumn = "Label")
MulticlassClassificationMetrics EvaluateMulticlassClassification(ITransformer model, IDataView testData, string labelColumn = "Label")
```

### RegressionTrainer

回归模型训练。

```csharp
ITransformer TrainSdca(IDataView trainData, string labelColumn = "Label", params string[] featureColumns)
ITransformer TrainFastTree(IDataView trainData, string labelColumn = "Label", int numberOfTrees = 100, int numberOfLeaves = 20, params string[] featureColumns)
ITransformer TrainLightGbm(IDataView trainData, string labelColumn = "Label", int numberOfIterations = 100, double learningRate = 0.1, params string[] featureColumns)
ITransformer TrainOls(IDataView trainData, string labelColumn = "Label", params string[] featureColumns)
ITransformer TrainOnlineGradientDescent(IDataView trainData, string labelColumn = "Label", double learningRate = 0.1, params string[] featureColumns)

RegressionMetrics Evaluate(ITransformer model, IDataView testData, string labelColumn = "Label")
```

### ClusteringTrainer

聚类模型训练。

```csharp
ITransformer TrainKMeans(IDataView trainData, int numberOfClusters = 3, params string[] featureColumns)

ClusteringMetrics Evaluate(ITransformer model, IDataView testData)
```

### ModelPredictor

模型预测和管理。

```csharp
// 预测
TOutput PredictSingle<TInput, TOutput>(ITransformer model, TInput input)
List<TOutput> PredictBatch<TInput, TOutput>(ITransformer model, IEnumerable<TInput> inputs)
IDataView Predict(ITransformer model, IDataView data)

// 模型管理
void SaveModel(ITransformer model, DataViewSchema inputSchema, string filePath)
(ITransformer Model, DataViewSchema Schema) LoadModel(string filePath)
```

## Old8Lang 绑定函数

以下函数可以在 Old8Lang 中直接调用：

| 函数名 | 说明 |
|--------|------|
| `InitMLContext(seed?)` | 初始化 ML 上下文 |
| `LoadDataFromCsv(filePath, hasHeader, separatorChar)` | 从 CSV 加载数据 |
| `SplitData(data, testFraction)` | 拆分数据为训练集和测试集 |
| `TrainLogisticRegression(trainData, labelColumn)` | 训练逻辑回归模型 |
| `TrainFastTreeClassification(trainData, labelColumn, numberOfTrees, numberOfLeaves)` | 训练快速树分类模型 |
| `TrainSdcaRegression(trainData, labelColumn)` | 训练 SDCA 回归模型 |
| `TrainFastTreeRegression(trainData, labelColumn, numberOfTrees, numberOfLeaves)` | 训练快速树回归模型 |
| `TrainKMeans(trainData, numberOfClusters)` | 训练 K-Means 聚类模型 |
| `EvaluateBinaryClassification(model, testData, labelColumn)` | 评估二分类模型 |
| `EvaluateRegression(model, testData, labelColumn)` | 评估回归模型 |
| `EvaluateClustering(model, testData)` | 评估聚类模型 |
| `SaveModel(model, data, filePath)` | 保存模型 |
| `LoadModel(filePath)` | 加载模型 |

## 依赖项

- Microsoft.ML >= 4.0.0
- Microsoft.ML.FastTree >= 4.0.0
- Microsoft.ML.LightGbm >= 4.0.0
- Microsoft.ML.ImageAnalytics >= 4.0.0

## 数据格式

### CSV 文件格式

默认情况下，库期望 CSV 文件包含以下列：

- Feature1, Feature2, Feature3, Feature4: 特征列
- Label: 标签列

示例：

```csv
Feature1,Feature2,Feature3,Feature4,Label
5.1,3.5,1.4,0.2,0
4.9,3.0,1.4,0.2,0
7.0,3.2,4.7,1.4,1
6.4,3.2,4.5,1.5,1
```

可以通过自定义数据类来支持不同的列结构。

## 注意事项

1. 在使用 Old8Lang 绑定函数前，必须先调用 `InitMLContext()` 初始化上下文
2. 训练大型模型可能需要较长时间和较多内存
3. 模型文件保存为 .zip 格式
4. 特征列需要进行适当的归一化处理（库会自动进行 MinMax 归一化）

## 许可证

本项目遵循 MIT 许可证。
