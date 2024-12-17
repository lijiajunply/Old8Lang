using System.Reflection.Emit;

namespace Old8Lang.Compiler;

public class LocalManager
{
    private readonly Dictionary<string, LocalBuilder> LocalVar = [];
    
    public LocalBuilder? GetLocalVar(string name)
    {
        return LocalVar.GetValueOrDefault(name);
    }
    
    public void AddLocalVar(string name, LocalBuilder index)
    {
        LocalVar[name] = index;
    }
    
    public void RemoveLocalVar(string name)
    {
        LocalVar.Remove(name);
    }

    public int GetCount() => LocalVar.Count;
}