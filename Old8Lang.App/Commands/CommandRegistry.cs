namespace Old8Lang.App.Commands;

/// <summary>
/// 命令注册器，管理所有命令
/// </summary>
public class CommandRegistry
{
    private readonly Dictionary<string, ICommand> _commands = new();

    public void Register(ICommand command)
    {
    }

    public ICommand? GetCommand(string name)
    {
        return null;
    }

    public IEnumerable<ICommand> GetAllCommands()
    {
        return [];
    }
}
