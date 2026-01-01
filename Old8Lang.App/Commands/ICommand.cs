namespace Old8Lang.App.Commands;

/// <summary>
/// 命令接口
/// </summary>
public interface ICommand
{
    /// <summary>
    /// 命令名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 命令描述
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 命令帮助信息
    /// </summary>
    string Help { get; }

    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="args">命令参数</param>
    /// <returns>执行结果（0 表示成功，非 0 表示失败）</returns>
    int Execute(string[] args);
}
