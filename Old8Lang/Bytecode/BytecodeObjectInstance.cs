namespace Old8Lang.Bytecode;

/// <summary>
/// 字节码模式的对象实例
/// </summary>
public class BytecodeObjectInstance
{
    /// <summary>类名</summary>
    public string ClassName { get; set; } = "";

    /// <summary>字段值字典</summary>
    public Dictionary<string, object?> Fields { get; set; } = new();

    public BytecodeObjectInstance(string className)
    {
        ClassName = className;
    }

    public override string ToString()
    {
        return $"{ClassName} instance";
    }
}
