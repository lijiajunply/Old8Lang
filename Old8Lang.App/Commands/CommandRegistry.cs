namespace Old8Lang.App.Commands;

/// <summary>
/// 命令注册器，管理所有命令
/// </summary>
public class CommandRegistry
{
    private readonly Dictionary<string, ICommand> _commands = new();

    /// <summary>
    /// 注册命令
    /// </summary>
    /// <param name="command">要注册的命令</param>
    public void Register(ICommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));
        
        _commands[command.Name] = command;
    }

    /// <summary>
    /// 获取指定名称的命令
    /// </summary>
    /// <param name="name">命令名称</param>
    /// <returns>命令实例，如果不存在则返回 null</returns>
    public ICommand? GetCommand(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
            
        _commands.TryGetValue(name, out var command);
        return command;
    }

    /// <summary>
    /// 获取所有已注册的命令
    /// </summary>
    /// <returns>所有命令的枚举</returns>
    public IEnumerable<ICommand> GetAllCommands()
    {
        return _commands.Values;
    }

    /// <summary>
    /// 检查命令是否已注册
    /// </summary>
    /// <param name="name">命令名称</param>
    /// <returns>如果命令已注册返回 true，否则返回 false</returns>
    public bool HasCommand(string name)
    {
        return !string.IsNullOrEmpty(name) && _commands.ContainsKey(name);
    }

    /// <summary>
    /// 清除所有已注册的命令
    /// </summary>
    public void Clear()
    {
        _commands.Clear();
    }

    /// <summary>
    /// 移除指定名称的命令
    /// </summary>
    /// <param name="name">命令名称</param>
    /// <returns>如果成功移除返回 true，否则返回 false</returns>
    public bool Remove(string name)
    {
        return !string.IsNullOrEmpty(name) && _commands.Remove(name);
    }
}
