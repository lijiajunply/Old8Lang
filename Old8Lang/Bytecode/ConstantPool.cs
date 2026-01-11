using System.Collections;

namespace Old8Lang.Bytecode;

/// <summary>
/// 常量池 - 存储字节码中使用的常量
/// </summary>
public class ConstantPool : IEnumerable<object>
{
    private readonly List<object> _constants = new();
    private readonly Dictionary<object, int> _constantIndexMap = new();

    /// <summary>常量数量</summary>
    public int Count => _constants.Count;

    /// <summary>
    /// 添加常量到常量池，如果已存在则返回已有索引
    /// </summary>
    public int AddConstant(object value)
    {
        if (value == null!)
            throw new ArgumentNullException(nameof(value));

        // 检查是否已存在
        if (_constantIndexMap.TryGetValue(value, out int existingIndex))
            return existingIndex;

        // 添加新常量
        int index = _constants.Count;
        _constants.Add(value);
        _constantIndexMap[value] = index;
        return index;
    }

    /// <summary>
    /// 获取指定索引的常量
    /// </summary>
    public object GetConstant(int index)
    {
        if (index < 0 || index >= _constants.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"常量池索引越界: {index}");

        return _constants[index];
    }

    /// <summary>
    /// 尝试获取常量的索引
    /// </summary>
    public bool TryGetConstantIndex(object value, out int index)
    {
        return _constantIndexMap.TryGetValue(value, out index);
    }

    /// <summary>
    /// 写入二进制流
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(_constants.Count);

        foreach (var constant in _constants)
        {
            if (constant is int intValue)
            {
                writer.Write((byte)1);
                writer.Write(intValue);
            }
            else if (constant is long longValue)
            {
                writer.Write((byte)2);
                writer.Write(longValue);
            }
            else if (constant is double doubleValue)
            {
                writer.Write((byte)3);
                writer.Write(doubleValue);
            }
            else if (constant is string stringValue)
            {
                writer.Write((byte)4);
                writer.Write(stringValue);
            }
            else if (constant is bool boolValue)
            {
                writer.Write((byte)5);
                writer.Write(boolValue);
            }
            else if (constant is char charValue)
            {
                writer.Write((byte)6);
                writer.Write(charValue);
            }
            else
            {
                throw new NotSupportedException($"不支持的常量类型: {constant.GetType()}");
            }
        }
    }

    /// <summary>
    /// 从二进制流读取
    /// </summary>
    public static ConstantPool ReadFrom(BinaryReader reader)
    {
        var pool = new ConstantPool();
        int count = reader.ReadInt32();

        for (int i = 0; i < count; i++)
        {
            byte type = reader.ReadByte();
            object constant = type switch
            {
                1 => reader.ReadInt32(),
                2 => reader.ReadInt64(),
                3 => reader.ReadDouble(),
                4 => reader.ReadString(),
                5 => reader.ReadBoolean(),
                6 => reader.ReadChar(),
                _ => throw new InvalidOperationException($"未知的常量类型: {type}")
            };

            pool._constants.Add(constant);
            pool._constantIndexMap[constant] = i;
        }

        return pool;
    }

    public IEnumerator<object> GetEnumerator() => _constants.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString()
    {
        return $"ConstantPool[{Count}]";
    }
}
