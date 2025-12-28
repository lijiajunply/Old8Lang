using Microsoft.ML;

namespace Old8Lang.MachineLearningLib;

/// <summary>
/// ML.NET 上下文包装器，提供统一的入口点
/// </summary>
public class MLContextWrapper
{
    public MLContext Context { get; }
    public DataLoader DataLoader { get; }
    public ClassificationTrainer ClassificationTrainer { get; }
    public RegressionTrainer RegressionTrainer { get; }
    public ClusteringTrainer ClusteringTrainer { get; }
    public ModelPredictor ModelPredictor { get; }

    /// <summary>
    /// 创建 MLContext 包装器
    /// </summary>
    /// <param name="seed">随机种子（用于可重现的结果）</param>
    public MLContextWrapper(int? seed = null)
    {
        Context = seed.HasValue ? new MLContext(seed.Value) : new MLContext();

        DataLoader = new DataLoader(Context);
        ClassificationTrainer = new ClassificationTrainer(Context);
        RegressionTrainer = new RegressionTrainer(Context);
        ClusteringTrainer = new ClusteringTrainer(Context);
        ModelPredictor = new ModelPredictor(Context);
    }
}
